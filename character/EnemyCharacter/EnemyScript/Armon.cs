using System;
using System.Linq;
using Godot;

public partial class Armon : EnemyCharacter
{
    private bool _firstTurnPassiveTriggered;

    public override string CharacterName { get; set; } = "Armon";

    protected override void OnTurnStart()
    {
        base.OnTurnStart();
        TriggerFirstTurnPassive();
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

    private void TriggerFirstTurnPassive()
    {
        if (_firstTurnPassiveTriggered || BattleNode == null)
            return;
        _firstTurnPassiveTriggered = true;

        var allies = IsPlayer
            ? BattleNode.PlayersList.Cast<Character>()
            : BattleNode.EnemiesList.Cast<Character>();

        bool revivedAny = false;
        foreach (var ally in allies.Where(x => x != null && x != this))
        {
            if (ally.State != CharacterState.Dying)
                continue;

            ally.Recover(ally.BattleMaxLife / 2, rebirth: true);
            revivedAny = true;
        }

        if (!revivedAny)
        {
            UpdataBlock(Math.Clamp(BattleSurvivability, 0, 999));
        }
    }
}

public partial class ArmonAttack : Skill
{
    private const int BaseDamage = 10;
    private const int SelfPowerGain = 5;

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
            ModifyPropertyStep(PropertyType.Power, SelfPowerGain),
            RelativeAllyBlockStep(-1),
            RelativeAllyBlockStep(1)
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
            SelfBlockStep(BaseBlock),
            ModifyPropertyStep(PropertyType.Survivability, SelfSurvivabilityGain),
            EnergyStep(EnergyGain)
        );
    }
}

public partial class ArmonSpecial : Skill
{
    private const int BaseBlock = 10;
    private const int PowerGainPerEnergy = 2;
    private const int SurvivabilityGainPerEnergy = 3;

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
            BlockStrikeAlignedTargetStep(BaseBlock),
            ConsumeAllEnergyGainPropertiesStep(PowerGainPerEnergy, SurvivabilityGainPerEnergy)
        );
    }
}

