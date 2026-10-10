using Godot;

// The same scene-authored row settings drive the editor preview and live hand.
[Tool]
public partial class BattleHandLayout : Control {
    [Export] public float CardScale { get; set; } = 0.9f;
    [Export] public float CardYOffset { get; set; } = 27f;
    [Export] public float HoverLift { get; set; } = -84f;
    [Export] public float HoverScale { get; set; } = 1.24f;
    [Export] public float AreaPadding { get; set; } = 18f;
    [Export] public float MinStepRatio { get; set; } = 0.42f;
    [Export] public float OverlapStepRatio { get; set; } = 5f / 6f;
    [Export] public float HoverSpreadRatio { get; set; } = 0.82f;
    [Export] public float HoverSpreadBase { get; set; } = 14f;
    [Export] public float MaxHoverSpread { get; set; } = 58f;
    [Export] public float SidePadding { get; set; } = 80f;
    [Export(PropertyHint.Range, "1,10,1")] public int PreviewCardCount { get; set; } = 5;

    public float GetCardStep(int count, Vector2 cardSize, float width) => count > 1
        ? Mathf.Clamp((Mathf.Max(cardSize.X, width - SidePadding) - cardSize.X) / (count - 1),
            cardSize.X * MinStepRatio, cardSize.X * OverlapStepRatio) : 0f;

    public override void _Process(double delta) {
        if (!Engine.IsEditorHint()) {
            SetProcess(false);
            return;
        }
        Vector2 cardSize = new Vector2(240f, 370f) * CardScale;
        int count = Mathf.Clamp(PreviewCardCount, 1, 10);
        float step = GetCardStep(count, cardSize, Size.X);
        float start = (Size.X - cardSize.X - (count - 1) * step) * 0.5f;
        for (int i = 0; i < 10; i++) {
            Control slot = GetNodeOrNull<Control>($"CardSlot{i}");
            if (slot == null)
                continue;
            slot.Visible = i < count;
            slot.Size = cardSize;
            slot.PivotOffsetRatio = Vector2.Zero;
            slot.PivotOffset = cardSize * 0.5f;
            slot.Position = new Vector2(start + i * step, Mathf.Max(0f, Size.Y - cardSize.Y) + CardYOffset);
            slot.Rotation = 0f;
            if (slot.GetNodeOrNull<Control>($"Card{i}") is { } card)
                card.Scale = Vector2.One * CardScale;
        }
    }
}
