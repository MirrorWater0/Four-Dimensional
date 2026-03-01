using System;
using System.Linq;
using System.Threading.Tasks;
using Godot;

public partial class Armon : EnemyCharacter
{
    private bool _firstTurnPassiveTriggered;

    public override string CharacterName { get; set; } = "Armon";

    public override void Initialize()
    {
        base.Initialize();
        BattleNode.StartEffectList.Add(StartPassive);
    }

    protected override void OnTurnStart()
    {
        base.OnTurnStart();
    }

    public override void EndAction()
    {
        base.EndAction();
        GrantFormationBlock();
    }

    private void GrantFormationBlock()
    {
        if (BattleNode == null)
            return;

        int block = Math.Clamp(BattleSurvivability, 0, 999);
        var allies = IsPlayer
            ? BattleNode.PlayersList.Cast<Character>()
            : BattleNode.EnemiesList.Cast<Character>();

        foreach (var ally in allies.Where(x => x != null && x.State == CharacterState.Normal))
        {
            ally.UpdataBlock(block);
        }
    }

    public async Task StartPassive()
    {
        for (int i = 0; i < BattleNode.EnemiesList.Count; i++)
        {
            BattleNode.EnemiesList[i].UpdataBlock(BattleSurvivability);
        }
    }
}

public partial class ArmonAttack : Skill
{
    private const int BaseDamage = 20;
    private const int SelfPowerGain = 5;
    private const int SelfSurvivabilityGain = 3;

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
            RelativeAllyBlockStep(-1),
            RelativeAllyBlockStep(1),
            ModifyPropertyStep(PropertyType.Power, SelfPowerGain),
            ModifyPropertyStep(PropertyType.Survivability, SelfSurvivabilityGain)
        );
    }
}

public partial class ArmonSurvive : Skill
{
    private const int BaseBlock = 10;
    private const int SelfSurvivabilityGain = 5;
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
            SelfBlockStep(BaseBlock, 2),
            ModifyPropertyStep(PropertyType.Survivability, SelfSurvivabilityGain),
            EnergyStep(EnergyGain)
        );
    }
}

public partial class ArmonSpecial : Skill
{
    private const int BaseBlock = 15;
    private const int PowerGainPerEnergy = 6;
    private const int SurvivabilityGainPerEnergy = 10;

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
            AttackPrimaryStep(BaseBlock + OwnerSurvivability),
            SelfBlockStep(BaseBlock),
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
