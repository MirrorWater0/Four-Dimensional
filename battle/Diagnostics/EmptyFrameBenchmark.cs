using System;
using System.Collections.Generic;
using Godot;

public partial class EmptyFrameBenchmark : Node
{
    private const int WarmupFrames = 300;
    private const int SampleFrames = 1200;

    private readonly List<float> _frameTimesMs = new(SampleFrames);
    private int _framesSeen;
    private int _gc0Start;
    private int _gc1Start;
    private int _gc2Start;
    private long _allocatedBytesStart;

    public override void _Ready()
    {
        GD.Print(
            $"[EmptyFrameBenchmark] begin warmup={WarmupFrames} samples={SampleFrames} renderer={RenderingServer.GetVideoAdapterName()}"
        );
    }

    public override void _Process(double delta)
    {
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

    private void PrintSummary()
    {
        double totalMs = 0.0;
        foreach (float frameMs in _frameTimesMs)
            totalMs += frameMs;

        double elapsedSeconds = totalMs / 1000.0;
        double allocatedMb = Math.Max(
            0.0,
            (GC.GetTotalAllocatedBytes(false) - _allocatedBytesStart) / (1024.0 * 1024.0)
        );
        GD.Print(
            "[EmptyFrameBenchmark] result "
                + $"samples={_frameTimesMs.Count} avg_fps={1000.0 / (totalMs / _frameTimesMs.Count):F1} "
                + $"allocated_mb={allocatedMb:F1} allocated_mb_s={allocatedMb / elapsedSeconds:F2} "
                + $"gc=({GC.CollectionCount(0) - _gc0Start},{GC.CollectionCount(1) - _gc1Start},{GC.CollectionCount(2) - _gc2Start})"
        );
    }
}
