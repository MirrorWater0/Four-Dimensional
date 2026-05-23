using System;
using System.Linq;
using Godot;

public partial class Mariya : PlayerCharacter
{
    private const int PassiveHealBase = 8;
    private const int PassiveUpgradeHealBonus = 8;

    public const string PassiveNameText = "治愈";
    public static string PassiveDescriptionText =>
        I18n.Format(
            "character.mariya.passive.description",
            "回合结束时：回复最低生命队友{heal}点基础生命。",
            ("heal", PassiveHealBase)
        );

    public override PackedScene CharaterScene { get; set; } = StartInterface._Mariya;
    public override string CharacterName { get; set; } = "Mariya";

    public override void Initialize()
    {
        base.Initialize();
        PassiveName = PassiveNameText;
        PassiveDescription = PassiveDescriptionText;
    }

    public override void OnTurnEnd()
    {
        TryHealLowestAlly();
        base.OnTurnEnd();
    }

    private void TryHealLowestAlly()
    {
        if (BattleNode == null)
            return;

        using var _ = BeginEffectSource("被动");

        var allies = IsPlayer
            ? BattleNode.PlayersList.Cast<Character>()
            : BattleNode.EnemiesList.Cast<Character>();

        var target = allies
            .Where(x => x.Life < x.BattleMaxLife && x.State == CharacterState.Normal)
            .OrderBy(x => x.Life)
            .FirstOrDefault();

        if (target == null)
            return;

        int heal = PassiveHealBase + (HasPassiveTalentUpgrade() ? PassiveUpgradeHealBonus : 0);
        target.Recover(heal, source: this);
    }
}

public partial class PlayerCharacterRegistry
{
    public PlayerInfoStructure Mariya = new PlayerInfoStructure()
    {
        CharacterName = I18n.Tr("character.mariya.name", "玛瑞娅"),
        PassiveName = I18n.Tr("character.mariya.passive.name", global::Mariya.PassiveNameText),
        PassiveDescription = global::Mariya.PassiveDescriptionText,
        LifeMax = 22,
        Power = 4,
        Survivability = 4,
        Speed = 7,
        CharacterScenePath = "res://character/PlayerCharacter/Mariya/Mariya.tscn",
        PortaitPath = "res://asset/PlayerCharater/Mariya/MariyaPortrait.png",
        TakenSkills = [SkillID.BasicAttack, SkillID.BasicDefense, SkillID.BasicSpecial],
    };
}
