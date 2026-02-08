using Godot;
using System;
using System.Linq;
using System.Threading.Tasks;

public partial class FollowingLight : Skill
{
    public FollowingLight(Character owner):base(Skill.SkillTypes.Attack)
    {
        Description = "对位置索引最大的敌人发动攻击，造成10+战斗力的伤害。";
    }
    public override string SkillName { set; get; } = "流光日影";

    public async override Task Effect()
    {
        await base.Effect();
        await Attack1(10);
        Character[] targets = (OwnerCharater.IsPlayer) switch
        {
            true => OwnerCharater.BattleNode.EnemiesList.Cast<Character>().ToArray(), false => OwnerCharater.BattleNode.PlayersList.Cast<Character>().ToArray(),
        };
        var MaxNum = targets.Max(x => x.PositionIndex);
        var target = targets.Where(x => x.PositionIndex == MaxNum).ToArray();

        await Task.Delay(1000);
    }
}
