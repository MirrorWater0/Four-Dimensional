using System;
using System.Linq;
using System.Threading.Tasks;
using Godot;

public partial class Nightingale : PlayerCharacter
{
    private const int PassiveUpgradeVulnerableStacks = 1;
    private Func<bool, Task> _teamTurnEndHandler;

    public const string PassiveNameText = "夜光";
    public static string PassiveDescriptionText =>
        I18n.Format(
            "character.nightingale.passive.description",
            "己方阵营回合结束时：造成{power}点伤害。",
            ("power", PropertyType.Power.GetDescription())
        );

    public override PackedScene CharaterScene { get; set; } = StartInterface._Nightingale;
    public override string CharacterName { get; set; } = "Nightingale";

    public override void Initialize()
    {
        base.Initialize();
        PassiveName = PassiveNameText;
        PassiveDescription = TalentTree.AppendPassiveUpgradeDescription(
            CharacterKey,
            PassiveDescriptionText,
            HasPassiveTalentUpgrade()
        );
        _teamTurnEndHandler ??= OnTeamTurnEnded;
        if (BattleNode != null && !BattleNode.TeamTurnEndEmitList.Contains(_teamTurnEndHandler))
            BattleNode.TeamTurnEndEmitList.Add(_teamTurnEndHandler);
    }

    public override void _ExitTree()
    {
        if (BattleNode != null && _teamTurnEndHandler != null)
            BattleNode.TeamTurnEndEmitList.Remove(_teamTurnEndHandler);
        base._ExitTree();
    }

    private async Task OnTeamTurnEnded(bool endedTeamIsPlayer)
    {
        if (!endedTeamIsPlayer || State == CharacterState.Dying)
            return;

        using var _ = BeginEffectSource(PassiveNameText);
        await ToSignal(GetTree().CreateTimer(0.5f), "timeout");
        var skill = new Skill(Skill.SkillTypes.Attack) { OwnerCharater = this };
        Character target = skill.ChosetargetByOrder(applyTaunt: true).FirstOrDefault();
        ApplyPassiveUpgradeBeforePursuit(target);
        await skill.Attack(BattlePower, target: target);
    }

    private void ApplyPassiveUpgradeBeforePursuit(Character target)
    {
        if (!HasPassiveTalentUpgrade() || target == null || target.State != CharacterState.Normal)
            return;

        bool targetHasVulnerable =
            target.HurtBuffs?.Any(buff =>
                buff != null && buff.ThisBuffName == Buff.BuffName.Vulnerable && buff.Stack > 0
            ) == true;
        if (targetHasVulnerable)
            HurtBuff.BuffAdd(
                Buff.BuffName.Vulnerable,
                target,
                PassiveUpgradeVulnerableStacks,
                this
            );
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
        Power = 5,
        Survivability = 4,
        Speed = 13,
        CharacterScenePath = "res://character/PlayerCharacter/Nightingale/Nightingale.tscn",
        PortaitPath = "res://asset/PlayerCharater/Nightingale/NightingalePortrait.png",
        TakenSkills = [SkillID.BasicAttack, SkillID.BasicDefense, SkillID.NightingaleBasicSpecial],
    };
}
