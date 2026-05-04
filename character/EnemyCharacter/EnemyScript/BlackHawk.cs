using System;
using System.Threading.Tasks;
using Godot;

public partial class BlackHawk : EnemyCharacter
{
    private const int PassivePowerGain = 1;
    private Func<Character, Task> _allyActionEndedHandler;

    public const string PassiveNameText = "暗羽蓄势";
    public static string PassiveDescriptionText =>
        $"己方角色行动后：获得{PassivePowerGain}点力量。";

    public override string CharacterName { get; set; } = "BlackHawk";

    public override void Initialize()
    {
        base.Initialize();
        PassiveName = PassiveNameText;
        PassiveDescription = PassiveDescriptionText;
        _allyActionEndedHandler ??= OnAllyActionEnded;
        if (BattleNode != null && !BattleNode.EmitList.Contains(_allyActionEndedHandler))
            BattleNode.EmitList.Add(_allyActionEndedHandler);
    }

    public override void _ExitTree()
    {
        if (BattleNode != null && _allyActionEndedHandler != null)
            BattleNode.EmitList.Remove(_allyActionEndedHandler);
        base._ExitTree();
    }

    private Task OnAllyActionEnded(Character actingCharacter)
    {
        if (
            actingCharacter == null
            || actingCharacter.IsPlayer != IsPlayer
            || actingCharacter.BattleNode != BattleNode
            || State != CharacterState.Normal
            || actingCharacter.IsSummon
        )
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

        MaxLife = 92;
        Power = 11;
        Survivability = 8;
        Speed = 14;
        SpecialIntentThreshold = 3;

        SkillIDs = [SkillID.BlackHawkAttack, SkillID.BlackHawkSurvive, SkillID.BlackHawkSpecial];

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
            AttackPrimaryStep(
                baseDamage: 0,
                powerMultiplier: PowerMultiplier,
                times: 3,
                prefix: "每段造成",
                suffix: "点伤害，共3段。"
            ),
            ApplyBuffFriendly(Buff.BuffName.Invisible, InvisibleStacks, RelativeTarget(0))
        );
    }
}

public partial class BlackHawkSurvive : Skill
{
    private const int HealAmount = 5;
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
            HealStep(HealAmount, RelativeTarget(0), descriptionOverride: $"治疗{HealAmount}点。"),
            HealStep(
                HealAmount,
                RelativeTarget(-1),
                descriptionOverride: $"上一位角色治疗{HealAmount}点。"
            ),
            ApplyBuffFriendly(Buff.BuffName.Invisible, InvisibleStacks, RelativeTarget(0)),
            ApplyBuffFriendly(Buff.BuffName.Invisible, InvisibleStacks, RelativeTarget(-1))
        );
    }
}

public partial class BlackHawkSpecial : Skill
{
    private const int SurvivabilityDown = 4;
    private const int VulnerableStacks = 6;
    private const int EnergyCost = 3;
    private const int MaxTargets = 2;
    int rtimes = 3;
    public BlackHawkSpecial()
        : base(SkillTypes.Special)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "黑羽风暴";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            LowerTargetPropertyStep(PropertyType.Survivability, SurvivabilityDown),
            ApplyBuffHostile(Buff.BuffName.Vulnerable, VulnerableStacks),
            EnergyTimesGateStep(
                energyCost: EnergyCost,
                onPassSteps:
                [
                    EnergyTimesWhileStep(
                        energyCost: 0,
                        times: () => rtimes,
                        loopSteps:
                        [
                            AoeDamageStep(
                                baseDamage: 0,
                                powerMultiplier: 1,
                                target: HostileTargets(MaxTargets)
                            ),
                        ]
                    ),
                ]
            )
        );
    }
}
