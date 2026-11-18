using Godot;

/// <summary>Explicit dependencies include all card resources in exported games.</summary>
[GlobalClass]
public partial class CardTuningCatalog : Resource {
    [Export] public Godot.Collections.Array<CardTuningResource> Cards { get; set; } = new();
}
