using Godot;

public partial class BattleUnitName : Label {
    [Export] public Color PlayerColor { get; set; } = new(0.48f, 0.79f, 0.66f);
    [Export] public Color EnemyColor { get; set; } = new(0.80f, 0.65f, 0.63f);

    public override void _Ready() {
        if (GetParent()?.GetParent() is Character character) {
            Text = character.CharacterName;
            SelfModulate = character.IsPlayer ? PlayerColor : EnemyColor;
        }
    }
}
