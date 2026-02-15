using System;
using System.Threading.Tasks;
using Godot;

public partial class KasiyaSpecialSkill : Node { }

public class TerminateLight : Skill
{
    int BasisDamage = 10;
    int UsedTimes = 1;
    int EnergyCost = 2;
    int addPower = 5;

    public TerminateLight()
        : base(SkillTypes.Special)
    {
        Description =
            $@"受到2倍力量加成{BasisDamage + OwnerCharater?.BattlePower}。
        可用{UsedTimes}次：若能量值大于等于{EnergyCost},则增加{addPower}层力量,并消耗{EnergyCost}点能量。";
    }

    public override string SkillName { get; set; } = "终末之光";

    public override async Task Effect()
    {
        await base.Effect();
        await Attack1(BasisDamage + 2 * OwnerCharater.BattlePower);
        if (UsedTimes > 0 && OwnerCharater.Energy >= EnergyCost)
        {
            IncreaseProperties(OwnerCharater, PropertyType.Power, addPower);
            OwnerCharater.UpdataEnergy(-EnergyCost);
            UsedTimes--;
        }
    }
}
