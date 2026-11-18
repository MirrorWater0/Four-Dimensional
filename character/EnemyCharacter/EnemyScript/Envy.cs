using System;
using System.Linq;
using System.Threading.Tasks;
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

        _skillUsedHandler ??= skill => TriggerPassive(skill);
        BattleNode.UsedSkills.ItemAdded -= _skillUsedHandler;
        BattleNode.UsedSkills.ItemAdded += _skillUsedHandler;
    }

    public override void _ExitTree()
    {
        if (BattleNode != null && _skillUsedHandler != null)
            BattleNode.UsedSkills.ItemAdded -= _skillUsedHandler;
        base._ExitTree();
    }

    protected override async Task PassiveAsync(Skill skill)
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
        PortaitPath = "res://asset/EnemyCharater/Envy.png";
        CharacterScene = GD.Load<PackedScene>("res://character/EnemyCharacter/Envy.tscn");
        SkillIDs = [SkillID.EnvyEliteAttack, SkillID.EnvyEliteSurvive, SkillID.EnvyEliteSpecial];
        // 刺击削生存，藏锋虚弱干扰，再夺辉削力量并成长。
        IntentionCycle = [SkillID.EnvyEliteAttack, SkillID.EnvyEliteSurvive, SkillID.EnvyEliteSpecial, random];

        PassiveName = global::Envy.PassiveNameText;
        PassiveDescription = global::Envy.PassiveDescriptionText;
    }
}

public partial class EnvyEliteAttack : Skill
{
    private int BaseDamage => V(nameof(BaseDamage));
    private int SurvivabilityDown => V(nameof(SurvivabilityDown));

    public override SkillTypes SkillType => SkillTypes.Attack;

    public override string SkillName { get; set; } = "觊觎刺击";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            AttackStep(baseDamage: BaseDamage, target: HostileTargetReference.One, times: V("HitCount"), multiplier: V("Multiplier")),
            LowerTargetPropertyStep(
                PropertyType.Survivability,
                SurvivabilityDown,
                HostileTargetReference.AttackKey
            ),
            AddCardsStep(SkillID.DazeStatus, V("DazeCount"), BattleCardPileTarget.DrawPileCards)
        );
    }
}

public partial class EnvyEliteSurvive : Skill
{
    private int BaseBlock => V(nameof(BaseBlock));

    public override SkillTypes SkillType => SkillTypes.Survive;

    public override string SkillName { get; set; } = "藏锋";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            BlockStep(baseBlock: BaseBlock, multiplier: V("Multiplier")),
            ApplyBuffHostile(Buff.BuffName.Weaken, V("WeakenStacks"), HostileTargetReference.All),
            AddCardsStep(SkillID.DazeStatus, V("DazeCount"), BattleCardPileTarget.DiscardPileCards)
        );
    }
}

public partial class EnvyEliteSpecial : Skill
{
    private int BaseDamage => V(nameof(BaseDamage));
    private int SurvivDown => V(nameof(SurvivDown));
    private int SelfPowerGain => V(nameof(SelfPowerGain));

    public override SkillTypes SkillType => SkillTypes.Attack;

    public override string SkillName { get; set; } = "夺辉";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            AttackStep(baseDamage: BaseDamage, target: HostileTargetReference.All, multiplier: V("Multiplier"), times: V("HitCount")),
            LowerTargetPropertyStep(
                PropertyType.Survivability,
                SurvivDown,
                HostileTargetReference.AttackKey
            ),
            ModifyPropertyStep(PropertyType.Power, SelfPowerGain)
        );
    }
}
