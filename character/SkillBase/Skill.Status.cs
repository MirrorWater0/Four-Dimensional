using System.Threading.Tasks;

public partial class VoidStatus : Skill
{
    private const int EnergyLossOnDraw = 1;

    public VoidStatus()
        : base(SkillTypes.Status)
    {
        UpdateDescription();
    }

    public override string SkillName
    {
        get => I18n.Tr("skill.void_status.name", "虚空之唤");
        set { }
    }
    public override int EnergyCost => 0;
    public override bool CanBePlayed => false;
    public override bool ExhaustsAtTurnEndInHand => true;

    public override void OnDrawnToHand(PlayerCharacter player)
    {
        if (player == null)
            return;

        player.BattleNode?.UpdataEnergy(player, -EnergyLossOnDraw, player);
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
        : base(SkillTypes.Status)
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

public partial class DazeStatus : Skill
{
    public DazeStatus()
        : base(SkillTypes.Status)
    {
        UpdateDescription();
    }

    public override string SkillName
    {
        get => I18n.Tr("skill.daze_status.name", "晕眩");
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

public partial class PlagueStatus : Skill
{
    private const int TeamDamageAtTurnEnd = 3;

    public PlagueStatus()
        : base(SkillTypes.Status)
    {
        UpdateDescription();
    }

    public override string SkillName
    {
        get => I18n.Tr("skill.plague_status.name", "瘟疫");
        set { }
    }
    public override int EnergyCost => 1;
    public override bool CanBePlayed => true;
    public override bool ExhaustsAfterUse => true;
    public override bool TriggersAtTurnEndInHand => true;

    public override async Task OnTurnEndInHand(PlayerCharacter player)
    {
        if (player?.BattleNode == null)
            return;

        Character[] targets = player.BattleNode.GetOrderedTeamCharacters(
            player.IsPlayer,
            includeSummons: true,
            dyingFilter: true
        );
        foreach (Character target in targets)
        {
            if (target == null || !Godot.GodotObject.IsInstanceValid(target))
                continue;

            await target.GetHurt(
                TeamDamageAtTurnEnd,
                source: player,
                damageKind: Character.DamageKind.Other
            );
        }
    }

    public override void UpdateDescription()
    {
        SetDescriptionLines(
            I18n.Tr("skill.status.desc.status_card", "状态牌。"),
            $"{I18n.Tr("keyword.exhaust", "消耗")}。",
            I18n.Format(
                "skill.plague_status.desc.turn_end_damage",
                "回合结束时若在手牌中：己方全阵受到{amount}点伤害。",
                ("amount", TeamDamageAtTurnEnd)
            )
        );
    }
}
