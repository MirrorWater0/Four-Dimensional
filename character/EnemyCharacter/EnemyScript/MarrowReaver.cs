using System;
using System.Threading.Tasks;
using Godot;

public partial class MarrowReaver : EnemyCharacter
{
    private const int PassiveMaxLifeDown = 2;

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

    public override async void OnDealUnblockedDamage(
        Character target,
        int actualDamage,
        DamageKind damageKind
    )
    {
        if (target == null || actualDamage <= 0 || target == this)
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
        PType = EnemyPositionType.FrontRow;
        PortaitPath = "res://asset/EnemyCharater/MarrowReaver.png";
        CharacterScene = GD.Load<PackedScene>("res://character/EnemyCharacter/MarrowReaver.tscn");

        MaxLife = 69;
        Power = 0;
        Survivability = 0;
        BasePowerContribution = 0;
        BaseSurvivabilityContribution = 0;
        SkillIDs = [SkillID.MarrowReaverAttack, SkillID.MarrowReaverSpecial];

        PassiveName = global::MarrowReaver.PassiveNameText;
        PassiveDescription = global::MarrowReaver.PassiveDescriptionText;
    }
}

public partial class MarrowReaverAttack : Skill
{
    private const int BaseDamage = 15;

    public MarrowReaverAttack()
        : base(SkillTypes.Attack)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "裂髓斩";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            AttackStep(baseDamage: BaseDamage, target: HostileTargetReference.All),
            ModifyPropertyStep(PropertyType.Survivability, 5)
        );
    }
}

public partial class MarrowReaverSurvive : Skill
{
    private const int BaseBlock = 32;
    private const int PowerGain = 5;

    public MarrowReaverSurvive()
        : base(SkillTypes.Survive)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "骨壳蓄力";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            BlockStep(baseBlock: BaseBlock, multiplier: 2),
            ModifyPropertyStep(PropertyType.Power, PowerGain)
        );
    }
}

public partial class MarrowReaverSpecial : Skill
{
    private const int BaseDamage = 12;
    private const int HitCount = 2;

    public MarrowReaverSpecial()
        : base(SkillTypes.Special)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "双刃蚀髓";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            AttackStep(baseDamage: BaseDamage, times: HitCount, target: HostileTargetReference.All)
        );
    }
}
