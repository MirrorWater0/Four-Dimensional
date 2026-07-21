using System;
using System.Linq;
using Godot;

public partial class Envy : EnemyCharacter
{
    private Action<Skill> _skillUsedHandler;

    public const string PassiveNameText = "嫉妒";
    public static string PassiveDescriptionText =>
        "敌方打出卡牌时：获得该卡牌中的正面增益（包括属性提升）。";

    public override string CharacterName { get; set; } = "嫉妒";

    public override void Initialize()
    {
        base.Initialize();
        PassiveName = PassiveNameText;
        PassiveDescription = PassiveDescriptionText;

        if (BattleNode == null)
            return;

        _skillUsedHandler ??= TriggerPassive;
        BattleNode.UsedSkills.ItemAdded -= _skillUsedHandler;
        BattleNode.UsedSkills.ItemAdded += _skillUsedHandler;
    }

    public override void _ExitTree()
    {
        if (BattleNode != null && _skillUsedHandler != null)
            BattleNode.UsedSkills.ItemAdded -= _skillUsedHandler;
        base._ExitTree();
    }

    private async void TriggerPassive(Skill skill)
    {
        if (
            State == CharacterState.Dying
            || skill?.OwnerCharater == null
            || skill.OwnerCharater.BattleNode != BattleNode
            || skill.OwnerCharater.IsPlayer == IsPlayer
        )
        {
            return;
        }

        Skill.PreviewEffectEntry[] effects = skill.BuildPreviewEffectEntriesRaw();
        var positiveBuffs = effects
            .Where(entry =>
                entry.Kind == Skill.PreviewEffectKind.Buff
                && entry.Value > 0
                && entry.BuffName.HasValue
                && Buff.GetNature(entry.BuffName.Value) == Nature.positive
                && (
                    entry.Target == null
                    || entry.Target.IsPlayer == skill.OwnerCharater.IsPlayer
                )
            )
            .GroupBy(entry => entry.BuffName.Value)
            .Select(group => new { BuffName = group.Key, Stacks = group.Max(entry => entry.Value) })
            .ToArray();
        var positiveProperties = effects
            .Where(entry =>
                entry.Kind == Skill.PreviewEffectKind.Property
                && entry.Value > 0
                && entry.PropertyType.HasValue
                && (
                    entry.Target == null
                    || entry.Target.IsPlayer == skill.OwnerCharater.IsPlayer
                )
            )
            .GroupBy(entry => entry.PropertyType.Value)
            .Select(group => new
            {
                PropertyType = group.Key,
                Value = group.Max(entry => entry.Value),
            })
            .ToArray();
        if (positiveBuffs.Length == 0 && positiveProperties.Length == 0)
            return;

        using var _ = BeginEffectSource("被动");
        foreach (var buff in positiveBuffs)
            Skill.TryApplyBuffToTarget(buff.BuffName, this, buff.Stacks, this);
        foreach (var property in positiveProperties)
            await IncreaseProperties(property.PropertyType, property.Value, this);
    }
}

public partial class EnvyEliteRegedit : EnemyRegedit
{
    public EnvyEliteRegedit()
    {
        CharacterName = "嫉妒";
        PType = EnemyPositionType.BackRow;
        PortaitPath = "res://asset/EnemyCharater/Envy.png";
        CharacterScene = GD.Load<PackedScene>("res://character/EnemyCharacter/Envy.tscn");

        MaxLife = 206;
        Power = 0;
        Survivability = 0;
        BasePowerContribution = 0;
        BaseSurvivabilityContribution = 0;
        SkillIDs = [SkillID.EnvyEliteAttack, SkillID.EnvyEliteSurvive, SkillID.EnvyEliteSpecial];

        PassiveName = global::Envy.PassiveNameText;
        PassiveDescription = global::Envy.PassiveDescriptionText;
    }
}

public partial class EnvyEliteAttack : Skill
{
    private const int BaseDamage = 9;
    private const int SurvivabilityDown = 2;

    public override SkillTypes SkillType => SkillTypes.Attack;

    public override string SkillName { get; set; } = "觊觎刺击";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            AttackStep(baseDamage: BaseDamage, target: HostileTargetReference.One, times: V("HitCount", 2)),
            LowerTargetPropertyStep(
                PropertyType.Survivability,
                SurvivabilityDown,
                HostileTargetReference.AttackKey
            ),
            AddCardsStep(SkillID.DazeStatus, V("DazeCount", 1), BattleCardPileTarget.DrawPileCards)
        );
    }
}

public partial class EnvyEliteSurvive : Skill
{
    private const int BaseBlock = 20;

    public override SkillTypes SkillType => SkillTypes.Survive;

    public override string SkillName { get; set; } = "藏锋";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            BlockStep(baseBlock: BaseBlock),
            ApplyBuffHostile(Buff.BuffName.Weaken, V("WeakenStacks", 1), HostileTargetReference.All),
            ModifyPropertyStep(PropertyType.Power, V("PowerGain", 1)),
            AddCardsStep(SkillID.DazeStatus, V("DazeCount", 2), BattleCardPileTarget.DiscardPileCards)
        );
    }
}

public partial class EnvyEliteSpecial : Skill
{
    private const int BaseDamage = 11;
    private const int PowerDown = 2;
    private const int SelfPowerGain = 2;

    public override SkillTypes SkillType => SkillTypes.Attack;

    public override string SkillName { get; set; } = "夺辉";
    public override int EnemySpecialIntentionCooldown => 3;

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            AttackStep(baseDamage: BaseDamage, target: HostileTargetReference.All),
            LowerTargetPropertyStep(
                PropertyType.Power,
                PowerDown,
                HostileTargetReference.AttackKey
            ),
            ModifyPropertyStep(PropertyType.Power, SelfPowerGain)
        );
    }
}
