using Godot;

public partial class Nightingale : PlayerCharacter
{
    public const string PassiveNameText = "夜光";
    public static string PassiveDescriptionText => I18n.Format(
        "character.nightingale.passive.description",
        "回合开始时：在手牌中加入1张{blade}。",
        ("blade", I18n.Tr("skill.blade.name", "利刃"))
    );

    public override PackedScene CharaterScene { get; set; } = StartInterface._Nightingale;
    public override string CharacterName { get; set; } = "Nightingale";

    public override void Initialize()
    {
        base.Initialize();
        PassiveName = PassiveNameText;
        UpdatePassiveDescription();
    }

    public override void OnTurnStart()
    {
        base.OnTurnStart();
        if (State == CharacterState.Dying || BattleNode == null || !GodotObject.IsInstanceValid(BattleNode))
            return;

        using var source = BeginEffectSource("被动");
        BattleNode.AddPlayerBattleStatusCardsToHand(this, SkillID.Blade, 1, source: this);
    }

    private void UpdatePassiveDescription()
    {
        PassiveDescription = TalentTree.AppendPassiveUpgradeDescription(
            CharacterKey,
            PassiveDescriptionText,
            HasPassiveTalentUpgrade()
        );
        InvalidateSkillTooltipCache();
    }
}

public partial class PlayerCharacterRegistry
{
    public PlayerInfoStructure Nightingale = new PlayerInfoStructure()
    {
        CharacterName = I18n.Tr("character.nightingale.name", "Nightingale"),
        PassiveName = I18n.Tr(
            "character.nightingale.passive.name",
            global::Nightingale.PassiveNameText
        ),
        PassiveDescription = global::Nightingale.PassiveDescriptionText,
        LifeMax = 41,
        Power = 4,
        Survivability = 2,
        CharacterScenePath = "res://character/PlayerCharacter/Nightingale/Nightingale.tscn",
        PortaitPath = "res://asset/PlayerCharater/Nightingale/NightingalePortrait.png",
        // 初始牌组：重复填写同一 SkillID 可配置多张同名卡。
        GainedSkills =
        [
            SkillID.BasicAttack,
            SkillID.BasicDefense,
            SkillID.BasicDefense,
            SkillID.NightingaleBasicSpecial,
        ],
        TakenSkills = [SkillID.BasicAttack, SkillID.BasicDefense, SkillID.NightingaleBasicSpecial],
    };
}
