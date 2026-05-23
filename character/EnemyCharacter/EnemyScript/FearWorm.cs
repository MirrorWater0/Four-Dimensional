using System;
using System.Threading.Tasks;
using Godot;

public partial class FearWorm : EnemyCharacter
{
    private const int PassiveAllyActionThreshold = 2;
    private const int PassivePowerGain = 8;
    private int _passiveAllyActionCount;
    private Func<Character, Task> _allyActionEndedHandler;

    public const string PassiveNameText = "蜕皮";
    public static string PassiveDescriptionText =>
        $"其他己方角色每行动{PassiveAllyActionThreshold}次：获得{PassivePowerGain}点力量。";

    public override void Initialize()
    {
        base.Initialize();
        PassiveName = PassiveNameText;
        PassiveDescription = PassiveDescriptionText;
        UpdatePassiveDescription();
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
            || actingCharacter == this
            || actingCharacter.IsPlayer != IsPlayer
            || actingCharacter.BattleNode != BattleNode
            || State != CharacterState.Normal
            || actingCharacter.IsSummon
        )
            return Task.CompletedTask;

        _passiveAllyActionCount++;
        UpdatePassiveDescription();
        if (_passiveAllyActionCount < PassiveAllyActionThreshold)
            return Task.CompletedTask;

        _passiveAllyActionCount = 0;
        UpdatePassiveDescription();
        TriggerPassive(null);
        return Task.CompletedTask;
    }

    private void UpdatePassiveDescription()
    {
        PassiveDescription =
            $"{PassiveDescriptionText}\n当前计数：{_passiveAllyActionCount}/{PassiveAllyActionThreshold}";
    }

    public override async void Passive(Skill skill)
    {
        using var _ = BeginEffectSource("被动");
        await IncreaseProperties(PropertyType.Power, PassivePowerGain, this);
    }

}

public partial class FearWormRegedit : EnemyRegedit
{
    public FearWormRegedit()
    {
        CharacterName = "FearWorm";
        PType = EnemyPositionType.BackRow;
        PortaitPath = "res://asset/EnemyCharater/FearWorm.png";
        CharacterScene = GD.Load<PackedScene>("res://character/EnemyCharacter/FearWorm.tscn");

        MaxLife = 30;
        Power = 13;
        Survivability = 5;
        Speed = 8;
        SkillIDs = [SkillID.FearWormAttack, SkillID.FearWormSurvive, SkillID.FearWormTermin];

        PassiveName = global::FearWorm.PassiveNameText;
        PassiveDescription = global::FearWorm.PassiveDescriptionText;
    }
}

public partial class FearWormAttack : Skill
{
    private const int BaseDamage = 0;
    private const int VulnerableStacks = 1;
    private const int MaxTargets = 3;
    private const int EnergyGain = 1;

    public FearWormAttack()
        : base(SkillTypes.Attack)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "恐惧咬噬";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            EnergyStep(EnergyGain),
            AttackStep(
                baseDamage: BaseDamage,
                target: HostileTargets(MaxTargets),
                times: 1,
                clampMax: 999
            ),
            ApplyBuffHostile(
                buffName: Buff.BuffName.Vulnerable,
                stacks: VulnerableStacks,
                target: HostileTargets(MaxTargets)
            )
        );
    }
}

public partial class FearWormSurvive : Skill
{
    private const int BaseBlock = 4;

    public FearWormSurvive()
        : base(SkillTypes.Survive)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "潜伏";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            BlockStep(baseBlock: BaseBlock),
            CarryStep(target: TargetReference.Next, skillIndex: 1)
        );
    }
}

public partial class FearWormTermin : Skill
{
    private const int BaseDamage = 6;
    private const int Power = 3;
    private const int StunStacks = 2;

    public FearWormTermin()
        : base(SkillTypes.Special)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "梦魇缠绕";
    public override int EnergyCost => 4;

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            ApplyBuffHostile(
                buffName: Buff.BuffName.Stun,
                stacks: StunStacks,
                target: HostileTargetReference.One
            ),
            AttackStep(BaseDamage)
        );
    }
}
