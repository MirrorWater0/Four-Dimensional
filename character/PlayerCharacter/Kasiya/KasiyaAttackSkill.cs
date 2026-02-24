using System;
using System.Threading.Tasks;
using Godot;

public partial class KasiyaAttackSkill { }

public partial class Determination : Skill
{
    private const int DamageImmuneStacks = 2;
    private const int BaseDamage = 6;

    public Determination()
        : base(SkillTypes.Attack)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "剑意已决";

    public override async Task Effect()
    {
        await base.Effect();
        await Attack1(BaseDamage + OwnerPower);
        HurtBuff.BuffAdd(Buff.BuffName.DamageImmune, OwnerCharater, DamageImmuneStacks);
    }

    public override void UpdateDescription()
    {
        int totalDamage = BaseDamage + OwnerPower;
        SetDescriptionLines(
            $"造成{BasePlusXWithBattleTotal(BaseDamage, totalDamage, StatX.Power)}点伤害。",
            $"获得{DamageImmuneStacks}层{Buff.BuffName.DamageImmune.GetDescription()}。"
        );
    }
}

public partial class Smite : Skill
{
    private const int BaseDamage = 15;
    private const int SurvivalDown = 5;
    int times = 2;

    public Smite()
        : base(Skill.SkillTypes.Attack)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "绝域剑杀";

    public override async Task Effect()
    {
        await base.Effect();
        Character[] targets = Chosetarget1();
        if (targets.Length > 0)
        {
            DescendingProperties(targets[0], PropertyType.Survivalibility, SurvivalDown);
            await Attack1(BaseDamage + OwnerPower);
        }
        if (times > 0)
        {
            times--;
            DescendingProperties(targets[0], PropertyType.Survivalibility, SurvivalDown);
        }
    }

    public override void UpdateDescription()
    {
        int totalDamage = BaseDamage + OwnerPower;
        SetDescriptionLines(
            $"降低目标{SurvivalDown}点{GetColoredPropertyLabel(PropertyType.Survivalibility)}。",
            $"造成{BasePlusXWithBattleTotal(BaseDamage, totalDamage, StatX.Power)}点伤害。",
            $"额外触发降低目标{SurvivalDown}点{GetColoredPropertyLabel(PropertyType.Survivalibility)}，触发次数：{times}。"
        );
    }
}

public partial class Charge : Skill
{
    private const int BaseDamage = 6;

    public Charge()
        : base(Skill.SkillTypes.Attack)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "无畏冲锋";

    public override async Task Effect()
    {
        await base.Effect();
        int damage = BaseDamage + OwnerPower;
        await Attack1(damage);
        OwnerCharater.UpdataBlock(damage);
    }

    public override void UpdateDescription()
    {
        int total = BaseDamage + OwnerPower;
        string damageText = BasePlusXWithBattleTotal(BaseDamage, total, StatX.Power);
        SetDescriptionLines($"造成{damageText}点伤害。", $"获得{damageText}点格挡。");
    }
}

public partial class Vower : Skill
{
    private const int BaseDamage = 8;
    int times = 2;

    public Vower()
        : base(Skill.SkillTypes.Attack)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "誓约者";

    public override async Task Effect()
    {
        await base.Effect();
        int damage = BaseDamage + OwnerPower;
        await Attack1(damage);
        if (times > 0)
        {
            times--;
            await Carry(GetAllyByRelative(-1), 1);
        }
    }

    public override void UpdateDescription()
    {
        int total = BaseDamage + OwnerPower;
        string damageText = BasePlusXWithBattleTotal(BaseDamage, total, StatX.Power);
        SetDescriptionLines(
            $"造成{damageText}点伤害。",
            $"连携上一位队友(生存技能)，剩余触发次数：{times}。"
        );
    }
}
