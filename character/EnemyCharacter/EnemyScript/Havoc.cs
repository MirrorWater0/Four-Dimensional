using System.Linq;
using Godot;

public partial class Havoc : EnemyCharacter
{
    private const int PassiveTriggerInterval = 3;
    private const int PassivePlagueCount = 1;
    private int _turnStartCount;

    public const string PassiveNameText = "灾厄脉冲";
    public static string PassiveDescriptionText =>
        $"每{PassiveTriggerInterval}次回合开始时：向所有角色抽牌堆加入{PassivePlagueCount}张{Skill.GetSkill(SkillID.PlagueStatus)?.SkillName ?? "瘟疫"}。";

    public override string CharacterName { get; set; } = "Havoc";

    public override void Initialize()
    {
        base.Initialize();
        PassiveName = PassiveNameText;
        PassiveDescription = PassiveDescriptionText;
    }

    public override void OnTurnStart()
    {
        base.OnTurnStart();
        _turnStartCount++;
        if (_turnStartCount % PassiveTriggerInterval == 0)
            TriggerPassive(null);
    }

    public override async void Passive(Skill skill)
    {
        using var _ = BeginEffectSource("被动");
        if (BattleNode == null)
            return;

        PlayerCharacter[] targets = BattleNode
            .GetOrderedTeamCharacters(!IsPlayer, includeSummons: false, dyingFilter: true)
            .OfType<PlayerCharacter>()
            .Where(target => target.State == CharacterState.Normal)
            .ToArray();
        if (targets.Length == 0)
            return;

        CharacterControl characterControl = BattleNode.CharacterControl;
        if (characterControl != null && GodotObject.IsInstanceValid(characterControl))
        {
            await characterControl.PlayStatusCardInsertAnimationAsync(
                targets
                    .Select(target => new CharacterControl.StatusCardInsertAnimationEntry(
                        target,
                        SkillID.PlagueStatus,
                        PassivePlagueCount,
                        this
                    ))
                    .ToArray()
            );
        }

        foreach (var target in targets)
        {
            BattleNode.AddPlayerBattleStatusCardsToDrawPile(
                target,
                SkillID.PlagueStatus,
                PassivePlagueCount,
                this
            );
        }
    }
}

public partial class HavocRegedit : EnemyRegedit
{
    public HavocRegedit()
    {
        CharacterName = "Havoc";
        PType = EnemyPositionType.BackRow;
        PortaitPath = "res://asset/EnemyCharater/Havoc_v4.png";
        CharacterScene = GD.Load<PackedScene>("res://character/EnemyCharacter/Havoc.tscn");

        MaxLife = 245;
        Power = 13;
        Survivability = 12;
        Speed = 23;
        SkillIDs = [SkillID.HavocAttack, SkillID.HavocSurvive, SkillID.HavocSpecial];

        PassiveName = global::Havoc.PassiveNameText;
        PassiveDescription = global::Havoc.PassiveDescriptionText;
    }
}

public partial class HavocAttack : Skill
{
    private const int BaseDamage = 2;
    private const int WeakenStacks = 1;

    public HavocAttack()
        : base(SkillTypes.Attack)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "裂壳横扫";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            AttackStep(baseDamage: BaseDamage, multiplier: 1, target: HostileTargetReference.Two)
        );
    }
}

public partial class HavocSurvive : Skill
{
    private const int BaseBlock = 8;
    private const int SurvivabilityGain = 2;

    public HavocSurvive()
        : base(SkillTypes.Survive)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "天灾之证";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            BlockStep(baseBlock: BaseBlock, multiplier: 2),
            ModifyPropertyStep(PropertyType.Survivability, SurvivabilityGain),
            ModifyPropertyStep(PropertyType.Power, 1)
        );
    }
}

public partial class HavocSpecial : Skill
{
    private const int DisasterStacks = 3;
    private const int SelfPowerGain = 2;

    public HavocSpecial()
        : base(SkillTypes.Special)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "崩坏回响";
    public override int EnergyCost => 7;

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            AttackStep(baseDamage: 0, multiplier: 1, target: HostileTargetReference.All),
            ApplyBuffHostile(Buff.BuffName.Disaster, DisasterStacks, HostileTargetReference.All),
            ModifyPropertyStep(PropertyType.Power, SelfPowerGain)
        );
    }
}
