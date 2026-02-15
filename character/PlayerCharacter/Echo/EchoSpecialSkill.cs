using System;
using System.Threading.Tasks;
using Godot;

public partial class EchoSpecialSkill : Node { }

public class EchonicResonance : Skill
{
    int cost = 1;
    int power = 1;

    public EchonicResonance()
        : base(SkillTypes.Special)
    {
        Description = "发动回响共鸣，对所有敌人造成（5+战斗力）的伤害。";
    }

    public override string SkillName { get; set; } = "回响共鸣";

    public override async Task Effect()
    {
        await base.Effect();

        do
        {
            await Attack1(OwnerCharater.BattlePower);
            IncreaseProperties(OwnerCharater, PropertyType.Power, power);
            if (OwnerCharater.Energy > 0)
                OwnerCharater.UpdataEnergy(-cost);
        } while (OwnerCharater.Energy > 0);
    }
}
