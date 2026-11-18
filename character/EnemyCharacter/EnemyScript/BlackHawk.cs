using System;
using System.Threading.Tasks;
using Godot;

public partial class BlackHawk : EnemyCharacter
{
    private static int PassivePowerGain => EnemyTuning.GetRequiredInt("BlackHawk", nameof(PassivePowerGain));
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

    protected override async Task PassiveAsync(Skill skill)
    {
        using var effectScope = BeginEffectSource("被动");
        await IncreaseProperties(PropertyType.Power, PassivePowerGain, this);
    }
}

public partial class BlackHawkRegedit : EnemyRegedit
{
    public BlackHawkRegedit()
    {
        CharacterName = "BlackHawk";
        PortaitPath = "res://asset/EnemyCharater/BlackHawk.png";
        CharacterScene = GD.Load<PackedScene>("res://character/EnemyCharacter/BlackHawk.tscn");
        HasAttackVulnerableIntention = true;
        SkillIDs = [SkillID.BlackHawkAttack, SkillID.BlackHawkSpecial];
        // 第四轮回翔随机施加脆弱，第五轮全阵风暴保证压到脆弱角色。
        IntentionCycle = [SkillID.BlackHawkAttack, random, SkillID.BlackHawkAttack, SkillID.BlackHawkSurvive, SkillID.BlackHawkSpecial];

        PassiveName = global::BlackHawk.PassiveNameText;
        PassiveDescription = global::BlackHawk.PassiveDescriptionText;
    }
}

public partial class BlackHawkAttack : Skill
{
    private int PowerMultiplier => V(nameof(PowerMultiplier));
    private int InvisibleStacks => V(nameof(InvisibleStacks));

    public override SkillTypes SkillType => SkillTypes.Attack;

    public override string SkillName { get; set; } = "裂羽连袭";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            AttackStep(baseDamage: V("BaseDamage"), multiplier: PowerMultiplier, times: V("HitCount")),
            ApplyBuffFriendly(Buff.BuffName.Invisible, InvisibleStacks, TargetReference.Self)
        );
    }
}

public partial class BlackHawkSurvive : Skill
{
    private int HealAmount => V(nameof(HealAmount));
    private int InvisibleStacks => V(nameof(InvisibleStacks));

    public override SkillTypes SkillType => SkillTypes.Survive;

    public override string SkillName { get; set; } = "夜幕回翔";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            HealStep(HealAmount, TargetReference.Previous),
            ModifyPropertyStep(PropertyType.Power, V("PowerGain"), TargetReference.Previous),
            ApplyBuffHostile(Buff.BuffName.Frail, V("FrailStacks"), HostileTargetReference.RandomPreview),
            ApplyBuffFriendly(Buff.BuffName.Invisible, InvisibleStacks, TargetReference.Self)
        );
    }
}

public partial class BlackHawkSpecial : Skill
{
    private int rtimes => V("LoopCount");

    public override SkillTypes SkillType => SkillTypes.Special;

    public override string SkillName { get; set; } = "黑羽风暴";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            WhileStep(
                times: () => rtimes,
                loopSteps:
                [
                    AttackStep(baseDamage: V("BaseDamage"), multiplier: V("Multiplier"), target: HostileTargetReference.All, times: V("HitCount")),
                ]
            )
        );
    }
}
