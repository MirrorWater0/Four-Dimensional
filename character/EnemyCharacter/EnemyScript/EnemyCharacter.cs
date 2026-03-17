using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;

public partial class EnemyCharacter : Character
{
    public EnemyRegedit Registry;
    public Control IntentionContorl => field ??= GetNode<Control>("Intention");
    public ColorRect AttackIntention => field ??= GetNode<ColorRect>("Intention/Attack");
    public ColorRect SurviveIntention => field ??= GetNode<ColorRect>("Intention/Survive");
    public ColorRect SpecialIntention => field ??= GetNode<ColorRect>("Intention/Special");
    private ProgressBar _lifebar;
    public Battle Battle => field ??= GetNode("/root/Battle") as Battle;
    Label label => field ??= GetNode<Label>("Label");
    public int IntentionIndex;

    public override void _Ready()
    {
        base._Ready();
        IsPlayer = false;
    }

    public override void Initialize()
    {
        if (Registry != null)
        {
            CharacterName = Registry.CharacterName;
            PassiveName = Registry.PassiveName;
            PassiveDescription = Registry.PassiveDescription;
            SetCombatStats(Registry.Power, Registry.Survivability, Registry.Speed, Registry.MaxLife);
            Life = BattleMaxLife;
            Skills = (Registry.SkillIDs ?? Array.Empty<SkillID>())
                .Select(Skill.GetSkill)
                .Where(x => x != null)
                .ToArray();
            if (Skills.Length == 0)
                Skills = new Skill[3];
        }
        base.Initialize();
    }

    public override async void StartAction()
    {
        await ToSignal(GetTree().CreateTimer(0.4f), "timeout");
        base.StartAction();

        await DisappearIntention();
        await Skills[IntentionIndex].Effect();
        EndAction();
    }

    public override void EndAction()
    {
        IntentionIndex = BattleNode.BattleIntentionRandom.Next(0, Skills.Length);
        DisplayIntention();

        if (BattleNode?.SuppressSpeedGainThisTurn != true)
        {
            BattleNode.EnemySpeed += BattleNode
                .EnemiesList.Where(x => x.State != CharacterState.Dying)
                .Sum(x => x.Speed);
        }
        base.EndAction();
    }

    public override async Task GetHurt(float damage)
    {
        await base.GetHurt(damage);
        Tween tween = CreateTween();
        tween.TweenProperty(this, "position", OriginalPosition + 20 * Vector2.Right, 0.3f);
        tween.TweenProperty(this, "position", OriginalPosition, 0.2f);
    }

    public async Task DisappearIntention()
    {
        Buff.GhostExplode(IntentionContorl, new Vector2(2, 2));
        await ToSignal(GetTree().CreateTimer(0.3f), "timeout");
        AttackIntention.Visible = false;
        SurviveIntention.Visible = false;
        SpecialIntention.Visible = false;
    }

    public void DisplayIntention()
    {
        var skill = Skills[IntentionIndex];
        IntentionContorl.Modulate = new Color(1, 1, 1, 0);
        IntentionContorl.Scale = new Vector2(1.8f, 1.8f);
        switch (skill.SkillType)
        {
            case Skill.SkillTypes.Attack:
                AttackIntention.Visible = true;
                break;
            case Skill.SkillTypes.Survive:
                SurviveIntention.Visible = true;
                break;
            case Skill.SkillTypes.Special:
                SpecialIntention.Visible = true;
                break;
        }

        Tween tween = CreateTween();
        tween.SetParallel(true);
        tween.TweenProperty(IntentionContorl, "modulate", new Color(1, 1, 1, 1), 0.2f);
        tween
            .TweenProperty(IntentionContorl, "scale", new Vector2(1f, 1f), 0.2f)
            .SetEase(Tween.EaseType.Out);
    }
}
