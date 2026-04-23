using System;
using System.Threading.Tasks;
using Godot;

public partial class Turbine : EnemyCharacter
{
    private const int StartThornStacks = 7;
    private const int ActionTriggerCount = 2;
    private const int TriggerThornStacks = 4;

    public const string PassiveNameText = "棘轮增压";
    public static string PassiveDescriptionText =>
        $"战斗开始时：获得{StartThornStacks}层{Buff.BuffName.Thorn.GetDescription()}。\n"
        + $"己方角色每行动{ActionTriggerCount}次：获得{TriggerThornStacks}层{Buff.BuffName.Thorn.GetDescription()}。";

    private int Count;
    private Func<Character, Task> _allyActionEndedHandler;

    public override string CharacterName { get; set; } = "Turbine";

    public override void Initialize()
    {
        base.Initialize();
        PassiveName = PassiveNameText;
        PassiveDescription = PassiveDescriptionText;
        BattleNode.StartEffectList.Add(StartPassive);
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

    private Task StartPassive()
    {
        using var _ = BeginEffectSource("被动");
        HurtBuff.BuffAdd(Buff.BuffName.Thorn, this, StartThornStacks, this);
        return Task.CompletedTask;
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

        Count++;
        TriggerPassive(null);
        return Task.CompletedTask;
    }

    public override void Passive(Skill skill)
    {
        if (Count < ActionTriggerCount)
            return;

        Count = 0;
        using var _ = BeginEffectSource("被动");
        HurtBuff.BuffAdd(Buff.BuffName.Thorn, this, TriggerThornStacks, this);
    }
}

public partial class TurbineAttack : Skill
{
    private const int BaseDamage = 20;
    private const int AllySurvivabilityGain = 5;

    public TurbineAttack()
        : base(SkillTypes.Attack)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "涡轮冲击";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            AttackPrimaryStep(BaseDamage),
            ModifyPropertyStep(
                PropertyType.Survivability,
                AllySurvivabilityGain,
                RelativeTarget(-1)
            )
        );
    }
}

public partial class TurbineSurvive : Skill
{
    private const int BaseSurvivabilityGain = 5;
    private const int SurvivabilityMultiplier = 2;
    private const int AllyPowerGain = 3;
    private const int AllyEnergyGain = 1;

    public TurbineSurvive()
        : base(SkillTypes.Survive)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "护压循环";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            BlockStep(0, BaseSurvivabilityGain, SurvivabilityMultiplier),
            ModifyPropertyStep(PropertyType.Power, AllyPowerGain, RelativeTarget(1)),
            EnergyStep(AllyEnergyGain, RelativeTarget(1))
        );
    }
}

public partial class TurbineSpecial : Skill
{
    private const int SelfPowerGain = 3;
    private const int EnergyCost = 3;
    private const int ThornStacks = 8;

    public TurbineSpecial()
        : base(SkillTypes.Special)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "超压模式";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            ModifyPropertyStep(PropertyType.Power, SelfPowerGain),
            HealStep(0, RelativeTarget(0)),
            ApplyBuffFriendly(
                buffName: Buff.BuffName.Thorn,
                stacks: ThornStacks,
                target: RelativeTarget(0)
            ),
            EnergyTimesGateStep(
                energyCost: EnergyCost,
                onPassSteps: [CarryRelativeAllyStep(relativeIndex: -1, skillIndex: 1)]
            )
        );
    }
}
