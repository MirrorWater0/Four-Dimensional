using System.Threading.Tasks;
using Godot;

public partial class Ferociouess : EnemyCharacter
{
    private static int StartDamageImmuneStacks => EnemyTuning.GetRequiredInt("Ferociouess", nameof(StartDamageImmuneStacks));
    private static int TurnEndDamageImmuneStacks => EnemyTuning.GetRequiredInt("Ferociouess", nameof(TurnEndDamageImmuneStacks));

    public const string PassiveNameText = "骨骼硬化";
    public static string PassiveDescriptionText =>
        $"战斗开始时：获得{StartDamageImmuneStacks}层{Buff.BuffName.DamageImmune.GetDescription()}。\n"
        + $"阵营回合结束时：获得{TurnEndDamageImmuneStacks}层{Buff.BuffName.DamageImmune.GetDescription()}。";

    public override string CharacterName { get; set; } = "Ferociouess";

    public override void Initialize()
    {
        base.Initialize();
        PassiveName = PassiveNameText;
        PassiveDescription = PassiveDescriptionText;
        BattleNode.StartEffectList.Add(StartPassive);
    }

    private Task StartPassive()
    {
        using var _ = BeginEffectSource("被动");
        HurtBuff.BuffAdd(Buff.BuffName.DamageImmune, this, StartDamageImmuneStacks, this);
        return Task.CompletedTask;
    }

    public override void OnTurnEnd()
    {
        using var _ = BeginEffectSource("被动");
        HurtBuff.BuffAdd(Buff.BuffName.DamageImmune, this, TurnEndDamageImmuneStacks, this);
        base.OnTurnEnd();
    }
}

public partial class FerociouessRegedit : EnemyRegedit
{
    public FerociouessRegedit()
    {
        CharacterName = "Ferociouess";
        PortaitPath = "res://asset/EnemyCharater/Ferociouess.png";
        CharacterScene = GD.Load<PackedScene>("res://character/EnemyCharacter/Ferociouess.tscn");
        SkillIDs = [SkillID.FerociouessAttack, SkillID.FerociouessSurvive];
        // 冲击后免疫并施加易伤，下一步狂暴兑现压力。
        IntentionCycle = [SkillID.FerociouessAttack, SkillID.FerociouessSurvive, SkillID.FerociouessSpecial, random];
        PassiveName = global::Ferociouess.PassiveNameText;
        PassiveDescription = global::Ferociouess.PassiveDescriptionText;
    }
}

public partial class FerociouessAttack : Skill
{
    private int BaseDamage => V(nameof(BaseDamage));
    private int SelfPowerGain => V(nameof(SelfPowerGain));

    public override SkillTypes SkillType => SkillTypes.Attack;

    public override string SkillName { get; set; } = "凶恶冲击";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            AttackStep(baseDamage: BaseDamage, target: HostileTargetReference.All, multiplier: V("Multiplier"), times: V("HitCount")),
            ModifyPropertyStep(PropertyType.Power, SelfPowerGain)
        );
    }
}

public partial class FerociouessSurvive : Skill
{
    private int BaseBlock => V(nameof(BaseBlock));
    private int DamageImmuneStacks => V(nameof(DamageImmuneStacks));
    private int VulnerableStacks => V(nameof(VulnerableStacks));

    public override SkillTypes SkillType => SkillTypes.Survive;

    public override string SkillName { get; set; } = "骨骼免疫";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            BlockStep(baseBlock: BaseBlock, multiplier: V("Multiplier")),
            ApplyBuffFriendly(
                buffName: Buff.BuffName.DamageImmune,
                stacks: DamageImmuneStacks,
                target: TargetReference.Self
            ),
            ApplyBuffHostile(
                buffName: Buff.BuffName.Vulnerable,
                stacks: VulnerableStacks,
                target: HostileTargetReference.All
            )
        );
    }
}

public partial class FerociouessSpecial : Skill
{
    private int HitCount => V(nameof(HitCount));

    public override SkillTypes SkillType => SkillTypes.Special;

    public override string SkillName { get; set; } = "狂暴";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            AttackStep(baseDamage: V("BaseDamage"), times: HitCount, target: HostileTargetReference.All, multiplier: V("Multiplier"))
        );
    }
}
