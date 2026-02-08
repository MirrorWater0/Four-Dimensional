using System;
using System.Linq;
using System.Threading.Tasks;
using Godot;

public partial class Combo : Skill
{
    public Combo()
        : base(Skill.SkillTypes.Special)
    {
        Description = "发动连续攻击，每消耗1点能量可额外发动一次攻击，每次攻击造成i+战斗力的伤害（i为攻击次数，从0开始递增）。";
    }

    public override string SkillName { set; get; } = "回响时刻";

    public override async Task Effect()
    {
        await base.Effect();

        int i = 0;
        do
        {
            await Attack1(i);
            if (OwnerCharater.Energy > 0)
                OwnerCharater.UpdataEnergy(-1);
        } while (OwnerCharater.Energy > 0);

    }
}
