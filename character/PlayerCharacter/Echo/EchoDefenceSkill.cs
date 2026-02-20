using System;
using System.Threading.Tasks;
using Godot;

public partial class EchoDefenceSkill { }

public partial class SoundBarrier : Skill
{
    public override string SkillName { get; set; } = "音障防护";
    private const int EnergyGain = 1;
    private const int BaseBlock = 10;

    public SoundBarrier()
        : base(SkillTypes.Survive)
    {
        UpdateDescription();
    }

    public override async Task Effect()
    {
        await base.Effect();
        OwnerCharater.UpdataEnergy(EnergyGain);
        OwnerCharater.UpdataBlock(BaseBlock + OwnerSurvivability);
        await Task.Delay(200);
        await Carry(OwnerCharater.BattleNode.PlayersList[0], 0);
    }

    public override void UpdateDescription()
    {
        int block = Math.Clamp(BaseBlock + OwnerSurvivability, 0, 999);
        SetDescriptionLines($"恢复{EnergyGain}点能量；获得{block}点格挡；连携下一位角色。");
    }
}
