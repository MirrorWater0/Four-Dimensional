using System;
using System.Threading.Tasks;
using Godot;

public partial class Turbine : EnemyCharacter
{
    private static int StartThornStacks => EnemyTuning.GetRequiredInt("Turbine", nameof(StartThornStacks));
    private static int ActionTriggerCount => EnemyTuning.GetRequiredInt("Turbine", nameof(ActionTriggerCount));
    private static int TriggerThornStacks => EnemyTuning.GetRequiredInt("Turbine", nameof(TriggerThornStacks));

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

public partial class TurbineRegedit : EnemyRegedit
{
    public TurbineRegedit()
    {
        CharacterName = "Turbine";
        PortaitPath = "res://asset/EnemyCharater/Turbine.png";
        CharacterScene = GD.Load<PackedScene>("res://character/EnemyCharacter/Turbine.tscn");
        SkillIDs = [SkillID.TurbineAttack, SkillID.TurbineSurvive, SkillID.TurbineSpecial];
        // 支援邻位，冲击强化，随机调整后以超压额外打出攻击卡。
        IntentionCycle = [SkillID.TurbineSurvive, SkillID.TurbineAttack, random, SkillID.TurbineSpecial, SkillID.TurbineAttack];

        PassiveName = global::Turbine.PassiveNameText;
        PassiveDescription = global::Turbine.PassiveDescriptionText;
    }
}

public partial class TurbineAttack : Skill
{
    private int BaseDamage => V(nameof(BaseDamage));
    private int AllySurvivabilityGain => V(nameof(AllySurvivabilityGain));

    public override SkillTypes SkillType => SkillTypes.Attack;

    public override string SkillName { get; set; } = "涡轮冲击";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            AttackStep(BaseDamage, multiplier: V("Multiplier"), times: V("HitCount")),
            ModifyPropertyStep(
                PropertyType.Survivability,
                AllySurvivabilityGain,
                TargetReference.Previous
            )
        );
    }
}

public partial class TurbineSurvive : Skill
{
    private int BaseSurvivabilityGain => V(nameof(BaseSurvivabilityGain));
    private int SurvivabilityMultiplier => V(nameof(SurvivabilityMultiplier));
    private int AllyPowerGain => V(nameof(AllyPowerGain));

    public override SkillTypes SkillType => SkillTypes.Survive;

    public override string SkillName { get; set; } = "循环";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            BlockStep(
                baseBlock: BaseSurvivabilityGain,
                multiplier: SurvivabilityMultiplier
            ),
            ModifyPropertyStep(PropertyType.Power, AllyPowerGain, TargetReference.Next)
        );
    }
}

public partial class TurbineSpecial : Skill
{

    private int WoundCount => V(nameof(WoundCount));

    public override SkillTypes SkillType => SkillTypes.Special;

    public override string SkillName { get; set; } = "超压模式";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            AttackStep(V("BaseDamage"), multiplier: V("Multiplier"), times: V("HitCount")),
            PlayDrawPileCardStep(SkillTypes.Attack),
            AddCardsStep(SkillID.WoundStatus, WoundCount)
        );
    }
}
