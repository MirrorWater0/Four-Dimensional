using Godot;

public partial class Echo : PlayerCharacter
{
    private const int PassiveEnergyGain = 1;
    private const int PassiveUpgradeTurnStartEnergyGain = 1;
    private const int PassiveUpgradeTurnStartTriggerLimit = 2;
    private int _passiveUpgradeTurnStartTriggerCount;

    public const string PassiveNameText = "余响";
    public static string PassiveDescriptionText =>
        I18n.Format(
            "character.echo.passive.description",
            "回合开始时：获得{energy}点能量。",
            ("energy", PassiveEnergyGain)
        );

    public override PackedScene CharaterScene { get; set; } = StartInterface._Echo;
    Label label => field ??= GetNode<Label>("Label");
    public override string CharacterName { get; set; } = "Echo";

    public override void Initialize()
    {
        base.Initialize();
        _passiveUpgradeTurnStartTriggerCount = 0;
        PassiveName = PassiveNameText;
        UpdatePassiveDescription();
    }

    public override void StartAction()
    {
        base.StartAction();
    }

    public override void EndAction()
    {
        base.EndAction();
    }

    public override void OnTurnStart()
    {
        base.OnTurnStart();
        using (BeginEffectSource("被动"))
        {
            UpdataEnergy(PassiveEnergyGain, this);
        }

        if (
            !HasPassiveTalentUpgrade()
            || _passiveUpgradeTurnStartTriggerCount >= PassiveUpgradeTurnStartTriggerLimit
        )
            return;

        _passiveUpgradeTurnStartTriggerCount++;
        using var _ = BeginEffectSource("被动强化");
        UpdataEnergy(PassiveUpgradeTurnStartEnergyGain, this);
        UpdatePassiveDescription();
    }

    private void UpdatePassiveDescription()
    {
        PassiveDescription = PassiveDescriptionText;
        if (HasPassiveTalentUpgrade())
            PassiveDescription +=
                "\n"
                + I18n.Tr(
                    "character.echo.passive.upgrade",
                    "被动强化：前2次回合开始时额外获得1点能量。"
                );
        InvalidateSkillTooltipCache();
    }
}

public partial class PlayerCharacterRegistry
{
    public PlayerInfoStructure Echo = new PlayerInfoStructure()
    {
        CharacterName = I18n.Tr("character.echo.name", "Echo"),
        PassiveName = I18n.Tr("character.echo.passive.name", global::Echo.PassiveNameText),
        PassiveDescription = global::Echo.PassiveDescriptionText,
        LifeMax = 41,
        Power = 3,
        Survivability = 4,
        Speed = 12,
        CharacterScenePath = "res://character/PlayerCharacter/Echo/Echo.tscn",
        PortaitPath = "res://asset/PlayerCharater/Echo/EchoPortrait.png",
        TakenSkills = [SkillID.BasicAttack, SkillID.BasicDefense, SkillID.EchoBasicSpecial],
    };
}
