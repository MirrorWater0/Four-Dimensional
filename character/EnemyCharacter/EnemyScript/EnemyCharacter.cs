using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;

public partial class EnemyCharacter : Character
{
    private const float DefaultIntentWeight = 3f;
    private const float SpecialIntentWeightMin = 0.5f;
    private const float SpecialIntentWeightMax = 7f;
    private static readonly Color IntentionTargetPreviewColor = new(1f, 0.32f, 0.32f, 1f);

    public EnemyRegedit Registry;
    public Control IntentionContorl => field ??= GetNode<Control>("Intention");
    public ColorRect AttackIntention => field ??= GetNode<ColorRect>("Intention/Attack");
    public ColorRect SurviveIntention => field ??= GetNode<ColorRect>("Intention/Survive");
    public ColorRect SpecialIntention => field ??= GetNode<ColorRect>("Intention/Special");
    private ProgressBar _lifebar;
    public Battle Battle => field ??= GetNode("/root/Battle") as Battle;
    Label label => field ??= GetNode<Label>("Label");
    public int IntentionIndex;
    public int SpecialIntentThreshold { get; private set; } = 3;
    private Character[] _intentionPreviewTargets = Array.Empty<Character>();
    private int _intentionPreviewHoverDepth;

    public override void _Ready()
    {
        base._Ready();
        IsPlayer = false;
        Hoverframe.MouseEntered += OnIntentionPreviewHoverEntered;
        Hoverframe.MouseExited += OnIntentionPreviewHoverExited;
        IntentionContorl.MouseEntered += OnIntentionPreviewHoverEntered;
        IntentionContorl.MouseExited += OnIntentionPreviewHoverExited;
    }

    public override void _ExitTree()
    {
        HideIntentionTargetPreview();
        base._ExitTree();
    }

    public override void Initialize()
    {
        if (Registry != null)
        {
            CharacterName = Registry.CharacterName;
            PassiveName = Registry.PassiveName;
            PassiveDescription = Registry.PassiveDescription;
            SpecialIntentThreshold = Math.Max(0, Registry.SpecialIntentThreshold);
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
        IntentionIndex = RollIntentionIndex();
        DisplayIntention();
        base.EndAction();
    }

    public int RollIntentionIndex()
    {
        if (Skills == null || Skills.Length == 0)
            return 0;

        float totalWeight = 0f;
        float[] weights = new float[Skills.Length];
        for (int i = 0; i < Skills.Length; i++)
        {
            float weight = GetIntentionWeight(Skills[i]);
            weights[i] = weight;
            totalWeight += weight;
        }

        if (totalWeight <= 0f)
        {
            for (int i = 0; i < Skills.Length; i++)
            {
                if (Skills[i] != null)
                    return i;
            }
            return 0;
        }

        float roll = (float)BattleNode.BattleIntentionRandom.NextDouble() * totalWeight;
        for (int i = 0; i < weights.Length; i++)
        {
            roll -= weights[i];
            if (roll <= 0f)
                return i;
        }

        return Math.Max(0, Skills.Length - 1);
    }

    private float GetIntentionWeight(Skill skill)
    {
        if (skill == null)
            return 0f;

        if (skill.SkillType != Skill.SkillTypes.Special)
            return DefaultIntentWeight;

        int energyDelta = Energy - SpecialIntentThreshold;
        return Math.Clamp(
            DefaultIntentWeight + energyDelta,
            SpecialIntentWeightMin,
            SpecialIntentWeightMax
        );
    }

    public async Task DisappearIntention()
    {
        HideIntentionTargetPreview();
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

        if (_intentionPreviewHoverDepth > 0)
            ShowIntentionTargetPreview();
    }

    private void OnIntentionPreviewHoverEntered()
    {
        _intentionPreviewHoverDepth++;
        if (_intentionPreviewHoverDepth == 1)
            ShowIntentionTargetPreview();
    }

    private void OnIntentionPreviewHoverExited()
    {
        _intentionPreviewHoverDepth = Math.Max(0, _intentionPreviewHoverDepth - 1);
        if (_intentionPreviewHoverDepth == 0)
            HideIntentionTargetPreview();
    }

    private void ShowIntentionTargetPreview()
    {
        HideIntentionTargetPreview();

        var skill = GetCurrentIntentionSkill();
        if (skill == null)
            return;

        _intentionPreviewTargets = skill
            .GetPreviewHostileTargets()
            .Where(GodotObject.IsInstanceValid)
            .Distinct()
            .ToArray();

        for (int i = 0; i < _intentionPreviewTargets.Length; i++)
        {
            _intentionPreviewTargets[i].ShowTargetPreview(IntentionTargetPreviewColor);
        }
    }

    private void HideIntentionTargetPreview()
    {
        if (_intentionPreviewTargets == null || _intentionPreviewTargets.Length == 0)
        {
            _intentionPreviewTargets = Array.Empty<Character>();
            return;
        }

        for (int i = 0; i < _intentionPreviewTargets.Length; i++)
        {
            if (GodotObject.IsInstanceValid(_intentionPreviewTargets[i]))
                _intentionPreviewTargets[i].HideTargetPreview();
        }

        _intentionPreviewTargets = Array.Empty<Character>();
    }

    private Skill GetCurrentIntentionSkill()
    {
        if (Skills == null || IntentionIndex < 0 || IntentionIndex >= Skills.Length)
            return null;

        return Skills[IntentionIndex];
    }
}
