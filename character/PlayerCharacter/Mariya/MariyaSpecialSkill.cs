public partial class MariyaSpecialSkill { }

public partial class QuietVeil : Skill
{
    public override SkillRarity Rarity => SkillRarity.Uncommon;
    private int InvisibleStacks => V(nameof(InvisibleStacks));
    private int SurvivabilityGain => V(nameof(SurvivabilityGain));

    public override SkillTypes SkillType => SkillTypes.Special;

    public override string SkillName { get; set; } = "静影庇护";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            ApplyBuffFriendly(
                buffName: Buff.BuffName.Invisible,
                stacks: InvisibleStacks,
                target: TargetReference.Self
            ),
            ModifyPropertyStep(PropertyType.Survivability, SurvivabilityGain)
        );
    }
}

public partial class TouchOfGod : Skill
{
    public override SkillRarity Rarity => SkillRarity.Uncommon;

    private int DivinityStacks => V(nameof(DivinityStacks));

    public override SkillTypes SkillType => SkillTypes.Special;

    public override string SkillName { get; set; } = "上帝之触";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            ApplyBuffFriendly(
                buffName: Buff.BuffName.Divinity,
                stacks: DivinityStacks,
                target: TargetReference.ManualFriendly
            )
        );
    }
}

public partial class EnergyTransfer : Skill
{
    public override SkillRarity Rarity => SkillRarity.Uncommon;
    private int AllyEnergyGain => V(nameof(AllyEnergyGain));

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
    private int BaseRebirthHeal => V(nameof(BaseRebirthHeal));

    public override SkillTypes SkillType => SkillTypes.Special;

    public override string SkillName { get; set; } = "死者苏生";

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
                V("RebirthIStacks"),
                TargetReference.HealKey
            )
        );
    }
}

public partial class GroupHealing : Skill
{
    public override SkillRarity Rarity => SkillRarity.Uncommon;

    private int BaseHeal => V(nameof(BaseHeal));

    public override SkillTypes SkillType => SkillTypes.Special;

    public override string SkillName { get; set; } = "圣光沐浴";

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
    private int PowerGain => V(nameof(PowerGain));
    private int DivinityStacks => V(nameof(DivinityStacks));

    public override SkillTypes SkillType => SkillTypes.Special;

    public override string SkillName { get; set; } = "诸神黄昏";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            ModifyPropertyStep(PropertyType.Power, PowerGain),
            ApplyBuffFriendly(
                buffName: Buff.BuffName.Divinity,
                stacks: DivinityStacks,
                target: TargetReference.All
            )
        );
    }
}

public partial class SacredLink : Skill
{
    public override SkillRarity Rarity => SkillRarity.Rare;
    public override SkillTypes SkillType => SkillTypes.Special;

    public override string SkillName { get; set; } = I18n.Tr("skill.sacred_link.name", "神圣号令");

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(this, PlayDrawPileCardStep(SkillTypes.none));
    }
}
