using System;
using Godot;

public partial class Tip : Control
{
    private static readonly Vector2 BackgroundPadding = new Vector2(28f, 24f);
    private static readonly Color DefaultTextColor = new Color(0.9f, 0.95f, 1f, 0.9f);
    private static readonly Color DefaultOutlineColor = new Color(0.01f, 0.02f, 0.05f, 0.64f);
    private const float FadeInDuration = 0.12f;
    private const float DefaultFadeOutDuration = 0.1f;

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
            Description.Text = normalizedText;
            _lastText = normalizedText;
            MarkLayoutDirty();
        }

        if (_layoutDirty)
            UpdateTooltipLayout();
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
            Description.Text = normalizedText;
            _lastText = normalizedText;
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

        _layoutDirty = false;
        _positionDirty = true;
    }

    private void ApplyDescriptionTheme()
    {
        if (Description == null)
            return;

        Description.AutowrapMode = WrapMode;
        Description.CustomMinimumSize = new Vector2(MinContentWidth, 0);
        Description.AddThemeFontSizeOverride("normal_font_size", NormalFontSize);
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

    private void MarkLayoutDirty()
    {
        _layoutDirty = true;
        _positionDirty = true;
    }
}
