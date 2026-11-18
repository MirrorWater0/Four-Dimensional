using Godot;

// Explicit references keep every enemy definition in exported games.
[GlobalClass]
public partial class EnemyTuningCatalog : Resource {
    [Export] public Godot.Collections.Array<EnemyTuningResource> Enemies { get; set; } = new();
}
