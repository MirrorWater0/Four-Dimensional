using Godot;
using System;
using System.Threading.Tasks;

public partial class Kasiya : PlayerCharater
{
    Label label => field ??= GetNode<Label>("Label");
    public override PackedScene CharaterScene { get; set; } = ChoseCharater._Kasiya;
    public override string CharaterName { get; set; }="Kasiya";

    public override void Initialize()
    {
        base.Initialize();
        if(Istest) test();
        
        
        BattleNode.UsedSkills.ItemAdded += item => { Passive(item);};
    }

    public void test()
    {
        TakenSkills = new Skill[] { new ReNewedSpirit(this),new Determination(this),new Smite(this) };
        BattleLifemax = 50;
        BattlePower = 10;
        BattleSurvivability = 1700;
        Speed = 20;
    }

    public override void Passive(Skill skill)
    {
        if (skill.OwnerCharater == this & skill.SkillType == Skill.SkillTypes.Defence)
        {
            Recovery(500);
        }
    }
}



