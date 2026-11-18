using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

public partial class HollowBulwark : EnemyCharacter
{
    private static int StartBarricadeStacks => EnemyTuning.GetRequiredInt("HollowBulwark", nameof(StartBarricadeStacks));
    private static int StartBlock => EnemyTuning.GetRequiredInt("HollowBulwark", nameof(StartBlock));

    public const string PassiveNameText = "空壳壁障";
    public static string PassiveDescriptionText =>
        $"战斗开始时: 获得{StartBarricadeStacks}层{Buff.BuffName.Barricade.GetDescription()}，获得{StartBlock}点格挡.";

    public override string CharacterName { get; set; } = "空壳壁卫";

    public override void Initialize()
    {
        base.Initialize();
        PassiveName = PassiveNameText;
        PassiveDescription = PassiveDescriptionText;
        BattleNode?.StartEffectList.Add(StartPassive);
    }

    public Task StartPassive()
    {
        using var _ = BeginEffectSource("被动");
        StartActionBuff.BuffAdd(Buff.BuffName.Barricade, this, StartBarricadeStacks, this);
        UpdataBlock(StartBlock, source: this);
        return Task.CompletedTask;
    }
}

public partial class HollowBulwarkRegedit : EnemyRegedit
{
    public HollowBulwarkRegedit()
    {
        CharacterName = "空壳壁卫";
        PortaitPath = "res://asset/EnemyCharater/HollowBulwark.png";
        CharacterScene = GD.Load<PackedScene>("res://character/EnemyCharacter/HollowBulwark.tscn");
        SkillIDs =
        [
            SkillID.HollowBulwarkAttack,
            SkillID.HollowBulwarkSurvive,
        ];
        // 第四轮闭合随机施加脆弱，后续进攻与侍从的全阵伤害共同施压。
        IntentionCycle = [SkillID.HollowBulwarkSurvive, SkillID.HollowBulwarkAttack, random, SkillID.HollowBulwarkSpecial, SkillID.HollowBulwarkAttack];
        PassiveName = global::HollowBulwark.PassiveNameText;
        PassiveDescription = global::HollowBulwark.PassiveDescriptionText;
    }
}

public partial class HollowBulwarkAttack : Skill
{
    private int BlockPerPower => V(nameof(BlockPerPower));

    public override SkillTypes SkillType => SkillTypes.Attack;

    public override string SkillName { get; set; } = "壁刃冲击";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            AttackStep(V("BaseDamage"), multiplier: V("Multiplier"), times: V("HitCount")),
            new BlockPowerGainStep(BlockPerPower)
        );
    }

    private sealed class BlockPowerGainStep : SkillStep
    {
        private readonly int _blockPerPower;

        public BlockPowerGainStep(int blockPerPower)
        {
            _blockPerPower = blockPerPower;
        }

        public override Task Execute(Skill skill)
        {
            Character owner = skill?.OwnerCharater;
            int powerGain = GetPowerGain(owner);
            return powerGain > 0
                ? owner.IncreaseProperties(PropertyType.Power, powerGain, owner)
                : Task.CompletedTask;
        }

        public override IEnumerable<string> Describe(Skill skill)
        {
            yield return I18n.Format(
                "skill.hollow_bulwark_attack.block_power",
                "每拥有{block}点格挡，获得1点力量。",
                ("block", _blockPerPower)
            );
        }

        public override IEnumerable<Character> PreviewTargets(Skill skill)
        {
            Character owner = skill?.OwnerCharater;
            if (GetPowerGain(owner) > 0)
                yield return owner;
        }

        public override IEnumerable<PreviewEffectEntry> PreviewEffects(Skill skill, PreviewDamageContext context)
        {
            Character owner = skill?.OwnerCharater;
            int powerGain = GetPowerGain(owner);
            if (powerGain > 0)
                yield return PreviewEffectEntry.Property(owner, PropertyType.Power, powerGain, owner);
        }

        private int GetPowerGain(Character owner) =>
            owner != null && GodotObject.IsInstanceValid(owner) && owner.State != Character.CharacterState.Dying
                ? Math.Max(0, owner.Block) / _blockPerPower
                : 0;
    }
}

public partial class HollowBulwarkSurvive : Skill
{
    private int BaseBlock => V(nameof(BaseBlock));

    public override SkillTypes SkillType => SkillTypes.Survive;

    public override string SkillName { get; set; } = "空壳";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(this, BlockStep(baseBlock: BaseBlock, multiplier: V("Multiplier")));
    }
}

public partial class HollowBulwarkSpecial : Skill
{
    public override SkillTypes SkillType => SkillTypes.Special;

    public override string SkillName { get; set; } = "闭合";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            AttackStep(V("BaseDamage"), multiplier: V("Multiplier"), times: V("HitCount")),
            ApplyBuffHostile(Buff.BuffName.Frail, V("FrailStacks"), HostileTargetReference.RandomPreview),
            ModifyPropertyStep(PropertyType.Survivability, V("SurvivabilityGain")),
            CustomStep(
                _ =>
                {
                    int currentBlock = OwnerCharater?.Block ?? 0;
                    if (currentBlock > 0)
                        OwnerCharater?.UpdataBlock(currentBlock, source: OwnerCharater);

                    return Task.CompletedTask;
                },
                _ => ["格挡翻倍."]
            )
        );
    }
}
