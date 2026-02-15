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
        var text =
            $"对前方目标造成{Math.Clamp(BaseDamage + OwnerPower, 0, 9999)}点伤害，并获得{DamageImmuneStacks}层{Buff.BuffName.DamageImmune.GetDescription()}。";
        SetDescriptionText(text);
    }
}

public partial class Smite : Skill
{
    private const int BaseDamage = 3;
    private const int SurvivalDown = 2;

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
            await DescendingProperties(targets[0], PropertyType.Survivalibility, SurvivalDown);
            await Attack1(BaseDamage + OwnerPower);
        }
    }

    public override void UpdateDescription()
    {
        var text =
            $"降低目标{SurvivalDown}点战斗生存能力，然后造成{Math.Clamp(BaseDamage + OwnerPower, 0, 9999)}点伤害。";
        SetDescriptionText(text);
    }
}

public partial class Charge : Skill
{
    private const int BaseDamage = 10;

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
        int damage = Math.Clamp(BaseDamage + OwnerPower, 0, 9999);
        SetDescriptionText($"发动攻击，造成{damage}点伤害，并获得{damage}点格挡。");
    }
}
