using Godot;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

public partial class Attack : Skill
{
    
    public Attack(Charater owner) : base(Skill.SkillTypes.Attack, owner){}


    public override string SkillName { set; get; } = "流影二段";

    public async override void Effect()
    {
        base.Effect();
        
        Attack2(1);
        OwnerCharater.EndAction();
    }
}
