using System;
using System.Threading.Tasks;
using Godot;

public partial class Evil : EnemyCharacter
{
    int Count = 0;
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
            BattleMaxLife = 50;
            BattlePower = 15;
            BattleSurvivability = 15;
            Speed = 13;
            Skills = [new EvilAttack(), new EvilSurvive(), new EvilTermin()];
        }

        base.Initialize();
        DyingBuff.BuffAdd(Buff.BuffName.RebirthI, this, 1);
        UpdataEnergy(1);
    }

    public override void StartAction()
    {
        Count++;
        Passive(null);
        base.StartAction();
    }

    public override void Passive(Skill skill)
    {
        if (Count == 2)
        {
            Count = 0;
            DyingBuff.BuffAdd(Buff.BuffName.RebirthI, this, 1);
        }
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
        int totalDamage = HitDamage + OwnerPower;
        string damageText = BasePlusXWithBattleTotal(HitDamage, totalDamage, StatX.Power);
        SetDescriptionLines($"二段攻击。", $"每段造成{damageText}点伤害。");
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
        IncreaseProperties(OwnerCharater, PropertyType.Survivability, SurvivabilityGain);
        DescendingProperties(Chosetarget1()[0], PropertyType.Power, DescendingNum);
    }

    public override void UpdateDescription()
    {
        int totalBlock = BaseBlock + OwnerSurvivability;
        string blockText = BasePlusXWithBattleTotal(BaseBlock, totalBlock, StatX.Survivability);
        SetDescriptionLines(
            $"获得{blockText}点格挡。",
            $"获得+{SurvivabilityGain}{GetColoredPropertyLabel(PropertyType.Survivability)}。",
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
            OwnerCharater.UpdataEnergy(-EnergyCostPerHit);
            await Attack1(OwnerCharater.BattlePower);
        }
    }

    public override void UpdateDescription()
    {
        int energy = Math.Max(OwnerEnergy, 0);
        int castTimes = Math.Max(1, energy + 1);
        string castTimesText = BasePlusXWithBattleTotal(1, castTimes, StatX.Energy);
        string perCastDamageText = XWithBattleTotal(StatX.Power, OwnerPower);
        SetDescriptionLines(
            $"施放{castTimesText}次。",
            $"每次造成{perCastDamageText}点伤害。",
            $"每次消耗{EnergyCostPerHit}点能量。"
        );
    }
}
