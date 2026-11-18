using System.Collections.Generic;
using Godot;

public partial class Map {
    private sealed class RevealPart {
        public Control Item;
        public Color Tint;
        public Vector2 AppliedOffset;
        public float Delay;
    }

    private readonly List<RevealPart> _revealParts = new();
    private Tween _mapRevealTween;
    private ColorRect _transitionMask;
    private bool _mapRevealPending;
    private bool _leavingForBattle;

    internal const string StartMapPath = "res://Map/Map.tscn";

    // Mark this particular map before _Ready, rather than waiting for SceneChanged.
    internal static Error ChangeSceneFromStart(SceneTree tree) {
        var scene = GD.Load<PackedScene>(StartMapPath);
        if (scene?.Instantiate<Map>() is not { } map) return Error.CantCreate;
        map._mapRevealPending = true;
        Error result = tree.ChangeSceneToNode(map);
        if (result != Error.Ok) map.Free();
        return result;
    }

    private void PrepareMapReveal() {
        _transitionMask = GetNodeOrNull<ColorRect>("/root/SceneTransitionLayer/Mask");
        if (_mapRevealPending) StageMapReveal();
    }

    internal void HideHudForBattleEntry() {
        if (_leavingForBattle || WarmupMode) return;
        ResetMapReveal();
        _leavingForBattle = true;
        StageMapReveal(includeRoute: false); // The selected node still plays its departure pulse.
    }

    internal void FinishBattleEntry() {
        if (!_leavingForBattle) return;
        _leavingForBattle = false;
        ResetMapReveal(); // Shared resource controls now belong to the battle HUD (or entry failed).
    }

    private void UpdateMapRevealState() {
        if (!_mapRevealPending || _battleWorldSuspensionRequested || _mapPeekModeActive || !IsVisibleInTree()) return;
        // Reveal the visible HUD under rewards as soon as the blackout lifts.
        if (GodotObject.IsInstanceValid(_transitionMask) && _transitionMask.IsVisibleInTree()
            && _transitionMask.Modulate.A >= 0.99f) return;
        _mapRevealPending = false;
        _mapRevealTween = CreateTween().SetParallel().SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
        foreach (var part in _revealParts) {
            if (!GodotObject.IsInstanceValid(part.Item)) continue;
            if (!part.Item.IsVisibleInTree()) {
                RestoreRevealPart(part);
                continue;
            }
            _mapRevealTween.TweenProperty(part.Item, "modulate:a", part.Tint.A, 0.30f).SetDelay(part.Delay);
            if (part.AppliedOffset != Vector2.Zero)
                _mapRevealTween.TweenMethod(Callable.From<Vector2>(value => ApplyRevealOffset(part, value)),
                    part.AppliedOffset, Vector2.Zero, 0.37f).SetDelay(part.Delay);
        }
        PlayerResourceState?.RevealPartyLifeBars(_mapRevealTween, 0.15f);
        _mapRevealTween.Chain().TweenCallback(Callable.From(() => {
            _revealParts.Clear();
            _mapRevealTween = null;
        }));
    }

    private void StageMapReveal(bool includeRoute = true) {
        if (includeRoute) {
            Reveal("LevelProgress/RoadConnections", 0f);
            Reveal("LevelProgress/RoadNodes", 0.045f);
        }
        Reveal("UI/SeedLabel", 0.06f);
        Reveal("PlayerResourceState/ItemsHContainer", 0.06f);
        Reveal("PlayerResourceState/ElectricityCoin", 0.08f);
        Reveal("PlayerResourceState/Difficulty", 0.09f);
        Reveal("PlayerResourceState/StatusPanel", 0.10f);
        Reveal("PlayerResourceState/RegionLabel", 0.11f);
        Reveal("PlayerResourceState/MapPeekButton", 0.12f);
        Reveal("PlayerResourceState/MenuButton", 0.12f);
        Reveal("PlayerResourceState/TransitionEnergyControl", 0.10f);
        Reveal("PlayerResourceState/RelicDock", 0.18f, new Vector2(0f, -8f));
        Reveal("MapLabel/RoadProbabilityPanel", 0.20f, new Vector2(24f, 0f));
        Reveal("UI/RoadAdvanceButton", 0.25f, new Vector2(14f, 0f));
        Reveal("UI/ReadyButton", 0.28f);
    }

    // Capture even controls that are still hidden by battle. Their visibility may be restored
    // later in the same frame, before the reveal clock starts.
    private void Reveal(string path, float delay, Vector2 offset = default) {
        var item = GetNodeOrNull<Control>(path);
        if (item == null) return;
        var part = new RevealPart { Item = item, Tint = item.Modulate, Delay = delay };
        _revealParts.Add(part);
        item.Modulate = part.Tint with { A = 0f };
        if (!_leavingForBattle && offset != Vector2.Zero) ApplyRevealOffset(part, offset);
    }

    // Remove only our displacement, preserving anchor/viewport changes during entry.
    private static void ApplyRevealOffset(RevealPart part, Vector2 offset) {
        if (!GodotObject.IsInstanceValid(part.Item)) return;
        part.Item.Position += offset - part.AppliedOffset;
        part.AppliedOffset = offset;
    }

    private static void RestoreRevealPart(RevealPart part) {
        if (!GodotObject.IsInstanceValid(part.Item)) return;
        part.Item.Modulate = part.Item.Modulate with { A = part.Tint.A };
        if (part.AppliedOffset != Vector2.Zero) ApplyRevealOffset(part, Vector2.Zero);
    }

    private void ResetMapReveal() {
        _mapRevealTween?.Kill();
        _mapRevealTween = null;
        if (GodotObject.IsInstanceValid(PlayerResourceState)) PlayerResourceState.FinishPartyLifeReveal();
        foreach (var part in _revealParts) RestoreRevealPart(part);
        _revealParts.Clear();
        _mapRevealPending = false;
    }
}
