using System;
using System.Linq;
using System.Threading.Tasks;
using Godot;

public partial class Fear : EnemyCharacter
{
    private const int StartFearStacks = 3;

    public const string PassiveNameText = "恐惧具现";
    public static string PassiveDescriptionText =>
        $"战斗开始时：令敌方全阵获得{StartFearStacks}层{Buff.BuffName.Fear.GetDescription()}。";

    public override string CharacterName { get; set; } = "恐惧";

    public override void Initialize()
    {
        base.Initialize();
        PassiveName = PassiveNameText;
        PassiveDescription = PassiveDescriptionText;
        BattleNode.StartEffectList.Add(StartPassive);
    }

    private Task StartPassive()
    {
        using var _ = BeginEffectSource("被动");
        var hostiles = BattleNode
            ?.GetTeamCharacters(!IsPlayer, includeSummons: true)
            .Where(x => x != null && x.State != CharacterState.Dying)
            .ToArray();
        if (hostiles == null)
            return Task.CompletedTask;

        foreach (var hostile in hostiles)
            SkillBuff.BuffAdd(Buff.BuffName.Fear, hostile, StartFearStacks, this);

        return Task.CompletedTask;
    }
}

public partial class FearEliteRegedit : EnemyRegedit
{
    public FearEliteRegedit()
    {
        CharacterName = "恐惧";
        PType = EnemyPositionType.BackRow;
        PortaitPath = "res://asset/EnemyCharater/FearElite.png";
        CharacterScene = GD.Load<PackedScene>("res://character/EnemyCharacter/Fear.tscn");

        MaxLife = 204;
        Power = 0;
        Survivability = 0;
        BasePowerContribution = 0;
        BaseSurvivabilityContribution = 0;
        SkillIDs = [SkillID.FearEliteAttack, SkillID.FearEliteSurvive, SkillID.FearEliteSpecial];

        PassiveName = global::Fear.PassiveNameText;
        PassiveDescription = global::Fear.PassiveDescriptionText;
    }
}

public partial class FearEliteAttack : Skill
{
    private const int BaseDamage = 10;

    public FearEliteAttack()
        : base(SkillTypes.Attack)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "惊惧凝视";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            AttackStep(baseDamage: BaseDamage, target: HostileTargetReference.One, times: 2)
        );
    }
}

public partial class FearEliteSurvive : Skill
{
    private const int BaseBlock = 21;

    public FearEliteSurvive()
        : base(SkillTypes.Survive)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "蜷缩异相";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            BlockStep(baseBlock: BaseBlock),
            ModifyPropertyStep(PropertyType.Power, 3)
        );
    }
}

public partial class FearEliteSpecial : Skill
{
    private const int BaseDamage = 10;
    private const int FearStacks = 2;

    public FearEliteSpecial()
        : base(SkillTypes.Special)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "梦魇扩散";
    public override int EnemySpecialIntentionCooldown => 2;

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            ApplyBuffHostile(Buff.BuffName.Fear, FearStacks, HostileTargetReference.All),
            AttackStep(baseDamage: BaseDamage, target: HostileTargetReference.All),
            HealStep(15)
        );
    }
}
