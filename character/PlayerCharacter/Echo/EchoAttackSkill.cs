using System;
using System.Threading.Tasks;
using Godot;

public partial class SacredOnslaught : Skill
{
    int cost = 1;

    public SacredOnslaught()
        : base(SkillTypes.Attack)
    {
        Description = "对最多4个目标发动攻击，每个目标受到2次攻击，每次造成0.4+战斗力的伤害。";
    }

    public override string SkillName { get; set; } = "圣域冲击";

    public override async Task Effect()
    {
        await base.Effect();
        Character[] targets = Chosetarget1();
        for (int i = 0; i < Math.Min(4, targets.Length); i++)
        {
            _ = Attack3(OwnerCharater.BattlePower, targets[i], 1);
        }
        if (OwnerCharater.Energy > 0)
        {
            OwnerCharater.UpdataEnergy(-cost);
            for (int i = 0; i < Math.Min(4, targets.Length); i++)
            {
                _ = Attack3(OwnerCharater.BattlePower, targets[i], 1);
            }
        }
        await Task.Delay(400);
    }
}
