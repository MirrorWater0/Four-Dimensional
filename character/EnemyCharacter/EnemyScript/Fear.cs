using System;
using System.Linq;
using System.Threading.Tasks;
using Godot;

public partial class Fear : EnemyCharacter
{
    private static int StartFearStacks => EnemyTuning.GetRequiredInt("FearElite", nameof(StartFearStacks));

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
        PortaitPath = "res://asset/EnemyCharater/FearElite.png";
        CharacterScene = GD.Load<PackedScene>("res://character/EnemyCharacter/Fear.tscn");
        SkillIDs = [SkillID.FearEliteAttack, SkillID.FearEliteSurvive, SkillID.FearEliteSpecial];
        // 开场被动已施加恐惧；凝视后蓄势，再扩散梦魇。
        IntentionCycle = [SkillID.FearEliteAttack, SkillID.FearEliteSurvive, SkillID.FearEliteSpecial, random, SkillID.FearEliteAttack];

        PassiveName = global::Fear.PassiveNameText;
        PassiveDescription = global::Fear.PassiveDescriptionText;
    }
}

public partial class FearEliteAttack : Skill
{
    private int BaseDamage => V(nameof(BaseDamage));

    public override SkillTypes SkillType => SkillTypes.Attack;

    public override string SkillName { get; set; } = "惊惧凝视";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            AttackStep(baseDamage: BaseDamage, target: HostileTargetReference.One, times: V("HitCount"), multiplier: V("Multiplier")),
            ApplyBuffHostile(Buff.BuffName.Vulnerable,4,HostileTargetReference.AttackKey)
        );
    }
}

public partial class FearEliteSurvive : Skill
{
    private int BaseBlock => V(nameof(BaseBlock));

    public override SkillTypes SkillType => SkillTypes.Survive;

    public override string SkillName { get; set; } = "蜷缩异相";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            BlockStep(baseBlock: BaseBlock, multiplier: V("Multiplier")),
            ModifyPropertyStep(PropertyType.Power, V("PowerGain"))
        );
    }
}

public partial class FearEliteSpecial : Skill
{
    private int BaseDamage => V(nameof(BaseDamage));
    private int FearStacks => V(nameof(FearStacks));

    public override SkillTypes SkillType => SkillTypes.Special;

    public override string SkillName { get; set; } = "梦魇扩散";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            ApplyBuffHostile(Buff.BuffName.Fear, FearStacks, HostileTargetReference.All),
            AttackStep(baseDamage: BaseDamage, target: HostileTargetReference.All, multiplier: V("Multiplier"), times: V("HitCount")),
            HealStep(V("BaseHeal"))
        );
    }
}
