using Godot;

[Tool]
public partial class BattleCubeNode : Node2D {
    [Export] public float CubeSize { get; set; } = 420f;
    [Export] public float LineWidth { get; set; } = 1.1f;
    [Export] public float RotationSpeed { get; set; } = 0.42f;
    [Export] public Color LineTint { get; set; } = new(0.82f, 0.84f, 0.88f, 1f);
    [Export(PropertyHint.Range, "0,1,0.01")] public float LineAlpha { get; set; } = 0.08f;
    [Export] public bool Animate { get; set; } = true;

    public override void _Process(double delta) {
        if (Animate && !Engine.IsEditorHint())
            Rotation += (float)delta * RotationSpeed;
    }
}
