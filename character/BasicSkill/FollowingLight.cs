using Godot;
using System;
using System.Linq;
using System.Threading.Tasks;

public partial class FollowingLight : Skill
{
    private const int Damage = 10;

    public FollowingLight(Character owner):base(Skill.SkillTypes.Attack)
    {
        UpdateDescription();
    }
    public override string SkillName { set; get; } = "流光日影";

    public async override Task Effect()
    {
        await base.Effect();
        await Attack1(Damage);
        Character[] targets = (OwnerCharater.IsPlayer) switch
        {
            true => OwnerCharater.BattleNode.EnemiesList.Cast<Character>().ToArray(), false => OwnerCharater.BattleNode.PlayersList.Cast<Character>().ToArray(),
        };
        var MaxNum = targets.Max(x => x.PositionIndex);
        var target = targets.Where(x => x.PositionIndex == MaxNum).ToArray();

        await Task.Delay(1000);
    }

    public override void UpdateDescription()
    {
        SetDescriptionText($"对目标发动攻击，造成{Damage}点伤害。");
    }
}
