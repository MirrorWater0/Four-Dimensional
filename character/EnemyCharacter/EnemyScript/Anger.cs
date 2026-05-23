using System;
using System.Threading.Tasks;
using Godot;

public partial class Anger : EnemyCharacter
{
    private const int PassivePowerGain = 3;
    private Action<Skill> _skillUsedHandler;

    public const string PassiveNameText = "愤怒";
    public static string PassiveDescriptionText =>
        $"敌方打出非攻击牌时：获得{PassivePowerGain}点力量。";

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
        )
        {
            return;
        }

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

        MaxLife = 168;
        Power = 13;
        Survivability = 10;
        Speed = 34;
        SkillIDs = [SkillID.AngerEliteAttack, SkillID.AngerEliteSurvive, SkillID.AngerEliteSpecial];

        PassiveName = global::Anger.PassiveNameText;
        PassiveDescription = global::Anger.PassiveDescriptionText;
    }
}

public partial class AngerEliteAttack : Skill
{
    private const int BaseDamage = 12;

    public AngerEliteAttack()
        : base(SkillTypes.Attack)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "裂镰扑杀";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(this, AttackStep(baseDamage: BaseDamage), BlockStep());
    }
}

public partial class AngerEliteSurvive : Skill
{
    private const int BaseBlock = 14;
    private const int SelfSurvivabilityGain = 4;

    public AngerEliteSurvive()
        : base(SkillTypes.Survive)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "伏身";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            BlockStep(baseBlock: BaseBlock),
            ModifyPropertyStep(PropertyType.Survivability, SelfSurvivabilityGain)
        );
    }
}

public partial class AngerEliteSpecial : Skill
{
    private const int BaseDamage = 0;
    private const int PowerMultiplier = 2;

    public AngerEliteSpecial()
        : base(SkillTypes.Special)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "狂怒弧刃";
    public override int EnergyCost => 4;

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            AttackStep(baseDamage: BaseDamage, multiplier: PowerMultiplier)
        );
    }
}
