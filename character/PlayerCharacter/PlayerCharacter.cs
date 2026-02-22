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
        PositionIndex = GameInfo.PlayerCharacters[CharacterIndex].PositionIndex;
        Skills =
        [
            Skill.GetSkill(GameInfo.PlayerCharacters[CharacterIndex].TakenSkills[0]),
            Skill.GetSkill(GameInfo.PlayerCharacters[CharacterIndex].TakenSkills[1]),
            Skill.GetSkill(GameInfo.PlayerCharacters[CharacterIndex].TakenSkills[2]),
        ];
        BattleLifemax = GameInfo.PlayerCharacters[CharacterIndex].LifeMax;
        Life = BattleLifemax;
        BattlePower = GameInfo.PlayerCharacters[CharacterIndex].Power;
        BattleSurvivability = GameInfo.PlayerCharacters[CharacterIndex].Survivability;
        Speed = GameInfo.PlayerCharacters[CharacterIndex].Speed;
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
