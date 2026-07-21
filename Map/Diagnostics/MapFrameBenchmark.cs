using System;
using System.Collections.Generic;
using Godot;

public partial class MapFrameBenchmark : Map
{
    private const int WarmupFrames = 300;
    private const int SampleFrames = 1200;

    private readonly List<float> _frameTimesMs = new(SampleFrames);
    private readonly InputEventMouseMotion _simulatedMouseMotion = new();
    private int _framesSeen;
    private int _gc0Start;
    private int _gc1Start;
    private int _gc2Start;
    private long _allocatedBytesStart;
    private int _simulatedMouseEventIndex;

    public override void _Ready()
    {
        base._Ready();
        GD.Print(
            $"[MapFrameBenchmark] begin warmup={WarmupFrames} samples={SampleFrames} renderer={RenderingServer.GetVideoAdapterName()}"
        );
    }

    public override void _Process(double delta)
    {
        for (int i = 0; i < 8; i++)
            SimulateMouseMotion();
        base._Process(delta);
        _framesSeen++;
        if (_framesSeen == WarmupFrames)
        {
            _gc0Start = GC.CollectionCount(0);
            _gc1Start = GC.CollectionCount(1);
            _gc2Start = GC.CollectionCount(2);
            _allocatedBytesStart = GC.GetTotalAllocatedBytes(false);
        }
        if (_framesSeen <= WarmupFrames)
            return;

        _frameTimesMs.Add((float)delta * 1000f);
        if (_frameTimesMs.Count < SampleFrames)
            return;

        PrintSummary();
        GetTree().Quit();
    }

    private void SimulateMouseMotion()
    {
        float phase = _simulatedMouseEventIndex++ * 0.010625f;
        Vector2 previousPosition = _simulatedMouseMotion.Position;
        Vector2 position = new(
            960f + Mathf.Sin(phase) * 780f,
            540f + Mathf.Cos(phase * 0.73f) * 440f
        );
        _simulatedMouseMotion.Position = position;
        _simulatedMouseMotion.Relative = position - previousPosition;
        _simulatedMouseMotion.Velocity = _simulatedMouseMotion.Relative * 120f;
        Input.ParseInputEvent(_simulatedMouseMotion);
    }

    private void PrintSummary()
    {
        float[] sorted = _frameTimesMs.ToArray();
        Array.Sort(sorted);
        double totalMs = 0.0;
        int below90Fps = 0;
        foreach (float frameMs in sorted)
        {
            totalMs += frameMs;
            if (frameMs > 1000f / 90f)
                below90Fps++;
        }

        double elapsedSeconds = totalMs / 1000.0;
        double allocatedMb = Math.Max(
            0.0,
            (GC.GetTotalAllocatedBytes(false) - _allocatedBytesStart) / (1024.0 * 1024.0)
        );
        int p99Index = Mathf.Clamp(
            Mathf.CeilToInt(sorted.Length * 0.99f) - 1,
            0,
            sorted.Length - 1
        );
        GD.Print(
            "[MapFrameBenchmark] result "
                + $"samples={sorted.Length} avg_fps={1000.0 / (totalMs / sorted.Length):F1} "
                + $"p99_ms={sorted[p99Index]:F3} max_ms={sorted[^1]:F3} below_90={below90Fps} "
                + $"allocated_mb_s={allocatedMb / elapsedSeconds:F2} "
                + $"gc=({GC.CollectionCount(0) - _gc0Start},{GC.CollectionCount(1) - _gc1Start},{GC.CollectionCount(2) - _gc2Start})"
        );
    }
}
