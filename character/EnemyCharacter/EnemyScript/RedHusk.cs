using System;
using System.Threading.Tasks;
using Godot;

public partial class RedHusk : EnemyCharacter
{
    private static int StartAutoArmorStacks => EnemyTuning.GetRequiredInt("RedHusk", nameof(StartAutoArmorStacks));

    public const string PassiveNameText = "赤壳护盾";
    public static string PassiveDescriptionText =>
        $"战斗开始时：获得{StartAutoArmorStacks}层{Buff.BuffName.AutoArmor.GetDescription()}。";

    public override void Initialize()
    {
        base.Initialize();
        PassiveName = PassiveNameText;
        PassiveDescription = PassiveDescriptionText;
        BattleNode.StartEffectList.Add(StartPassive);
    }

    public Task StartPassive()
    {
        using var _ = BeginEffectSource("被动");
        HurtBuff.BuffAdd(Buff.BuffName.AutoArmor, this, StartAutoArmorStacks, this);
        return Task.CompletedTask;
    }
}

public partial class RedHuskRegedit : EnemyRegedit
{
    public RedHuskRegedit()
    {
        CharacterName = "RedHusk";
        PortaitPath = "res://asset/EnemyCharater/RedHusk.png";
        CharacterScene = GD.Load<PackedScene>("res://character/EnemyCharacter/RedHusk.tscn");
        SkillIDs = [SkillID.RedHuskAttack];
        // 第四轮聚合压缩随机施加脆弱，第五轮全阵突刺兑现压力。
        IntentionCycle = [SkillID.RedHuskAttack, random, SkillID.RedHuskAttack2, SkillID.RedHuskAttack];
        RandomizeIntentionCycleStart = true;
        IntentionCycleInsertions = [(SkillID.RedHuskSpecial,1),(SkillID.RedHuskSurvive,6)];
        PassiveName = global::RedHusk.PassiveNameText;
        PassiveDescription = global::RedHusk.PassiveDescriptionText;
    }
}

public partial class RedHuskAttack : Skill
{
    private int BaseDamage => V(nameof(BaseDamage));

    public override SkillTypes SkillType => SkillTypes.Attack;

    public override string SkillName { get; set; } = "重拳突刺";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            AttackStep(
                baseDamage: BaseDamage,
                multiplier: V("Multiplier"),
                target: HostileTargetReference.All,
                times: V("HitCount")
            )
        );
    }
}

public partial class RedHuskSurvive : Skill
{

    public override SkillTypes SkillType => SkillTypes.Survive;

    public override string SkillName { get; set; } = "聚合压缩";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            BlockStep(10),
            ModifyPropertyStep(PropertyType.Power, V("PowerGain"))
        );
    }
}
public partial class RedHuskAttack2 : Skill
{

    public override SkillTypes SkillType => SkillTypes.Attack;

    public override string SkillName { get; set; } = "轰鸣";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            AttackStep(baseDamage: V("BaseDamage"), multiplier: V("Multiplier"), times: V("HitCount")),
            ModifyPropertyStep(PropertyType.Power, V("PowerGain")),
            ApplyBuffHostile(Buff.BuffName.Frail, V("FrailStacks"), HostileTargetReference.AttackKey)
        );
    }
}
public partial class RedHuskSpecial : Skill
{
    private int AutoArmorStacks => V(nameof(AutoArmorStacks));

    public override SkillTypes SkillType => SkillTypes.Special;

    public override string SkillName { get; set; } = "护壳重生";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            ApplyBuffFriendly(
                buffName: Buff.BuffName.AutoArmor,
                stacks: AutoArmorStacks,
                target: TargetReference.Self
            )
        );
    }
}
