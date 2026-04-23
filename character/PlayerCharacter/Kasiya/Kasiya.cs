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
