using Godot;
using System;
using System.Threading.Tasks;

public partial class SacredOnslaught : Skill
{
    public SacredOnslaught() : base(SkillTypes.Special)
    {
        Description = "对最多4个目标发动攻击，每个目标受到2次基础伤害（40%+战斗力）的打击。";
    }

    public override string SkillName { get; set; } = "圣域冲击";

    public async override Task Effect()
    {
        await base.Effect();
        Character[] targets = Chosetarget1();
        for (int i = 0; i < Math.Min(4, targets.Length); i++)
        {
            Attack3(0.4f, targets[i], 2);
        }
        await Task.Delay(400);
        OwnerCharater.EndAction();
    }
}
