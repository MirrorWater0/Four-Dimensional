using System;
using System.Collections.Generic;
using System.Text;
using Godot;

public partial class Tip : Control
{
    private static readonly Vector2 BackgroundPadding = new Vector2(28f, 24f);
    private static readonly Color DefaultTextColor = new Color(0.9f, 0.95f, 1f, 0.9f);
    private static readonly Color DefaultOutlineColor = new Color(0.01f, 0.02f, 0.05f, 0.64f);
    private const float FadeInDuration = 0.12f;
    private const float DefaultFadeOutDuration = 0.1f;
    private const string BuffIconTagPrefix = "[buff_icon=";
    private const string BuffIconTagSuffix = "]";
    private const float BuffTooltipIconSize = 24f;
    private const float BuffTooltipIconSourceSize = 40f;

    [Export]
    public bool FollowMouse = true;

    [Export(PropertyHint.Range, "0,1,0.01")]
    public float FadeOutDuration = DefaultFadeOutDuration;

    /// <summary>
    /// Offset in pixels from the anchor point (mouse or manual position).
    /// Negative X/Y means "place to the left/above" (edge-aligned).
    /// </summary>
    [Export]
    public Vector2 AnchorOffset = new Vector2(20f, 20f);

    [Export(PropertyHint.Range, "0,1,0.01")]
    public float AnchorHeightRatio = 0f;

    [Export]
    public float MinContentWidth = 340f;

    [Export]
    public Vector2 ContentPadding = BackgroundPadding;

    [Export]
    public int NormalFontSize = 19;

    [Export]
    public int OutlineSize = 2;

    [Export]
    public Color DefaultColor = DefaultTextColor;

    [Export]
    public Color OutlineColor = DefaultOutlineColor;

    [Export]
    public TextServer.AutowrapMode WrapMode = TextServer.AutowrapMode.WordSmart;

    private Vector2 _manualAnchorPosition = Vector2.Zero;
    private bool _layoutDirty = true;
    private bool _positionDirty = true;
    private string _lastText = string.Empty;
    private Tween _fadeTween;
    private readonly List<BuffIconPlacement> _buffIconPlacements = new();
    private readonly List<ColorRect> _buffTooltipIcons = new();

    private readonly struct BuffIconPlacement
    {
        public readonly Buff.BuffName BuffName;
        public readonly int LineIndex;

        public BuffIconPlacement(Buff.BuffName buffName, int lineIndex)
        {
            BuffName = buffName;
            LineIndex = lineIndex;
        }
    }

    public Panel bg => field ??= GetNode<Panel>("bg");
    public RichTextLabel Description => field ??= GetNode<RichTextLabel>("Description");

    public override void _Ready()
    {
        if (Description != null)
        {
            ApplyDescriptionTheme();
        }

        if (bg != null)
            bg.Position = Vector2.Zero;

        Visible = false;

        // Connect to size changed signal to update tooltip size
        if (Description != null)
        {
            Description.Resized += OnDescriptionResized;
        }
    }

    private void OnDescriptionResized()
    {
        MarkLayoutDirty();
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        // Only position if visible
        if (!Visible)
            return;

        if (_layoutDirty)
            UpdateTooltipLayout();

        if (FollowMouse || _positionDirty)
            UpdateTooltipPosition();
    }

    /// <summary>
    /// Get the actual tooltip size (description size)
    /// </summary>
    private Vector2 GetTooltipSize()
    {
        if (bg != null)
            return bg.Size;

        if (Description != null)
            return Description.Size;

        return Size;
    }

    /// <summary>
    /// Calculate tooltip position relative to the anchor point, clamping to viewport bounds.
    /// </summary>
    private Vector2 CalculateAnchorPosition(
        Vector2 anchorPos,
        Vector2 tooltipSize,
        Vector2 viewportSize
    )
    {
        const float margin = 10f;

        float x = anchorPos.X + AnchorOffset.X;
        float y = anchorPos.Y + AnchorOffset.Y;

        if (AnchorOffset.X < 0)
            x -= tooltipSize.X;
        if (AnchorOffset.Y < 0)
            y -= tooltipSize.Y;

        if (AnchorHeightRatio > 0f)
            y -= tooltipSize.Y * AnchorHeightRatio;

        float minX = margin;
        float maxX = viewportSize.X - tooltipSize.X - margin;
        float minY = margin;
        float maxY = viewportSize.Y - tooltipSize.Y - margin;

        // If the tooltip is larger than the viewport, avoid invalid Clamp(min > max).
        x = maxX < minX ? minX : Mathf.Clamp(x, minX, maxX);
        y = maxY < minY ? minY : Mathf.Clamp(y, minY, maxY);
        return new Vector2(x, y);
    }

    /// <summary>
    /// Set the tooltip text and make it visible
    /// </summary>
    public void SetText(string text)
    {
        ShowTooltip(text, followMouse: true, manualAnchorPosition: default);
    }

    public void PreloadText(string text)
    {
        if (Description == null)
            return;

        ApplyDescriptionTheme();
        string normalizedText = text ?? string.Empty;
        if (!string.Equals(_lastText, normalizedText, StringComparison.Ordinal))
        {
            Description.Text = ExtractBuffIconTags(normalizedText);
            _lastText = normalizedText;
            RebuildBuffTooltipIcons();
            MarkLayoutDirty();
        }

        if (_layoutDirty)
            UpdateTooltipLayout();
    }

    public void RefreshTextSizeFromSettings()
    {
        ApplyDescriptionTheme();
        MarkLayoutDirty();
        if (Visible)
        {
            UpdateTooltipLayout();
            UpdateTooltipPosition();
        }
    }

    /// <summary>
    /// Hide the tooltip
    /// </summary>
    public void HideTooltip()
    {
        _fadeTween?.Kill();

        if (!Visible)
        {
            _positionDirty = true;
            return;
        }

        if (FadeOutDuration <= 0f || !IsInsideTree())
        {
            Visible = false;
            _positionDirty = true;
            return;
        }

        _fadeTween = CreateTween();
        _fadeTween.TweenProperty(this, "modulate:a", 0f, FadeOutDuration)
            .SetEase(Tween.EaseType.In)
            .SetTrans(Tween.TransitionType.Quad);
        _fadeTween.TweenCallback(
            Callable.From(() =>
            {
                Visible = false;
                _positionDirty = true;
            })
        );
    }

    /// <summary>
    /// Set tooltip text and show it at a specific position
    /// </summary>
    public void ShowAtPosition(string text, Vector2 position)
    {
        ShowTooltip(text, followMouse: false, manualAnchorPosition: position);
    }

    public void ShowPreloaded(bool followMouse = true, Vector2? manualAnchorPosition = null)
    {
        FollowMouse = followMouse;
        _manualAnchorPosition = manualAnchorPosition ?? Vector2.Zero;
        if (_layoutDirty)
            UpdateTooltipLayout();
        UpdateTooltipPosition();
        _fadeTween?.Kill();
        Modulate = new Color(1f, 1f, 1f, 0f);
        Visible = true;
        _fadeTween = CreateTween();
        _fadeTween.TweenProperty(this, "modulate:a", 1f, FadeInDuration)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Quad);
    }

    private void ShowTooltip(string text, bool followMouse, Vector2 manualAnchorPosition)
    {
        if (Description == null)
            return;

        FollowMouse = followMouse;
        _manualAnchorPosition = manualAnchorPosition;
        string normalizedText = text ?? string.Empty;
        bool textChanged = false;
        if (!string.Equals(_lastText, normalizedText, StringComparison.Ordinal))
        {
            ApplyDescriptionTheme();
            Description.Text = ExtractBuffIconTags(normalizedText);
            _lastText = normalizedText;
            RebuildBuffTooltipIcons();
            MarkLayoutDirty();
            textChanged = true;
        }

        bool needsLayoutRefresh = _layoutDirty || textChanged;
        if (needsLayoutRefresh)
            UpdateTooltipLayout();

        UpdateTooltipPosition();
        _fadeTween?.Kill();
        Modulate = new Color(1f, 1f, 1f, 0f);
        Visible = true;
        _fadeTween = CreateTween();
        _fadeTween.TweenProperty(this, "modulate:a", 1f, FadeInDuration)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Quad);
        if (needsLayoutRefresh)
            CallDeferred(nameof(RefreshTooltipAfterShow));
    }

    private void RefreshTooltipAfterShow()
    {
        if (!Visible)
            return;

        MarkLayoutDirty();
        UpdateTooltipLayout();
        UpdateTooltipPosition();
    }

    private void UpdateTooltipLayout()
    {
        if (Description == null)
            return;

        ApplyDescriptionTheme();
        Description.CustomMinimumSize = new Vector2(MinContentWidth, 0);

        float contentWidth = Math.Max(
            Description.CustomMinimumSize.X,
            Description.GetCombinedMinimumSize().X
        );
        float contentHeight = Math.Max(
            Description.GetCombinedMinimumSize().Y,
            Description.GetContentHeight()
        );

        if (contentWidth <= 0f)
            contentWidth = Description.Size.X;
        if (contentHeight <= 0f)
            contentHeight = Description.Size.Y;

        Description.Size = new Vector2(contentWidth, contentHeight);

        if (bg != null)
            bg.Size = new Vector2(contentWidth, contentHeight) + ContentPadding;

        PositionBuffTooltipIcons();
        _layoutDirty = false;
        _positionDirty = true;
    }

    private void ApplyDescriptionTheme()
    {
        if (Description == null)
            return;

        Description.AutowrapMode = WrapMode;
        Description.CustomMinimumSize = new Vector2(MinContentWidth, 0);
        Description.AddThemeFontSizeOverride(
            "normal_font_size",
            UserSettings.ScaleTextFontSize(NormalFontSize)
        );
        Description.AddThemeConstantOverride("outline_size", OutlineSize);
        Description.AddThemeColorOverride("default_color", DefaultColor);
        Description.AddThemeColorOverride("font_outline_color", OutlineColor);
    }

    private void UpdateTooltipPosition()
    {
        Viewport viewport = GetViewport();
        if (viewport == null)
            return;

        Vector2 anchorPos = FollowMouse ? viewport.GetMousePosition() : _manualAnchorPosition;
        Vector2 tooltipSize = GetTooltipSize();
        Vector2 viewportSize = viewport.GetVisibleRect().Size;
        Position = CalculateAnchorPosition(anchorPos, tooltipSize, viewportSize);
        _positionDirty = false;
    }

    private string ExtractBuffIconTags(string text)
    {
        _buffIconPlacements.Clear();
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        var cleaned = new StringBuilder(text.Length);
        int lineIndex = 0;
        for (int i = 0; i < text.Length;)
        {
            if (text[i] == '\n')
            {
                cleaned.Append(text[i]);
                i++;
                lineIndex++;
                continue;
            }

            if (text.IndexOf(BuffIconTagPrefix, i, StringComparison.Ordinal) == i)
            {
                int nameStart = i + BuffIconTagPrefix.Length;
                int tagEnd = text.IndexOf(BuffIconTagSuffix, nameStart, StringComparison.Ordinal);
                if (tagEnd > nameStart)
                {
                    string nameText = text.Substring(nameStart, tagEnd - nameStart);
                    if (Enum.TryParse(nameText, out Buff.BuffName buffName))
                        _buffIconPlacements.Add(new BuffIconPlacement(buffName, lineIndex));

                    i = tagEnd + BuffIconTagSuffix.Length;
                    continue;
                }
            }

            cleaned.Append(text[i]);
            i++;
        }

        return cleaned.ToString();
    }

    private void RebuildBuffTooltipIcons()
    {
        ClearBuffTooltipIcons();
        foreach (var placement in _buffIconPlacements)
        {
            var icon = Buff.CreateBuffTooltipIcon(placement.BuffName);
            if (icon == null)
                continue;

            icon.Visible = false;
            icon.ZIndex = 1;
            icon.Size = new Vector2(BuffTooltipIconSourceSize, BuffTooltipIconSourceSize);
            icon.Scale = Vector2.One * (BuffTooltipIconSize / BuffTooltipIconSourceSize);
            AddChild(icon);
            _buffTooltipIcons.Add(icon);
        }
    }

    private void ClearBuffTooltipIcons()
    {
        for (int i = 0; i < _buffTooltipIcons.Count; i++)
        {
            var icon = _buffTooltipIcons[i];
            if (icon != null && GodotObject.IsInstanceValid(icon))
            {
                icon.Visible = false;
                icon.QueueFree();
            }
        }

        _buffTooltipIcons.Clear();
    }

    private void PositionBuffTooltipIcons()
    {
        if (_buffTooltipIcons.Count == 0 || Description == null)
            return;

        float lineHeight = UserSettings.ScaleTextFontSize(NormalFontSize) + 4f;
        float x = Description.Position.X + 1f;
        int count = Math.Min(_buffTooltipIcons.Count, _buffIconPlacements.Count);
        for (int i = 0; i < count; i++)
        {
            var icon = _buffTooltipIcons[i];
            if (icon == null || !GodotObject.IsInstanceValid(icon))
                continue;

            float y = Description.Position.Y + _buffIconPlacements[i].LineIndex * lineHeight + 6f;
            icon.Position = new Vector2(x, y);
            icon.Visible = true;
        }
    }

    private void MarkLayoutDirty()
    {
        _layoutDirty = true;
        _positionDirty = true;
    }
}
