using Godot;
using System;

public partial class Tip : Control
{
    private ColorRect _bg;
    private Label _description;
    
    public ColorRect bg => _bg ??= GetNode<ColorRect>("bg");
    public Label Description => _description ??= GetNode<Label>("Description");
    
    public override void _Ready()
    {
        _bg = GetNode<ColorRect>("bg");
        _description = GetNode<Label>("Description");
        
        if (_description != null)
        {
            _description.AutowrapMode = TextServer.AutowrapMode.WordSmart;
            _description.CustomMinimumSize = new Vector2(300, 0);
        }
        
        Visible = false;
        
        // Connect to size changed signal to update tooltip size
        if (_description != null)
        {
            _description.Resized += OnDescriptionResized;
        }
    }
    
    private void OnDescriptionResized()
    {
        // Update background size when description size changes
        if (_bg != null && _description != null)
        {
            _bg.Size = _description.Size;
        }
    }
    
    public override void _Process(double delta)
    {
        base._Process(delta);
        
        // Only position if visible
        if (!Visible) return;
        
        // Get mouse position in viewport coordinates
        Viewport viewport = GetViewport();
        if (viewport == null) return;
        
        Vector2 mousePos = viewport.GetMousePosition();
        Vector2 tooltipSize = GetTooltipSize();
        Vector2 viewportSize = viewport.GetVisibleRect().Size;
        
        // Calculate tooltip position based on quadrant to avoid going off-screen
        Vector2 targetPosition = CalculateQuadrantPosition(mousePos, tooltipSize, viewportSize);
        
        // Set the calculated position
        Position = targetPosition;
    }
    
    /// <summary>
    /// Get the actual tooltip size (description size)
    /// </summary>
    private Vector2 GetTooltipSize()
    {
        if (_description != null)
        {
            return _description.Size;
        }
        return Size;
    }
    
    /// <summary>
    /// Calculate tooltip position to display in bottom-right corner of mouse,
    /// clamping to viewport bounds if necessary
    /// </summary>
    private Vector2 CalculateQuadrantPosition(Vector2 mousePos, Vector2 tooltipSize, Vector2 viewportSize)
    {
        const float offset = 20f;
        const float margin = 10f;
        
        // Default position: right and down from mouse
        Vector2 targetPosition = new Vector2(mousePos.X + offset, mousePos.Y + offset);
        
        // Clamp to viewport bounds if tooltip would go off-screen
        // Clamp X position
        if (targetPosition.X + tooltipSize.X > viewportSize.X - margin)
        {
            targetPosition.X = viewportSize.X - tooltipSize.X - margin;
        }
        
        // Clamp Y position
        if (targetPosition.Y + tooltipSize.Y > viewportSize.Y - margin)
        {
            targetPosition.Y = viewportSize.Y - tooltipSize.Y - margin;
        }
        
        // Ensure minimum position to avoid going off left/top edges
        targetPosition.X = Mathf.Max(targetPosition.X, margin);
        targetPosition.Y = Mathf.Max(targetPosition.Y, margin);
        
        return targetPosition;
    }

    /// <summary>
    /// Set the tooltip text and make it visible
    /// </summary>
    public void SetText(string text)
    {
        if (_description != null)
        {
            _description.Text = text;
            Visible = true;
        }
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
        if (_description != null)
        {
            _description.Text = text;
            
            // Force update the label size immediately
            _description.ForceUpdateTransform();
            
            // Calculate position based on quadrant to stay within screen bounds
            Viewport viewport = GetViewport();
            if (viewport != null)
            {
                Vector2 tooltipSize = GetTooltipSize();
                Vector2 viewportSize = viewport.GetVisibleRect().Size;
                Vector2 targetPosition = CalculateQuadrantPosition(position, tooltipSize, viewportSize);
                Position = targetPosition;
            }
            else
            {
                Position = position;
            }
            
            Visible = true;
        }
    }
}
