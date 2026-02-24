using System;
using System.Linq;
using System.Threading.Tasks;
using Godot;

public partial class Nightingale : PlayerCharacter
{
    public override PackedScene CharaterScene { get; set; } = StartInterface._Nightingale;
    public override string CharacterName { get; set; } = "Nightingale";

    public override void Initialize()
    {
        base.Initialize();
        BattleNode.EmitList.Add(Pursuit);
    }

    public async Task Pursuit(Character character)
    {
        if (character.IsPlayer && character != this && State != CharacterState.Dying)
        {
            await ToSignal(GetTree().CreateTimer(0.5f), "timeout");
            var skill = new Skill(Skill.SkillTypes.Attack) { OwnerCharater = this };
            await skill.Attack1(BattlePower);
        }
    }
}
