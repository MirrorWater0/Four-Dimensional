public abstract partial class ColorlessSkill : Skill
{
    public override bool IsColorless => true;
}

public partial class Blade : ColorlessSkill
{
    public override SkillTypes SkillType => SkillTypes.Attack;

    public override string SkillName { get; set; } = "利刃";
    public override int EnergyCost => 0;
    public override bool ExhaustsAfterUse => true;

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(this, AttackStep(baseDamage: 0, multiplier: 1));
    }
}

public partial class Calmness : ColorlessSkill
{
    public override SkillTypes SkillType => SkillTypes.Special;

    public override string SkillName { get; set; } = "镇静";
    public override int EnergyCost => 0;
    public override bool ExhaustsAfterUse => true;

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(this, DrawCardsStep(2));
    }
}

public partial class DefenceFocus : ColorlessSkill
{
    public override SkillTypes SkillType => SkillTypes.Survive;

    public override string SkillName { get; set; } = "防御专注";
    public override int EnergyCost => 0;
    public override bool ExhaustsAfterUse => true;

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(this, BlockStep(0));
    }
}