using System;
using System.Linq;
using System.Threading.Tasks;
using Godot;

public partial class FearWorm : EnemyCharacter
{
    public override void Initialize()
    {
        base.Initialize();
        SpecialBuff.BuffAdd(Buff.BuffName.DebuffImmunity, this, 1);
    }
}

public partial class FearWormAttack : Skill
{
    private const int BaseDamage = 2;
    private const int VulnerableStacks = 1;
    private const int MaxTargets = 3;
    int energyGain = 1;

    public FearWormAttack()
        : base(SkillTypes.Attack)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "恐惧咬噬";

    public override async Task Effect()
    {
        await base.Effect();
        OwnerCharater.UpdataEnergy(energyGain);
        var targets = Chosetarget1();

        int damage = Math.Clamp(BaseDamage + OwnerPower, 0, 999);
        int count = Math.Min(MaxTargets, targets.Length);
        for (int i = 0; i < count; i++)
        {
            var target = targets[i];
            _ = Attack3(damage, target, 1);
            await Task.Delay(80);
        }
        await Task.Delay(400);
        for (int i = 0; i < count; i++)
        {
            var target = targets[i];
            HurtBuff.BuffAdd(Buff.BuffName.Vulnerable, target, VulnerableStacks);
            await Task.Delay(80);
        }
    }

    public override void UpdateDescription()
    {
        int totalDamage = BaseDamage + OwnerPower;
        string damageText = BasePlusXWithBattleTotal(BaseDamage, totalDamage, StatX.Power);
        SetDescriptionLines(
            $"获得{energyGain}点能量。",
            $"对至多{MaxTargets}名目标造成{damageText}点伤害。",
            $"使其各获得{VulnerableStacks}层{Buff.BuffName.Vulnerable.GetDescription()}。"
        );
    }
}

public partial class FearWormSurvive : Skill
{
    private const int DebuffImmunityStacks = 1;
    private const int BaseBlock = 5;

    public FearWormSurvive()
        : base(SkillTypes.Survive)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "蜕皮潜伏";

    public override async Task Effect()
    {
        await base.Effect();
        SpecialBuff.BuffAdd(Buff.BuffName.DebuffImmunity, OwnerCharater, DebuffImmunityStacks);
        OwnerCharater.UpdataBlock(BaseBlock + OwnerSurvivability);
        await Carry(OwnerCharater.BattleNode.EnemiesList[0], 0);
    }

    public override void UpdateDescription()
    {
        int totalBlock = BaseBlock + OwnerSurvivability;
        string blockText = BasePlusXWithBattleTotal(BaseBlock, totalBlock, StatX.Survivability);
        SetDescriptionLines(
            $"获得{DebuffImmunityStacks}层{Buff.BuffName.DebuffImmunity.GetDescription()}。",
            $"获得{blockText}点格挡。",
            $"连携下一位角色使用攻击技能。"
        );
    }
}

public partial class FearWormTermin : Skill
{
    private const int BaseDamage = 5;
    private const int PowerDown = 3;
    private const int StunStacks = 1;
    int cost = 2;

    public FearWormTermin()
        : base(SkillTypes.Special)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "梦魇缠绕";

    public override async Task Effect()
    {
        await base.Effect();

        var targets = Chosetarget1();
        if (targets.Length == 0)
            return;

        var target = targets[0];

        DescendingProperties(target, PropertyType.Power, PowerDown);
        StartActionBuff.BuffAdd(Buff.BuffName.Stun, target, StunStacks);

        int damage = Math.Clamp(BaseDamage + OwnerPower, 0, 9999);
        await AttackAnimation(target);
        await target.GetHurt(damage);
    }

    public override void UpdateDescription()
    {
        int totalDamage = BaseDamage + OwnerPower;
        string damageText = BasePlusXWithBattleTotal(BaseDamage, totalDamage, StatX.Power);
        SetDescriptionLines(
            $"造成{damageText}点伤害。",
            $"下降目标{PowerDown}点{GetColoredPropertyLabel(PropertyType.Power)}。",
            $"消耗{cost}点能量:",
            $"使目标获得{StunStacks}层{Buff.BuffName.Stun.GetDescription()}。"
        );
    }
}
