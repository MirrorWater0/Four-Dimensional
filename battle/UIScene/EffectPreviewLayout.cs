using Godot;

public partial class EffectPreviewLayout : VBoxContainer {
    [Export] public Vector2 TargetOffset { get; set; } = new(8f, 0f);
    [Export] public float ViewportMargin { get; set; } = 12f;
}
