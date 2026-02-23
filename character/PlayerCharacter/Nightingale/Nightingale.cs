using System;
using System.Threading.Tasks;
using Godot;

public partial class Nightingale : PlayerCharacter
{
    public async Task Pursuit(Character character)
    {
        if (character.IsPlayer)
        {
            var skill = new Skill(Skill.SkillTypes.Attack) { OwnerCharater = this };
            await skill.Attack1(BattlePower);
        }
    }
}
