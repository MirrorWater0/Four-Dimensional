using Godot;

public partial class Nightingale : PlayerCharacter
{
    private const int PassiveUpgradeZeroCostDamageBonus = 3;

    public const string PassiveNameText = "夜光";
    public static string PassiveDescriptionText =>
        I18n.Format(
            "character.nightingale.passive.turn_end_attack_description",
            "回合开始时：获得1次{attack}。",
            ("attack", Buff.GetBuffDisplayName(Buff.BuffName.TemporaryAttackCount))
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

        if (State == CharacterState.Dying || BattleNode == null)
            return;

        using var _ = BeginEffectSource(PassiveNameText);
        AttackCountBuff.Modify(this, 1, temporary: true, source: this);
    }

    public override int GetSkillAttackDamageBonus(Skill skill)
    {
        if (!HasPassiveTalentUpgrade() || skill == null || skill.CardEnergyCost != 0)
            return 0;

        return PassiveUpgradeZeroCostDamageBonus;
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
        LifeMax = 36,
        Power = 4,
        Survivability = 3,
        CharacterScenePath = "res://character/PlayerCharacter/Nightingale/Nightingale.tscn",
        PortaitPath = "res://asset/PlayerCharater/Nightingale/NightingalePortrait.png",
        TakenSkills = [SkillID.BasicAttack, SkillID.BasicDefense, SkillID.NightingaleBasicSpecial],
    };
}
