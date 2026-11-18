using System.Threading.Tasks;
using Godot;

public partial class FearWorm : EnemyCharacter
{
    private static int PassivePowerGain => EnemyTuning.GetRequiredInt("FearWorm", nameof(PassivePowerGain));

    public const string PassiveNameText = "蜕皮";
    public static string PassiveDescriptionText =>
        $"回合结束时：获得{PassivePowerGain}点力量。";

    public override void Initialize()
    {
        base.Initialize();
        PassiveName = PassiveNameText;
        PassiveDescription = PassiveDescriptionText;
    }

    public override void OnTurnEnd()
    {
        TriggerPassive(null);
        base.OnTurnEnd();
    }

    protected override async Task PassiveAsync(Skill skill)
    {
        if (BattleNode?.QueueEnemyPhaseEndPowerGain(this, PassivePowerGain, this, "被动") == true)
            return;

        using var _ = BeginEffectSource("被动");
        await IncreaseProperties(PropertyType.Power, PassivePowerGain, this);
    }
}

public partial class FearWormRegedit : EnemyRegedit
{
    public FearWormRegedit()
    {
        CharacterName = "FearWorm";
        PortaitPath = "res://asset/EnemyCharater/FearWorm.png";
        CharacterScene = GD.Load<PackedScene>("res://character/EnemyCharacter/FearWorm.tscn");
        HasAttackVulnerableIntention = true;
        SkillIDs = [SkillID.FearWormAttack, SkillID.FearWormTermin];
        // 梦魇起手，咬噬施加易伤，潜伏蓄势后进入随机段。
        IntentionCycle = [SkillID.FearWormTermin, SkillID.FearWormAttack, random];
        PassiveName = global::FearWorm.PassiveNameText;
        PassiveDescription = global::FearWorm.PassiveDescriptionText;
    }
}

public partial class FearWormAttack : Skill
{
    private int BaseDamage => V(nameof(BaseDamage));
    private int VulnerableStacks => V(nameof(VulnerableStacks));

    public override SkillTypes SkillType => SkillTypes.Attack;

    public override string SkillName { get; set; } = "恐惧咬噬";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            AttackStep(
                baseDamage: BaseDamage,
                target: HostileTargetReference.All,
                times: V("HitCount"),
                clampMax: V("ClampMax"),
                multiplier: V("Multiplier")
            ),
            ApplyBuffHostile(
                buffName: Buff.BuffName.Vulnerable,
                stacks: VulnerableStacks,
                target: HostileTargetReference.AttackKey
            )
        );
    }
}

public partial class FearWormSurvive : Skill
{
    private int BaseBlock => V(nameof(BaseBlock));

    public override SkillTypes SkillType => SkillTypes.Survive;

    public override string SkillName { get; set; } = "潜伏";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            BlockStep(baseBlock: BaseBlock, multiplier: V("Multiplier")),
            ModifyPropertyStep(PropertyType.Power, V("PowerGain"), TargetReference.Self)
        );
    }
}

public partial class FearWormTermin : Skill
{
    private int BaseDamage => V(nameof(BaseDamage));

    public override SkillTypes SkillType => SkillTypes.Special;

    public override string SkillName { get; set; } = "梦魇缠绕";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(this, AttackStep(BaseDamage, multiplier: V("Multiplier"), times: V("HitCount")));
    }
}
