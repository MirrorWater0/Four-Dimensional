using System;
using System.Threading.Tasks;
using Godot;

public partial class CharacterControl {
    private void StartBattleCardAnimation(Func<Task> animation, string name) {
        _ = ObserveBattleCardAnimationAsync(animation, name);
    }

    private async Task ObserveBattleCardAnimationAsync(Func<Task> animation, string name) {
        try {
            await animation();
        }
        catch (Exception ex) {
            GD.PushWarning($"CharacterControl: {name} failed: {ex}");
        }
    }
}
