using System;
using System.Threading.Tasks;
using Godot;

public partial class Anger : EnemyCharacter
{
    private const int PassivePowerGain = 2;
    private Action<Skill> _skillUsedHandler;

    public const string PassiveNameText = "愤怒";
    public static string PassiveDescriptionText =>
        $"敌方打出非攻击牌时：敌阵回合结束时获得{PassivePowerGain}点力量。";

    public override string CharacterName { get; set; } = "怒镰兽";

    public override void Initialize()
    {
        base.Initialize();
        PassiveName = PassiveNameText;
        PassiveDescription = PassiveDescriptionText;
        _skillUsedHandler ??= skill => TriggerPassive(skill);
        BattleNode.UsedSkills.ItemAdded -= _skillUsedHandler;
        BattleNode.UsedSkills.ItemAdded += _skillUsedHandler;
    }

    public override void _ExitTree()
    {
        if (BattleNode != null && _skillUsedHandler != null)
            BattleNode.UsedSkills.ItemAdded -= _skillUsedHandler;
        base._ExitTree();
    }

    public override async void Passive(Skill skill)
    {
        if (
            State == CharacterState.Dying
            || skill?.OwnerCharater == null
            || skill.OwnerCharater.BattleNode != BattleNode
            || skill.OwnerCharater.IsPlayer == IsPlayer
            || skill.SkillType == Skill.SkillTypes.Attack
            || skill.SkillType == Skill.SkillTypes.none
            || skill.IsStatusCard
        )
        {
            return;
        }

        if (BattleNode?.QueueEnemyPhaseEndPowerGain(this, PassivePowerGain, this, "被动") == true)
            return;

        using var _ = BeginEffectSource("被动");
        await IncreaseProperties(PropertyType.Power, PassivePowerGain, this);
    }
}

public partial class AngerEliteRegedit : EnemyRegedit
{
    public AngerEliteRegedit()
    {
        CharacterName = "愤怒";
        PType = EnemyPositionType.FrontRow;
        PortaitPath = "res://asset/EnemyCharater/AngerElite.png";
        CharacterScene = GD.Load<PackedScene>("res://character/EnemyCharacter/AngerElite.tscn");

        MaxLife = 118;
        Power = 0;
        Survivability = 0;
        BasePowerContribution = 0;
        BaseSurvivabilityContribution = 0;
        HasAttackVulnerableIntention = true;
        SkillIDs = [SkillID.AngerEliteAttack, SkillID.AngerEliteSpecial];
        OpeningIntentionSkillIDs = [SkillID.AngerEliteSurvive];
        PassiveName = global::Anger.PassiveNameText;
        PassiveDescription = global::Anger.PassiveDescriptionText;
    }
}

public partial class AngerEliteAttack : Skill
{
    private const int BaseDamage = 18;

    public AngerEliteAttack()
        : base(SkillTypes.Attack)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "裂镰";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            AttackStep(baseDamage: BaseDamage),
            ApplyBuffHostile(Buff.BuffName.Vulnerable, 9, HostileTargetReference.AttackKey)
        );
    }
}

public partial class AngerEliteSurvive : Skill
{
    private const int BaseBlock = 15;
    private const int SelfSurvivabilityGain = 4;

    public AngerEliteSurvive()
        : base(SkillTypes.Survive)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "伏身";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(this, BlockStep(baseBlock: BaseBlock));
    }
}

public partial class AngerEliteSpecial : Skill
{
    private const int BaseDamage = 8;
    private const int HitCount = 2;

    public AngerEliteSpecial()
        : base(SkillTypes.Special)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "狂怒弧刃";
    public override int EnergyCost => 4;

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(this, AttackStep(baseDamage: BaseDamage, times: HitCount));
    }
}
