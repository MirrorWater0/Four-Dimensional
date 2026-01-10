using Godot;
using System;
using System.Threading.Tasks;

public partial class SacredOnslaught : Skill
{
    public SacredOnslaught(Charater owner) : base(SkillTypes.Attack, owner)
    {
        OwnerCharater = owner;
        Description = "对最多4个目标发动攻击，每个目标受到2次基础伤害（40%+战斗力）的打击。";
    }

    public override string SkillName { get; set; } = "圣域冲击";

    public async override Task Effect()
    {
        await base.Effect();
        for (int i = 0; i < Math.Min(4 ,Chosetarget1().Length); i++)
        {
            Attack3(0.4f,Chosetarget1()[i],2);
        }
        await Task.Delay(400);
        OwnerCharater.EndAction();
    }
}
