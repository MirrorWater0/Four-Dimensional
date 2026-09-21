using System;
using Godot;

public partial class WarThrall : SummonCharacter
{
    internal const int MaxLifeStat = 16;
    internal const int PowerStat = 0;
    internal const int BasePowerContributionStat = 0;
    internal const int SurvivabilityStat = 0;

    public override string CharacterName { get; set; } = "战仆";

    private static int GetEffectiveMaxLife()
    {
        int maxLife = EnemyTuning.TryGetInt(
            nameof(WarThrall),
            nameof(EnemyRegedit.MaxLife),
            out int tunedMaxLife
        )
            ? tunedMaxLife
            : MaxLifeStat;
        return Math.Max(1, maxLife);
    }

    public static string GetPassiveDescription()
    {
        var attack = Skill.GetSkill(SkillID.WarThrallAttack);
        attack.SetPreviewStats(PowerStat, SurvivabilityStat, 1, isPlayer: false);
        attack.UpdateDescription();
        return BuildPassiveDescription(
            maxLife: MaxLifeStat,
            power: PowerStat,
            survivability: SurvivabilityStat,
            attack
        );
    }

    public override void Initialize()
    {
        PassiveName = "召唤物";
        PassiveDescription = GetPassiveDescription();
        Skills = [Skill.GetSkill(SkillID.WarThrallAttack)];
        SetBaseCombatStatContributions(BasePowerContributionStat, SurvivabilityStat);
        SetCombatStats(PowerStat, SurvivabilityStat, GetEffectiveMaxLife());
        base.Initialize();
    }

    public override bool RefreshTuning()
    {
        int newMaxLife = GetEffectiveMaxLife();
        if (newMaxLife == BattleMaxLife)
            return false;

        int oldMaxLife = Math.Max(1, BattleMaxLife);
        int missingLife = Math.Max(0, oldMaxLife - Life);
        bool wasFullLife = Life >= oldMaxLife;

        SetCombatStats(BattlePower, BattleSurvivability, newMaxLife);
        Life = wasFullLife ? newMaxLife : Math.Clamp(newMaxLife - missingLife, 0, newMaxLife);
        SyncLifeBarsToCurrent(syncBufferValue: true);
        InvalidateHoverTooltipCache();
        return true;
    }
}

public partial class WarThrallAttack : Skill
{
    private const int BaseDamage = 7;
    private const int SelfPowerGain = 1;

    public override SkillTypes SkillType => SkillTypes.Attack;

    public override string SkillName { get; set; } = "冲锋陷阵";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            AttackStep(baseDamage: BaseDamage),
            ModifyPropertyStep(PropertyType.Power, SelfPowerGain)
        );
    }
}
