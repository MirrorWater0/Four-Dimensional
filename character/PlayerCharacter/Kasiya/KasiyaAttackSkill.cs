using System;
using System.Threading.Tasks;
using Godot;

public partial class KasiyaAttackSkill { }

public partial class Determination : Skill
{
    int num = 2;
    int BasisDamage = 6;

    public Determination()
        : base(SkillTypes.Attack)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "剑意已决";

    public override async Task Effect()
    {
        await base.Effect();
        await Attack1(6 + OwnerCharater.BattlePower);
        HurtBuff.BuffAdd(Buff.BuffName.DamageImmune, OwnerCharater, 2);
    }

    public override void UpdateDescription()
    {
        var text =
            $"造成{Math.Clamp(BasisDamage + (OwnerCharater?.BattlePower ?? 0), 0, 9999)}点的伤害，并获得{num}层{Buff.BuffName.DamageImmune.GetDescription()}。";
        Description = GlobalFunction.ColorizeNumbers(text);
    }
}

public partial class Smite : Skill
{
    int num = 3;
    int BasisDamage = 3;

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
            await DescendingProperties(targets[0], PropertyType.Survivalibility, 2);
            await Attack1(BasisDamage + OwnerCharater.BattlePower);
        }
    }

    public override void UpdateDescription()
    {
        var text =
            $"降低目标{num}点战斗生存能力，然后发动攻击，造成{BasisDamage + (OwnerCharater?.BattlePower ?? 0)}的伤害。";
        Description = GlobalFunction.ColorizeNumbers(text);
    }
}

public partial class Charge : Skill
{
    public Charge()
        : base(Skill.SkillTypes.Attack)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "无畏冲锋";

    public override async Task Effect()
    {
        await base.Effect();
        await Attack1(10 + OwnerCharater.BattlePower);
        OwnerCharater.UpdataBlock(10 + OwnerCharater.BattlePower);
    }

    public override void UpdateDescription()
    {
        Description = "发动攻击，造成10+战斗力的伤害，然后获得等于攻击伤害值的格挡。";
    }
}
