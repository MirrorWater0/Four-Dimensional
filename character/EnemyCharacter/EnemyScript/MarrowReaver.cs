using System;
using System.Threading.Tasks;
using Godot;

public partial class MarrowReaver : EnemyCharacter
{
    private static int PassiveMaxLifeDown => EnemyTuning.GetRequiredInt("MarrowReaver", nameof(PassiveMaxLifeDown));

    public const string PassiveNameText = "蚀髓";
    public static string PassiveDescriptionText =>
        $"每次造成未被格挡的伤害时: 永久降低目标{PassiveMaxLifeDown}点生命上限.";

    public override string CharacterName { get; set; } = "蚀髓刃虫";

    public override void Initialize()
    {
        base.Initialize();
        PassiveName = PassiveNameText;
        PassiveDescription = PassiveDescriptionText;
    }

    public override void OnDealUnblockedDamage(
        Character target,
        int actualDamage,
        DamageKind damageKind
    )
    {
        if (target == null || actualDamage <= 0 || target == this)
            return;

        BattleNode?.QueueBattleEffect(() => ApplyUnblockedDamagePassiveAsync(target), "蚀髓");
    }

    private async Task ApplyUnblockedDamagePassiveAsync(Character target) {
        if (!GodotObject.IsInstanceValid(this) || !GodotObject.IsInstanceValid(target))
            return;
        using var _ = BeginEffectSource("被动");
        int loss = Math.Min(PassiveMaxLifeDown, Math.Max(0, target.BattleMaxLife - 1));
        if (loss <= 0)
            return;

        await target.DescendingMaxLifeFromPassive(loss, this);
        ApplyPermanentMaxLifeLoss(target, loss);
    }

    private static void ApplyPermanentMaxLifeLoss(Character target, int loss)
    {
        if (loss <= 0 || target is not PlayerCharacter player)
            return;

        if (GameInfo.PlayerCharacters == null || GameInfo.PlayerCharacters.Length == 0)
            return;

        int index = player.CharacterIndex;
        if (index < 0 || index >= GameInfo.PlayerCharacters.Length)
            return;

        PlayerInfoStructure info = GameInfo.PlayerCharacters[index];
        info.LifeMax = Math.Max(1, info.LifeMax - loss);
        info.Life = Math.Clamp(info.Life, 0, info.LifeMax);
        info.LifeInitialized = true;
        GameInfo.PlayerCharacters[index] = info;
    }
}

public partial class MarrowReaverRegedit : EnemyRegedit
{
    public MarrowReaverRegedit()
    {
        CharacterName = "蚀髓刃虫";
        PortaitPath = "res://asset/EnemyCharater/MarrowReaver.png";
        CharacterScene = GD.Load<PackedScene>("res://character/EnemyCharacter/MarrowReaver.tscn");
        SkillIDs = [SkillID.MarrowReaverAttack, SkillID.MarrowReaverSpecial];
        // 第四轮蓄力随机施加脆弱，第五轮全阵裂髓斩保证命中脆弱角色。
        IntentionCycle = [SkillID.MarrowReaverAttack, SkillID.MarrowReaverSpecial, random, SkillID.MarrowReaverSurvive, SkillID.MarrowReaverAttack];

        PassiveName = global::MarrowReaver.PassiveNameText;
        PassiveDescription = global::MarrowReaver.PassiveDescriptionText;
    }
}

public partial class MarrowReaverAttack : Skill
{
    private int BaseDamage => V(nameof(BaseDamage));

    public override SkillTypes SkillType => SkillTypes.Attack;

    public override string SkillName { get; set; } = "裂髓斩";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            AttackStep(baseDamage: BaseDamage, target: HostileTargetReference.All, multiplier: V("Multiplier"), times: V("HitCount"))
        );
    }
}

public partial class MarrowReaverSurvive : Skill
{
    private int BaseBlock => V(nameof(BaseBlock));
    private int PowerGain => V(nameof(PowerGain));

    public override SkillTypes SkillType => SkillTypes.Survive;

    public override string SkillName { get; set; } = "骨壳蓄力";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            BlockStep(baseBlock: BaseBlock, multiplier: V("Multiplier")),
            ModifyPropertyStep(PropertyType.Power, PowerGain),
            ApplyBuffHostile(Buff.BuffName.Frail, V("FrailStacks"), HostileTargetReference.RandomPreview)
        );
    }
}

public partial class MarrowReaverSpecial : Skill
{
    private int BaseDamage => V(nameof(BaseDamage));
    private int HitCount => V(nameof(HitCount));

    public override SkillTypes SkillType => SkillTypes.Attack;

    public override string SkillName { get; set; } = "双刃蚀髓";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            AttackStep(baseDamage: BaseDamage, times: HitCount, multiplier: V("Multiplier")),
            AddCardsStep(SkillID.WoundStatus, V("WoundCount"))
        );
    }
}
