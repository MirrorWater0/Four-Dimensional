public partial class VoidStatus : Skill
{
    private const int EnergyLossOnDraw = 1;

    public VoidStatus()
        : base(SkillTypes.none)
    {
        UpdateDescription();
    }

    public override string SkillName
    {
        get => I18n.Tr("skill.void_status.name", "虚空");
        set { }
    }
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
        SetDescriptionLines(
            I18n.Format(
                "skill.void_status.desc.on_draw_lose_energy",
                "抽到时: 失去{amount}点能量.",
                ("amount", EnergyLossOnDraw)
            ),
            I18n.Tr("skill.status.desc.ethereal", "虚无.")
        );
    }
}

public partial class WoundStatus : Skill
{
    public WoundStatus()
        : base(SkillTypes.none)
    {
        UpdateDescription();
    }

    public override string SkillName
    {
        get => I18n.Tr("skill.wound_status.name", "伤口");
        set { }
    }
    public override int EnergyCost => 0;
    public override bool CanBePlayed => false;

    public override void UpdateDescription()
    {
        SetDescriptionLines(
            I18n.Tr("skill.status.desc.status_card", "状态牌。"),
            I18n.Tr("skill.status.desc.unplayable", "不可打出。")
        );
    }
}

public partial class StunStatus : Skill
{
    public StunStatus()
        : base(SkillTypes.none)
    {
        UpdateDescription();
    }

    public override string SkillName
    {
        get => I18n.Tr("skill.stun_status.name", "眩晕");
        set { }
    }
    public override int EnergyCost => 0;
    public override bool CanBePlayed => false;
    public override bool ExhaustsAtTurnEndInHand => true;

    public override void UpdateDescription()
    {
        SetDescriptionLines(
            I18n.Tr("skill.status.desc.status_card", "状态牌。"),
            I18n.Tr("skill.status.desc.unplayable", "不可打出。"),
            I18n.Tr("skill.status.desc.ethereal", "虚无。")
        );
    }
}
