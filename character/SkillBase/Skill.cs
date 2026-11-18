using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Godot;

public enum PropertyType
{
    [Description("力量")]
    Power,

    [Description("生存")]
    Survivability,

    [Description("生命上限")]
    MaxLife,
}

public partial class Skill
{
    public const int XEnergyCost = -1;
    private const float EnemySkillDamageMultiplier = 1f;

    private int _previewPower;
    private int _previewSurvivability;
    private int _previewBasePowerContribution;
    private int _previewBaseSurvivabilityContribution;
    private int _previewEnergy = 1;
    private int _previewPlayerIndex = -1;
    private bool _previewIsPlayer = true;
    private bool _previewUsesEnemySkillDamageScaling = true;

    public static PackedScene AttackScene = ResourceLoader.Load<PackedScene>(
        "res://battle/Effect/AttackEffect.tscn"
    );
    public static PackedScene BurnScene = ResourceLoader.Load<PackedScene>(
        "res://battle/Effect/burn.tscn"
    );

    public enum SkillTypes
    {
        [Description("攻击")]
        Attack = 0,

        [Description("生存")]
        Survive = 1,

        [Description("特殊")]
        Special = 2,

        [Description("无")]
        none = 3,

        [Description("状态")]
        Status = 4,

        [Description("能力")]
        Ability = 5,

        [Description("常驻")]
        Permanent = 6,
    }

    private string _skillName;
    protected virtual string SkillNameKey => $"skill.{I18n.ToSnakeCase(GetType().Name)}.name";
    public virtual string SkillName
    {
        get
        {
            if (I18n.HasTranslation(SkillNameKey))
                return I18n.Tr(SkillNameKey, _skillName);

            if (I18n.IsEnglishLocale())
                return I18n.HumanizeSnakeCase(GetSkillNameToken());

            return _skillName;
        }
        set => _skillName = value;
    }
    public virtual SkillTypes SkillType => SkillTypes.none;
    public Character OwnerCharater;
    public SkillID? SkillId { get; internal set; }
    public ulong BattleCardInstanceId { get; internal set; }
    public virtual int EnergyCost => V((OwnerCharater != null ? !OwnerCharater.IsPlayer : !_previewIsPlayer)
        ? "EnemyEnergyCost" : nameof(EnergyCost));
    public virtual int ExhaustAfterExecutions => V(nameof(ExhaustAfterExecutions));
    public bool ExhaustsAfterUse => ExhaustAfterExecutions > 0;
    public virtual bool ExhaustsAtTurnEndInHand => false;
    public virtual bool IntrinsicRetainsAtTurnEndInHand => false;
    public virtual bool HasImmediateKeyword => HasBattleCardKeyword(BattleCardKeyword.Immediate);
    public virtual bool RetainsAtTurnEndInHand =>
        IntrinsicRetainsAtTurnEndInHand || HasToolboxRetainFeature() || HasBattleRetainFeature();
    public virtual bool TriggersAtTurnEndInHand => false;
    public virtual bool CanBePlayed =>
        SkillType != SkillTypes.none && SkillType != SkillTypes.Status;
    public bool IsStatusCard => SkillType == SkillTypes.Status;
    public bool IsAbilityCard => SkillType == SkillTypes.Ability;
    public bool Enable;
    public string Description;
    public bool Upgraded = false;
    private int _queuedExtraSkillExecutions;
    private int _prepaidDisplayedEnergy;
    private int _paidEnergyForCurrentEffect;
    private int _energyCostWaiverDepth;

    protected Skill()
    {
        UpdateDescription();
    }

    internal static Skill CreatePlaceholder(SkillTypes skillType) => new PlaceholderSkill(skillType);

    private sealed class PlaceholderSkill : Skill
    {
        private readonly SkillTypes _skillType;

        internal PlaceholderSkill(SkillTypes skillType) => _skillType = skillType;

        public override SkillTypes SkillType => _skillType;

        public override int EnergyCost => 0;
        public override int ExhaustAfterExecutions => 0;

        protected override SkillPlan BuildPlan() => null;
    }

    private string GetSkillNameToken()
    {
        const string prefix = "skill.";
        const string suffix = ".name";
        if (
            SkillNameKey.StartsWith(prefix, StringComparison.Ordinal)
            && SkillNameKey.EndsWith(suffix, StringComparison.Ordinal)
            && SkillNameKey.Length > prefix.Length + suffix.Length
        )
        {
            return SkillNameKey[prefix.Length..^suffix.Length];
        }

        return I18n.ToSnakeCase(GetType().Name);
    }

    public virtual Task Effect() => OwnerCharater?.BattleNode is Battle battle
        ? battle.ExecuteBattleEffectAsync(ExecuteEffectAsync, SkillName)
        : ExecuteEffectAsync();

    private async Task ExecuteEffectAsync()
    {
        if (OwnerCharater == null || !GodotObject.IsInstanceValid(OwnerCharater)
            || OwnerCharater.BattleNode?.ShouldAbortSkillResolution() == true)
            return;
        using var _ = OwnerCharater?.BattleNode?.PushEffectSource(OwnerCharater, SkillName);
        bool effectExecuted = false;
        try
        {
            OwnerCharater?.DisableSkill();
            if (OwnerCharater?.SkillBuffs != null)
            {
                var stun = OwnerCharater.SkillBuffs.FirstOrDefault(x =>
                    x != null && x.ThisBuffName == Buff.BuffName.Stun && x.Stack > 0
                );
                if (stun != null)
                {
                    effectExecuted = true;
                    await stun.Trigger(this);
                    await ResolveBattleOverAfterEffectAsync();
                    return;
                }
            }

            Battle actionBattle = OwnerCharater?.BattleNode;
            bool installsAction = IsActionCard && OwnerCharater is PlayerCharacter
                && actionBattle != null && !actionBattle.IsResolvingActionCards;
            if (installsAction && !actionBattle.CanInstallActionCard(this)) {
                RefundDisplayedEnergy();
                return;
            }
            if (!TryPayEnergyCostForEffect())
                return;

            effectExecuted = true;
            RecordSkillUse();
            if (actionBattle != null)
                await actionBattle.ResolvePendingBattleEffectsAsync();
            foreach (var buff in OwnerCharater.SkillBuffs.ToArray())
            {
                if (actionBattle != null)
                    await actionBattle.ExecuteBattleEffectAsync(() => buff.Trigger(this), buff.ThisBuffName.ToString());
                else
                    await buff.Trigger(this);
            }
            if (installsAction) {
                actionBattle.InstallActionCard(this, _paidEnergyForCurrentEffect);
                _actionInitialExtraExecutions = ConsumeQueuedExtraSkillExecutions();
                return;
            }
            var plan = GetPlan();
            if (plan != null)
            {
                await plan.Execute();
                try
                {
                    int extraSkillExecutions = ConsumeQueuedExtraSkillExecutions();
                    if (await ResolveBattleOverAfterEffectAsync())
                        return;

                    for (int i = 0; i < extraSkillExecutions; i++)
                    {
                        if (
                            OwnerCharater == null
                            || OwnerCharater.State == Character.CharacterState.Dying
                            || OwnerCharater.BattleNode?.ShouldAbortSkillResolution() == true
                        )
                        {
                            break;
                        }

                        await PlayEchoCardAnimationAsync();
                        RecordSkillUse();
                        if (actionBattle != null)
                            await actionBattle.ResolvePendingBattleEffectsAsync();
                        await plan.Execute();
                        if (await ResolveBattleOverAfterEffectAsync())
                            return;
                    }
                }
                finally
                {
                    // Echo repeats share the normal trigger's single exhaust use.
                    actionBattle?.RecordBattleCardExecution(this);
                }
            }
            else if (effectExecuted)
            {
                // Playable cards without an effect plan still count as executed (e.g. Plague).
                actionBattle?.RecordBattleCardExecution(this);
                await ResolveBattleOverAfterEffectAsync();
            }
        }
        finally
        {
            _prepaidDisplayedEnergy = 0;
            _paidEnergyForCurrentEffect = 0;
            // Action cards keep this round's choice even if installation was cancelled.
            if (!IsActionCard)
                ClearPreviewableRandomHostileTargets();
            ClearLockedExecutionTargets();
            ClearManualFriendlyTarget();
        }
    }

    private async Task<bool> ResolveBattleOverAfterEffectAsync()
    {
        Battle battle = OwnerCharater?.BattleNode;
        if (battle == null)
            return false;

        return await battle.ResolveBattleOverAfterSkillAsync();
    }

    private void RecordSkillUse()
    {
        if (OwnerCharater?.TriggersSkillUseEvents != true)
            return;

        OwnerCharater.BattleNode?.UsedSkills.Add(this);
        OwnerCharater.BattleNode?.NotifyTalentSkillPlayed(this);
    }

    internal void QueueExtraSkillExecutions(int count)
    {
        if (count <= 0)
            return;

        _queuedExtraSkillExecutions += count;
    }

    protected bool HasToolboxRetainFeature()
    {
        if (GameInfo.HasToolboxRetain(this))
            return true;

        return OwnerCharater == null
            && _previewPlayerIndex >= 0
            && SkillId is SkillID skillId
            && GameInfo.GetToolboxRetainCount(_previewPlayerIndex, skillId) > 0;
    }

    protected bool HasBattleRetainFeature() =>
        HasBattleCardKeyword(BattleCardKeyword.Retain);

    protected bool HasBattleExhaustAfterUseFeature() =>
        HasBattleCardKeyword(BattleCardKeyword.ExhaustAfterUse);

    protected bool HasBattleVoidnessFeature() =>
        HasBattleCardKeyword(BattleCardKeyword.Voidness);

    internal bool HasBattleCardKeyword(BattleCardKeyword keyword) =>
        OwnerCharater?.BattleNode?.HasBattleCardKeyword(this, keyword) == true;

    public int ExhaustExecutionLimit => GetExhaustExecutionLimit(OwnerCharater?.BattleNode);
    public int RemainingExecutions => Math.Max(0, ExhaustExecutionLimit
        - (OwnerCharater?.BattleNode?.GetBattleCardExecutionCount(this) ?? 0));
    internal bool ResolvesExhaustsAfterUse => ShouldExhaustAfterUse(OwnerCharater?.BattleNode);

    private int GetExhaustExecutionLimit(Battle battle) => IsAbilityCard ? 0
        : Math.Max(ExhaustAfterExecutions,
            battle?.HasBattleCardKeyword(this, BattleCardKeyword.ExhaustAfterUse) == true ? 1 : 0);

    internal bool ShouldExhaustAfterUse(Battle battle)
    {
        // Ownerless cards still have execution counts and keywords in their battle.
        int limit = GetExhaustExecutionLimit(battle);
        return limit > 0 && (battle?.GetBattleCardExecutionCount(this) ?? 0) >= limit;
    }

    internal bool ResolvesExhaustsAtTurnEndInHand =>
        ExhaustsAtTurnEndInHand || HasBattleVoidnessFeature();

    internal bool ShowsExhaustKeyword => ExhaustExecutionLimit > 0;

    internal bool ShowsVoidnessKeyword => ResolvesExhaustsAtTurnEndInHand;

    internal bool ShowsRetainKeyword =>
        OwnerCharater?.BattleNode?.ShouldShowRetainKeyword(this) ?? RetainsAtTurnEndInHand;

    private int ConsumeQueuedExtraSkillExecutions()
    {
        int queuedCount = _queuedExtraSkillExecutions;
        _queuedExtraSkillExecutions = 0;
        return Math.Max(queuedCount, 0);
    }

    private async Task PlayEchoCardAnimationAsync()
    {
        CharacterControl characterControl = OwnerCharater?.BattleNode?.CharacterControl;
        if (characterControl == null || !GodotObject.IsInstanceValid(characterControl))
            return;

        await characterControl.PlayEchoCardAsync(OwnerCharater, this);
    }

    /// <summary>
    /// For non-battle usage (e.g. previews), set preview stats so UpdateDescription can work without a Character instance.
    /// </summary>
    public void SetPreviewStats(
        int power,
        int survivability,
        int energy = 1,
        bool isPlayer = true,
        bool? useEnemySkillDamageScaling = null,
        int basePowerContribution = 0,
        int baseSurvivabilityContribution = 0,
        int playerIndex = -1
    )
    {
        _previewPower = power;
        _previewSurvivability = survivability;
        _previewBasePowerContribution = basePowerContribution;
        _previewBaseSurvivabilityContribution = baseSurvivabilityContribution;
        _previewEnergy = energy;
        _previewPlayerIndex = playerIndex;
        _previewIsPlayer = isPlayer;
        _previewUsesEnemySkillDamageScaling = useEnemySkillDamageScaling ?? !isPlayer;
    }

    protected int OwnerPower =>
        OwnerCharater != null
            ? OwnerCharater.GetEffectivePowerForSkillScaling() + (_actionPreview?.PowerDelta(OwnerCharater) ?? 0)
            : _previewPower + _previewBasePowerContribution;
    protected int OwnerSurvivability =>
        OwnerCharater != null
            ? OwnerCharater.GetEffectiveSurvivabilityForSkillScaling() + (_actionPreview?.SurvivabilityDelta(OwnerCharater) ?? 0)
            : _previewSurvivability + _previewBaseSurvivabilityContribution;
    protected int OwnerEnergy => OwnerCharater?.CurrentEnergy ?? _previewEnergy;
    protected bool IsInBattle => OwnerCharater?.BattleNode != null;
    public int RequiredEnergyCost => EnergyCost;
    public int CardEnergyCost => UsesXEnergyCost ? 1 : RequiredEnergyCost;
    public bool UsesXEnergyCost => RequiredEnergyCost == XEnergyCost;
    public string CardEnergyCostText => UsesXEnergyCost ? "X" : CardEnergyCost.ToString();
    public bool RequiresExternalEnergyPayment => RequiredEnergyCost != 0;
    internal bool IsEnergyCostWaived => _energyCostWaiverDepth > 0;

    public int ComputePreviewCacheRevision()
    {
        Character owner = OwnerCharater;
        int power =
            owner != null
                ? owner.GetEffectivePowerForSkillScaling()
                : _previewPower + _previewBasePowerContribution;
        int survivability =
            owner != null
                ? owner.GetEffectiveSurvivabilityForSkillScaling()
                : _previewSurvivability + _previewBaseSurvivabilityContribution;

        HashCode hash = new();
        hash.Add(SkillTuning.Revision);
        hash.Add(OwnerEnergy);
        hash.Add(power);
        hash.Add(survivability);
        hash.Add(Description, StringComparer.Ordinal);
        hash.Add(owner?.BattleNode?.HandPreviewContextRevision ?? 0);
        return hash.ToHashCode();
    }

    protected int DamageFromPower(int baseDamage = 0, int multiplier = 1, int clampMax = 9999)
    {
        int damage = baseDamage + OwnerPower * multiplier;
        damage = ApplyOwnerSkillDamageScaling(Math.Clamp(damage, 0, clampMax));
        damage += GetOwnerSkillAttackDamageBonus();
        return damage;
    }

    private int GetOwnerSkillAttackDamageBonus()
    {
        return OwnerCharater is PlayerCharacter player ? player.GetSkillAttackDamageBonus(this) : 0;
    }

    private bool UsesEnemySkillDamageScaling()
    {
        if (OwnerCharater != null)
        {
            if (OwnerCharater.IsPlayer)
                return false;

            var levelType = OwnerCharater.BattleNode?.CurrentLevelNode?.Type;
            return levelType != LevelNode.LevelType.Elite && levelType != LevelNode.LevelType.Boss;
        }

        return !_previewIsPlayer && _previewUsesEnemySkillDamageScaling;
    }

    private int ApplyOwnerSkillDamageScaling(int damage)
    {
        damage = Math.Max(0, damage);
        if (damage <= 0 || !UsesEnemySkillDamageScaling())
            return damage;

        return Math.Max(1, (int)MathF.Ceiling(damage * EnemySkillDamageMultiplier));
    }

    protected int BlockFromSurvivability(int baseBlock = 0, int multiplier = 1, int clampMax = 999)
    {
        int block = baseBlock + OwnerSurvivability * multiplier;
        return Math.Clamp(block, 0, clampMax);
    }

    internal int GetPaidEnergyLoopCount()
    {
        if (_actionPaidEnergy.HasValue)
            return Math.Max(0, _actionPaidEnergy.Value);
        if (OwnerCharater?.BattleNode?.TryGetInstalledActionPaidEnergy(this, out int installedEnergy) == true)
            return Math.Max(0, installedEnergy);
        if (IsEnergyCostWaived)
            return Math.Max(0, OwnerCharater?.CurrentEnergy ?? 0);

        if (_paidEnergyForCurrentEffect > 0)
            return Math.Max(0, _paidEnergyForCurrentEffect);

        if (OwnerCharater == null)
            return 0;

        return Math.Max(0, OwnerCharater.CurrentEnergy);
    }

    internal IDisposable BeginEnergyCostWaiver()
    {
        _energyCostWaiverDepth++;
        return new EnergyCostWaiverScope(this);
    }

    private void EndEnergyCostWaiver()
    {
        _energyCostWaiverDepth = Math.Max(0, _energyCostWaiverDepth - 1);
    }

    private sealed class EnergyCostWaiverScope : IDisposable
    {
        private Skill _skill;

        public EnergyCostWaiverScope(Skill skill)
        {
            _skill = skill;
        }

        public void Dispose()
        {
            _skill?.EndEnergyCostWaiver();
            _skill = null;
        }
    }

    public bool CanUseCurrentEnergy(Character executionContext = null)
    {
        Character actor = IsStatusCard ? executionContext ?? OwnerCharater : OwnerCharater;
        if (actor == null || !GodotObject.IsInstanceValid(actor) || actor.State == Character.CharacterState.Dying)
            return false;

        return CanUseEnergy(actor.CurrentEnergy, executionContext);
    }

    public bool CanUseEnergy(int availableEnergy, Character executionContext = null)
    {
        Character actor = IsStatusCard ? executionContext ?? OwnerCharater : OwnerCharater;
        if (actor == null || !GodotObject.IsInstanceValid(actor) || actor.State == Character.CharacterState.Dying)
            return false;

        if (!CanBePlayed)
            return false;

        if (UsesXEnergyCost)
            return availableEnergy >= 0;

        return availableEnergy >= CardEnergyCost;
    }

    public virtual void OnDrawnToHand(PlayerCharacter player) { }

    public virtual Task OnTurnEndInHand(PlayerCharacter player) => Task.CompletedTask;

    public bool TrySpendDisplayedEnergy()
    {
        if (OwnerCharater == null)
            return UsesXEnergyCost || CardEnergyCost <= 0;

        int availableEnergy = OwnerCharater.CurrentEnergy;

        if (UsesXEnergyCost)
        {
            int xPaymentCost = Math.Max(0, availableEnergy);
            if (xPaymentCost > 0)
            {
                if (
                    OwnerCharater.BattleNode?.UpdataEnergy(
                        OwnerCharater,
                        -xPaymentCost,
                        OwnerCharater
                    ) != -xPaymentCost
                )
                    return false;
            }

            _prepaidDisplayedEnergy = xPaymentCost;
            _paidEnergyForCurrentEffect = xPaymentCost;
            return true;
        }

        if (CardEnergyCost <= 0)
            return true;

        int paymentCost = CardEnergyCost;
        if (availableEnergy < paymentCost)
            return false;

        if (
            OwnerCharater.BattleNode?.UpdataEnergy(OwnerCharater, -paymentCost, OwnerCharater)
            != -paymentCost
        )
            return false;

        _prepaidDisplayedEnergy += paymentCost;
        _paidEnergyForCurrentEffect = paymentCost;
        return true;
    }

    public void RefundDisplayedEnergy()
    {
        if (_prepaidDisplayedEnergy <= 0 || OwnerCharater == null)
            return;

        int refund = _prepaidDisplayedEnergy;
        _prepaidDisplayedEnergy = 0;
        _paidEnergyForCurrentEffect = 0;
        OwnerCharater.BattleNode?.UpdataEnergy(OwnerCharater, refund, OwnerCharater);
    }

    private bool TryPayEnergyCostForEffect()
    {
        if (IsEnergyCostWaived)
            return true;

        if (OwnerCharater == null)
            return CardEnergyCost <= 0;

        if (UsesXEnergyCost)
        {
            if (
                _prepaidDisplayedEnergy == _paidEnergyForCurrentEffect
                && (_prepaidDisplayedEnergy > 0 || OwnerCharater.CurrentEnergy == 0)
            )
                return true;

            int paymentCost = Math.Max(0, OwnerCharater.CurrentEnergy);
            if (paymentCost > 0)
            {
                if (
                    OwnerCharater.BattleNode?.UpdataEnergy(
                        OwnerCharater,
                        -paymentCost,
                        OwnerCharater
                    ) != -paymentCost
                )
                    return false;
            }

            _paidEnergyForCurrentEffect = paymentCost;
            return true;
        }

        int cost = CardEnergyCost;
        if (cost <= 0)
            return true;

        if (_prepaidDisplayedEnergy >= cost)
        {
            _paidEnergyForCurrentEffect = Math.Max(_paidEnergyForCurrentEffect, cost);
            return true;
        }

        if (OwnerCharater.CurrentEnergy < cost)
            return false;

        if (OwnerCharater.BattleNode?.UpdataEnergy(OwnerCharater, -cost, OwnerCharater) != -cost)
            return false;

        _paidEnergyForCurrentEffect = cost;
        return true;
    }

    public static bool HasTauntBuff(Character target) =>
        target?.HurtBuffs?.Any(buff =>
            buff != null && buff.ThisBuffName == Buff.BuffName.Taunt && buff.Stack > 0
        ) == true;

    private static bool HasInvisibleBuff(Character target) =>
        target?.HasActiveStartActionBuff(Buff.BuffName.Invisible) == true;

    public static bool IsSelectableHostileTarget(Character target) =>
        IsSelectableHostileTarget(null, target);

    public static bool IsSelectableHostileTarget(Character owner, Character target)
    {
        if (target == null || !GodotObject.IsInstanceValid(target))
            return false;

        return target.State == Character.CharacterState.Normal;
    }

    public static bool IsCurrentlyHostileTargetable(
        Character owner,
        Character target,
        bool applyTaunt = true
    )
    {
        if (!IsSelectableHostileTarget(owner, target))
            return false;

        return ChooseHostileTargetsByOrder(
            owner,
            applyTaunt: applyTaunt
        ).Contains(target);
    }

    public static bool IsValidHostileExecutionTarget(Character owner, Character target)
    {
        if (target == null || !GodotObject.IsInstanceValid(target))
            return false;

        return target.State
            is Character.CharacterState.Normal
                or Character.CharacterState.Dying;
    }

    public static Character[] FilterHostileTargetSequence(
        IEnumerable<Character> orderedTargets,
        bool returnDummyWhenEmpty = false,
        Character dummyTarget = null,
        bool applyTaunt = false,
        bool respectInvisible = true
    )
    {
        Character[] ordered =
            orderedTargets?.Where(target => target != null).ToArray() ?? Array.Empty<Character>();
        Character[] visibleTargets = respectInvisible
            ? ordered.Where(target => !HasInvisibleBuff(target)).ToArray()
            : ordered;

        // If everyone is invisible, fall back to the original ordered sequence and
        // continue target selection normally to avoid clearing the target list.
        Character[] selectableTargets = visibleTargets.Length > 0 ? visibleTargets : ordered;
        Character[] tauntTargets = applyTaunt
            ? selectableTargets.Where(HasTauntBuff).ToArray()
            : Array.Empty<Character>();
        Character[] targets =
            applyTaunt && tauntTargets.Length > 0 ? tauntTargets : selectableTargets;

        if (targets.Length > 0 || !returnDummyWhenEmpty)
            return targets;

        return dummyTarget != null ? [dummyTarget] : Array.Empty<Character>();
    }

    public static Character[] ChooseHostileTargetsByOrder(
        Character owner,
        bool byBehindRow = false,
        bool returnDummyWhenEmpty = true,
        bool normalOnly = true,
        bool dyingFilter = false,
        bool applyTaunt = false,
        bool respectInvisible = true
    )
    {
        if (owner?.BattleNode == null)
            return Array.Empty<Character>();

        IEnumerable<Character> source = owner
            .BattleNode.GetOrderedTeamCharacters(
                !owner.IsPlayer,
                includeSummons: true,
                dyingFilter: dyingFilter
            )
            .Where(target => target != null);

        if (normalOnly)
            source = source.Where(target => IsSelectableHostileTarget(owner, target));

        IEnumerable<Character> ordered = byBehindRow
            ? source.OrderByDescending(target => target.PositionIndex)
            : source.OrderBy(target => target.PositionIndex);

        return FilterHostileTargetSequence(
            ordered,
            returnDummyWhenEmpty,
            owner.BattleNode?.dummy,
            applyTaunt,
            respectInvisible
        );
    }

    private Character[] GetHostileTargetsInTeamOrder(
        bool dyingFilter,
        bool returnDummyWhenEmpty = false,
        bool applyTaunt = false,
        bool respectInvisible = true
    )
    {
        if (OwnerCharater?.BattleNode == null)
            return Array.Empty<Character>();

        if (_actionPreview != null)
            return _actionPreview.HostileTargets(OwnerCharater, false, applyTaunt, respectInvisible);
        Character[] orderedTargets = OwnerCharater.BattleNode.GetOrderedTeamCharacters(
            !OwnerCharater.IsPlayer,
            includeSummons: true,
            dyingFilter: dyingFilter
        );
        return FilterHostileTargetSequence(
            orderedTargets,
            returnDummyWhenEmpty,
            OwnerCharater.BattleNode?.dummy,
            applyTaunt,
            respectInvisible
        );
    }

    public Character[] ChosetargetByOrder(
        bool byBehindRow = false,
        bool applyTaunt = false,
        bool respectInvisible = true
    ) =>
        _actionPreview?.HostileTargets(OwnerCharater, byBehindRow, applyTaunt, respectInvisible)
        ?? ChooseHostileTargetsByOrder(
            OwnerCharater,
            byBehindRow,
            returnDummyWhenEmpty: true,
            applyTaunt: applyTaunt,
            respectInvisible: respectInvisible
        );

    private static bool IsDummyTarget(Skill skill, Character target)
    {
        Character dummy = skill?.OwnerCharater?.BattleNode?.dummy;
        return target != null && dummy != null && target == dummy;
    }

    public Character GetAllyByRelative(int Where, bool dyingFilter = false)
    {
        if (OwnerCharater?.BattleNode == null)
            return null;

        Character[] ally = OwnerCharater.BattleNode.GetOrderedTeamCharacters(
            OwnerCharater.IsPlayer,
            includeSummons: false,
            dyingFilter: dyingFilter
        );

        if (!OwnerCharater.IsFullCharacter && Where == 0)
            return OwnerCharater;

        if (ally.Length == 0)
            return null;

        Character anchor = OwnerCharater;
        if (!OwnerCharater.IsFullCharacter && OwnerCharater is SummonCharacter summon)
            anchor = summon.Summoner ?? OwnerCharater;

        int currentIndex = Array.IndexOf(ally, anchor);
        if (currentIndex == -1)
            currentIndex = 0;

        if (Where == 0)
            return ally[currentIndex];

        if (ally.Length <= 1)
            return null;

        int direction = Math.Sign(Where);
        int remainingSteps = Math.Abs(Where);
        int targetIndex = currentIndex;
        int guard = ally.Length * remainingSteps * 2;
        while (remainingSteps > 0 && guard-- > 0)
        {
            targetIndex = (targetIndex + direction + ally.Length) % ally.Length;
            if (targetIndex == currentIndex)
                continue;
            remainingSteps--;
        }

        return remainingSteps == 0 && targetIndex != currentIndex ? ally[targetIndex] : null;
    }

    public Character[] GetAllAllyWithOrder(bool dyingFilter = false, bool includeSummons = false)
    {
        if (OwnerCharater?.BattleNode == null)
            return Array.Empty<Character>();

        return OwnerCharater.BattleNode.GetOrderedTeamCharacters(
            OwnerCharater.IsPlayer,
            includeSummons,
            dyingFilter
        );
    }

    public Character GetAllyByIndex(int index, bool dyingFilter = false)
    {
        if (OwnerCharater?.BattleNode == null)
            return null;

        var allies = OwnerCharater
            .BattleNode.GetOrderedTeamCharacters(OwnerCharater.IsPlayer, includeSummons: false)
            .ToList();
        if (allies.Count == 0)
            return null;

        int safeIndex = (index % allies.Count + allies.Count) % allies.Count;
        if (dyingFilter)
        {
            while (allies[safeIndex].State == Character.CharacterState.Dying)
            {
                if (safeIndex + 1 >= allies.Count / 2.0)
                {
                    safeIndex = (safeIndex - 1) % allies.Count;
                }
                else
                {
                    safeIndex = (safeIndex + 1) % allies.Count;
                }
            }
        }

        return allies[safeIndex];
    }

    public async Task SwapPositionIndex(
        Character first,
        Character second,
        float disappearDuration = 0.28f,
        float moveDuration = 0.22f,
        float appearDuration = 0.28f
    )
    {
        if (first == null || second == null || first == second)
            return;

        var battle = first.BattleNode;
        if (battle == null || second.BattleNode == null)
            return;
        if (!GodotObject.IsInstanceValid(battle) || battle != second.BattleNode)
            return;
        if (first.IsPlayer != second.IsPlayer)
            return;

        await Task.WhenAll(
            TweenSpriteProgress(first, 1f, disappearDuration),
            TweenSpriteProgress(second, 1f, disappearDuration)
        );

        int tempIndex = first.PositionIndex;
        first.PositionIndex = second.PositionIndex;
        second.PositionIndex = tempIndex;

        SwapBattleOrder(battle, first, second);
        Vector2 firstTarget = battle.GetFormationPosition(first.PositionIndex, first.IsPlayer ? -1 : 1);
        Vector2 secondTarget = battle.GetFormationPosition(second.PositionIndex, second.IsPlayer ? -1 : 1);
        UpdateZIndexByPosition(first);
        UpdateZIndexByPosition(second);

        await Task.WhenAll(
            TweenCharacterPosition(first, firstTarget, moveDuration),
            TweenCharacterPosition(second, secondTarget, moveDuration)
        );
        first.Position = firstTarget;
        second.Position = secondTarget;
        first.OriginalPosition = firstTarget;
        second.OriginalPosition = secondTarget;
        battle.RefreshSummonPositions(first);
        battle.RefreshSummonPositions(second);
        battle.RefreshTurnOrderPreview();

        await Task.WhenAll(
            TweenSpriteProgress(first, 0f, appearDuration),
            TweenSpriteProgress(second, 0f, appearDuration)
        );
    }

    private static async Task TweenSpriteProgress(Character character, float target, float duration)
    {
        if (character?.Sprite == null || !GodotObject.IsInstanceValid(character.Sprite))
            return;

        if (!TryGetProgressMaterial(character.Sprite, out ShaderMaterial material))
            return;

        Tween tween = character.CreateTween();
        tween.TweenMethod(
            Callable.From<float>(value => material.SetShaderParameter("progress", value)),
            GetShaderParameterFloat(material, "progress"),
            target,
            Math.Max(0f, duration)
        );
        await character.ToSignal(tween, "finished");
    }

    private static float GetShaderParameterFloat(ShaderMaterial material, string parameterName)
    {
        if (material == null)
            return 0f;

        Variant value = material.GetShaderParameter(parameterName);
        return value.VariantType switch
        {
            Variant.Type.Float => (float)value.AsDouble(),
            Variant.Type.Int => value.AsInt64(),
            _ => 0f,
        };
    }

    private static async Task TweenCharacterPosition(
        Character character,
        Vector2 target,
        float duration
    )
    {
        if (character == null || !GodotObject.IsInstanceValid(character))
            return;

        Tween tween = character.CreateTween();
        tween.TweenProperty(character, "position", target, Math.Max(0f, duration));
        await character.ToSignal(tween, "finished");
    }


    private static void UpdateZIndexByPosition(Character character)
    {
        if (character == null)
            return;

        character.ZIndex = Math.Max(character.PositionIndex, 0);
    }

    private static bool TryGetProgressMaterial(Node sprite, out ShaderMaterial material)
    {
        material = null;

        if (sprite is CanvasItem canvas && canvas.Material is ShaderMaterial canvasMaterial)
        {
            material = canvasMaterial;
            return true;
        }

        if (sprite.GetClass() == "SpineSprite")
        {
            Variant normalVariant = sprite.Get("normal_material");
            if (normalVariant.VariantType == Variant.Type.Object)
            {
                material = normalVariant.As<ShaderMaterial>();
                if (material != null)
                    return true;
            }
        }

        return false;
    }

    private static void SwapBattleOrder(Battle battle, Character first, Character second)
    {
        if (battle == null || first == null || second == null)
            return;

        if (first.IsPlayer)
        {
            var list = battle.PlayersList;
            if (first is not PlayerCharacter p1 || second is not PlayerCharacter p2)
                return;

            int firstIndex = list.IndexOf(p1);
            int secondIndex = list.IndexOf(p2);
            if (firstIndex < 0 || secondIndex < 0 || firstIndex == secondIndex)
                return;

            SwapListOrderThenMoveActingToBack(battle, list, firstIndex, secondIndex);
            return;
        }

        var enemyList = battle.EnemiesList;
        if (first is not EnemyCharacter e1 || second is not EnemyCharacter e2)
            return;

        int enemyFirstIndex = enemyList.IndexOf(e1);
        int enemySecondIndex = enemyList.IndexOf(e2);
        if (enemyFirstIndex < 0 || enemySecondIndex < 0 || enemyFirstIndex == enemySecondIndex)
            return;

        SwapListOrderThenMoveActingToBack(battle, enemyList, enemyFirstIndex, enemySecondIndex);
    }

    private static void SwapListOrderThenMoveActingToBack<T>(
        Battle battle,
        List<T> list,
        int firstIndex,
        int secondIndex
    )
        where T : Character
    {
        (list[firstIndex], list[secondIndex]) = (list[secondIndex], list[firstIndex]);

        Character actingCharacter = battle.CurrentActionCharacter;
        if (actingCharacter == null)
            return;

        int actingIndex = list.FindIndex(character => character == actingCharacter);
        if (actingIndex < 0 || actingIndex == list.Count - 1)
            return;

        T acting = list[actingIndex];
        list.RemoveAt(actingIndex);
        list.Add(acting);
    }

    private static bool CanContinueAttack(Character owner, Character target)
    {
        return owner != null
            && owner.State != Character.CharacterState.Dying
            && IsValidHostileExecutionTarget(owner, target);
    }

    private Character ResolvePrimaryTarget(bool byBehindRow)
    {
        Character[] targets = ChosetargetByOrder(byBehindRow: byBehindRow, applyTaunt: true);
        if (targets.Length == 0 || IsDummyTarget(this, targets[0]))
            return null;
        return targets[0];
    }

    private void SpawnAttackHitEffect(Character target, SwordSlashSequence sequence = null, int hitIndex = 0)
    {
        if (!GodotObject.IsInstanceValid(target) || !target.IsInsideTree()
            || target.IsQueuedForDeletion() || target.WarmupMode
            || target.State == Character.CharacterState.Dying)
            return;

        Node2D parent = target.GetParent() as Node2D ?? target;
        Transform2D toLocal = parent.GetGlobalTransformWithCanvas().AffineInverse();
        Vector2 center = toLocal * target.GetVisualCenterGlobalPosition();
        Control frame = target.GetNodeOrNull<Control>("Hoverframe");
        if (GodotObject.IsInstanceValid(frame) && frame.IsInsideTree() && frame.Size.Y > 0f)
        {
            Transform2D frameToParent = toLocal * frame.GetGlobalTransformWithCanvas();
            center = frameToParent * (frame.Size * 0.5f);
        }
        SwordSlashVfx.Spawn(parent, center,
            flipHorizontally: GodotObject.IsInstanceValid(OwnerCharater) && !OwnerCharater.IsPlayer,
            sequenceSeed: sequence?.Seed ?? 0, hitIndex: hitIndex, hitCount: sequence?.HitCount ?? 1);
    }

    private async Task ExecuteAttackSequence(
        Character target,
        int damage,
        int times,
        bool playHitEffectForFirstHit,
        bool delayAfterLastHit,
        bool applyAttackBuff
    )
    {
        if (target == null || IsDummyTarget(this, target))
            return;

        damage = Math.Clamp(damage, 0, 9999);
        times = Math.Max(0, times);
        if (times <= 0)
            return;

        if (OwnerCharater?.BattleNode is Battle battle)
            await battle.ResolvePendingBattleEffectsAsync();

        var slashSequence = new SwordSlashSequence(times);
        await AttackAnimation(target, slashSequence, playHitEffect: !playHitEffectForFirstHit);

        int modifiedDamage = damage;
        if (applyAttackBuff)
        {
            modifiedDamage = Math.Clamp(
                AttackBuff.ApplyOutgoingDamageModifiers(
                    OwnerCharater,
                    damage,
                    target,
                    consumeStacks: true
                ),
                0,
                9999
            );
        }

        for (int i = 0; i < times; i++)
        {
            if (!CanContinueAttack(OwnerCharater, target))
                break;

            if (playHitEffectForFirstHit || i > 0)
                SpawnAttackHitEffect(target, slashSequence, i);

            await target.GetHurt(
                modifiedDamage,
                OwnerCharater,
                damageKind: Character.DamageKind.Attack
            );

            if (delayAfterLastHit || i < times - 1)
                await Task.Delay(100);
        }
    }

    public async Task Attack(
        int damage,
        int times = 1,
        bool byBehindRow = false,
        Character target = null,
        bool playHitEffectForFirstHit = false,
        bool delayAfterLastHit = true,
        bool applyAttackBuff = true
    )
    {
        Character resolvedTarget = target ?? ResolvePrimaryTarget(byBehindRow);
        await ExecuteAttackSequence(
            resolvedTarget,
            damage,
            times,
            playHitEffectForFirstHit,
            delayAfterLastHit,
            applyAttackBuff
        );
    }

    public async Task AOE(int damage, int Num, int times, bool byBehindRow = false)
    {
        var targets = ChosetargetByOrder(byBehindRow: byBehindRow, applyTaunt: true);
        if (targets.Length == 0 || Num <= 0 || times <= 0)
            return;

        int count = Math.Min(Num, targets.Length);
        await AttackAreaAsync(targets.Take(count).ToArray(), damage, times, prepareEveryHit: true);
    }

    public async Task AttackAnimation(Character target, SwordSlashSequence sequence = null,
        bool playHitEffect = true)
    {
        if (target == null || IsDummyTarget(this, target))
            return;

        CharacterEffect.Spawn(OwnerCharater, "explode");
        await Task.Delay(300);
        AudioManager.PlayAttack(OwnerCharater);
        if (playHitEffect)
            SpawnAttackHitEffect(target, sequence);
    }

}
