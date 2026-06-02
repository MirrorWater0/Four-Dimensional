using System;
using System.Threading.Tasks;
using Godot;

public partial class Kasiya : PlayerCharacter
{
    private const int PassiveAttackBaseHeal = -4;
    private const int PassiveUpgradeSpecialPowerGain = 1;

    public const string PassiveNameText = "战意";
    public static string PassiveDescriptionText =>
        I18n.Format(
            "character.kasiya.passive.description",
            "当其他队友使用攻击技能：回复{heal}点基础生命。",
            ("heal", PassiveAttackBaseHeal)
        );

    Label label => field ??= GetNode<Label>("Label");
    public override PackedScene CharaterScene { get; set; } = StartInterface._Kasiya;
    public override string CharacterName { get; set; } = "Kasiya";

    public override void Initialize()
    {
        base.Initialize();
        PassiveName = PassiveNameText;
        PassiveDescription = TalentTree.AppendPassiveUpgradeDescription(
            CharacterKey,
            PassiveDescriptionText,
            HasPassiveTalentUpgrade()
        );

        BattleNode.UsedSkills.ItemAdded += skill => TriggerPassive(skill);
    }

    public override async void Passive(Skill skill)
    {
        if (State == CharacterState.Dying)
            return;
        using var _ = BeginEffectSource("被动");
        if (
            skill?.OwnerCharater == null
            || skill.OwnerCharater == this
            || !skill.OwnerCharater.IsPlayer
        )
            return;

        if (skill.SkillType == Skill.SkillTypes.Attack)
        {
            Recover(PassiveAttackBaseHeal, source: this);
            return;
        }

        if (skill.SkillType == Skill.SkillTypes.Special && HasPassiveTalentUpgrade())
            await IncreaseProperties(PropertyType.Power, PassiveUpgradeSpecialPowerGain, this);
    }
}

public partial class PlayerCharacterRegistry
{
    public PlayerInfoStructure Kasiya = new PlayerInfoStructure()
    {
        CharacterName = I18n.Tr("character.kasiya.name", "Kasiya"),
        PassiveName = I18n.Tr("character.kasiya.passive.name", global::Kasiya.PassiveNameText),
        PassiveDescription = global::Kasiya.PassiveDescriptionText,
        LifeMax = 25,
        Power = 5,
        Survivability = 5,
        Speed = 6,
        CharacterScenePath = "res://character/PlayerCharacter/Kasiya/kasiya.tscn",
        PortaitPath = "res://asset/PlayerCharater/Kasiya/KasiyaPortrait.png",
        TakenSkills = [SkillID.BasicAttack, SkillID.BasicDefense, SkillID.BasicSpecial],
    };
}
