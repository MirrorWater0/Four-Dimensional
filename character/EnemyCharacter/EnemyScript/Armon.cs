using System;
using System.Linq;
using System.Threading.Tasks;
using Godot;

public partial class Armon : EnemyCharacter
{
    public const string PassiveNameText = "矩阵核心";
    public const string PassiveDescriptionText =
        "战斗开始时：全队获得等同于该角色生存的格挡。\n回合结束时：全队获得等同于该角色生存的格挡。";

    public override string CharacterName { get; set; } = "Armon";

    public override void Initialize()
    {
        base.Initialize();
        PassiveName = PassiveNameText;
        PassiveDescription = PassiveDescriptionText;
        BattleNode.StartEffectList.Add(StartPassive);
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

        int block = Math.Clamp(BattleSurvivability, 0, 999);
        var allies = BattleNode.GetTeamCharacters(IsPlayer, includeSummons: true);

        foreach (var ally in allies.Where(x => x != null && x.State == CharacterState.Normal))
        {
            ally.UpdataBlock(block, source: this);
        }
    }

    public Task StartPassive()
    {
        using var _ = BeginEffectSource("被动");
        var allies = BattleNode.GetTeamCharacters(IsPlayer, includeSummons: true).ToArray();
        for (int i = 0; i < allies.Length; i++)
        {
            allies[i].UpdataBlock(BattleSurvivability, source: this);
        }
        return Task.CompletedTask;
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

        MaxLife = 75;
        Power = 10;
        Survivability = 11;
        Speed = 9;
        SpecialIntentThreshold = 2;

        SkillIDs = [SkillID.ArmonAttack, SkillID.ArmonSurvive, SkillID.ArmonSpecial];

        PassiveName = global::Armon.PassiveNameText;
        PassiveDescription = global::Armon.PassiveDescriptionText;
    }
}

public partial class ArmonAttack : Skill
{
    private const int BaseDamage = 18;
    private const int SelfPowerGain = 3;
    private const int SelfSurvivabilityGain = 2;

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
            AttackPrimaryStep(BaseDamage),
            BlockStep(-1),
            BlockStep(1),
            ModifyPropertyStep(PropertyType.Power, SelfPowerGain),
            ModifyPropertyStep(PropertyType.Survivability, SelfSurvivabilityGain)
        );
    }
}

public partial class ArmonSurvive : Skill
{
    private const int BaseBlock = 0;
    private const int PowerGain = 5;
    private const int EnergyGain = 1;

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
            BlockStep(0, BaseBlock, 2),
            ModifyPropertyStep(PropertyType.Power, PowerGain),
            EnergyStep(EnergyGain)
        );
    }
}

public partial class ArmonSpecial : Skill
{
    private const int BaseBlock = 3;
    private const int PowerGainPerEnergy = 4;
    private const int SurvivabilityGainPerEnergy = 5;

    public ArmonSpecial()
        : base(SkillTypes.Special)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "矩阵过载";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            AttackPrimaryStep(BaseBlock),
            EnergyTimesWhileStep(
                energyCost: 2,
                loopSteps: new[]
                {
                    ModifyPropertyStep(PropertyType.Power, PowerGainPerEnergy),
                    ModifyPropertyStep(PropertyType.Survivability, SurvivabilityGainPerEnergy),
                }
            )
        );
    }
}

