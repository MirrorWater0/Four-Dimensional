using System;
using Godot;

public partial class Tip : Control
{
    private static readonly Vector2 BackgroundPadding = new Vector2(28f, 24f);

    [Export]
    public bool FollowMouse = true;

    /// <summary>
    /// Offset in pixels from the anchor point (mouse or manual position).
    /// Negative X/Y means "place to the left/above" (edge-aligned).
    /// </summary>
    [Export]
    public Vector2 AnchorOffset = new Vector2(20f, 20f);

    private Vector2 _manualAnchorPosition = Vector2.Zero;

    public Panel bg => field ??= GetNode<Panel>("bg");
    public RichTextLabel Description => field ??= GetNode<RichTextLabel>("Description");

    public override void _Ready()
    {
        if (Description != null)
        {
            Description.AutowrapMode = TextServer.AutowrapMode.WordSmart;
            Description.CustomMinimumSize = new Vector2(340, 0);
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
        UpdateTooltipLayout();
        if (Visible)
            UpdateTooltipPosition();
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        // Only position if visible
        if (!Visible)
            return;

        UpdateTooltipLayout();
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

    /// <summary>
    /// Hide the tooltip
    /// </summary>
    public void HideTooltip()
    {
        Visible = false;
    }

    /// <summary>
    /// Set tooltip text and show it at a specific position
    /// </summary>
    public void ShowAtPosition(string text, Vector2 position)
    {
        ShowTooltip(text, followMouse: false, manualAnchorPosition: position);
    }

    private void ShowTooltip(string text, bool followMouse, Vector2 manualAnchorPosition)
    {
        if (Description == null)
            return;

        FollowMouse = followMouse;
        _manualAnchorPosition = manualAnchorPosition;
        Description.Text = text ?? string.Empty;

        UpdateTooltipLayout();
        UpdateTooltipPosition();
        Visible = true;
        CallDeferred(nameof(RefreshTooltipAfterShow));
    }

    private void RefreshTooltipAfterShow()
    {
        if (!Visible)
            return;

        UpdateTooltipLayout();
        UpdateTooltipPosition();
    }

    private void UpdateTooltipLayout()
    {
        if (Description == null)
            return;

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
            bg.Size = new Vector2(contentWidth, contentHeight) + BackgroundPadding;
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
    }
}
