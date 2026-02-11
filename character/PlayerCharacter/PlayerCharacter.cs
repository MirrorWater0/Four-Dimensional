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
    }

    public override void StartAction()
    {
        base.StartAction();
        for (int j = 0; j < SkillButtonControl.GetChildCount(); j++)
        {
            SkillButtonControl.GetChild<SkillButton>(j).Enable();
            SkillButtonControl.GetChild<Button>(j).GetChild<Label>(0).Modulate = new Color(
                1,
                1,
                1,
                1f
            );
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
        GD.Print("ButtonNum:", SkillButtonControl.GetChildCount());
        for (int j = 0; j < SkillButtonControl.GetChildCount(); j++)
        {
            SkillButtonControl.GetChild<Button>(j).Disabled = true;
            SkillButtonControl.GetChild<Button>(j).GetChild<Label>(0).Modulate = new Color(
                1,
                1,
                1,
                0.3f
            );
        }
    }

    public override void Dying()
    {
        BattleNode.PlayerDyingNum++;
        base.Dying();
    }
}
