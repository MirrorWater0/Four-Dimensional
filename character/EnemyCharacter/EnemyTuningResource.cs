using Godot;

[GlobalClass]
public partial class EnemyTuningResource : Resource {
    [Export] public string EnemyKey { get; set; } = string.Empty;
    [Export] public Godot.Collections.Dictionary<string, int> Values { get; set; } = new();

    public EnemyTuningResource Copy() {
        var copy = new EnemyTuningResource { EnemyKey = EnemyKey };
        foreach (var pair in Values)
            copy.Values[pair.Key] = pair.Value;
        return copy;
    }
}
