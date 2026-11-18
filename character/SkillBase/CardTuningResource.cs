using Godot;

/// <summary>Base values only; character ownership and temporary battle state belong to Skill.</summary>
[GlobalClass]
public partial class CardTuningResource : Resource {
    [Export] public string SkillKey { get; set; } = string.Empty;
    [Export] public string DisplayName { get; set; } = string.Empty;
    [Export] public Godot.Collections.Dictionary<string, int> Values { get; set; } = new();

    public CardTuningResource Copy() {
        var copy = new CardTuningResource { SkillKey = SkillKey, DisplayName = DisplayName };
        foreach (var pair in Values)
            copy.Values[pair.Key] = pair.Value;
        return copy;
    }
}
