using System;
using System.Linq;
using System.Threading.Tasks;
using Godot;

public partial class Combo : Skill
{
    public Combo()
        : base(Skill.SkillTypes.Attack)
    {
        Description = "发动连续攻击，每消耗1点能量可额外发动一次攻击，每次攻击伤害递增。";
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
