using System;
using System.Threading.Tasks;
using Godot;

public partial class Anger : EnemyCharacter
{
    private static int PassivePowerGain => EnemyTuning.GetRequiredInt("AngerElite", nameof(PassivePowerGain));
    private Action<Skill> _skillUsedHandler;

    public const string PassiveNameText = "愤怒";
    public static string PassiveDescriptionText =>
        $"敌方打出非攻击牌时：敌阵回合结束时获得{PassivePowerGain}点力量。";

    public override string CharacterName { get; set; } = "怒镰兽";

    public override void Initialize()
    {
        base.Initialize();
        PassiveName = PassiveNameText;
        PassiveDescription = PassiveDescriptionText;
        _skillUsedHandler ??= skill => TriggerPassive(skill);
        BattleNode.UsedSkills.ItemAdded -= _skillUsedHandler;
        BattleNode.UsedSkills.ItemAdded += _skillUsedHandler;
    }

    public override void _ExitTree()
    {
        if (BattleNode != null && _skillUsedHandler != null)
            BattleNode.UsedSkills.ItemAdded -= _skillUsedHandler;
        base._ExitTree();
    }

    protected override async Task PassiveAsync(Skill skill)
    {
        if (
            State == CharacterState.Dying
            || skill?.OwnerCharater == null
            || skill.OwnerCharater.BattleNode != BattleNode
            || skill.OwnerCharater.IsPlayer == IsPlayer
            || skill.SkillType == Skill.SkillTypes.Attack
            || skill.SkillType == Skill.SkillTypes.none
            || skill.IsStatusCard
        )
        {
            return;
        }

        if (BattleNode?.QueueEnemyPhaseEndPowerGain(this, PassivePowerGain, this, "被动") == true)
            return;

        using var _ = BeginEffectSource("被动");
        await IncreaseProperties(PropertyType.Power, PassivePowerGain, this);
    }
}

public partial class AngerEliteRegedit : EnemyRegedit
{
    public AngerEliteRegedit()
    {
        CharacterName = "愤怒";
        PortaitPath = "res://asset/EnemyCharater/AngerElite.png";
        CharacterScene = GD.Load<PackedScene>("res://character/EnemyCharacter/AngerElite.tscn");
        HasAttackVulnerableIntention = true;
        SkillIDs = [SkillID.AngerEliteAttack];
        // 已有易伤，作为覆盖率补位；第四轮弧刃才追加全阵脆弱。
        IntentionCycle = [SkillID.AngerEliteAttack, SkillID.AngerEliteSurvive, SkillID.AngerEliteAttack, SkillID.AngerEliteSpecial, random];
        PassiveName = global::Anger.PassiveNameText;
        PassiveDescription = global::Anger.PassiveDescriptionText;
    }
}

public partial class AngerEliteAttack : Skill
{
    private int BaseDamage => V(nameof(BaseDamage));

    public override SkillTypes SkillType => SkillTypes.Attack;

    public override string SkillName { get; set; } = "裂镰";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            AttackStep(baseDamage: BaseDamage, multiplier: V("Multiplier"), times: V("HitCount")),
            ApplyBuffHostile(Buff.BuffName.Vulnerable, V("VulnerableStacks"), HostileTargetReference.AttackKey)
        );
    }
}

public partial class AngerEliteSurvive : Skill
{
    private int BaseBlock => V(nameof(BaseBlock));

    public override SkillTypes SkillType => SkillTypes.Survive;

    public override string SkillName { get; set; } = "伏身";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            BlockStep(baseBlock: BaseBlock, multiplier: V("Multiplier")),
            ApplyBuffHostile(Buff.BuffName.Vulnerable, V("VulnerableStacks"), HostileTargetReference.All)
        );
    }
}

public partial class AngerEliteSpecial : Skill
{
    private int BaseDamage => V(nameof(BaseDamage));
    private int HitCount => V(nameof(HitCount));

    public override SkillTypes SkillType => SkillTypes.Special;

    public override string SkillName { get; set; } = "狂怒弧刃";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            AttackStep(baseDamage: BaseDamage, times: HitCount, multiplier: V("Multiplier"))
        );
    }
}
