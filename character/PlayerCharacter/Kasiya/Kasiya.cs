using System;
using System.Linq;
using System.Threading.Tasks;
using Godot;

public partial class Kasiya : PlayerCharacter
{
    private const int PassiveAttackBlock = 2;
    private const int PassiveUpgradePartyMaxLife = 1;
    private Action<Skill> _skillUsedHandler;

    public const string PassiveNameText = "战意";
    public static string PassiveDescriptionText =>
        I18n.Format(
            "character.kasiya.passive.description",
            "当任意己方角色打出攻击牌：随机一个己方角色获得{block}点格挡。",
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

        _skillUsedHandler ??= skill => TriggerPassive(skill);
        BattleNode.UsedSkills.ItemAdded -= _skillUsedHandler;
        BattleNode.UsedSkills.ItemAdded += _skillUsedHandler;

        if (HasPassiveTalentUpgrade())
            BattleNode.StartEffectList.Add(TriggerPassiveUpgradeAtBattleStartAsync);
    }

    public override void _ExitTree()
    {
        if (BattleNode != null && _skillUsedHandler != null)
            BattleNode.UsedSkills.ItemAdded -= _skillUsedHandler;

        base._ExitTree();
    }

    public override void Passive(Skill skill)
    {
        if (State == CharacterState.Dying)
            return;
        if (
            skill?.OwnerCharater == null
            || skill.OwnerCharater.IsPlayer != IsPlayer
            || skill.SkillType != Skill.SkillTypes.Attack
        )
            return;

        using var _ = BeginEffectSource("被动");

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

    private async Task TriggerPassiveUpgradeAtBattleStartAsync()
    {
        if (State != CharacterState.Normal || BattleNode?.PlayersList == null)
            return;

        using var _ = BeginEffectSource("被动");
        Character[] players = BattleNode
            .PlayersList.Where(player =>
                player != null
                && GodotObject.IsInstanceValid(player)
                && !player.IsSummon
                && player.State == CharacterState.Normal
            )
            .Cast<Character>()
            .ToArray();

        await Task.WhenAll(
            players.Select(player =>
                player.IncreaseMaxLifeFromPassive(PassiveUpgradePartyMaxLife, this)
            )
        );
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
