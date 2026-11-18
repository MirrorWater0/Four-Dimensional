using System.Threading.Tasks;
using Godot;

public partial class VoidRotor : EnemyCharacter
{
    private static int PassivePowerGain => EnemyTuning.GetRequiredInt("VoidRotor", nameof(PassivePowerGain));

    public const string PassiveNameText = "虚空蚀咒";
    public static string PassiveDescriptionText =>
        $"己方阵营回合结束时：己方全阵获得{PassivePowerGain}点力量。";

    public override string CharacterName { get; set; } = "VoidRotor";

    public override void Initialize()
    {
        base.Initialize();
        PassiveName = PassiveNameText;
        PassiveDescription = PassiveDescriptionText;
    }

    public override void OnTurnEnd()
    {
        TriggerPassive(null);
        base.OnTurnEnd();
    }

    protected override async Task PassiveAsync(Skill skill)
    {
        if (BattleNode == null)
            return;

        using var effectSource = BeginEffectSource("被动");
        foreach (Character target in BattleNode.GetTeamCharacters(IsPlayer, includeSummons: true))
        {
            if (target == null || target.State != CharacterState.Normal)
                continue;

            if (BattleNode.QueueEnemyPhaseEndPowerGain(target, PassivePowerGain, this, "被动"))
            {
                continue;
            }

            await BattleNode.ExecuteBattleEffectAsync(() => target.IncreaseProperties(PropertyType.Power, PassivePowerGain, this), "被动：力量提升");
        }
    }
}

public partial class VoidRotorRegedit : EnemyRegedit
{
    public VoidRotorRegedit()
    {
        CharacterName = "VoidRotor";
        PortaitPath = "res://asset/EnemyCharater/VoidRotor.png";
        CharacterScene = GD.Load<PackedScene>("res://character/EnemyCharacter/VoidRotor.tscn");
        SkillIDs = [SkillID.VoidRotorAttack, SkillID.VoidRotorSurvive, SkillID.VoidRotorSpecial];
        // 裂刃虚弱，蚀甲干扰，灌注后再追击。
        IntentionCycle = [SkillID.VoidRotorAttack, SkillID.VoidRotorSurvive, SkillID.VoidRotorSpecial, SkillID.VoidRotorAttack, random];

        PassiveName = global::VoidRotor.PassiveNameText;
        PassiveDescription = global::VoidRotor.PassiveDescriptionText;
    }
}

public partial class VoidRotorAttack : Skill
{
    private int BaseDamage => V(nameof(BaseDamage));
    private int PowerMultiplier => V(nameof(PowerMultiplier));

    public override SkillTypes SkillType => SkillTypes.Attack;

    public override string SkillName { get; set; } = "裂刃斩击";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            AttackStep(baseDamage: BaseDamage, multiplier: PowerMultiplier, times: V("HitCount")),
            ApplyBuffHostile(Buff.BuffName.Weaken, V("WeakenStacks"), HostileTargetReference.AttackKey)
        );
    }
}

public partial class VoidRotorSurvive : Skill
{
    private int BaseBlock => V(nameof(BaseBlock));

    public override SkillTypes SkillType => SkillTypes.Survive;

    public override string SkillName { get; set; } = "蚀甲压制";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            BlockStep(baseBlock: BaseBlock, multiplier: V("Multiplier")),
            AddCardsStep(SkillID.DazeStatus, V("DazeCount"))
        );
    }
}

public partial class VoidRotorSpecial : Skill
{
    private int BaseDamage => V(nameof(BaseDamage));
    private int DazeCardsPerTarget => V(nameof(DazeCardsPerTarget));

    public override SkillTypes SkillType => SkillTypes.Special;

    public override string SkillName { get; set; } = "虚界灌注";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            AttackStep(baseDamage: BaseDamage, multiplier: V("Multiplier"), times: V("HitCount")),
            AddCardsStep(
                SkillID.DazeStatus,
                DazeCardsPerTarget,
                BattleCardPileTarget.DiscardPileCards
            )
        );
    }
}
