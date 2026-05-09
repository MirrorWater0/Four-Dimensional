using Godot;

public partial class Echo : PlayerCharacter
{
    private const int PassiveSkillUseThreshold = 2;
    private const int PassiveEnergyGain = 1;
    private int _passiveSkillUseCount;

    public const string PassiveNameText = "余响";
    public static string PassiveDescriptionText =>
        $"每使用{PassiveSkillUseThreshold}张技能：获得{PassiveEnergyGain}点能量。";

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
        _passiveSkillUseCount = 0;
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

    public override void Passive(Skill skill)
    {
        using var _ = BeginEffectSource("被动");
        if (skill?.OwnerCharater != this)
            return;

        _passiveSkillUseCount++;
        if (_passiveSkillUseCount < PassiveSkillUseThreshold)
            return;

        _passiveSkillUseCount = 0;
        UpdataEnergy(PassiveEnergyGain, this);
    }
}

public partial class PlayerCharacterRegistry
{
    public PlayerInfoStructure Echo = new PlayerInfoStructure()
    {
        CharacterName = "Echo",
        PassiveName = global::Echo.PassiveNameText,
        PassiveDescription = global::Echo.PassiveDescriptionText,
        LifeMax = 53,
        Power = 9,
        Survivability = 11,
        Speed = 10,
        CharacterScenePath = "res://character/PlayerCharacter/Echo/Echo.tscn",
        PortaitPath = "res://asset/PlayerCharater/Echo/EchoPortrait.png",
        TakenSkills = [SkillID.BasicAttack, SkillID.BasicDefense, SkillID.BasicSpecial],
    };
}
