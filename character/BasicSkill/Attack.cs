using Godot;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

public partial class Attack : Skill
{
    
    public Attack(Charater owner) : base(Skill.SkillTypes.Attack, owner)
    {
        Description = "对前方敌人发动二段连续攻击，每次造成基础伤害+战斗力的伤害。";
    }


    public override string SkillName { set; get; } = "流影二段";

    public async override void Effect()
    {
        base.Effect();
        
        Attack2(1);
        OwnerCharater.EndAction();
    }
}
