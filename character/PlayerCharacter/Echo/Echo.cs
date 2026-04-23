using System;
using Godot;

public partial class Echo : PlayerCharacter
{
    private const int PassiveSurvivePowerGain = 2;
    private const int PassiveNonSurviveEnergyGain = 1;

    public const string PassiveNameText = "余响";
    public static string PassiveDescriptionText =>
        $"使用生存技能时：获得{PassiveSurvivePowerGain}点力量。\n"
        + $"使用非生存技能时：获得{PassiveNonSurviveEnergyGain}点能量。";

    public override PackedScene CharaterScene { get; set; } = StartInterface._Echo;
    Label label => field ??= GetNode<Label>("Label");
    public override string CharacterName { get; set; } = "Echo";

    public override void _Ready()
    {
        base._Ready();
        label.Text = PositionIndex.ToString();
    }

    public override void Initialize()
    {
        base.Initialize();
        PassiveName = PassiveNameText;
        PassiveDescription = PassiveDescriptionText;
        BattleNode.UsedSkills.ItemAdded += skill => TriggerPassive(skill);
    }

    public override void StartAction()
    {
        base.StartAction();
    }

    public override void EndAction()
    {
        base.EndAction();
    }

    public override async void Passive(Skill skill)
    {
        using var _ = BeginEffectSource("被动");
        if (skill.OwnerCharater != this)
            return;

        if (skill.SkillType != Skill.SkillTypes.Survive)
        {
            UpdataEnergy(PassiveNonSurviveEnergyGain, this);
            return;
        }

        await IncreaseProperties(PropertyType.Power, PassiveSurvivePowerGain, this);
    }
}
