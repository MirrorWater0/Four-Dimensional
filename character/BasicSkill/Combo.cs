using System;
using System.Linq;
using System.Threading.Tasks;
using Godot;

public partial class Combo : Skill
{
    private const int EnergyCostPerHit = 1;

    public Combo()
        : base(Skill.SkillTypes.Special)
    {
        UpdateDescription();
    }

    public override string SkillName { set; get; } = "回响时刻";

    public override async Task Effect()
    {
        await base.Effect();

        do
        {
            await Attack1(OwnerCharater.BattlePower);
            if (OwnerCharater.Energy > 0)
                OwnerCharater.UpdataEnergy(-EnergyCostPerHit);
        } while (OwnerCharater.Energy > 0);
    }

    public override void UpdateDescription()
    {
        SetDescriptionText(
            $"连续攻击，每次造成当前战斗力（{Math.Clamp(OwnerPower, 0, 9999)}）点伤害；每次攻击后消耗{EnergyCostPerHit}点能量，直到能量耗尽。"
        );
    }
}
