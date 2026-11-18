using System.Collections.Generic;
using Godot;

public partial class LevelProgress {
    private sealed class RoadSegment(int stage, Line2D road, Line2D travelled, float progress) {
        public readonly int Stage = stage;
        public readonly Line2D Road = road;
        public readonly Line2D Travelled = travelled;
        public float Progress = progress;
        public Tween TravelTween;
    }

    private readonly List<RoadSegment> _roadSegments = new();
    private Tween _roadMorphTween;
    private int _roadFromLine;
    private float _roadMorphProgress = 1f;
    public bool IsWorldLineTransitioning { get; private set; }

    private Vector2 GetRoadPosition(float stage) {
        float progress = stage / (MapLength - 1);
        Vector2 from = WorldLineVisuals.GetPoint(progress, _roadFromLine);
        Vector2 to = WorldLineVisuals.GetPoint(progress, GameInfo.CurrentWorldLine);
        Vector2 point = from.Lerp(to, _roadMorphProgress);
        return new Vector2(MapLeftMargin + stage * NodeSpacingX, 530f + (point.Y - 0.5f) * 405f);
    }

    private void ResetRoadTransition() {
        _roadMorphTween?.Kill();
        _roadMorphTween = null;
        foreach (RoadSegment segment in _roadSegments)
            segment.TravelTween?.Kill();
        _roadFromLine = GameInfo.CurrentWorldLine;
        _roadMorphProgress = 1f;
        IsWorldLineTransitioning = false;
    }

    private void BeginRoadTransition(int fromLine) {
        _roadMorphTween?.Kill();
        _roadFromLine = fromLine;
        _roadMorphProgress = 0f;
        IsWorldLineTransitioning = true;
        RefreshNodeInteractivity();

        _roadMorphTween = CreateTween();
        _roadMorphTween.TweenMethod(Callable.From<float>(UpdateRoadMorph), 0f, 1f, 0.85f)
            .SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.InOut);
        _roadMorphTween.TweenCallback(Callable.From(() => {
            IsWorldLineTransitioning = false;
            _roadMorphTween = null;
            RefreshNodeInteractivity();
        }));
    }

    private void UpdateRoadMorph(float progress) {
        _roadMorphProgress = progress;
        foreach (var layer in _mapNodes)
            foreach (LevelNode node in layer)
                node.Position = GetRoadPosition(node.SelfCoordinate.X) - node.Size * 0.5f;
        foreach (RoadSegment segment in _roadSegments) {
            var points = new Vector2[RoadSamplesPerStage + 1];
            for (int sample = 0; sample <= RoadSamplesPerStage; sample++)
                points[sample] = GetRoadPosition(segment.Stage + sample / (float)RoadSamplesPerStage);
            segment.Road.Points = points;
            UpdateTravelledSegment(segment);
        }
        RefreshRoadColors();
    }

    private void UpdateTravelledSegment(RoadSegment segment) {
        if (segment.Progress <= 0f) {
            segment.Travelled.ClearPoints();
            return;
        }
        int samples = (int)(segment.Progress * RoadSamplesPerStage);
        var points = new List<Vector2>(samples + 2);
        for (int sample = 0; sample <= samples; sample++)
            points.Add(GetRoadPosition(segment.Stage + sample / (float)RoadSamplesPerStage));
        if (segment.Progress < 1f)
            points.Add(GetRoadPosition(segment.Stage + segment.Progress));
        segment.Travelled.Points = points.ToArray();
    }
}
