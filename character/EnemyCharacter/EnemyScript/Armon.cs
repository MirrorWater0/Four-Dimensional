using System;
using System.Linq;
using Godot;

public partial class Armon : EnemyCharacter
{
    private const int TurnEndBaseBlock = 4;

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
        PType = EnemyPositionType.FrontRow;
        PortaitPath = "res://asset/EnemyCharater/Armon.png";
        CharacterScene = GD.Load<PackedScene>("res://character/EnemyCharacter/Armon.tscn");

        MaxLife = 65;
        Power = 0;
        Survivability = 0;
        BasePowerContribution = 0;
        BaseSurvivabilityContribution = 0;
        SkillIDs = [SkillID.ArmonAttack, SkillID.ArmonSpecial];

        PassiveName = global::Armon.PassiveNameText;
        PassiveDescription = global::Armon.UpdatedPassiveDescriptionText;
    }
}

public partial class ArmonAttack : Skill
{
    private const int BaseDamage = 7;

    public override SkillTypes SkillType => SkillTypes.Attack;

    public override string SkillName { get; set; } = "矩阵斩击";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(this, AttackStep(BaseDamage, times: 2));
    }
}

public partial class ArmonSurvive : Skill
{
    private const int BaseBlock = 10;

    public override SkillTypes SkillType => SkillTypes.Survive;

    public override string SkillName { get; set; } = "矩阵护盾";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            BlockStep(baseBlock: BaseBlock),
            AddCardsStep(
                SkillID.DazeStatus,
                V("DazeCount", 1),
                BattleCardPileTarget.DiscardPileCards
            )
        );
    }
}

public partial class ArmonSpecial : Skill
{
    private const int OverloadTimes = 3;
    private const int PowerGainPerLoop = 2;

    public override SkillTypes SkillType => SkillTypes.Special;

    public override string SkillName { get; set; } = "矩阵过载";
    public override int EnergyCost => Cost(0);

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            AttackStep(baseDamage: V("BaseDamage", 9)),
            ModifyPropertyStep(PropertyType.Power, PowerGainPerLoop, TargetReference.Self)
        );
    }
}
