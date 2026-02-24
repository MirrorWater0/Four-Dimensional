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
        BattleMaxLife = info.LifeMax;
        Life = BattleMaxLife;
        BattlePower = info.Power;
        BattleSurvivability = info.Survivability;
        Speed = info.Speed;
        base.Initialize();
        IsPlayer = true;
        BlockLabel.Position += new Vector2(230, 0);
        BlockLabel.HorizontalAlignment = HorizontalAlignment.Left;
    }

    public override void StartAction()
    {
        if (StartActionBuffs.Any(x => x.ThisBuffName == Buff.BuffName.Stun))
        {
            base.StartAction();
            return;
        }

        base.StartAction();
        for (int j = 0; j < SkillButtonControl.GetChildCount(); j++)
        {
            var skillButton = SkillButtonControl.GetChild<SkillButton>(j);
            skillButton.Enable();
            skillButton.Modulate = SkillButton.EnabledModulate;
        }
        SelfFrame.Selected.Visible = true;
        BattleNode.RetreatButton.Disabled = false;
    }

    public override void EndAction()
    {
        SelfFrame.Selected.Visible = false;
        BattleNode.PlayerSpeed += BattleNode
            .PlayersList.Where(x => x.State != CharacterState.Dying)
            .Sum(x => x.Speed);

        BattleNode.RetreatButton.Disabled = true;
        DisableSkill();
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
