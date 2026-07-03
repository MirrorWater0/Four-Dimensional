using System;
using System.Linq;
using Godot;

public partial class Kasiya : PlayerCharacter
{
    private const int PassiveAttackBlock = 2;
    private const int PassiveUpgradeAllyBlock = 1;

    public const string PassiveNameText = "战意";
    public static string PassiveDescriptionText =>
        I18n.Format(
            "character.kasiya.passive.description",
            "当其他队友使用攻击技能：随机一个角色获得{block}点格挡。",
            ("block", PassiveAttackBlock)
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

    public override void Passive(Skill skill)
    {
        if (State == CharacterState.Dying)
            return;
        if (skill?.OwnerCharater == null || !skill.OwnerCharater.IsPlayer)
            return;

        using var _ = BeginEffectSource("被动");

        if (
            skill.OwnerCharater == this
            && skill.SkillType == Skill.SkillTypes.Attack
            && HasPassiveTalentUpgrade()
        )
        {
            ApplyPassiveUpgradeAllyBlock();
            return;
        }

        if (skill.OwnerCharater == this)
            return;

        if (skill.SkillType == Skill.SkillTypes.Attack)
            ApplyPassiveBlockToRandomAlly();
    }

    private void ApplyPassiveBlockToRandomAlly()
    {
        Character[] candidates =
            BattleNode
                ?.GetTeamCharacters(isPlayer: true, includeSummons: false)
                .Where(ally =>
                    ally != null
                    && GodotObject.IsInstanceValid(ally)
                    && ally.State != CharacterState.Dying
                )
                .ToArray() ?? Array.Empty<Character>();

        if (candidates.Length == 0)
            return;

        Random rng = BattleNode.BattleIntentionRandom ?? new Random();
        candidates[rng.Next(candidates.Length)].UpdataBlock(PassiveAttackBlock, source: this);
    }

    private void ApplyPassiveUpgradeAllyBlock()
    {
        Character[] allies =
            BattleNode
                ?.GetTeamCharacters(isPlayer: true, includeSummons: true)
                .Where(ally =>
                    ally != null
                    && ally != this
                    && GodotObject.IsInstanceValid(ally)
                    && ally.State != CharacterState.Dying
                )
                .ToArray() ?? Array.Empty<Character>();

        foreach (Character ally in allies)
            ally.UpdataBlock(PassiveUpgradeAllyBlock, source: this);
    }
}

public partial class PlayerCharacterRegistry
{
    public PlayerInfoStructure Kasiya = new PlayerInfoStructure()
    {
        CharacterName = I18n.Tr("character.kasiya.name", "Kasiya"),
        PassiveName = I18n.Tr("character.kasiya.passive.name", global::Kasiya.PassiveNameText),
        PassiveDescription = global::Kasiya.PassiveDescriptionText,
        LifeMax = 42,
        Power = 4,
        Survivability = 4,
        CharacterScenePath = "res://character/PlayerCharacter/Kasiya/kasiya.tscn",
        PortaitPath = "res://asset/PlayerCharater/Kasiya/KasiyaPortrait.png",
        TakenSkills = [SkillID.BasicAttack, SkillID.BasicDefense, SkillID.KasiyaBasicSpecial],
    };
}
