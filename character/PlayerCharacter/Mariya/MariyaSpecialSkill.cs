public partial class MariyaSpecialSkill { }

public partial class EnergyTransfer : Skill
{
    public override SkillRarity Rarity => SkillRarity.Uncommon;
    private const int AllyEnergyGain = 3;
    public override int EnergyCost => 1;

    public override SkillTypes SkillType => SkillTypes.Special;

    public override string SkillName { get; set; } = "能量传输";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(this, EnergyStep(delta: AllyEnergyGain));
    }
}

public partial class RearlineRevival : Skill
{
    public override SkillRarity Rarity => SkillRarity.Rare;
    private const int BaseRebirthHeal = 4;

    public override SkillTypes SkillType => SkillTypes.Special;

    public override string SkillName { get; set; } = "死者苏生";
    public override int EnergyCost => 3;
    public override bool ExhaustsAfterUse => true;

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            HealStep(
                baseHeal: BaseRebirthHeal,
                target: TargetReference.ManualFriendly,
                preferNonFull: true
            ),
            ApplyBuffFriendly(
                Buff.BuffName.RebirthI,
                V("RebirthIStacks", 2),
                TargetReference.HealKey
            )
        );
    }
}

public partial class GroupHealing : Skill
{
    public override SkillRarity Rarity => SkillRarity.Uncommon;
    public override bool ExhaustsAfterUse => true;

    private const int BaseHeal = 8;

    public override SkillTypes SkillType => SkillTypes.Special;

    public override string SkillName { get; set; } = "圣光沐浴";
    public override int EnergyCost => 3;

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            HealStep(
                baseHeal: BaseHeal,
                target: TargetReference.All,
                preferNonFull: false,
                includeSummonsWhenAll: false
            )
        );
    }
}

public partial class Ragnarok : Skill
{
    public override SkillRarity Rarity => SkillRarity.Rare;
    private const int PowerGain = 4;
    private const int DivinityStacks = 3;

    public override SkillTypes SkillType => SkillTypes.Special;
    public override bool ExhaustsAfterUse => true;

    public override string SkillName { get; set; } = "诸神黄昏";
    public override int EnergyCost => 3;

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            ModifyPropertyStep(PropertyType.Power, PowerGain),
            ApplyBuffFriendly(
                buffName: Buff.BuffName.Divinity,
                stacks: DivinityStacks,
                target: TargetReference.Self
            )
        );
    }
}
