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
    public virtual int EnergyCost => GetDefaultEnergyCost();
    public virtual int EnemySpecialIntentionCooldown => SkillType == SkillTypes.Special ? 1 : 0;
    public virtual bool ExhaustsAfterUse => false;
    public virtual bool ExhaustsAtTurnEndInHand => false;
    public virtual bool IntrinsicRetainsAtTurnEndInHand => false;
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

    public virtual async Task Effect()
    {
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

            if (!TryPayEnergyCostForEffect())
                return;

            effectExecuted = true;
            RecordSkillUse();
            foreach (var buff in OwnerCharater.SkillBuffs)
            {
                await buff.Trigger(this);
            }
            var plan = GetPlan();
            if (plan != null)
            {
                await plan.Execute();
                if (await ResolveBattleOverAfterEffectAsync())
                    return;

                int extraSkillExecutions = ConsumeQueuedExtraSkillExecutions();
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
                    await plan.Execute();
                    if (await ResolveBattleOverAfterEffectAsync())
                        return;
                }
            }
            else if (effectExecuted)
            {
                await ResolveBattleOverAfterEffectAsync();
            }
        }
        finally
        {
            _prepaidDisplayedEnergy = 0;
            _paidEnergyForCurrentEffect = 0;
            _previewableRandomHostileTargets.Clear();
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

    internal bool ResolvesExhaustsAfterUse =>
        !IsAbilityCard && (ExhaustsAfterUse || HasBattleExhaustAfterUseFeature());

    internal bool ResolvesExhaustsAtTurnEndInHand =>
        ExhaustsAtTurnEndInHand || HasBattleVoidnessFeature();

    internal bool ShowsExhaustKeyword => ResolvesExhaustsAfterUse;

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
            ? OwnerCharater.GetEffectivePowerForSkillScaling()
            : _previewPower + _previewBasePowerContribution;
    protected int OwnerSurvivability =>
        OwnerCharater != null
            ? OwnerCharater.GetEffectiveSurvivabilityForSkillScaling()
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

    public bool CanUseCurrentEnergy()
    {
        if (OwnerCharater == null || OwnerCharater.State == Character.CharacterState.Dying)
            return false;

        return CanUseEnergy(OwnerCharater.CurrentEnergy);
    }

    public bool CanUseEnergy(int availableEnergy)
    {
        if (OwnerCharater == null || OwnerCharater.State == Character.CharacterState.Dying)
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

    private int GetDefaultEnergyCost()
    {
        bool isEnemy = OwnerCharater != null ? !OwnerCharater.IsPlayer : !_previewIsPlayer;
        if (isEnemy)
        {
            if (SkillType == SkillTypes.Attack || SkillType == SkillTypes.Survive)
                return 0;
        }

        return SkillType switch
        {
            SkillTypes.Attack => 1,
            SkillTypes.Survive => 1,
            SkillTypes.Special => 1,
            SkillTypes.Ability => 1,
            _ => 0,
        };
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
        ChooseHostileTargetsByOrder(
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

    private void SpawnAttackHitEffect(Character target)
    {
        var attack = AttackEffect.Spawn(target);
        if (attack == null)
            return;

        attack.GlobalPosition = target.GlobalPosition;
        attack.PlayAttack();
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

        OwnerCharater?.BattleNode?.NotifyAllyAttackExecuted(OwnerCharater);

        await AttackAnimation(target);

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
                SpawnAttackHitEffect(target);

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
        if (targets.Length == 0)
            return;

        int count = Math.Min(Num, targets.Length);
        List<Task> tasks = new();
        for (int hit = 0; hit < times; hit++)
        {
            for (int i = 0; i < count; i++)
            {
                if (IsDummyTarget(this, targets[i]))
                    continue;

                tasks.Add(
                    Attack(
                        damage,
                        times: 1,
                        target: targets[i],
                        playHitEffectForFirstHit: true,
                        delayAfterLastHit: true
                    )
                );

                if (i < count - 1)
                    await YieldBatchedCombatFrameAsync();
            }
        }

        if (tasks.Count > 0)
            await Task.WhenAll(tasks);
    }

    private async Task YieldBatchedCombatFrameAsync()
    {
        if (
            OwnerCharater != null
            && GodotObject.IsInstanceValid(OwnerCharater)
            && OwnerCharater.GetTree() != null
        )
        {
            await OwnerCharater.ToSignal(
                OwnerCharater.GetTree(),
                SceneTree.SignalName.ProcessFrame
            );
            return;
        }

        await Task.Yield();
    }

    public async Task AttackAnimation(Character target)
    {
        if (target == null || IsDummyTarget(this, target))
            return;

        AttackEffect attack = AttackEffect.Spawn(OwnerCharater);
        CharacterEffect.Spawn(OwnerCharater, "explode");
        await Task.Delay(300);
        AudioManager.PlayAttack(OwnerCharater);
        if (attack != null && GodotObject.IsInstanceValid(attack))
        {
            attack.GlobalPosition = target.GlobalPosition;
            attack.PlayAttack();
        }
    }

    public async Task Carry(Character target, int skillIndex)
    {
        if (
            target == null
            || !target.IsFullCharacter
            || target.State == Character.CharacterState.Dying
            || target == OwnerCharater
        )
            return;

        if (!TryGetCarrySkillType(skillIndex, out SkillTypes skillType))
            return;

        Skill carriedSkill = target.BattleNode?.DrawCarrySkill(target, skillType);
        if (carriedSkill == null)
            return;

        Relic.ApplyCarryRelicEffects(target);

        carriedSkill.OwnerCharater = target;
        carriedSkill.UpdateDescription();
        CharacterControl characterControl = target.BattleNode?.CharacterControl;
        if (
            OwnerCharater is PlayerCharacter
            && target is PlayerCharacter
            && characterControl != null
            && GodotObject.IsInstanceValid(characterControl)
        )
        {
            await characterControl.QueueCarryCardAsync(target, carriedSkill);
            return;
        }

        using (carriedSkill.BeginEnergyCostWaiver())
        {
            await carriedSkill.Effect();
        }
    }

    private static bool TryGetCarrySkillType(int skillIndex, out SkillTypes skillType)
    {
        skillType = skillIndex switch
        {
            0 => SkillTypes.none,
            1 => SkillTypes.Attack,
            2 => SkillTypes.Survive,
            3 => SkillTypes.Special,
            4 => SkillTypes.Ability,
            _ => SkillTypes.none,
        };

        return skillIndex is >= 0 and <= 4;
    }
}
