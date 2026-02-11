using System;
using System.Threading.Tasks;
using Godot;

public partial class EchoSpecialSkill : Node { }

public class EchonicResonance : Skill
{
    public EchonicResonance()
        : base(SkillTypes.Special)
    {
        Description = "发动回响共鸣，对所有敌人造成（5+战斗力）的伤害。";
    }

    public override string SkillName { get; set; } = "回响共鸣";

    public override async Task Effect()
    {
        await base.Effect();
        Character[] targets = Chosetarget1();
        foreach (var target in targets)
        {
            if (target.State == Character.CharacterState.Normal)
            {
                _ = Attack3(5, target, 1);
            }
        }
        await Task.Delay(200);
    }
}
