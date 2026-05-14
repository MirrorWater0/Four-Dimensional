using System;
using System.Threading.Tasks;
using Godot;

public partial class Ferociouess : EnemyCharacter
{
    private const int StartDamageImmuneStacks = 4;
    private const int ActionTriggerCount = 2;
    private const int TriggerDamageImmuneStacks = 1;

    public const string PassiveNameText = "骨骼硬化";
    public static string PassiveDescriptionText =>
        $"战斗开始时：获得{StartDamageImmuneStacks}层{Buff.BuffName.DamageImmune.GetDescription()}。\n"
        + $"每有{ActionTriggerCount}个己方角色行动后：获得{TriggerDamageImmuneStacks}层{Buff.BuffName.DamageImmune.GetDescription()}。";

    private int Count = 0;
    private string _basePassiveDescription;
    private Func<Character, Task> _allyActionEndedHandler;

    public override string CharacterName { get; set; } = "Ferociouess";

    public override void Initialize()
    {
        base.Initialize();
        PassiveName = PassiveNameText;
        PassiveDescription = PassiveDescriptionText;
        _basePassiveDescription = PassiveDescriptionText;
        UpdatePassiveDescription();
        BattleNode.StartEffectList.Add(StartPassive);
        _allyActionEndedHandler ??= OnAllyActionEnded;
        if (BattleNode != null && !BattleNode.EmitList.Contains(_allyActionEndedHandler))
            BattleNode.EmitList.Add(_allyActionEndedHandler);
    }

    private Task StartPassive()
    {
        using var _ = BeginEffectSource("被动");
        HurtBuff.BuffAdd(Buff.BuffName.DamageImmune, this, StartDamageImmuneStacks, this);
        return Task.CompletedTask;
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
        )
            return Task.CompletedTask;

        Count++;
        TriggerPassive(null);
        UpdatePassiveDescription();
        return Task.CompletedTask;
    }

    public override void Passive(Skill skill)
    {
        if (Count < ActionTriggerCount)
            return;

        Count = 0;
        using var _ = BeginEffectSource("被动");
        HurtBuff.BuffAdd(Buff.BuffName.DamageImmune, this, TriggerDamageImmuneStacks, this);
    }

    private void UpdatePassiveDescription()
    {
        if (string.IsNullOrWhiteSpace(_basePassiveDescription))
            _basePassiveDescription = PassiveDescription ?? string.Empty;

        PassiveDescription = $"{_basePassiveDescription}\n当前计数：{Count}/{ActionTriggerCount}";
    }
}

public partial class FerociouessRegedit : EnemyRegedit
{
    public FerociouessRegedit()
    {
        CharacterName = "Ferociouess";
        PType = EnemyPositionType.FrontRow;
        PortaitPath = "res://asset/EnemyCharater/Ferociouess.png";
        CharacterScene = GD.Load<PackedScene>("res://character/EnemyCharacter/Ferociouess.tscn");

        MaxLife = 35;
        Power = 5;
        Survivability = 11;
        Speed = 7;
        SkillIDs =
        [
            SkillID.FerociouessAttack,
            SkillID.FerociouessSurvive,
            SkillID.FerociouessSpecial,
        ];

        PassiveName = global::Ferociouess.PassiveNameText;
        PassiveDescription = global::Ferociouess.PassiveDescriptionText;
    }
}

public partial class FerociouessAttack : Skill
{
    private const int BaseDamage = 16;
    private const int SelfPowerGain = 2;

    public FerociouessAttack()
        : base(SkillTypes.Attack)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "凶恶冲击";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            AoeDamageStep(baseDamage: BaseDamage, target: HostileTargets(0)),
            ModifyPropertyStep(PropertyType.Power, SelfPowerGain)
        );
    }
}

public partial class FerociouessSurvive : Skill
{
    private const int BaseBlock = 0;
    private const int DamageImmuneStacks = 1;
    private const int VulnerableStacks = 1;

    public FerociouessSurvive()
        : base(SkillTypes.Survive)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "骨骼免疫";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            BlockStep(0, BaseBlock),
            ApplyBuffFriendly(
                buffName: Buff.BuffName.DamageImmune,
                stacks: DamageImmuneStacks,
                target: TargetReference.Self
            ),
            ApplyBuffHostile(
                buffName: Buff.BuffName.Vulnerable,
                stacks: VulnerableStacks,
                target: HostileTargets(0)
            ),
            ModifyPropertyStep(PropertyType.Power, 3)
        );
    }
}

public partial class FerociouessSpecial : Skill
{
    private const int BaseDamage = 5;
    private const int BurstPowerMultiplier = 2;

    public FerociouessSpecial()
        : base(SkillTypes.Special)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "狂暴";
    public override int EnergyCost => 4;

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            AoeDamageStep(baseDamage: BaseDamage, target: HostileTargets(0)),
            AoeDamageStep(
                baseDamage: 0,
                powerMultiplier: BurstPowerMultiplier,
                target: HostileTargets(0)
            )
        );
    }
}
