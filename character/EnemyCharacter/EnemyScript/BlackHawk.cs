using System;
using System.Threading.Tasks;
using Godot;

public partial class BlackHawk : EnemyCharacter
{
    private const int PassivePowerGain = 1;
    private Func<bool, Task> _teamTurnEndHandler;

    public const string PassiveNameText = "暗羽蓄势";
    public static string PassiveDescriptionText =>
        $"己方阵营回合结束时：获得{PassivePowerGain}点力量。";

    public override string CharacterName { get; set; } = "BlackHawk";

    public override void Initialize()
    {
        base.Initialize();
        PassiveName = PassiveNameText;
        PassiveDescription = PassiveDescriptionText;
        _teamTurnEndHandler ??= OnTeamTurnEnded;
        if (BattleNode != null && !BattleNode.TeamTurnEndEmitList.Contains(_teamTurnEndHandler))
            BattleNode.TeamTurnEndEmitList.Add(_teamTurnEndHandler);
    }

    public override void _ExitTree()
    {
        if (BattleNode != null && _teamTurnEndHandler != null)
            BattleNode.TeamTurnEndEmitList.Remove(_teamTurnEndHandler);
        base._ExitTree();
    }

    private Task OnTeamTurnEnded(bool endedTeamIsPlayer)
    {
        if (State != CharacterState.Normal || endedTeamIsPlayer != IsPlayer)
            return Task.CompletedTask;

        TriggerPassive(null);
        return Task.CompletedTask;
    }

    public override void Passive(Skill skill)
    {
        using var effectScope = BeginEffectSource("被动");
        _ = IncreaseProperties(PropertyType.Power, PassivePowerGain, this);
    }
}

public partial class BlackHawkRegedit : EnemyRegedit
{
    public BlackHawkRegedit()
    {
        CharacterName = "BlackHawk";
        PType = EnemyPositionType.BackRow;
        PortaitPath = "res://asset/EnemyCharater/BlackHawk.png";
        CharacterScene = GD.Load<PackedScene>("res://character/EnemyCharacter/BlackHawk.tscn");

        MaxLife = 52;
        Power = 0;
        Survivability = 0;
        BasePowerContribution = 0;
        BaseSurvivabilityContribution = 0;
        HasAttackVulnerableIntention = true;
        SkillIDs = [SkillID.BlackHawkAttack, SkillID.BlackHawkSpecial];

        PassiveName = global::BlackHawk.PassiveNameText;
        PassiveDescription = global::BlackHawk.PassiveDescriptionText;
    }
}

public partial class BlackHawkAttack : Skill
{
    private const int PowerMultiplier = 1;
    private const int InvisibleStacks = 1;

    public BlackHawkAttack()
        : base(SkillTypes.Attack)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "裂羽连袭";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            AttackStep(baseDamage: 7, multiplier: PowerMultiplier, times: 3),
            ApplyBuffFriendly(Buff.BuffName.Invisible, InvisibleStacks, TargetReference.Self)
        );
    }
}

public partial class BlackHawkSurvive : Skill
{
    private const int HealAmount = 10;
    private const int InvisibleStacks = 2;

    public BlackHawkSurvive()
        : base(SkillTypes.Survive)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "夜幕回翔";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            HealStep(HealAmount, TargetReference.Previous),
            ModifyPropertyStep(PropertyType.Power, 2, TargetReference.Previous),
            ApplyBuffFriendly(Buff.BuffName.Invisible, InvisibleStacks, TargetReference.Self)
        );
    }
}

public partial class BlackHawkSpecial : Skill
{
    int rtimes = 2;

    public BlackHawkSpecial()
        : base(SkillTypes.Special)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "黑羽风暴";
    public override int EnergyCost => 5;

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            WhileStep(
                times: () => rtimes,
                loopSteps:
                [
                    AttackStep(baseDamage: 5, multiplier: 1, target: HostileTargetReference.All),
                ]
            )
        );
    }
}
