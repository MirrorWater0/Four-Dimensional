using Godot;
using System;
using System.Linq;
using System.Threading.Tasks;

public partial class FollowingLight : Skill
{
    public FollowingLight(Charater owner):base(Skill.SkillTypes.Attack,owner)
    {
        Description = "对最前方的敌人发动攻击，造成基础伤害+战斗力的伤害。";
    }
    public override string SkillName { set; get; } = "流光日影";

    public async override void Effect()
    {
        base.Effect();
        Attack1(1);
        Charater[] targets = (OwnerCharater.IsPlayer) switch
        {
            true => OwnerCharater.BattleNode.Players, false => OwnerCharater.BattleNode.Enemies,
        };
        var MaxNum = targets.Max(x => x.PositionIndex);
        var target = targets.Where(x => x.PositionIndex == MaxNum).ToArray();

        await Task.Delay(1000);
        OwnerCharater.EndAction();
    }
}
