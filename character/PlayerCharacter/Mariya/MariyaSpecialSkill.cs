public partial class MariyaSpecialSkill { }

public partial class EnergyTransfer : Skill
{
    public override SkillRarity Rarity => SkillRarity.Uncommon;
    private const int AllyEnergyGain = 3;
    public override int EnergyCost => 1;

    public EnergyTransfer()
        : base(SkillTypes.Special)
    {
        UpdateDescription();
    }

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

    public RearlineRevival()
        : base(SkillTypes.Special)
    {
        UpdateDescription();
    }

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
            ApplyBuffFriendly(Buff.BuffName.RebirthI, 2, TargetReference.HealKey)
        );
    }
}

public partial class GroupHealing : Skill
{
    public override SkillRarity Rarity => SkillRarity.Uncommon;
    public override bool ExhaustsAfterUse => true;

    private const int BaseHeal = 8;

    public GroupHealing()
        : base(SkillTypes.Special)
    {
        UpdateDescription();
    }

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
    private const int DivinityStacks = 2;
    public override bool ExhaustsAfterUse => true;

    public Ragnarok()
        : base(SkillTypes.Special)
    {
        UpdateDescription();
    }

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

public partial class HolyOfHolies : Skill
{
    public override SkillRarity Rarity => SkillRarity.Uncommon;
    private const int EnergySourcesGain = 1;

    public HolyOfHolies()
        : base(SkillTypes.Special)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "至圣";
    public override int EnergyCost => 2;
    public override bool ExhaustsAfterUse => true;

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            ModifyPropertyStep(PropertyType.EnergySources, EnergySourcesGain)
        );
    }
}

public partial class SanctuaryForm : Skill
{
    public override SkillRarity Rarity => SkillRarity.Rare;
    private const int SanctuaryStacks = 1;

    public SanctuaryForm()
        : base(SkillTypes.Special)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "圣域形态";
    public override int EnergyCost => 3;
    public override bool ExhaustsAfterUse => true;

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            ApplyBuffFriendly(
                buffName: Buff.BuffName.Sanctuary,
                stacks: SanctuaryStacks,
                target: TargetReference.All
            )
        );
    }
}
