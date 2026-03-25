using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;

public partial class PlayerCharacter : Character
{
    public Frame SelfFrame;
    public Control SkillButtonControl;
    public List<Skill> UntakeSkills;
    public bool Istest;
    public int CharacterIndex;

    public override void Initialize()
    {
        var info = GameInfo.PlayerCharacters[CharacterIndex];

        PositionIndex = info.PositionIndex;
        PassiveName = info.PassiveName;
        PassiveDescription = info.PassiveDescription;
        Skills =
        [
            Skill.GetSkill(info.TakenSkills[0]),
            Skill.GetSkill(info.TakenSkills[1]),
            Skill.GetSkill(info.TakenSkills[2]),
        ];
        SetCombatStats(
            info.Power + EquipmentProperty(PropertyType.Power, info),
            info.Survivability + EquipmentProperty(PropertyType.Survivability, info),
            info.Speed + EquipmentProperty(PropertyType.Speed, info),
            info.LifeMax + EquipmentProperty(PropertyType.MaxLife, info)
        );
        Life = BattleMaxLife;
        base.Initialize();
        IsPlayer = true;
        BlockLabel.Position += new Vector2(230, 0);
        BlockLabel.HorizontalAlignment = HorizontalAlignment.Left;
    }

    public int EquipmentProperty(PropertyType type, PlayerInfoStructure info)
    {
        if (info.Equipments == null || info.Equipments.Length == 0)
            return 0;

        int value = 0;
        foreach (var equipment in info.Equipments)
        {
            if (equipment == null)
                continue;

            value += type switch
            {
                PropertyType.Power => equipment.Power,
                PropertyType.Survivability => equipment.Survivability,
                PropertyType.Speed => equipment.Speed,
                PropertyType.MaxLife => equipment.MaxLife,
                _ => 0,
            };
        }

        return value;
    }

    public override void StartAction()
    {
        base.StartAction();
        for (int j = 0; j < SkillButtonControl.GetChildCount(); j++)
        {
            var skillButton = SkillButtonControl.GetChild<SkillButton>(j);
            skillButton.Enable();
            skillButton.Modulate = SkillButton.EnabledModulate;
        }
        SelfFrame.Selected.Visible = true;
        BattleNode.RetreatButton.Disabled = !BattleNode.CanManualRetreat();
        BattleNode?.MapNode?.PlayerResourceState?.SetItemsEnabled(true);
    }

    public override void EndAction()
    {
        SelfFrame.Selected.Visible = false;
        if (BattleNode?.SuppressSpeedGainThisTurn != true)
        {
            BattleNode.PlayerSpeed += BattleNode
                .PlayersList.Where(x => x.State != CharacterState.Dying)
                .Sum(x => x.Speed);
        }

        BattleNode.RetreatButton.Disabled = true;
        DisableSkill();
        BattleNode?.MapNode?.PlayerResourceState?.SetItemsEnabled(false);
        base.EndAction();
    }

    public override async Task GetHurt(float damage)
    {
        await base.GetHurt(damage);
        Tween tween = CreateTween();
        tween.TweenProperty(this, "position", OriginalPosition + 20 * Vector2.Left, 0.3f);
        tween.TweenProperty(this, "position", OriginalPosition, 0.2f);
    }

    public override void DisableSkill()
    {
        for (int j = 0; j < SkillButtonControl.GetChildCount(); j++)
        {
            var skillButton = SkillButtonControl.GetChild<SkillButton>(j);
            skillButton.Disabled = true;
            skillButton.Modulate = SkillButton.DisabledModulate;
        }
    }
}
