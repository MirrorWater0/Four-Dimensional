public partial class VoidStatus : Skill
{
    private const int EnergyLossOnDraw = 1;

    public VoidStatus()
        : base(SkillTypes.none)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "虚空";
    public override int EnergyCost => 0;
    public override bool CanBePlayed => false;
    public override bool ExhaustsAtTurnEndInHand => true;

    public override void OnDrawnToHand(PlayerCharacter player)
    {
        if (player == null)
            return;

        player.UpdataEnergy(-EnergyLossOnDraw, player);
    }

    public override void UpdateDescription()
    {
        SetDescriptionLines($"抽到时: 失去{EnergyLossOnDraw}点能量.", "虚无.");
    }
}

public partial class WoundStatus : Skill
{
    public WoundStatus()
        : base(SkillTypes.none)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "伤口";
    public override int EnergyCost => 0;
    public override bool CanBePlayed => false;

    public override void UpdateDescription()
    {
        SetDescriptionLines("状态牌。", "不可打出。");
    }
}
