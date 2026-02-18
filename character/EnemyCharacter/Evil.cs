using System;
using System.Threading.Tasks;
using Godot;

public partial class Evil : EnemyCharacter
{
    public override string CharacterName { get; set; } = "Evil";
    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        base._Ready();
    }

    public override void Initialize()
    {
        if (Registry == null)
        {
            BattleLifemax = 50;
            BattlePower = 15;
            BattleSurvivability = 15;
            Speed = 13;
            Skills = [new EvilAttack(), new EvilSurvive(), new EvilTermin()];
        }

        base.Initialize();
        DyingBuff.BuffAdd(Buff.BuffName.RebirthI, this, 1);
        UpdataEnergy(1);
    }
}

public partial class EvilAttack : Skill
{
    private const int HitDamage = 5;

    public EvilAttack()
        : base(Skill.SkillTypes.Attack)
    {
        UpdateDescription();
    }

    public override string SkillName { set; get; } = "流影二段";

    public override async Task Effect()
    {
        await base.Effect();
        await Attack2(HitDamage + OwnerPower);
    }

    public override void UpdateDescription()
    {
        SetDescriptionLines($"二段攻击；每段造成{HitDamage + OwnerPower}点伤害。");
    }
}

public partial class EvilSurvive : Skill
{
    private const int SurvivabilityGain = 3;
    private const int BaseBlock = 8;
    int DescendingNum = 3;

    public EvilSurvive()
        : base(SkillTypes.Survive)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "增幅";

    public override async Task Effect()
    {
        await base.Effect();
        OwnerCharater.UpdataBlock(BaseBlock + OwnerSurvivability);
        IncreaseProperties(OwnerCharater, PropertyType.Survivalibility, SurvivabilityGain);
        DescendingProperties(Chosetarget1()[0], PropertyType.Power, DescendingNum);
    }

    public override void UpdateDescription()
    {
        int block = Math.Clamp(BaseBlock + OwnerSurvivability, 0, 999);
        SetDescriptionLines(
            $"获得{block}点格挡。",
            $"获得+{SurvivabilityGain}{GetColoredPropertyLabel(PropertyType.Survivalibility)}",
            $"下降目标{DescendingNum}点{GetColoredPropertyLabel(PropertyType.Power)}。"
        );
    }
}

public partial class EvilTermin : Skill
{
    private const int EnergyCostPerHit = 1;

    public EvilTermin()
        : base(Skill.SkillTypes.Special)
    {
        UpdateDescription();
    }

    public override string SkillName { set; get; } = "回响时刻";

    public override async Task Effect()
    {
        await base.Effect();
        await Attack1(OwnerCharater.BattlePower);
        while (OwnerCharater.Energy > 0)
        {
            await Attack1(OwnerCharater.BattlePower);
            OwnerCharater.UpdataEnergy(-EnergyCostPerHit);
        }
    }

    public override void UpdateDescription()
    {
        int energy = Math.Max(OwnerEnergy, 0);
        int castTimes = Math.Max(1, energy + 1);
        SetDescriptionLines(
            $"施放{castTimes}次；每次造成{Math.Clamp(OwnerPower, 0, 9999)}点伤害；每次消耗{EnergyCostPerHit}点能量。"
        );
    }
}
