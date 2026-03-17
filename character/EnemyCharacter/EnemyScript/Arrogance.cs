using System.Threading.Tasks;
using Godot;

public partial class Arrogance : EnemyCharacter
{
    private const int StartStunStacks = 2;

    public override string CharacterName { get; set; } = "Arrogance";

    public override void Initialize()
    {
        base.Initialize();
        BattleNode.StartEffectList.Add(StartPassive);
    }

    public Task StartPassive()
    {
        SkillBuff.BuffAdd(Buff.BuffName.Stun, this, StartStunStacks);
        return Task.CompletedTask;
    }
}

public partial class ArroganceAttack : Skill
{
    private const int BaseDamage = 10;
    private const int MaxTargets = 2;

    public ArroganceAttack()
        : base(SkillTypes.Attack)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "双重压制";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            AoeDamageStep(baseDamage: BaseDamage, maxTargets: MaxTargets),
            LowerTargetPropertyStep(PropertyType.Survivability, 5)
        );
    }
}

public partial class ArroganceSurvive : Skill
{
    private const int BaseBlock = 17;
    private const int VulnerableStacks = 2;

    public ArroganceSurvive()
        : base(SkillTypes.Survive)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "傲慢壁垒";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            SelfBlockStep(baseBlock: BaseBlock),
            ApplyBuffAll(
                buffName: Buff.BuffName.Vulnerable,
                stacks: VulnerableStacks,
                targetCamp: BuffTargetCamp.Hostile
            )
        );
    }
}

public partial class ArroganceSpecial : Skill
{
    private const int PursuitStacks = 3;
    private const int PursuitEnergyCost = 3;

    public ArroganceSpecial()
        : base(SkillTypes.Special)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "虚无追击";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            HealFriendlyRelative(4),
            ModifyPropertyStep(PropertyType.Power, 3),
            ApplyBuffFriendly(
                buffName: Buff.BuffName.Pursuit,
                stacks: PursuitStacks,
                index: 0,
                dyingFilter: false,
                energyCost: PursuitEnergyCost
            )
        );
    }
}
