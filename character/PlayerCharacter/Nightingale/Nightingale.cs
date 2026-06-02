using System;
using System.Linq;
using System.Threading.Tasks;
using Godot;

public partial class Nightingale : PlayerCharacter
{
    private const int PassiveUpgradeVulnerableStacks = 1;

    public const string PassiveNameText = "夜光";
    public static string PassiveDescriptionText =>
        I18n.Format(
            "character.nightingale.passive.description",
            "队友结束回合时：追击一次：造成{power}点伤害。",
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
        BattleNode.EmitList.Add(Pursuit);
    }

    public async Task Pursuit(Character character)
    {
        if (character.IsPlayer && character != this && State != CharacterState.Dying)
        {
            using var _ = BeginEffectSource("追击");
            await ToSignal(GetTree().CreateTimer(0.5f), "timeout");
            var skill = new Skill(Skill.SkillTypes.Attack) { OwnerCharater = this };
            Character target = skill.ChosetargetByOrder(applyTaunt: true).FirstOrDefault();
            ApplyPassiveUpgradeBeforePursuit(target);
            await skill.Attack(BattlePower, target: target);
        }
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
        LifeMax = 22,
        Power = 4,
        Survivability = 4,
        Speed = 8,
        CharacterScenePath = "res://character/PlayerCharacter/Nightingale/Nightingale.tscn",
        PortaitPath = "res://asset/PlayerCharater/Nightingale/NightingalePortrait.png",
        TakenSkills = [SkillID.BasicAttack, SkillID.BasicDefense, SkillID.BasicSpecial],
    };
}
