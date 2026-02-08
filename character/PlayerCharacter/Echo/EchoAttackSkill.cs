using System;
using System.Threading.Tasks;
using Godot;

public partial class SacredOnslaught : Skill
{
    public SacredOnslaught()
        : base(SkillTypes.Special)
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
            _ = Attack3(0.4f, targets[i], 2);
        }
        await Task.Delay(400);
    }
}
