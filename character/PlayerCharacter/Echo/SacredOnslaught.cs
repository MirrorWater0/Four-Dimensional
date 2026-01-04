using Godot;
using System;

public partial class SacredOnslaught : Skill
{
    public SacredOnslaught(Charater owner) : base(SkillTypes.Attack, owner)
    {
        OwnerCharater = owner;
    }

    public override string SkillName { get; set; } = "圣域冲击";

    public override void Effect()
    {
        base.Effect();
        for (int i = 0; i < Math.Min(4 ,Chosetarget1().Length); i++)
        {
            Attack3(0.4f,Chosetarget1()[i],2);
        }
        OwnerCharater.EndAction();
    }
}
