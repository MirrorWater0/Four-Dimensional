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
    private Character[] _previewTargets = Array.Empty<Character>();
    private readonly List<Label> _previewDamageLabels = new();
    private static readonly Color TargetPreviewColor = new(1f, 0.32f, 0.32f, 1f);
    private static readonly Vector2 DamagePreviewLabelOffset = new(-50f, -130f);
    private static readonly Color DamagePreviewColor = new(1f, 0.84f, 0.63f, 1f);
    private static readonly Color DamagePreviewOutlineColor = new(0.02f, 0.03f, 0.06f, 0.95f);

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

        // Show tooltip with skill description
        if (SelfSkill != null && globalTooltip != null)
        {
            globalTooltip.FollowMouse = true;
            SelfSkill.UpdateDescription();
            globalTooltip.SetText(SelfSkill.Description);
        }

        ShowTargetPreview();
        ShowDamagePreview();
    }

    public void mouse_exited()
    {
        Modulate -= changeColor;

        // Hide tooltip
        globalTooltip?.HideTooltip();

        HideDamagePreview();
        HideTargetPreview();
    }

    public override void _ExitTree()
    {
        HideDamagePreview();
        HideTargetPreview();
        FreeDamagePreviewLabels();
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

    private void ShowTargetPreview()
    {
        HideTargetPreview();
        if (SelfSkill == null)
            return;

        _previewTargets = SelfSkill.GetPreviewHostileTargets();
        if (_previewTargets == null || _previewTargets.Length == 0)
        {
            _previewTargets = Array.Empty<Character>();
            return;
        }

        foreach (var target in _previewTargets.Where(GodotObject.IsInstanceValid))
        {
            target.ShowTargetPreview(TargetPreviewColor);
        }
    }

    private void ShowDamagePreview()
    {
        HideDamagePreview();
        if (SelfSkill == null)
            return;

        var entries = SelfSkill.GetPreviewHostileDamageEntries();
        if (entries == null || entries.Length == 0)
            return;

        var layer = EnsureTipLayer();
        if (layer == null)
            return;

        for (int i = 0; i < entries.Length; i++)
        {
            var entry = entries[i];
            if (entry.Target == null || !GodotObject.IsInstanceValid(entry.Target))
                continue;

            var label = GetOrCreateDamageLabel(layer, i);
            ShowDamageLabel(label, entry, GetTargetScreenPosition(entry.Target));
        }
    }

    private void HideTargetPreview()
    {
        if (_previewTargets == null || _previewTargets.Length == 0)
        {
            _previewTargets = Array.Empty<Character>();
            return;
        }

        foreach (var target in _previewTargets.Where(GodotObject.IsInstanceValid))
        {
            target.HideTargetPreview();
        }

        _previewTargets = Array.Empty<Character>();
    }

    private void HideDamagePreview()
    {
        for (int i = 0; i < _previewDamageLabels.Count; i++)
        {
            if (GodotObject.IsInstanceValid(_previewDamageLabels[i]))
                _previewDamageLabels[i].Visible = false;
        }
    }

    private void FreeDamagePreviewLabels()
    {
        for (int i = 0; i < _previewDamageLabels.Count; i++)
        {
            if (GodotObject.IsInstanceValid(_previewDamageLabels[i]))
                _previewDamageLabels[i].QueueFree();
        }
        _previewDamageLabels.Clear();
    }

    private Label GetOrCreateDamageLabel(CanvasLayer layer, int index)
    {
        while (_previewDamageLabels.Count <= index)
        {
            var label = CreateDamageLabel();
            layer.AddChild(label);
            _previewDamageLabels.Add(label);
        }

        var pooledLabel = _previewDamageLabels[index];
        if (!GodotObject.IsInstanceValid(pooledLabel))
        {
            pooledLabel = CreateDamageLabel();
            layer.AddChild(pooledLabel);
            _previewDamageLabels[index] = pooledLabel;
        }
        else if (pooledLabel.GetParent() == null)
        {
            layer.AddChild(pooledLabel);
        }

        return pooledLabel;
    }

    private static Label CreateDamageLabel()
    {
        var label = new Label
        {
            Visible = false,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            ClipText = false,
        };
        label.AddThemeFontSizeOverride("font_size", 28);
        label.AddThemeConstantOverride("outline_size", 5);
        label.AddThemeColorOverride("font_color", DamagePreviewColor);
        label.AddThemeColorOverride("font_outline_color", DamagePreviewOutlineColor);
        return label;
    }

    private void ShowDamageLabel(Label label, Skill.PreviewDamageEntry entry, Vector2 targetScreenPosition)
    {
        label.Text =
            entry.HitCount > 1
                ? $"{entry.Damage}({entry.HitCount}次)"
                : entry.Damage.ToString();
        label.AddThemeColorOverride("font_color", DamagePreviewColor);
        label.AddThemeColorOverride("font_outline_color", DamagePreviewOutlineColor);
        label.Modulate = Colors.White;
        label.Scale = Vector2.One;
        label.Visible = true;

        Vector2 size = label.GetCombinedMinimumSize();
        if (size == Vector2.Zero)
            size = new Vector2(120f, 44f);
        label.Size = size;

        Vector2 anchor = targetScreenPosition + DamagePreviewLabelOffset;
        label.Position = anchor - size / 2f;
    }

    private CanvasLayer EnsureTipLayer()
    {
        var root = GetTree()?.Root;
        if (root == null)
            return null;

        var existingLayer = root.GetNodeOrNull<CanvasLayer>("TipLayer");
        if (existingLayer != null)
            return existingLayer;

        existingLayer = new CanvasLayer { Layer = 6, Name = "TipLayer" };
        root.AddChild(existingLayer);
        return existingLayer;
    }

    private static Vector2 GetTargetScreenPosition(Character target)
    {
        if (target == null || !GodotObject.IsInstanceValid(target))
            return Vector2.Zero;

        return target.GetGlobalTransformWithCanvas().Origin;
    }
}
