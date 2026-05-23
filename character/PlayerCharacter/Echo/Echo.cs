using Godot;

public partial class Echo : PlayerCharacter
{
    private const int PassiveSkillUseThreshold = 2;
    private const int PassiveEnergyGain = 1;
    private const int PassiveUpgradeFirstTurnEnergyGain = 1;
    private int _passiveSkillUseCount;
    private bool _hasGainedPassiveUpgradeFirstTurnEnergy;

    public const string PassiveNameText = "余响";
    public static string PassiveDescriptionText =>
        I18n.Format(
            "character.echo.passive.description",
            "每使用{threshold}张技能：获得{energy}点能量。",
            ("threshold", PassiveSkillUseThreshold),
            ("energy", PassiveEnergyGain)
        );

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
        _hasGainedPassiveUpgradeFirstTurnEnergy = false;
        PassiveName = PassiveNameText;
        UpdatePassiveDescription();
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

    public override void OnTurnStart()
    {
        base.OnTurnStart();
        if (_hasGainedPassiveUpgradeFirstTurnEnergy || !HasPassiveTalentUpgrade())
            return;

        _hasGainedPassiveUpgradeFirstTurnEnergy = true;
        using var _ = BeginEffectSource("被动强化");
        UpdataEnergy(PassiveUpgradeFirstTurnEnergyGain, this);
        UpdatePassiveDescription();
    }

    public override void Passive(Skill skill)
    {
        using var _ = BeginEffectSource("被动");
        if (skill?.OwnerCharater != this)
            return;

        _passiveSkillUseCount++;
        if (_passiveSkillUseCount < PassiveSkillUseThreshold)
        {
            UpdatePassiveDescription();
            return;
        }

        _passiveSkillUseCount = 0;
        UpdatePassiveDescription();
        UpdataEnergy(PassiveEnergyGain, this);
    }

    private void UpdatePassiveDescription()
    {
        PassiveDescription =
            PassiveDescriptionText
            + "\n"
            + I18n.Format(
                "character.passive.current_count",
                "当前计数：{current}/{max}",
                ("current", _passiveSkillUseCount),
                ("max", PassiveSkillUseThreshold)
            );
        if (HasPassiveTalentUpgrade())
            PassiveDescription +=
                "\n"
                + I18n.Tr(
                    "character.echo.passive.upgrade",
                    "被动强化：第一次回合开始时额外获得1点能量。"
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
        LifeMax = 26,
        Power = 5,
        Survivability = 6,
        Speed = 8,
        CharacterScenePath = "res://character/PlayerCharacter/Echo/Echo.tscn",
        PortaitPath = "res://asset/PlayerCharater/Echo/EchoPortrait.png",
        TakenSkills = [SkillID.BasicAttack, SkillID.BasicDefense, SkillID.BasicSpecial],
    };
}
