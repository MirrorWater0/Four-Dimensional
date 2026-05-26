using System;
using System.Linq;
using Godot;

public partial class Armon : EnemyCharacter
{
    private const int FirstTurnEndBlockMultiplier = 2;

    private bool _grantedFirstTurnEndBlock;

    public const string PassiveNameText = "矩阵核心";

    public static string UpdatedPassiveDescriptionText =>
        $"第一次回合结束时：全阵获得{FirstTurnEndBlockMultiplier}x（生存）点格挡。";

    public override string CharacterName { get; set; } = "Armon";

    public override void Initialize()
    {
        base.Initialize();
        PassiveName = PassiveNameText;
        PassiveDescription = UpdatedPassiveDescriptionText;
        _grantedFirstTurnEndBlock = false;
    }

    public override void OnTurnEnd()
    {
        if (!_grantedFirstTurnEndBlock)
        {
            _grantedFirstTurnEndBlock = true;
            GrantFormationBlock();
        }

        base.OnTurnEnd();
    }

    private void GrantFormationBlock()
    {
        if (BattleNode == null)
            return;

        using var _ = BeginEffectSource("被动");

        int block = Math.Clamp(BattleSurvivability * FirstTurnEndBlockMultiplier, 0, 999);
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

        MaxLife = 74;
        Power = 20;
        Survivability = 14;
        Speed = 7;
        SkillIDs = [SkillID.ArmonAttack, SkillID.ArmonSurvive, SkillID.ArmonSpecial];

        PassiveName = global::Armon.PassiveNameText;
        PassiveDescription = global::Armon.UpdatedPassiveDescriptionText;
    }
}

public partial class ArmonAttack : Skill
{
    private const int BaseDamage = 0;

    public ArmonAttack()
        : base(SkillTypes.Attack)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "矩阵斩击";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            AttackStep(BaseDamage),
            BlockStep(target: TargetReference.Previous),
            BlockStep(target: TargetReference.Next)
        );
    }
}

public partial class ArmonSurvive : Skill
{
    private const int BaseBlock = 0;
    private const int EnergyGain = 2;

    public ArmonSurvive()
        : base(SkillTypes.Survive)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "矩阵护盾";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            BlockStep(baseBlock: BaseBlock, multiplier: 2),
            AddStatusCardsToDrawPileStep(SkillID.DazeStatus, 1, HostileTargetReference.All),
            EnergyStep(EnergyGain)
        );
    }
}

public partial class ArmonSpecial : Skill
{
    private const int PowerGainPerEnergy = 2;
    private const int SurvivabilityGainPerEnergy = 2;

    public ArmonSpecial()
        : base(SkillTypes.Special)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "矩阵过载";
    public override int EnergyCost => XEnergyCost;

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            AttackStep(baseDamage: 0),
            EnergyTimesWhileStep(
                paidEnergyPerLoop: 2,
                loopSteps: new[]
                {
                    ModifyPropertyStep(PropertyType.Power, PowerGainPerEnergy, TargetReference.All),
                    ModifyPropertyStep(
                        PropertyType.Survivability,
                        SurvivabilityGainPerEnergy,
                        TargetReference.All
                    ),
                }
            )
        );
    }
}
