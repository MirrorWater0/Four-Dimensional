using System;
using System.Threading.Tasks;
using Godot;

public partial class FearWorm : EnemyCharacter
{
    private const int PassiveDebuffImmunityStacks = 1;
    private const int PassiveAllyActionThreshold = 2;
    private const int PassivePowerGain = 10;
    private int _passiveAllyActionCount;
    private Func<Character, Task> _allyActionEndedHandler;

    public const string PassiveNameText = "蜕皮";
    public static string PassiveDescriptionText =>
        $"初始：获得{PassiveDebuffImmunityStacks}层{Buff.BuffName.DebuffImmunity.GetDescription()}。\n"
        + $"\u5176\u4ed6\u5df1\u65b9\u89d2\u8272\u6bcf\u884c\u52a8{PassiveAllyActionThreshold}\u6b21\uff1a\u83b7\u5f97{PassivePowerGain}\u70b9\u529b\u91cf\u3002";

    public override void Initialize()
    {
        base.Initialize();
        PassiveName = PassiveNameText;
        PassiveDescription = PassiveDescriptionText;
        UpdatePassiveDescription();
        using var _ = BeginEffectSource("被动");
        SpecialBuff.BuffAdd(Buff.BuffName.DebuffImmunity, this, PassiveDebuffImmunityStacks, this);
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
            $"{PassiveDescriptionText}\n\u5f53\u524d\u8ba1\u6570\uff1a{_passiveAllyActionCount}/{PassiveAllyActionThreshold}";
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

        MaxLife = 45;
        Power = 12;
        Survivability = 6;
        Speed = 9;
        SpecialIntentThreshold = 2;

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
            AoeDamageStep(
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
    private const int DebuffImmunityStacks = 1;
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
            ApplyBuffFriendly(
                buffName: Buff.BuffName.DebuffImmunity,
                stacks: DebuffImmunityStacks,
                target: TargetReference.Self
            ),
            BlockStep(0, BaseBlock),
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
    public override int EnergyCost => 3;

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            ApplyBuffHostile(
                buffName: Buff.BuffName.Stun,
                stacks: StunStacks,
                target: HostileTargets(1)
            ),
            ModifyPropertyStep(PropertyType.Power, Power),
            AttackPrimaryStep(BaseDamage)
        );
    }
}
