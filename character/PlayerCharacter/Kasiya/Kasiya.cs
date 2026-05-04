using System;
using System.Threading.Tasks;
using Godot;

public partial class Kasiya : PlayerCharacter
{
    private const int PassiveAttackBaseHeal = -5;
    private const int PassiveSurvivePowerGain = 1;

    public const string PassiveNameText = "战意";
    public static string PassiveDescriptionText =>
        $"当其他队友使用攻击技能：回复{PassiveAttackBaseHeal}点基础生命。\n"
        + $"当其他队友使用生存技能：获得{PassiveSurvivePowerGain}点力量。";

    Label label => field ??= GetNode<Label>("Label");
    public override PackedScene CharaterScene { get; set; } = StartInterface._Kasiya;
    public override string CharacterName { get; set; } = "Kasiya";

    public override void Initialize()
    {
        base.Initialize();
        PassiveName = PassiveNameText;
        PassiveDescription = PassiveDescriptionText;

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

        if (skill.SkillType == Skill.SkillTypes.Survive)
            await IncreaseProperties(PropertyType.Power, PassiveSurvivePowerGain, this);
    }
}

public partial class PlayerCharacterRegistry
{
    public PlayerInfoStructure Kasiya = new PlayerInfoStructure()
    {
        CharacterName = "Kasiya",
        PassiveName = global::Kasiya.PassiveNameText,
        PassiveDescription = global::Kasiya.PassiveDescriptionText,
        LifeMax = 60,
        Power = 12,
        Survivability = 12,
        Speed = 8,
        CharacterScenePath = "res://character/PlayerCharacter/Kasiya/kasiya.tscn",
        PortaitPath = "res://asset/PlayerCharater/Kasiya/KasiyaPortrait.png",
        TakenSkills = [SkillID.BasicAttack, SkillID.BasicDefense, SkillID.BasicSpecial],
    };
}
