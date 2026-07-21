using System;
using System.Collections.Generic;
using Godot;

public partial class BattleFrameBenchmark : Battle
{
    [Export(PropertyHint.Range, "0,1200,1")]
    public int WarmupFrames = 300;

    [Export(PropertyHint.Range, "120,7200,1")]
    public int SampleFrames = 1200;

    private readonly List<float> _frameTimesMs = new(1200);
    private int _framesSeen;
    private int _gc0Start;
    private int _gc1Start;
    private int _gc2Start;
    private long _allocatedBytesStart;
    private bool _starfieldDisabled;
    private bool _mouseTrailDisabled;
    private bool _noGcRegionRequested;
    private bool _noGcRegionActive;
    private bool _characterFramesDisabled;
    private bool _spineProcessingDisabled;
    private bool _linesDisabled;
    private bool _childProcessingDisabled;
    private bool _childProcessModeDisabled;
    private bool _characterBranchesDisabled;
    private bool _uiBranchesDisabled;
    private bool _characterControlLayerDisabled;
    private bool _characterControlDisabled;
    private bool _cardRowDisabled;
    private bool _handHoverTimerDisabled;
    private bool _mouseMotionSimulated;
    private readonly InputEventMouseMotion _simulatedMouseMotion = new();

    public override void _Ready()
    {
        base._Ready();
        _frameTimesMs.Capacity = Math.Max(_frameTimesMs.Capacity, SampleFrames);
        _starfieldDisabled = HasUserArgument("--disable-starfield");
        if (_starfieldDisabled)
            DisableStarfieldForBenchmark();
        else
            EnableStarfieldForBenchmark();
        _mouseTrailDisabled = HasUserArgument("--disable-mouse-trail");
        if (_mouseTrailDisabled)
            DisableMouseTrailForBenchmark();
        _noGcRegionRequested = HasUserArgument("--no-gc-region");
        _characterFramesDisabled = HasUserArgument("--disable-character-frames");
        if (_characterFramesDisabled)
            DisableCharacterFramesForBenchmark();
        _spineProcessingDisabled = HasUserArgument("--disable-spine-processing");
        _linesDisabled = HasUserArgument("--disable-lines");
        _childProcessingDisabled = HasUserArgument("--disable-child-processing");
        _childProcessModeDisabled = HasUserArgument("--disable-child-process-mode");
        _characterBranchesDisabled = HasUserArgument("--disable-character-branches");
        _uiBranchesDisabled = HasUserArgument("--disable-ui-branches");
        _characterControlLayerDisabled = HasUserArgument("--disable-character-control-layer");
        _characterControlDisabled = HasUserArgument("--disable-character-control");
        _cardRowDisabled = HasUserArgument("--disable-card-row");
        _handHoverTimerDisabled = HasUserArgument("--disable-hand-hover-timer");
        _mouseMotionSimulated = HasUserArgument("--simulate-mouse-motion");
        GD.Print(
            $"[FrameBenchmark] begin variant={GetVariantName()} warmup={WarmupFrames} samples={SampleFrames} renderer={RenderingServer.GetVideoAdapterName()}"
        );
    }

    public override void _Process(double delta)
    {
        if (_mouseMotionSimulated)
            SimulateMouseMotion();
        base._Process(delta);
        _framesSeen++;
        if (_framesSeen == WarmupFrames)
        {
            if (_characterFramesDisabled)
                DisableCharacterFramesForBenchmark();
            if (_spineProcessingDisabled)
                DisableSpineProcessingForBenchmark(this);
            if (_linesDisabled)
                DisableLinesForBenchmark(this);
            if (_childProcessingDisabled)
                DisableChildProcessingForBenchmark(this);
            if (_childProcessModeDisabled)
                DisableChildProcessModeForBenchmark(this);
            if (_characterBranchesDisabled)
                DisableCharacterBranchesForBenchmark();
            if (_uiBranchesDisabled)
                DisableUiBranchesForBenchmark();
            if (_characterControlLayerDisabled)
                DisableBranchProcessMode("CharacterControlLayer");
            if (_characterControlDisabled)
                DisableBranchProcessMode("CharacterControlLayer/CharacterControl");
            if (_cardRowDisabled)
                DisableBranchProcessMode(
                    "CharacterControlLayer/CharacterControl/ActionAreaRoot/CardRow"
                );
            if (_handHoverTimerDisabled)
                DisableBranchProcessMode(
                    "CharacterControlLayer/CharacterControl/HandHoverValidationTimer"
                );
            PrintStarfieldState();
            if (_noGcRegionRequested)
            {
                try
                {
                    _noGcRegionActive = GC.TryStartNoGCRegion(256L * 1024L * 1024L, true);
                }
                catch (InvalidOperationException)
                {
                    _noGcRegionActive = false;
                }
            }

            ResetSampleCounters();
        }
        if (_framesSeen <= WarmupFrames)
            return;

        _frameTimesMs.Add((float)delta * 1000f);
        if (_frameTimesMs.Count < SampleFrames)
            return;

        PrintSummary();
        if (_noGcRegionActive)
        {
            try
            {
                GC.EndNoGCRegion();
            }
            catch (InvalidOperationException) { }
            _noGcRegionActive = false;
        }
        GetTree().Quit();
    }

    private void ResetSampleCounters()
    {
        _gc0Start = GC.CollectionCount(0);
        _gc1Start = GC.CollectionCount(1);
        _gc2Start = GC.CollectionCount(2);
        _allocatedBytesStart = GC.GetTotalAllocatedBytes(false);
    }

    private void PrintSummary()
    {
        float[] sorted = _frameTimesMs.ToArray();
        Array.Sort(sorted);

        double totalMs = 0.0;
        int below60Fps = 0;
        int below72Fps = 0;
        int below90Fps = 0;
        for (int i = 0; i < sorted.Length; i++)
        {
            float frameMs = sorted[i];
            totalMs += frameMs;
            if (frameMs > 1000f / 60f)
                below60Fps++;
            if (frameMs > 1000f / 72f)
                below72Fps++;
            if (frameMs > 1000f / 90f)
                below90Fps++;
        }

        double averageMs = totalMs / sorted.Length;
        double averageFps = 1000.0 / averageMs;
        float p95Ms = Percentile(sorted, 0.95f);
        float p99Ms = Percentile(sorted, 0.99f);
        float maximumMs = sorted[^1];
        int slowestOnePercentCount = Math.Max(1, Mathf.CeilToInt(sorted.Length * 0.01f));
        double slowestOnePercentMs = 0.0;
        for (int i = sorted.Length - slowestOnePercentCount; i < sorted.Length; i++)
            slowestOnePercentMs += sorted[i];
        slowestOnePercentMs /= slowestOnePercentCount;
        double elapsedSeconds = totalMs / 1000.0;
        double allocatedMb = Math.Max(
            0.0,
            (GC.GetTotalAllocatedBytes(false) - _allocatedBytesStart) / (1024.0 * 1024.0)
        );

        GD.Print(
            "[FrameBenchmark] result "
                + $"variant={GetVariantName()} samples={sorted.Length} avg_fps={averageFps:F1} avg_ms={averageMs:F3} "
                + $"p95_ms={p95Ms:F3} p99_ms={p99Ms:F3} max_ms={maximumMs:F3} "
                + $"one_percent_low_fps={1000.0 / slowestOnePercentMs:F1} "
                + $"below_90={below90Fps} below_72={below72Fps} below_60={below60Fps} "
                + $"allocated_mb={allocatedMb:F1} allocated_mb_s={allocatedMb / elapsedSeconds:F2} "
                + $"gc=({GC.CollectionCount(0) - _gc0Start},{GC.CollectionCount(1) - _gc1Start},{GC.CollectionCount(2) - _gc2Start})"
        );
    }

    private void DisableStarfieldForBenchmark()
    {
        var starfield = GetNodeOrNull<StarfieldBackground3D>("bg/StarfieldBackground3D");
        if (starfield == null)
            return;

        starfield.Visible = false;
        starfield.ProcessMode = ProcessModeEnum.Disabled;
        starfield.SetProcess(false);
        SubViewport viewport = starfield.GetNodeOrNull<SubViewport>(
            "StarfieldViewportContainer/StarfieldViewport"
        );
        if (viewport != null)
            viewport.RenderTargetUpdateMode = SubViewport.UpdateMode.Disabled;
    }

    private void EnableStarfieldForBenchmark()
    {
        var starfield = GetNodeOrNull<StarfieldBackground3D>("bg/StarfieldBackground3D");
        if (starfield == null)
            return;

        starfield.Visible = true;
        starfield.ProcessMode = ProcessModeEnum.Always;
        starfield.SetProcess(true);
        SubViewport viewport = starfield.GetNodeOrNull<SubViewport>(
            "StarfieldViewportContainer/StarfieldViewport"
        );
        if (viewport != null)
            viewport.RenderTargetUpdateMode = SubViewport.UpdateMode.Always;
    }

    private void PrintStarfieldState()
    {
        var starfield = GetNodeOrNull<StarfieldBackground3D>("bg/StarfieldBackground3D");
        SubViewport viewport = starfield?.GetNodeOrNull<SubViewport>(
            "StarfieldViewportContainer/StarfieldViewport"
        );
        GD.Print(
            "[FrameBenchmark] starfield "
                + $"requested={!_starfieldDisabled} visible={starfield?.Visible} "
                + $"processing={starfield?.IsProcessing()} viewport_mode={viewport?.RenderTargetUpdateMode} "
                + $"viewport_size={viewport?.Size} window_focus={GetWindow()?.HasFocus()}"
        );
    }

    private void DisableMouseTrailForBenchmark()
    {
        var mouseTrail = GetNodeOrNull<CanvasLayer>("/root/MouseTrail");
        if (mouseTrail == null)
            return;

        mouseTrail.Visible = false;
        mouseTrail.ProcessMode = ProcessModeEnum.Disabled;
        mouseTrail.SetProcess(false);
    }

    private string GetVariantName()
    {
        if (_characterFramesDisabled)
            return AddGcVariant("no_character_frames");
        if (_spineProcessingDisabled)
            return AddGcVariant("no_spine_processing");
        if (_linesDisabled)
            return AddGcVariant("no_lines");
        if (_childProcessingDisabled)
            return AddGcVariant("no_child_processing");
        if (_childProcessModeDisabled)
            return AddGcVariant("no_child_process_mode");
        if (_characterBranchesDisabled)
            return AddGcVariant("no_character_branches");
        if (_uiBranchesDisabled)
            return AddGcVariant("no_ui_branches");
        if (_characterControlLayerDisabled)
            return AddGcVariant("no_character_control_layer");
        if (_characterControlDisabled)
            return AddGcVariant("no_character_control");
        if (_cardRowDisabled)
            return AddGcVariant("no_card_row");
        if (_handHoverTimerDisabled)
            return AddGcVariant("no_hand_hover_timer");
        if (_mouseMotionSimulated)
            return AddGcVariant("simulated_mouse_motion");
        if (_starfieldDisabled && _mouseTrailDisabled)
            return AddGcVariant("no_starfield_no_mouse_trail");
        if (_starfieldDisabled)
            return AddGcVariant("no_starfield");
        if (_mouseTrailDisabled)
            return AddGcVariant("no_mouse_trail");
        return AddGcVariant("full");
    }

    private string AddGcVariant(string variant) =>
        _noGcRegionRequested ? $"{variant}_no_gc" : variant;

    private void DisableCharacterFramesForBenchmark()
    {
        CharacterControl control = GetNodeOrNull<CharacterControl>(
            "CharacterControlLayer/CharacterControl"
        );
        if (control == null)
            return;

        Frame[] frames =
        {
            control.CharaterFrame1,
            control.CharaterFrame2,
            control.CharaterFrame3,
            control.CharaterFrame4,
        };
        foreach (Frame frame in frames)
        {
            if (frame != null && GodotObject.IsInstanceValid(frame))
                frame.SetProcess(false);
        }
    }

    private static void DisableSpineProcessingForBenchmark(Node node)
    {
        if (node == null)
            return;

        foreach (Node child in node.GetChildren())
        {
            if (child.IsClass("SpineSprite"))
                child.ProcessMode = ProcessModeEnum.Disabled;
            DisableSpineProcessingForBenchmark(child);
        }
    }

    private static void DisableLinesForBenchmark(Node node)
    {
        if (node == null)
            return;

        foreach (Node child in node.GetChildren())
        {
            if (child is Line line)
            {
                line.Visible = false;
                line.ProcessMode = ProcessModeEnum.Disabled;
                line.SetProcess(false);
            }
            DisableLinesForBenchmark(child);
        }
    }

    private static void DisableChildProcessingForBenchmark(Node node)
    {
        if (node == null)
            return;

        foreach (Node child in node.GetChildren())
        {
            child.SetProcess(false);
            child.SetPhysicsProcess(false);
            DisableChildProcessingForBenchmark(child);
        }
    }

    private static void DisableChildProcessModeForBenchmark(Node node)
    {
        if (node == null)
            return;

        foreach (Node child in node.GetChildren())
            child.ProcessMode = ProcessModeEnum.Disabled;
    }

    private void DisableCharacterBranchesForBenchmark()
    {
        Node left = GetNodeOrNull<Node>("Left");
        Node right = GetNodeOrNull<Node>("Right");
        if (left != null)
            left.ProcessMode = ProcessModeEnum.Disabled;
        if (right != null)
            right.ProcessMode = ProcessModeEnum.Disabled;
    }

    private void DisableUiBranchesForBenchmark()
    {
        string[] paths =
        {
            "CharacterControlLayer",
            "CanvasLayer",
            "CardPlayOverlay",
            "BattlePileOverlayLayer",
            "UI",
        };
        foreach (string path in paths)
        {
            Node branch = GetNodeOrNull<Node>(path);
            if (branch != null)
                branch.ProcessMode = ProcessModeEnum.Disabled;
        }
    }

    private void DisableBranchProcessMode(string path)
    {
        Node branch = GetNodeOrNull<Node>(path);
        if (branch != null)
            branch.ProcessMode = ProcessModeEnum.Disabled;
    }

    private void SimulateMouseMotion()
    {
        float phase = _framesSeen * 0.085f;
        Vector2 previousPosition = _simulatedMouseMotion.Position;
        Vector2 position = new(
            960f + Mathf.Sin(phase) * 780f,
            850f + Mathf.Cos(phase * 0.73f) * 180f
        );
        _simulatedMouseMotion.Position = position;
        _simulatedMouseMotion.Relative = position - previousPosition;
        _simulatedMouseMotion.Velocity = _simulatedMouseMotion.Relative * 120f;
        Input.ParseInputEvent(_simulatedMouseMotion);
    }

    private static bool HasUserArgument(string expected)
    {
        foreach (string argument in OS.GetCmdlineUserArgs())
        {
            if (string.Equals(argument, expected, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private static float Percentile(float[] sorted, float percentile)
    {
        if (sorted.Length == 0)
            return 0f;

        int index = Mathf.Clamp(
            Mathf.CeilToInt(sorted.Length * percentile) - 1,
            0,
            sorted.Length - 1
        );
        return sorted[index];
    }
}
