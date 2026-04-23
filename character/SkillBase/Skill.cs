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

    [Description("速度")]
    Speed,

    [Description("生命上限")]
    MaxLife,
}

public partial class Skill
{
    private int _previewPower;
    private int _previewSurvivability;
    private int _previewEnergy = 1;

    public static PackedScene AttackScene = ResourceLoader.Load<PackedScene>(
        "res://battle/Effect/AttackEffect.tscn"
    );
    public static PackedScene BurnScene = ResourceLoader.Load<PackedScene>(
        "res://battle/Effect/burn.tscn"
    );

    public enum SkillTypes
    {
        [Description("攻击")]
        Attack,

        [Description("生存")]
        Survive,

        [Description("特殊")]
        Special,

        [Description("无")]
        none,
    }

    public virtual string SkillName { set; get; }
    public SkillTypes SkillType;
    public Character OwnerCharater;
    public bool Enable;
    public string Description;
    public bool Upgraded = false;

    public Skill(SkillTypes skillType)
    {
        SkillType = skillType;
    }

    public virtual async Task Effect()
    {
        using var _ = OwnerCharater?.BattleNode?.PushEffectSource(OwnerCharater, SkillName);
        if (OwnerCharater?.SkillBuffs != null)
        {
            var stun = OwnerCharater.SkillBuffs.FirstOrDefault(x =>
                x != null && x.ThisBuffName == Buff.BuffName.Stun && x.Stack > 0
            );
            if (stun != null)
            {
                await stun.Trigger(this);
                return;
            }
        }

        OwnerCharater.DisableSkill();
        if (OwnerCharater?.TriggersSkillUseEvents != false)
            OwnerCharater.BattleNode.UsedSkills.Add(this);
        foreach (var buff in OwnerCharater.SkillBuffs)
        {
            await buff.Trigger(this);
        }
        var plan = GetPlan();
        if (plan != null)
        {
            await plan.Execute();
        }
    }

    /// <summary>
    /// For non-battle usage (e.g. previews), set preview stats so UpdateDescription can work without a Character instance.
    /// </summary>
    public void SetPreviewStats(int power, int survivability, int energy = 1)
    {
        _previewPower = power;
        _previewSurvivability = survivability;
        _previewEnergy = energy;
    }

    protected int OwnerPower => OwnerCharater != null ? OwnerCharater.BattlePower : _previewPower;
    protected int OwnerSurvivability =>
        OwnerCharater != null ? OwnerCharater.BattleSurvivability : _previewSurvivability;
    protected int OwnerEnergy => OwnerCharater?.Energy ?? _previewEnergy;
    protected bool IsInBattle => OwnerCharater?.BattleNode != null;

    protected int DamageFromPower(int baseDamage = 0, int powerMultiplier = 1, int clampMax = 9999)
    {
        int damage = baseDamage + OwnerPower * powerMultiplier;
        return Math.Clamp(damage, 0, clampMax);
    }

    protected int BlockFromSurvivability(
        int baseBlock = 0,
        int survivabilityMultiplier = 1,
        int clampMax = 999
    )
    {
        int block = baseBlock + OwnerSurvivability * survivabilityMultiplier;
        return Math.Clamp(block, 0, clampMax);
    }

    protected bool TrySpendEnergy(int cost)
    {
        if (OwnerCharater == null)
            return false;
        if (OwnerCharater.Energy < cost)
            return false;
        OwnerCharater.UpdataEnergy(-cost, OwnerCharater);
        return true;
    }

    public Character[] ChosetargetByOrder(bool byBehindRow = false)
    {
        if (OwnerCharater?.BattleNode == null)
            return [];

        static int Row(int pos) => pos > 0 ? (pos - 1) % 3 : 0;
        static int Col(int pos) => pos > 0 ? (pos - 1) / 3 : 0;

        int attackerRow = Row(OwnerCharater.PositionIndex);

        IEnumerable<Character> source = OwnerCharater.BattleNode.GetOrderedTeamCharacters(
            !OwnerCharater.IsPlayer,
            includeSummons: true
        );

        var ordered = byBehindRow
            ? source
                .Where(x => x != null)
                .OrderBy(x => Math.Abs(Row(x.PositionIndex) - attackerRow))
                .ThenBy(x => Row(x.PositionIndex))
                .ThenByDescending(x => Col(x.PositionIndex))
                .Where(x => x.State == Character.CharacterState.Normal)
                .ToArray()
            : source
                .Where(x => x != null)
                .OrderBy(x => Math.Abs(Row(x.PositionIndex) - attackerRow))
                .ThenBy(x => Row(x.PositionIndex))
                .ThenBy(x => Col(x.PositionIndex))
                .Where(x => x.State == Character.CharacterState.Normal)
                .ToArray();

        var visible = ordered
            .Where(x =>
                x.StartActionBuffs.Any(b => b.ThisBuffName == Buff.BuffName.Invisible) == false
            )
            .ToArray();

        // If everyone is invisible, still allow targeting to avoid soft-lock.
        var targets = visible.Length > 0 ? visible : ordered;

        var tauntTargets = targets
            .Where(target =>
                target.HurtBuffs.Any(buff =>
                    buff != null && buff.ThisBuffName == Buff.BuffName.Taunt && buff.Stack > 0
                )
            )
            .ToArray();
        if (tauntTargets.Length > 0)
            targets = tauntTargets;

        return targets.Length > 0 ? targets : [OwnerCharater.BattleNode.dummy];
    }

    private static bool IsDummyTarget(Skill skill, Character target)
    {
        Character dummy = skill?.OwnerCharater?.BattleNode?.dummy;
        return target != null && dummy != null && target == dummy;
    }

    public Character GetAllyByRelative(int Where, bool dyingFilter = false)
    {
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

        int totalOffset = currentIndex + Where;
        int targetIndex = (totalOffset % ally.Length + ally.Length) % ally.Length;

        return ally[targetIndex];
    }

    public Character[] GetAllAllyWithOrder(bool dyingFilter = false, bool includeSummons = false)
    {
        return OwnerCharater.BattleNode.GetOrderedTeamCharacters(
            OwnerCharater.IsPlayer,
            includeSummons,
            dyingFilter
        );
    }

    public Character GetAllyByIndex(int index, bool dyingFilter = false)
    {
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
        Vector2 firstTarget = ComputeBattlePosition(first.PositionIndex, first.IsPlayer);
        Vector2 secondTarget = ComputeBattlePosition(second.PositionIndex, second.IsPlayer);
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
            (float)material.GetShaderParameter("progress"),
            target,
            Math.Max(0f, duration)
        );
        await character.ToSignal(tween, "finished");
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

    private static Vector2 ComputeBattlePosition(int positionIndex, bool isPlayer)
    {
        float bGapY = 140f;
        float bGapX = 280f;
        float bSkew = 10f;
        int row = positionIndex > 0 ? (positionIndex - 1) % 3 : 0;
        int col = positionIndex > 0 ? (positionIndex - 1) / 3 : 0;
        int side = isPlayer ? -1 : 1;
        float xPos = col * bGapX * side - (row * bSkew - 100 * (row - 1));
        float yPos = row * bGapY;
        return new Vector2(xPos, yPos);
    }

    private static void UpdateZIndexByPosition(Character character)
    {
        if (character == null)
            return;

        int row = character.PositionIndex > 0 ? (character.PositionIndex - 1) % 3 : 0;
        character.ZIndex = row;
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

            (list[firstIndex], list[secondIndex]) = (list[secondIndex], list[firstIndex]);
            return;
        }

        var enemyList = battle.EnemiesList;
        if (first is not EnemyCharacter e1 || second is not EnemyCharacter e2)
            return;

        int enemyFirstIndex = enemyList.IndexOf(e1);
        int enemySecondIndex = enemyList.IndexOf(e2);
        if (enemyFirstIndex < 0 || enemySecondIndex < 0 || enemyFirstIndex == enemySecondIndex)
            return;

        (enemyList[enemyFirstIndex], enemyList[enemySecondIndex]) = (
            enemyList[enemySecondIndex],
            enemyList[enemyFirstIndex]
        );
    }

    private static bool CanContinueAttack(Character owner, Character target)
    {
        return
            owner != null
            && owner.State != Character.CharacterState.Dying
            && target != null
            && target.State == Character.CharacterState.Normal;
    }

    private Character ResolvePrimaryTarget(bool byBehindRow)
    {
        Character[] targets = ChosetargetByOrder(byBehindRow: byBehindRow);
        if (targets.Length == 0 || IsDummyTarget(this, targets[0]))
            return null;
        return targets[0];
    }

    private void SpawnAttackHitEffect(Character target)
    {
        var attack = AttackScene.Instantiate() as AttackEffect;
        target.AddChild(attack);
        attack.AnimationPlayer0.Play("Attack1");
        attack.GlobalPosition = target.GlobalPosition;
    }

    private async Task ExecuteAttackSequence(
        Character target,
        int damage,
        int times,
        bool playHitEffectForFirstHit,
        bool delayAfterLastHit
    )
    {
        if (target == null || IsDummyTarget(this, target))
            return;

        damage = Math.Clamp(damage, 0, 9999);
        times = Math.Max(0, times);
        if (times <= 0)
            return;

        await AttackAnimation(target);

        for (int i = 0; i < times; i++)
        {
            if (!CanContinueAttack(OwnerCharater, target))
                break;

            if (playHitEffectForFirstHit || i > 0)
                SpawnAttackHitEffect(target);

            int modifiedDamage = Math.Clamp(
                AttackBuff.ApplyOutgoingDamageModifiers(
                    OwnerCharater,
                    damage,
                    target,
                    consumeStacks: true
                ),
                0,
                9999
            );
            await target.GetHurt(modifiedDamage, OwnerCharater);

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
        bool delayAfterLastHit = true
    )
    {
        Character resolvedTarget = target ?? ResolvePrimaryTarget(byBehindRow);
        await ExecuteAttackSequence(
            resolvedTarget,
            damage,
            times,
            playHitEffectForFirstHit,
            delayAfterLastHit
        );
    }

    public async Task AOE(int damage, int Num, int times, bool byBehindRow = false)
    {
        var targets = ChosetargetByOrder(byBehindRow: byBehindRow);
        if (targets.Length == 0)
            return;

        int count = Math.Min(Num, targets.Length);
        List<Task> tasks = new();
        for (int i = 0; i < count; i++)
        {
            if (IsDummyTarget(this, targets[i]))
                continue;
            tasks.Add(
                Attack(
                    damage,
                    times: times,
                    target: targets[i],
                    playHitEffectForFirstHit: true,
                    delayAfterLastHit: true
                )
            );
        }
        if (tasks.Count == 0)
            return;
        await Task.WhenAll(tasks);
    }

    public async Task AttackAnimation(Character target)
    {
        if (target == null || IsDummyTarget(this, target))
            return;

        AttackEffect attack = AttackScene.Instantiate() as AttackEffect;
        OwnerCharater.AddChild(attack);
        var effect = OwnerCharater.CharacterEffectScene.Instantiate() as CharacterEffect;
        OwnerCharater.AddChild(effect);
        effect.Animation.Play("explode");
        await effect.ToSignal(effect.Animation, "animation_finished");
        attack.AnimationPlayer0.Play("Attack1");
        attack.GlobalPosition = target.GlobalPosition;
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
        await target.Skills[skillIndex].Effect();
    }

    public static Skill GetSkill(SkillID skillID)
    {
        return skillID switch
        {
            SkillID.Determination => new Determination(),
            SkillID.ReNewedSpirit => new ReNewedSpirit(),
            SkillID.TerminateLight => new TerminateLight(),
            SkillID.Smite => new Smite(),
            SkillID.Charge => new Charge(),
            SkillID.VulnerablePurge => new VulnerablePurge(),
            SkillID.VulnerabilityStrike => new VulnerabilityStrike(),
            SkillID.DeSurviveSkill => new ShockWave(),
            SkillID.SacredOnslaught => new SacredOnslaught(),
            SkillID.ResonantSlash => new ResonantSlash(),
            SkillID.EchoPuncture => new EchoPuncture(),
            SkillID.BreakStrike => new BreakStrike(),
            SkillID.EchonicResonance => new EchonicResonance(),
            SkillID.SonicBoom => new SonicBoom(),
            SkillID.PhaseEcho => new PhaseEcho(),
            SkillID.SoundBarrier => new SoundBarrier(),
            SkillID.SonicDeflection => new SonicDeflection(),
            SkillID.TuningStance => new TuningStance(),
            SkillID.ResonantWard => new ResonantWard(),
            SkillID.DissonantField => new DissonantField(),
            SkillID.ReverbChain => new ReverbChain(),
            SkillID.RelayShift => new RelayShift(),
            SkillID.EvilAttack => new EvilAttack(),
            SkillID.EvilSurvive => new EvilSurvive(),
            SkillID.EvilTermin => new EvilTermin(),
            SkillID.ShockWave => new ShockWave(),
            SkillID.AbsouluteDefense => new AbsouluteDefense(),
            SkillID.TauntingGuard => new TauntingGuard(),
            SkillID.WeakpointBulwark => new WeakpointBulwark(),
            SkillID.BarrierDuplication => new BarrierDuplication(),
            SkillID.HolySeal => new HolySeal(),
            SkillID.AegisPledge => new AegisPledge(),
            SkillID.VulnerabilityConversion => new VulnerabilityConversion(),
            SkillID.FearWormAttack => new FearWormAttack(),
            SkillID.FearWormSurvive => new FearWormSurvive(),
            SkillID.FearWormTermin => new FearWormTermin(),
            SkillID.MendSlash => new MendSlash(),
            SkillID.SiphonSlash => new SiphonSlash(),
            SkillID.SwapSlash => new SwapSlash(),
            SkillID.ShatterSlash => new ShatterSlash(),
            SkillID.FinalGuard => new FinalGuard(),
            SkillID.RebirthPrayer => new RebirthPrayer(),
            SkillID.Sacrifice => new Sacrifice(),
            SkillID.RearlineRevival => new RearlineRevival(),
            SkillID.GroupHealing => new GroupHealing(),
            SkillID.ShadowAmbush => new ShadowAmbush(),
            SkillID.ShadowExecution => new ShadowExecution(),
            SkillID.StasisBlade => new StasisBlade(),
            SkillID.ContinuousPierce => new ContinuousPierce(),
            SkillID.VeilStep => new VeilStep(),
            SkillID.TempoSurge => new TempoSurge(),
            SkillID.LongNight => new LongNight(),
            SkillID.RequiemBloom => new RequiemBloom(),
            SkillID.Vower => new Vower(),
            SkillID.FlashOfLight => new FlashOfLight(),
            SkillID.CrystalGuard => new CrystalGuard(),
            SkillID.QuietVeil => new QuietVeil(),
            SkillID.EnergyTransfer => new EnergyTransfer(),
            SkillID.EnergyRelay => new EnergyRelay(),
            SkillID.Swift => new Swift(),
            SkillID.AfterimageWard => new AfterimageWard(),
            SkillID.StarWard => new StarWard(),
            SkillID.TwilightParadox => new TwilightParadox(),
            SkillID.ArmonAttack => new ArmonAttack(),
            SkillID.ArmonSurvive => new ArmonSurvive(),
            SkillID.ArmonSpecial => new ArmonSpecial(),
            SkillID.ArroganceAttack => new ArroganceAttack(),
            SkillID.ArroganceSurvive => new ArroganceSurvive(),
            SkillID.ArroganceSpecial => new ArroganceSpecial(),
            SkillID.AlienBodyAttack => new AlienBodyAttack(),
            SkillID.AlienBodySurvive => new AlienBodySurvive(),
            SkillID.AlienBodySpecial => new AlienBodySpecial(),
            SkillID.RedHuskAttack => new RedHuskAttack(),
            SkillID.RedHuskSurvive => new RedHuskSurvive(),
            SkillID.RedHuskSpecial => new RedHuskSpecial(),
            SkillID.WarAttack => new WarAttack(),
            SkillID.WarSurvive => new WarSurvive(),
            SkillID.WarSpecial => new WarSpecial(),
            SkillID.WarThrallAttack => new WarThrallAttack(),
            SkillID.FerociouessAttack => new FerociouessAttack(),
            SkillID.FerociouessSurvive => new FerociouessSurvive(),
            SkillID.FerociouessSpecial => new FerociouessSpecial(),
            SkillID.TurbineAttack => new TurbineAttack(),
            SkillID.TurbineSurvive => new TurbineSurvive(),
            SkillID.TurbineSpecial => new TurbineSpecial(),
            SkillID.BlackHawkAttack => new BlackHawkAttack(),
            SkillID.BlackHawkSurvive => new BlackHawkSurvive(),
            SkillID.BlackHawkSpecial => new BlackHawkSpecial(),
            SkillID.BasicAttack => new BasicAttack(),
            SkillID.BasicDefense => new BasicDefense(),
            SkillID.BasicSpecial => new BasicSpecial(),
            _ => null,
        };
    }
}

public enum SkillID
{
    #region Player Characters

    #region Echo
    [PlayerSkill(PlayerCharacterKey.Echo)]
    SacredOnslaught = 5,

    [PlayerSkill(PlayerCharacterKey.Echo)]
    ResonantSlash = 6,

    [PlayerSkill(PlayerCharacterKey.Echo)]
    EchoPuncture = 7,

    [PlayerSkill(PlayerCharacterKey.Nightingale)]
    BreakStrike = 8,

    [PlayerSkill(PlayerCharacterKey.Echo)]
    EchonicResonance = 9,

    [PlayerSkill(PlayerCharacterKey.Echo)]
    SonicBoom = 10,

    [PlayerSkill(PlayerCharacterKey.Echo)]
    PhaseEcho = 11,

    [PlayerSkill(PlayerCharacterKey.Echo)]
    SoundBarrier = 12,

    [PlayerSkill(PlayerCharacterKey.Echo)]
    SonicDeflection = 13,

    [PlayerSkill(PlayerCharacterKey.Echo)]
    TuningStance = 14,

    [PlayerSkill(PlayerCharacterKey.Echo)]
    ResonantWard = 15,

    [PlayerSkill(PlayerCharacterKey.Echo)]
    DissonantField = 74,

    [PlayerSkill(PlayerCharacterKey.Echo)]
    ReverbChain = 75,

    [PlayerSkill(PlayerCharacterKey.Echo)]
    RelayShift = 86,
    #endregion

    #region Kasiya
    [PlayerSkill(PlayerCharacterKey.Kasiya)]
    Determination = 0,

    [PlayerSkill(PlayerCharacterKey.Kasiya)]
    ReNewedSpirit = 1,

    [PlayerSkill(PlayerCharacterKey.Kasiya)]
    TerminateLight = 2,

    [PlayerSkill(PlayerCharacterKey.Kasiya)]
    Smite = 3,

    [PlayerSkill(PlayerCharacterKey.Kasiya)]
    Charge = 4,

    [PlayerSkill(PlayerCharacterKey.Kasiya)]
    VulnerablePurge = 76,

    [PlayerSkill(PlayerCharacterKey.Kasiya)]
    VulnerabilityStrike = 77,

    DeSurviveSkill = 19,

    [PlayerSkill(PlayerCharacterKey.Kasiya)]
    ShockWave = 20,

    [PlayerSkill(PlayerCharacterKey.Kasiya)]
    AbsouluteDefense = 21,

    [PlayerSkill(PlayerCharacterKey.Kasiya)]
    TauntingGuard = 22,

    [PlayerSkill(PlayerCharacterKey.Kasiya)]
    WeakpointBulwark = 82,

    [PlayerSkill(PlayerCharacterKey.Kasiya)]
    BarrierDuplication = 85,

    [PlayerSkill(PlayerCharacterKey.Kasiya)]
    HolySeal = 23,

    [PlayerSkill(PlayerCharacterKey.Kasiya)]
    Vower = 36,

    [PlayerSkill(PlayerCharacterKey.Kasiya)]
    AegisPledge = 70,

    [PlayerSkill(PlayerCharacterKey.Kasiya)]
    VulnerabilityConversion = 78,
    #endregion

    #region Mariya
    [PlayerSkill(PlayerCharacterKey.Mariya)]
    MendSlash = 27,

    [PlayerSkill(PlayerCharacterKey.Mariya)]
    SiphonSlash = 59,

    [PlayerSkill(PlayerCharacterKey.Mariya)]
    ShatterSlash = 60,

    [PlayerSkill(PlayerCharacterKey.Mariya)]
    FinalGuard = 28,

    [PlayerSkill(PlayerCharacterKey.Mariya)]
    RebirthPrayer = 29,

    [PlayerSkill(PlayerCharacterKey.Mariya)]
    Sacrifice = 34,

    [PlayerSkill(PlayerCharacterKey.Mariya)]
    CrystalGuard = 38,

    [PlayerSkill(PlayerCharacterKey.Mariya)]
    SwapSlash = 53,

    [PlayerSkill(PlayerCharacterKey.Mariya)]
    QuietVeil = 58,

    [PlayerSkill(PlayerCharacterKey.Mariya)]
    EnergyTransfer = 61,

    [PlayerSkill(PlayerCharacterKey.Mariya)]
    RearlineRevival = 80,

    [PlayerSkill(PlayerCharacterKey.Mariya)]
    EnergyRelay = 81,

    [PlayerSkill(PlayerCharacterKey.Mariya)]
    GroupHealing = 83,
    #endregion

    #region Nightingale
    [PlayerSkill(PlayerCharacterKey.Nightingale)]
    ShadowAmbush = 30,

    [PlayerSkill(PlayerCharacterKey.Nightingale)]
    ShadowExecution = 31,

    [PlayerSkill(PlayerCharacterKey.Nightingale)]
    StasisBlade = 65,

    [PlayerSkill(PlayerCharacterKey.Nightingale)]
    ContinuousPierce = 87,

    [PlayerSkill(PlayerCharacterKey.Nightingale)]
    VeilStep = 32,

    [PlayerSkill(PlayerCharacterKey.Nightingale)]
    TempoSurge = 33,

    [PlayerSkill(PlayerCharacterKey.Nightingale)]
    LongNight = 35,

    [PlayerSkill(PlayerCharacterKey.Nightingale)]
    RequiemBloom = 66,

    [PlayerSkill(PlayerCharacterKey.Nightingale)]
    FlashOfLight = 37,

    [PlayerSkill(PlayerCharacterKey.Nightingale)]
    Swift = 39,

    [PlayerSkill(PlayerCharacterKey.Nightingale)]
    AfterimageWard = 84,

    [PlayerSkill(PlayerCharacterKey.Nightingale)]
    StarWard = 40,

    [PlayerSkill(PlayerCharacterKey.Nightingale)]
    TwilightParadox = 79,
    #endregion

    #endregion

    #region Enemies

    #region Evil
    EvilAttack = 16,
    EvilSurvive = 17,
    EvilTermin = 18,
    #endregion

    #region FearWorm
    FearWormAttack = 24,
    FearWormSurvive = 25,
    FearWormTermin = 26,
    #endregion

    #region Armon
    ArmonAttack = 41,
    ArmonSurvive = 42,
    ArmonSpecial = 43,
    #endregion

    #region Arrogance
    ArroganceAttack = 44,
    ArroganceSurvive = 45,
    ArroganceSpecial = 46,
    #endregion

    #region AlienBody
    AlienBodyAttack = 47,
    AlienBodySurvive = 48,
    AlienBodySpecial = 49,
    #endregion

    #region RedHusk
    RedHuskAttack = 50,
    RedHuskSurvive = 51,
    RedHuskSpecial = 52,
    #endregion

    #region War
    WarAttack = 54,
    WarSurvive = 55,
    WarSpecial = 56,
    WarThrallAttack = 57,
    #endregion

    #region Ferociouess
    FerociouessAttack = 62,
    FerociouessSurvive = 63,
    FerociouessSpecial = 64,
    #endregion

    #region Turbine
    TurbineAttack = 67,
    TurbineSurvive = 68,
    TurbineSpecial = 69,
    #endregion

    #region BlackHawk
    BlackHawkAttack = 88,
    BlackHawkSurvive = 89,
    BlackHawkSpecial = 90,
    #endregion

    #region Basis
    BasicAttack = 71,
    BasicDefense = 72,
    BasicSpecial = 73,
    #endregion

    #endregion
}
