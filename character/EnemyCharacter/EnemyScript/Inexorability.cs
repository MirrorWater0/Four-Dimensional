using System;
using System.Threading.Tasks;
using Godot;

public partial class Inexorability : EnemyCharacter
{
    private static int HurtPowerDown => EnemyTuning.GetRequiredInt("Inexorability", nameof(HurtPowerDown));

    public const string PassiveNameText = "不可违逆";
    public static string PassiveDescriptionText => $"受到攻击时：攻击者失去{HurtPowerDown}点力量。";

    public override string CharacterName { get; set; } = "Inexorability";

    public override void Initialize()
    {
        base.Initialize();
        PassiveName = PassiveNameText;
        PassiveDescription = PassiveDescriptionText;
    }

    public override Task GetHurt(
        float damage,
        Character source = null,
        DamageKind damageKind = DamageKind.Other,
        bool ignoreBlock = false
    ) => BattleNode is Battle battle
        ? battle.ExecuteBattleEffectAsync(() => GetHurtAndPassiveAsync(damage, source, damageKind, ignoreBlock), "不可违逆：受伤与被动")
        : GetHurtAndPassiveAsync(damage, source, damageKind, ignoreBlock);

    private async Task GetHurtAndPassiveAsync(
        float damage,
        Character source = null,
        DamageKind damageKind = DamageKind.Other,
        bool ignoreBlock = false
    )
    {
        await base.GetHurt(damage, source, damageKind, ignoreBlock);

        if (
            damageKind != DamageKind.Attack
            || source == null
            || !GodotObject.IsInstanceValid(source)
            || source == this
            || source.State == CharacterState.Dying
        )
        {
            return;
        }

        using var _ = BeginEffectSource("被动");
        await source.DescendingProperties(PropertyType.Power, HurtPowerDown, this);
    }
}

public partial class InexorabilityRegedit : EnemyRegedit
{
    public InexorabilityRegedit()
    {
        CharacterName = "Inexorability";
        PortaitPath = "res://asset/EnemyCharater/Inexorability.png";
        CharacterScene = GD.Load<PackedScene>("res://character/EnemyCharacter/Inexorability.tscn");
        SkillIDs =
        [
            SkillID.InexorabilityAttack,
            SkillID.InexorabilitySurvive,
            SkillID.InexorabilitySpecial,
        ];
        // 防御成长后压制，以全阵律令结束一轮。
        IntentionCycle = [SkillID.InexorabilitySurvive, SkillID.InexorabilityAttack, random, SkillID.InexorabilitySpecial];

        PassiveName = global::Inexorability.PassiveNameText;
        PassiveDescription = global::Inexorability.PassiveDescriptionText;
    }
}

public partial class InexorabilityAttack : Skill
{
    private int BaseDamage => V(nameof(BaseDamage));

    public override SkillTypes SkillType => SkillTypes.Attack;

    public override string SkillName { get; set; } = "命定压制";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            AttackStep(baseDamage: BaseDamage, multiplier: V("Multiplier"), times: V("HitCount"))
        );
    }
}

public partial class InexorabilitySurvive : Skill
{
    private int BaseBlock => V(nameof(BaseBlock));
    private int PowerGain => V(nameof(PowerGain));

    public override SkillTypes SkillType => SkillTypes.Survive;

    public override string SkillName { get; set; } = "存续封缄";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            BlockStep(baseBlock: BaseBlock, multiplier: V("Multiplier")),
            ModifyPropertyStep(PropertyType.Power, PowerGain)
        );
    }
}

public partial class InexorabilitySpecial : Skill
{
    private int BaseDamage => V(nameof(BaseDamage));
    private int PowerMultiplier => V(nameof(PowerMultiplier));

    public override SkillTypes SkillType => SkillTypes.Special;

    public override string SkillName { get; set; } = "终局律令";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            AttackStep(
                baseDamage: BaseDamage,
                multiplier: PowerMultiplier,
                target: HostileTargetReference.All,
                times: V("HitCount")
            )
        );
    }
}
