using System;
using System.Linq;
using System.Threading.Tasks;
using Godot;

public partial class Nightingale : PlayerCharacter
{
    public const string PassiveNameText = "夜光";
    public static string PassiveDescriptionText =>
        $"队友结束回合时：追击一次：造成{PropertyType.Power.GetDescription()}点伤害。";

    public override PackedScene CharaterScene { get; set; } = StartInterface._Nightingale;
    public override string CharacterName { get; set; } = "Nightingale";

    public override void Initialize()
    {
        base.Initialize();
        PassiveName = PassiveNameText;
        PassiveDescription = PassiveDescriptionText;
        BattleNode.EmitList.Add(Pursuit);
    }

    public async Task Pursuit(Character character)
    {
        if (character.IsPlayer && character != this && State != CharacterState.Dying)
        {
            using var _ = BeginEffectSource("追击");
            await ToSignal(GetTree().CreateTimer(0.5f), "timeout");
            var skill = new Skill(Skill.SkillTypes.Attack) { OwnerCharater = this };
            await skill.Attack(BattlePower);
        }
    }
}
