using Godot;

public partial class Map {
    private WorldLinePanel RoadWorldLinePanel =>
        field ??= GetNodeOrNull<WorldLinePanel>("MapLabel/RoadProbabilityPanel");
    private Button RoadAdvanceButton => field ??= GetNodeOrNull<Button>("UI/RoadAdvanceButton");
    private bool _roadWorldLineUiConnected;

    private void SetupRoadWorldLineUi() {
        if (_roadWorldLineUiConnected || LevelProgressNode == null)
            return;
        _roadWorldLineUiConnected = true;
        RoadWorldLinePanel?.Bind(LevelProgressNode);
        if (RoadAdvanceButton != null)
            RoadAdvanceButton.Pressed += () => LevelProgressNode?.AdvanceAlongRoad();
        UpdateRoadWorldLineUi();
    }

    private void UpdateRoadWorldLineUi() {
        if (!_roadWorldLineUiConnected || LevelProgressNode == null)
            return;
        bool showRoadUi = IsMapPeekModeActive || !HasBlockingOverlay();
        if (RoadWorldLinePanel != null) {
            RoadWorldLinePanel.Visible = showRoadUi;
            if (showRoadUi)
                RoadWorldLinePanel.Refresh();
        }
        if (RoadAdvanceButton == null)
            return;
        RoadAdvanceButton.Visible = showRoadUi && !IsMapPeekModeActive;
        RoadAdvanceButton.Disabled = !LevelProgressNode.CanAdvanceAlongRoad();
        RoadAdvanceButton.Text = LevelProgressNode.IsWorldLineTransitioning
            ? "轨迹重构中…" : LevelProgressNode.GetRoadAdvanceText();
    }
}
