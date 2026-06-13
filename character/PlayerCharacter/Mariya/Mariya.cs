using System;
using System.Linq;
using Godot;

public partial class Mariya : PlayerCharacter
{
    public const int BattleEndPassiveHeal = 3;

    public const string PassiveNameText = "治愈";
    public static string PassiveDescriptionText =>
        I18n.Format(
            "character.mariya.passive.description",
            "战斗结束时：我方全阵恢复{heal}点生命。",
            ("heal", BattleEndPassiveHeal)
        );

    public override PackedScene CharaterScene { get; set; } = StartInterface._Mariya;
    public override string CharacterName { get; set; } = "Mariya";

    public override void Initialize()
    {
        base.Initialize();
        PassiveName = PassiveNameText;
        PassiveDescription = TalentTree.AppendPassiveUpgradeDescription(
            CharacterKey,
            PassiveDescriptionText,
            HasPassiveTalentUpgrade()
        );
    }

    public void TriggerBattleEndPassive()
    {
        if (BattleNode == null || State != CharacterState.Normal)
            return;

        using var _ = BeginEffectSource("被动");
        foreach (
            var target in BattleNode
                .PlayersList.Where(x => x != null && !x.IsSummon)
                .Cast<Character>()
        )
        {
            target.Recover(BattleEndPassiveHeal, rebirth: true, source: this);
        }
    }
}

public partial class PlayerCharacterRegistry
{
    public PlayerInfoStructure Mariya = new PlayerInfoStructure()
    {
        CharacterName = I18n.Tr("character.mariya.name", "玛瑞娅"),
        PassiveName = I18n.Tr("character.mariya.passive.name", global::Mariya.PassiveNameText),
        PassiveDescription = global::Mariya.PassiveDescriptionText,
        LifeMax = 34,
        Power = 4,
        Survivability = 3,
        Speed = 12,
        CharacterScenePath = "res://character/PlayerCharacter/Mariya/Mariya.tscn",
        PortaitPath = "res://asset/PlayerCharater/Mariya/MariyaPortrait.png",
        TakenSkills = [SkillID.BasicAttack, SkillID.BasicDefense, SkillID.MariyaBasicSpecial],
    };
}
