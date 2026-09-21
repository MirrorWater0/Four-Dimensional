using System.Threading.Tasks;
using Godot;

public partial class HollowBulwark : EnemyCharacter
{
    private const int StartBarricadeStacks = 1;
    private const int StartBlock = 100;

    public const string PassiveNameText = "空壳壁障";
    public static string PassiveDescriptionText =>
        $"战斗开始时: 获得{StartBarricadeStacks}层{Buff.BuffName.Barricade.GetDescription()}，获得{StartBlock}点格挡.";

    public override string CharacterName { get; set; } = "空壳壁卫";

    public override void Initialize()
    {
        base.Initialize();
        PassiveName = PassiveNameText;
        PassiveDescription = PassiveDescriptionText;
        BattleNode?.StartEffectList.Add(StartPassive);
    }

    public Task StartPassive()
    {
        using var _ = BeginEffectSource("被动");
        StartActionBuff.BuffAdd(Buff.BuffName.Barricade, this, StartBarricadeStacks, this);
        UpdataBlock(StartBlock, source: this);
        return Task.CompletedTask;
    }
}

public partial class HollowBulwarkRegedit : EnemyRegedit
{
    public HollowBulwarkRegedit()
    {
        CharacterName = "空壳壁卫";
        PType = EnemyPositionType.FrontRow;
        PortaitPath = "res://asset/EnemyCharater/HollowBulwark.png";
        CharacterScene = GD.Load<PackedScene>("res://character/EnemyCharacter/HollowBulwark.tscn");

        MaxLife = 50;
        Power = 0;
        Survivability = 0;
        BaseSurvivabilityContribution = 0;
        SkillIDs =
        [
            SkillID.HollowBulwarkAttack,
            SkillID.HollowBulwarkSurvive,
            SkillID.HollowBulwarkSpecial,
        ];
        OpeningIntentionSkillIDs = [SkillID.HollowBulwarkSpecial];
        PassiveName = global::HollowBulwark.PassiveNameText;
        PassiveDescription = global::HollowBulwark.PassiveDescriptionText;
    }
}

public partial class HollowBulwarkAttack : Skill
{
    public override SkillTypes SkillType => SkillTypes.Attack;

    public override string SkillName { get; set; } = "壁刃冲击";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(this, AttackStep(V("BaseDamage", 24)));
    }
}

public partial class HollowBulwarkSurvive : Skill
{
    private const int BaseBlock = 30;

    public override SkillTypes SkillType => SkillTypes.Survive;

    public override string SkillName { get; set; } = "空壳";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(this, BlockStep(baseBlock: BaseBlock, multiplier: V("Multiplier", 1)));
    }
}

public partial class HollowBulwarkSpecial : Skill
{
    public override SkillTypes SkillType => SkillTypes.Special;

    public override string SkillName { get; set; } = "闭合";
    public override int EnemySpecialIntentionCooldown => 2;

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            AttackStep(V("BaseDamage", 16)),
            ModifyPropertyStep(PropertyType.Survivability, V("SurvivabilityGain", 10)),
            CustomStep(
                _ =>
                {
                    int currentBlock = OwnerCharater?.Block ?? 0;
                    if (currentBlock > 0)
                        OwnerCharater?.UpdataBlock(currentBlock, source: OwnerCharater);

                    return Task.CompletedTask;
                },
                _ => ["格挡翻倍."]
            )
        );
    }
}
