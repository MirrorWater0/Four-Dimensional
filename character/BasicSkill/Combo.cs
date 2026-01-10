using System;
using System.Linq;
using System.Threading.Tasks;
using Godot;

public partial class Combo : Skill
{
    public Combo(Charater owner)
        : base(Skill.SkillTypes.Attack, owner)
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
            if (OwnerCharater.Energe > 0)
                OwnerCharater.UpdataEnerge(-1);
        } while (OwnerCharater.Energe > 0);

        OwnerCharater.EndAction();
    }
}
