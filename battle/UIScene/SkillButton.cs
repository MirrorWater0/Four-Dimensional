using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public partial class SkillButton : Button
{
    public const float DisabledAlpha = 0.7f;
    public static readonly Color EnabledModulate = new Color(1, 1, 1, 1f);
    public static readonly Color DisabledModulate = new Color(1, 1, 1, DisabledAlpha);

    [Export]
    public Skill.SkillTypes MySkillType;
    public Frame SelfFrame => field ??= GetParent().GetParent() as Frame;
    public VBoxContainer SelfContainer => field ??= GetParent() as VBoxContainer;

    public Vector2 PositionIndex;
    public Skill SelfSkill;
    private ColorRect ShockWave => field ??= GetNode<ColorRect>("ShockWave");
    private ColorRect AbleRing => field ??= GetNode<ColorRect>("AbleRing");
    private ColorRect SparkLight => field ??= GetNode<ColorRect>("SparkLight");
    private ColorRect SwordIcon => field ??= GetNode<ColorRect>("SwordIcon");
    private ColorRect RhomboidIcon => field ??= GetNode<ColorRect>("RhomboidIcon");
    private ColorRect TerminateSkillIcon => field ??= GetNode<ColorRect>("TerminateSkillIcon");
    Color HangColor = new Color(0.6f, 0.7f, 1.2f);
    bool animating = false;
    public Tip globalTooltip => field ??= EnsureGlobalTooltip();

    public override void _Ready()
    {
        MouseEntered += mouse_entered;
        MouseExited += mouse_exited;
        Modulate = new Color(0.9f, 0.9f, 0.9f);

        ApplySkillTypeIcons();
    }

    public void SetSkillType(Skill.SkillTypes skillType)
    {
        MySkillType = skillType;
        ApplySkillTypeIcons();
    }

    private void ApplySkillTypeIcons()
    {
        if (!IsInsideTree())
            return;

        SwordIcon.Visible = true;
        RhomboidIcon.Visible = true;
        TerminateSkillIcon.Visible = true;

        switch (MySkillType)
        {
            case Skill.SkillTypes.Attack:
                RhomboidIcon.Visible = false;
                TerminateSkillIcon.Visible = false;
                break;
            case Skill.SkillTypes.Survive:
                SwordIcon.Visible = false;
                TerminateSkillIcon.Visible = false;
                break;
            case Skill.SkillTypes.Special:
                RhomboidIcon.Visible = false;
                SwordIcon.Visible = false;
                break;
        }
    }

    public override void _Process(double delta)
    {
        if (Disabled)
        {
            AbleRing.Visible = false;
        }
        else
        {
            AbleRing.Visible = true;
        }
    }

    Color changeColor = 0.4f * new Color(0.5f, 0.5f, 0.5f, 0.4f);

    public void mouse_entered()
    {
        Modulate += changeColor;

        if (SelfSkill != null && globalTooltip != null)
        {
            globalTooltip.FollowMouse = true;
            SelfSkill.UpdateDescription();
            string buffTooltipText = Skill.BuildKeywordTooltipText(SelfSkill);
            if (string.IsNullOrWhiteSpace(buffTooltipText))
                globalTooltip.HideTooltip();
            else
                globalTooltip.SetText(buffTooltipText);
        }

    }

    public void mouse_exited()
    {
        Modulate -= changeColor;

        // Hide tooltip
        globalTooltip?.HideTooltip();

    }

    public override void _ExitTree()
    {
        base._ExitTree();
    }

    private Tip EnsureGlobalTooltip()
    {
        var tree = GetTree();
        var root = tree?.Root;
        if (root == null)
            return null;

        var layer = root.GetNodeOrNull<CanvasLayer>("TipLayer");
        if (layer == null)
        {
            layer = new CanvasLayer { Layer = 6, Name = "TipLayer" };
            root.AddChild(layer);
        }

        var tip = layer.GetNodeOrNull<Tip>("Tip");
        if (tip != null)
            return tip;

        var tipScene = GD.Load<PackedScene>("res://battle/UIScene/Tip.tscn");
        if (tipScene == null)
            return null;

        tip = tipScene.Instantiate<Tip>();
        tip.Name = "Tip";
        tip.FollowMouse = true;
        tip.AnchorOffset = new Vector2(20f, 20f);
        layer.AddChild(tip);
        return tip;
    }

    public void Enable()
    {
        Disabled = false;
        var tween = ShockWave.CreateTween();

        CreateTween().Parallel().TweenCallback(Callable.From(() => animating = false));
        tween
            .TweenMethod(
                Callable.From<float>(value =>
                    ((ShaderMaterial)SparkLight.Material).SetShaderParameter("progress", value)
                ),
                0.3,
                1f,
                0.4f
            )
            .SetEase(Tween.EaseType.Out);

        tween
            .Parallel()
            .TweenMethod(
                Callable.From<float>(value =>
                    ((ShaderMaterial)ShockWave.Material).SetShaderParameter("progress", value)
                ),
                0.3,
                1f,
                0.4f
            )
            .SetEase(Tween.EaseType.Out);
    }

    private static string BuildBuffTooltipText(Skill skill)
    {
        if (skill == null)
            return string.Empty;

        string description = skill.Description ?? string.Empty;
        if (string.IsNullOrWhiteSpace(description))
            return string.Empty;

        string plainDescription = StripBbCodeTags(description);
        List<(Buff.BuffName Name, int Index)> matchedBuffs = new();

        foreach (Buff.BuffName buffName in Enum.GetValues(typeof(Buff.BuffName)))
        {
            string displayName = Buff.GetBuffDisplayName(buffName);
            if (string.IsNullOrWhiteSpace(displayName))
                continue;

            int matchIndex = plainDescription.IndexOf(displayName, StringComparison.Ordinal);
            if (matchIndex < 0)
                continue;

            matchedBuffs.Add((buffName, matchIndex));
        }

        if (matchedBuffs.Count == 0)
            return string.Empty;

        return string.Join(
            "\n\n",
            matchedBuffs
                .OrderBy(entry => entry.Index)
                .Select(entry => BuildBuffTooltipEntry(entry.Name))
                .Where(entry => !string.IsNullOrWhiteSpace(entry))
        );
    }

    private static string BuildBuffTooltipEntry(Buff.BuffName buffName)
    {
        string effectText = Buff.GetBuffEffectText(buffName);
        if (string.IsNullOrWhiteSpace(effectText))
            return string.Empty;

        string formattedEffect = GlobalFunction.ColorizeKeywords(
            GlobalFunction.ColorizeNumbers(effectText)
        );
        string plainBuffName = Buff.GetBuffDisplayName(buffName);
        return $"{Buff.BuildTooltipIconTag(buffName)}          [outline_size=0][color=#a8f0ad]{plainBuffName}[/color][/outline_size]\n{formattedEffect}";
    }

    private static string StripBbCodeTags(string text)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        char[] buffer = new char[text.Length];
        int count = 0;
        bool inTag = false;

        foreach (char ch in text)
        {
            if (ch == '[')
            {
                inTag = true;
                continue;
            }

            if (ch == ']')
            {
                inTag = false;
                continue;
            }

            if (!inTag)
                buffer[count++] = ch;
        }

        return new string(buffer, 0, count);
    }
}
