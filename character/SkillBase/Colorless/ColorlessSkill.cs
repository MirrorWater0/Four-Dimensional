public abstract partial class ColorlessSkill : Skill
{
    public override bool IsActionCard => false;
    public override bool IsColorless => true;
}

public partial class Blade : ColorlessSkill
{
    public override SkillTypes SkillType => SkillTypes.Attack;

    public override string SkillName { get; set; } = "利刃";

    public override bool HasImmediateKeyword => true;

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(this, AttackStep(baseDamage: V("BaseDamage"), multiplier: V("Multiplier"), times: V("HitCount")));
    }
}

public partial class Calmness : ColorlessSkill
{
    public override SkillTypes SkillType => SkillTypes.Special;

    public override string SkillName { get; set; } = "镇静";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(this, DrawCardsStep(V("DrawCount")));
    }
}

public partial class DefenceFocus : ColorlessSkill
{
    public override SkillTypes SkillType => SkillTypes.Permanent;

    public override string SkillName { get; set; } = "防御专注";

    public override bool HasImmediateKeyword => true;

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(this, BlockStep(V("BaseBlock"), multiplier: V("Multiplier")));
    }
}
