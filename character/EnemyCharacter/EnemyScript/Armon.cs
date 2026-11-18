using System;
using System.Linq;
using Godot;

public partial class Armon : EnemyCharacter
{
    private static int TurnEndBaseBlock =>
        EnemyTuning.GetRequiredInt("Armon", nameof(TurnEndBaseBlock));

    public const string PassiveNameText = "矩阵核心";

    public static string UpdatedPassiveDescriptionText =>
        $"每回合结束时：全阵获得{TurnEndBaseBlock}点格挡。";

    public override string CharacterName { get; set; } = "Armon";

    public override void Initialize()
    {
        base.Initialize();
        PassiveName = PassiveNameText;
        PassiveDescription = UpdatedPassiveDescriptionText;
    }

    public override void OnTurnEnd()
    {
        GrantFormationBlock();

        base.OnTurnEnd();
    }

    private void GrantFormationBlock()
    {
        if (BattleNode == null)
            return;

        using var _ = BeginEffectSource("被动");

        int block = Math.Clamp(TurnEndBaseBlock, 0, 999);
        var allies = BattleNode.GetTeamCharacters(IsPlayer, includeSummons: true);

        foreach (var ally in allies.Where(x => x != null && x.State == CharacterState.Normal))
        {
            ally.UpdataBlock(block, source: this);
        }
    }
}

public partial class ArmonRegedit : EnemyRegedit
{
    public ArmonRegedit()
    {
        CharacterName = "Armon";
        PortaitPath = "res://asset/EnemyCharater/Armon.png";
        CharacterScene = GD.Load<PackedScene>("res://character/EnemyCharacter/Armon.tscn");
        SkillIDs = [SkillID.ArmonAttack, SkillID.ArmonSpecial];
        // 第三轮护盾随机施加脆弱，由另外两名队友利用；不随机起始到后段。
        IntentionCycle = [random, SkillID.ArmonSpecial, SkillID.ArmonSurvive];
        RandomizeIntentionCycleStart = true;
        PassiveName = global::Armon.PassiveNameText;
        PassiveDescription = global::Armon.UpdatedPassiveDescriptionText;
    }
}

public partial class ArmonAttack : Skill
{
    private int BaseDamage => V(nameof(BaseDamage));

    public override SkillTypes SkillType => SkillTypes.Attack;

    public override string SkillName { get; set; } = "矩阵斩击";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            AttackStep(BaseDamage, times: V("HitCount"), multiplier: V("Multiplier"))
        );
    }
}

public partial class ArmonSurvive : Skill
{
    private int BaseBlock => V(nameof(BaseBlock));

    public override SkillTypes SkillType => SkillTypes.Survive;

    public override string SkillName { get; set; } = "矩阵护盾";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            BlockStep(baseBlock: BaseBlock, multiplier: V("Multiplier")),
            ModifyPropertyStep(PropertyType.Power, V("power"), TargetReference.Self),
            ApplyBuffHostile(Buff.BuffName.Frail, V("FrailStacks"), HostileTargetReference.RandomPreview)
        );
    }
}

public partial class ArmonSpecial : Skill
{
    public override SkillTypes SkillType => SkillTypes.Special;

    public override string SkillName { get; set; } = "矩阵过载";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            AttackStep(
                baseDamage: V("BaseDamage"),
                multiplier: V("Multiplier"),
                times: V("HitCount")
            )
        );
    }
}
