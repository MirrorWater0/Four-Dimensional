using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;

public partial class EnemyCharacter : Character
{
    public Control IntentionContorl => field ??= GetNode<Control>("Intention");
    public ColorRect AttackIntention => field ??= GetNode<ColorRect>("Intention/Attack");
    private ProgressBar _lifebar;
    public Battle Battle => field ??= GetNode("/root/Battle") as Battle;
    Label label => field ??= GetNode<Label>("Label");
    public int IntentionIndex;
    public override void _Ready()
    {
        base._Ready();
        IntentionContorl.Visible = true;
        IsPlayer = false;
    }

    public override void Initialize()
    {
        base.Initialize();
    }

    public override async void StartAction()
    {
        base.StartAction();
        await Skills[IntentionIndex].Effect();
        EndAction();
    }

    public override void EndAction()
    {
        IntentionIndex = BattleNode.BattleIntentionRandom.Next(0, Skills.Length);
        DisplayIntention();

        BattleNode.EnemySpeed += BattleNode
            .EnemiesList.Where(x => x.State != CharacterState.Dying)
            .Sum(x => x.Speed);
        base.EndAction();
    }

    public override async Task GetHurt(float damage)
    {
        await base.GetHurt(damage);
        Tween tween = CreateTween();
        tween.TweenProperty(this, "position", OriginalPosition + 20 * Vector2.Right, 0.3f);
        tween.TweenProperty(this, "position", OriginalPosition, 0.2f);
    }

    public override void Dying()
    {
        BattleNode.EnemiesDyingNum++;
        base.Dying();
    }

    public void DisplayIntention()
    {
        var skill = Skills[IntentionIndex];
        switch (skill.SkillType)
        {
            case Skill.SkillTypes.Attack:
                AttackIntention.Visible = true;
                break;
            case Skill.SkillTypes.Defence:
                break;
            case Skill.SkillTypes.Special:
                break;
        }
    }
}
