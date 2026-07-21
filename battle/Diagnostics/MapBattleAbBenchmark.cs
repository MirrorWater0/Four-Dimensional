using System;
using System.Collections.Generic;
using System.Diagnostics;
using Godot;

public partial class MapBattleAbBenchmark : Node
{
    private const int InitialWarmupFrames = 180;
    private const int TransitionWarmupFrames = 90;
    private const int SampleFrames = 480;
    private const int MouseEventsPerFrame = 8;

    private enum BenchmarkPhase
    {
        Starting,
        ActiveMapWarmup,
        ActiveMapSample1,
        SuspendedMapWarmup,
        SuspendedMapSample,
        RestoredMapWarmup,
        ActiveMapSample2,
        Complete,
    }

    private enum MapSuspendMode
    {
        Full,
        LogicOnly,
        RenderOnly,
        InputOnly,
    }

    private readonly List<float> _frameTimesMs = new(SampleFrames);
    private readonly InputEventMouseMotion[] _simulatedMouseMotions =
        CreateMouseMotionEventPool();
    private readonly List<NodeState> _mapStates = new();
    private readonly Process _process = Process.GetCurrentProcess();
    private BenchmarkPhase _phase = BenchmarkPhase.Starting;
    private Map _map;
    private CanvasLayer _battleLayer;
    private int _phaseFrames;
    private int _simulatedMouseEventIndex;
    private int _mouseEventsPerFrame = MouseEventsPerFrame;
    private MapSuspendMode _suspendMode = MapSuspendMode.Full;
    private bool _reverseOrder;
    private bool _productionMode;
    private bool _resourceInputBenchmark;
    private bool _resourceVisibilityBenchmark;
    private bool _resourceMousePath;
    private bool _traceResourceHover;
    private int _gc0Start;
    private int _gc1Start;
    private int _gc2Start;
    private long _allocatedBytesStart;
    private TimeSpan _cpuStart;
    private ulong _wallStartUsec;
    private double _processTimeTotalMs;
    private double _physicsTimeTotalMs;
    private double _drawCallsTotal;
    private double _renderedObjectsTotal;
    private double _nodeCountTotal;
    private int _focusedFrames;
    private int _focusTransitions;
    private bool _lastFocused;
    private Godot.Environment _mapEnvironment;
    private ProcessModeEnum _mapEnvironmentProcessMode;

    private sealed class NodeState
    {
        public Node Node;
        public ProcessModeEnum ProcessMode;
        public bool Processing;
        public bool PhysicsProcessing;
        public bool ProcessingInput;
        public bool ProcessingUnhandledInput;
        public bool? Visible;
        public bool? CameraEnabled;
        public Control.MouseFilterEnum? MouseFilter;
    }

    public override async void _Ready()
    {
        SetProcess(false);
        int? requestedMouseEvents = GetUserArgumentInt("--benchmark-mouse-events=");
        _mouseEventsPerFrame = requestedMouseEvents.HasValue
            ? Math.Clamp(requestedMouseEvents.Value, 0, 64)
            : HasUserArgument("--benchmark-no-mouse")
                ? 0
                : MouseEventsPerFrame;
        if (HasUserArgument("--benchmark-logic-only"))
            _suspendMode = MapSuspendMode.LogicOnly;
        else if (HasUserArgument("--benchmark-render-only"))
            _suspendMode = MapSuspendMode.RenderOnly;
        else if (HasUserArgument("--benchmark-input-only"))
            _suspendMode = MapSuspendMode.InputOnly;
        _reverseOrder = HasUserArgument("--benchmark-reverse");
        _productionMode = HasUserArgument("--benchmark-production");
        _resourceInputBenchmark = HasUserArgument("--benchmark-resource-input");
        _resourceVisibilityBenchmark = HasUserArgument("--benchmark-resource-visibility");
        _resourceMousePath = HasUserArgument("--benchmark-resource-mouse-path");
        _traceResourceHover = HasUserArgument("--trace-resource-hover");

        PackedScene mapScene = GD.Load<PackedScene>("res://Map/Map.tscn");
        _map = mapScene?.Instantiate<Map>();
        if (_map == null)
        {
            GD.PushError("[MapBattleAB] failed to instantiate Map.tscn");
            GetTree().Quit(1);
            return;
        }

        _map.Name = "Map";
        var mapEntered = ToSignal(_map, Node.SignalName.TreeEntered);
        GetTree().Root.CallDeferred(Node.MethodName.AddChild, _map);
        await mapEntered;
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

        _battleLayer = new CanvasLayer { Name = "BenchmarkBattleLayer", Layer = 4 };
        GetTree().Root.AddChild(_battleLayer);

        PackedScene battleScene = GD.Load<PackedScene>("res://battle/Battle.tscn");
        Battle battle = battleScene?.Instantiate<Battle>();
        if (battle == null)
        {
            GD.PushError("[MapBattleAB] failed to instantiate Battle.tscn");
            GetTree().Quit(1);
            return;
        }

        battle.Name = "Battle";
        _battleLayer.AddChild(battle);
        await battle.WhenPresentationReadyAsync();
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

        if (!_productionMode && !_resourceInputBenchmark && !_resourceVisibilityBenchmark)
            _map.SetBattleWorldSuspended(false);
        if (
            _reverseOrder
            && !_productionMode
            && !_resourceInputBenchmark
            && !_resourceVisibilityBenchmark
        )
            SuspendMapWorld();
        if (_resourceInputBenchmark)
            SetLegacyResourceInput(!_reverseOrder);

        GD.Print(
            "[MapBattleAB] begin "
                + $"warmup={InitialWarmupFrames} samples={SampleFrames} mouse_events_per_frame={_mouseEventsPerFrame} "
                + $"suspend_mode={_suspendMode} reverse_order={_reverseOrder} production={_productionMode} "
                + $"resource_input={_resourceInputBenchmark} "
                + $"resource_visibility={_resourceVisibilityBenchmark} "
                + $"resource_mouse_path={_resourceMousePath} "
                + $"renderer={RenderingServer.GetVideoAdapterName()}"
        );
        _phase = BenchmarkPhase.ActiveMapWarmup;
        _phaseFrames = 0;
        SetProcess(true);
    }

    public override void _Process(double delta)
    {
        for (int i = 0; i < _mouseEventsPerFrame; i++)
            SimulateMouseMotion(i);

        if (_traceResourceHover && delta * 1000.0 >= 20.0)
        {
            Control hovered = GetViewport()?.GuiGetHoveredControl();
            GD.Print(
                "[ResourceHoverTrace] "
                    + $"frame={Engine.GetProcessFrames()} delta_ms={delta * 1000.0:F1} "
                    + $"mouse={GetViewport()?.GetMousePosition()} "
                    + $"hovered={(hovered == null ? "none" : hovered.GetPath())}"
            );
        }

        _phaseFrames++;
        switch (_phase)
        {
            case BenchmarkPhase.ActiveMapWarmup:
                if (_phaseFrames >= InitialWarmupFrames)
                    BeginSample(BenchmarkPhase.ActiveMapSample1);
                break;

            case BenchmarkPhase.ActiveMapSample1:
                CollectSample(delta);
                if (_frameTimesMs.Count >= SampleFrames)
                {
                    if (_resourceVisibilityBenchmark)
                    {
                        PrintSummary("resource_visible_1");
                        SetResourceVisibility(false);
                        _phase = BenchmarkPhase.SuspendedMapWarmup;
                        _phaseFrames = 0;
                        break;
                    }

                    if (_resourceInputBenchmark)
                    {
                        PrintSummary(
                            _reverseOrder
                                ? "resource_input_on_demand_1"
                                : "resource_input_always_1"
                        );
                        SetLegacyResourceInput(_reverseOrder);
                        _phase = BenchmarkPhase.SuspendedMapWarmup;
                        _phaseFrames = 0;
                        break;
                    }

                    if (_productionMode)
                    {
                        PrintSummary("production_auto_suspended");
                        _phase = BenchmarkPhase.Complete;
                        GD.Print("[MapBattleAB] complete");
                        FinishBenchmark();
                        break;
                    }

                    PrintSummary(_reverseOrder ? SuspendedVariant("1") : "map_active_1");
                    if (_reverseOrder)
                        RestoreMapWorld();
                    else
                        SuspendMapWorld();
                    _phase = BenchmarkPhase.SuspendedMapWarmup;
                    _phaseFrames = 0;
                }
                break;

            case BenchmarkPhase.SuspendedMapWarmup:
                if (_phaseFrames >= TransitionWarmupFrames)
                    BeginSample(BenchmarkPhase.SuspendedMapSample);
                break;

            case BenchmarkPhase.SuspendedMapSample:
                CollectSample(delta);
                if (_frameTimesMs.Count >= SampleFrames)
                {
                    if (_resourceVisibilityBenchmark)
                    {
                        PrintSummary("resource_hidden");
                        SetResourceVisibility(true);
                        _phase = BenchmarkPhase.RestoredMapWarmup;
                        _phaseFrames = 0;
                        break;
                    }

                    if (_resourceInputBenchmark)
                    {
                        PrintSummary(
                            _reverseOrder
                                ? "resource_input_always_middle"
                                : "resource_input_on_demand_middle"
                        );
                        SetLegacyResourceInput(!_reverseOrder);
                        _phase = BenchmarkPhase.RestoredMapWarmup;
                        _phaseFrames = 0;
                        break;
                    }

                    PrintSummary(_reverseOrder ? "map_active_middle" : SuspendedVariant("middle"));
                    if (_reverseOrder)
                        SuspendMapWorld();
                    else
                        RestoreMapWorld();
                    _phase = BenchmarkPhase.RestoredMapWarmup;
                    _phaseFrames = 0;
                }
                break;

            case BenchmarkPhase.RestoredMapWarmup:
                if (_phaseFrames >= TransitionWarmupFrames)
                    BeginSample(BenchmarkPhase.ActiveMapSample2);
                break;

            case BenchmarkPhase.ActiveMapSample2:
                CollectSample(delta);
                if (_frameTimesMs.Count >= SampleFrames)
                {
                    if (_resourceVisibilityBenchmark)
                    {
                        PrintSummary("resource_visible_2");
                        SetResourceVisibility(true);
                        _phase = BenchmarkPhase.Complete;
                        GD.Print("[MapBattleAB] complete");
                        FinishBenchmark();
                        break;
                    }

                    if (_resourceInputBenchmark)
                    {
                        PrintSummary(
                            _reverseOrder
                                ? "resource_input_on_demand_2"
                                : "resource_input_always_2"
                        );
                        SetLegacyResourceInput(false);
                        _phase = BenchmarkPhase.Complete;
                        GD.Print("[MapBattleAB] complete");
                        FinishBenchmark();
                        break;
                    }

                    PrintSummary(_reverseOrder ? SuspendedVariant("2") : "map_active_2");
                    if (_reverseOrder)
                        RestoreMapWorld();
                    _phase = BenchmarkPhase.Complete;
                    GD.Print("[MapBattleAB] complete");
                    FinishBenchmark();
                }
                break;
        }
    }

    public override void _ExitTree()
    {
        if (_battleLayer != null && GodotObject.IsInstanceValid(_battleLayer))
            _battleLayer.QueueFree();
        if (_map != null && GodotObject.IsInstanceValid(_map))
            _map.QueueFree();
    }

    private void BeginSample(BenchmarkPhase samplePhase)
    {
        _phase = samplePhase;
        _phaseFrames = 0;
        _frameTimesMs.Clear();
        _gc0Start = GC.CollectionCount(0);
        _gc1Start = GC.CollectionCount(1);
        _gc2Start = GC.CollectionCount(2);
        _allocatedBytesStart = GC.GetTotalAllocatedBytes(false);
        _process.Refresh();
        _cpuStart = _process.TotalProcessorTime;
        _wallStartUsec = Time.GetTicksUsec();
        _processTimeTotalMs = 0.0;
        _physicsTimeTotalMs = 0.0;
        _drawCallsTotal = 0.0;
        _renderedObjectsTotal = 0.0;
        _nodeCountTotal = 0.0;
        _focusedFrames = 0;
        _focusTransitions = 0;
        _lastFocused = GetWindow()?.HasFocus() == true;
    }

    private void CollectSample(double delta)
    {
        _frameTimesMs.Add((float)delta * 1000f);
        _processTimeTotalMs += Performance.GetMonitor(Performance.Monitor.TimeProcess) * 1000.0;
        _physicsTimeTotalMs +=
            Performance.GetMonitor(Performance.Monitor.TimePhysicsProcess) * 1000.0;
        _drawCallsTotal += Performance.GetMonitor(Performance.Monitor.RenderTotalDrawCallsInFrame);
        _renderedObjectsTotal +=
            Performance.GetMonitor(Performance.Monitor.RenderTotalObjectsInFrame);
        _nodeCountTotal += Performance.GetMonitor(Performance.Monitor.ObjectNodeCount);
        bool focused = GetWindow()?.HasFocus() == true;
        if (focused)
            _focusedFrames++;
        if (focused != _lastFocused)
            _focusTransitions++;
        _lastFocused = focused;
    }

    private void PrintSummary(string variant)
    {
        float[] sorted = _frameTimesMs.ToArray();
        Array.Sort(sorted);

        double totalMs = 0.0;
        int below90Fps = 0;
        int below72Fps = 0;
        int below60Fps = 0;
        foreach (float frameMs in sorted)
        {
            totalMs += frameMs;
            if (frameMs > 1000f / 90f)
                below90Fps++;
            if (frameMs > 1000f / 72f)
                below72Fps++;
            if (frameMs > 1000f / 60f)
                below60Fps++;
        }

        _process.Refresh();
        double wallSeconds = Math.Max(0.001, (Time.GetTicksUsec() - _wallStartUsec) / 1_000_000.0);
        double cpuSeconds = (_process.TotalProcessorTime - _cpuStart).TotalSeconds;
        double allocatedMb = Math.Max(
            0.0,
            (GC.GetTotalAllocatedBytes(false) - _allocatedBytesStart) / (1024.0 * 1024.0)
        );
        double averageMs = totalMs / sorted.Length;
        double slowestOnePercentMs = AverageSlowestPercent(sorted, 0.01f);

        GD.Print(
            "[MapBattleAB] result "
                + $"variant={variant} samples={sorted.Length} avg_fps={1000.0 / averageMs:F1} avg_ms={averageMs:F3} "
                + $"p95_ms={Percentile(sorted, 0.95f):F3} p99_ms={Percentile(sorted, 0.99f):F3} max_ms={sorted[^1]:F3} "
                + $"one_percent_low_fps={1000.0 / slowestOnePercentMs:F1} "
                + $"below_90={below90Fps} below_72={below72Fps} below_60={below60Fps} "
                + $"cpu_one_core_pct={100.0 * cpuSeconds / wallSeconds:F1} "
                + $"process_ms={_processTimeTotalMs / sorted.Length:F3} physics_ms={_physicsTimeTotalMs / sorted.Length:F3} "
                + $"draw_calls={_drawCallsTotal / sorted.Length:F1} rendered_objects={_renderedObjectsTotal / sorted.Length:F1} "
                + $"nodes={_nodeCountTotal / sorted.Length:F0} "
                + $"focus_pct={100.0 * _focusedFrames / sorted.Length:F1} focus_transitions={_focusTransitions} "
                + $"allocated_mb_s={allocatedMb / wallSeconds:F2} "
                + $"gc=({GC.CollectionCount(0) - _gc0Start},{GC.CollectionCount(1) - _gc1Start},{GC.CollectionCount(2) - _gc2Start})"
        );
    }

    private void SuspendMapWorld()
    {
        _mapStates.Clear();
        string[] worldBranches =
        {
            "DragButton",
            "LevelProgress",
            "MapLabel",
            "UI",
            "BattleReadyLayer",
            "SiteUI",
            "MaskLayer",
            "Camera",
        };

        if (_suspendMode == MapSuspendMode.InputOnly)
        {
            SuspendInputBranch(_map, recurse: false);
            foreach (string path in worldBranches)
                SuspendInputBranch(_map.GetNodeOrNull(path), recurse: true);
            GD.Print(
                "[MapBattleAB] map world suspended mode=InputOnly; rendering and frame processing kept active"
            );
            return;
        }

        bool disableProcessing = _suspendMode != MapSuspendMode.RenderOnly;
        bool hideRendering = _suspendMode != MapSuspendMode.LogicOnly;
        SuspendBranch(_map, disableProcessing, hide: false);

        foreach (string path in worldBranches)
            SuspendBranch(_map.GetNodeOrNull(path), disableProcessing, hideRendering);

        WorldEnvironment environment = _map.GetNodeOrNull<WorldEnvironment>("WorldEnvironment");
        if (_suspendMode != MapSuspendMode.InputOnly && environment != null)
        {
            _mapEnvironment = environment.Environment;
            _mapEnvironmentProcessMode = environment.ProcessMode;
            if (hideRendering)
                environment.Environment = null;
            if (disableProcessing)
                environment.ProcessMode = ProcessModeEnum.Disabled;
        }

        GD.Print(
            $"[MapBattleAB] map world suspended mode={_suspendMode}; PlayerResourceState and MenuLayer kept active"
        );
    }

    private void RestoreMapWorld()
    {
        foreach (NodeState state in _mapStates)
        {
            if (state.Node == null || !GodotObject.IsInstanceValid(state.Node))
                continue;

            state.Node.ProcessMode = state.ProcessMode;
            state.Node.SetProcess(state.Processing);
            state.Node.SetPhysicsProcess(state.PhysicsProcessing);
            state.Node.SetProcessInput(state.ProcessingInput);
            state.Node.SetProcessUnhandledInput(state.ProcessingUnhandledInput);
            if (state.Visible.HasValue && state.Node is CanvasItem canvasItem)
                canvasItem.Visible = state.Visible.Value;
            if (state.Visible.HasValue && state.Node is CanvasLayer canvasLayer)
                canvasLayer.Visible = state.Visible.Value;
            if (state.CameraEnabled.HasValue && state.Node is Camera2D camera)
                camera.Enabled = state.CameraEnabled.Value;
            if (state.MouseFilter.HasValue && state.Node is Control control)
                control.MouseFilter = state.MouseFilter.Value;
        }

        WorldEnvironment environment = _map.GetNodeOrNull<WorldEnvironment>("WorldEnvironment");
        if (_suspendMode != MapSuspendMode.InputOnly && environment != null)
        {
            environment.Environment = _mapEnvironment;
            environment.ProcessMode = _mapEnvironmentProcessMode;
        }
        _mapStates.Clear();
        GD.Print("[MapBattleAB] map world restored");
    }

    private void SuspendBranch(Node branch, bool disableProcessing, bool hide)
    {
        if (branch == null)
            return;

        CaptureNodeState(branch);
        if (disableProcessing)
        {
            branch.ProcessMode = ProcessModeEnum.Disabled;
            branch.SetProcess(false);
            branch.SetPhysicsProcess(false);
            branch.SetProcessInput(false);
            branch.SetProcessUnhandledInput(false);
        }
        if (hide && branch is CanvasItem canvasItem)
            canvasItem.Visible = false;
        if (hide && branch is CanvasLayer canvasLayer)
            canvasLayer.Visible = false;
        if (branch is Camera2D cameraNode)
            cameraNode.Enabled = false;
    }

    private void SuspendInputBranch(Node branch, bool recurse)
    {
        if (branch == null)
            return;

        CaptureNodeState(branch);
        branch.SetProcessInput(false);
        branch.SetProcessUnhandledInput(false);
        if (branch is Control control)
            control.MouseFilter = Control.MouseFilterEnum.Ignore;

        if (!recurse)
            return;
        foreach (Node child in branch.GetChildren())
            SuspendInputBranch(child, recurse: true);
    }

    private void CaptureNodeState(Node branch)
    {
        _mapStates.Add(
            new NodeState
            {
                Node = branch,
                ProcessMode = branch.ProcessMode,
                Processing = branch.IsProcessing(),
                PhysicsProcessing = branch.IsPhysicsProcessing(),
                ProcessingInput = branch.IsProcessingInput(),
                ProcessingUnhandledInput = branch.IsProcessingUnhandledInput(),
                Visible = branch is CanvasItem item
                    ? item.Visible
                    : branch is CanvasLayer layer ? layer.Visible : null,
                CameraEnabled = branch is Camera2D camera ? camera.Enabled : null,
                MouseFilter = branch is Control control ? control.MouseFilter : null,
            }
        );
    }

    private void SimulateMouseMotion(int eventSlot)
    {
        float phase = _simulatedMouseEventIndex++
            * (_resourceMousePath ? 0.006f : 0.010625f);
        InputEventMouseMotion motion = _simulatedMouseMotions[eventSlot];
        Vector2 previousPosition = motion.Position;
        Vector2 position = _resourceMousePath
            ? new Vector2(
                960f + Mathf.Sin(phase) * 940f,
                ((_simulatedMouseEventIndex / 960) & 1) == 0 ? 30f : 108f
            )
            : new Vector2(
                960f + Mathf.Sin(phase) * 780f,
                540f + Mathf.Cos(phase * 0.73f) * 440f
            );
        motion.Position = position;
        motion.Relative = position - previousPosition;
        motion.Velocity = motion.Relative * 120f;
        Input.ParseInputEvent(motion);
    }

    private void SetLegacyResourceInput(bool enabled)
    {
        PlayerResourceState resourceState = _map?.PlayerResourceState;
        if (resourceState == null || !GodotObject.IsInstanceValid(resourceState))
            return;

        resourceState.SetProcessUnhandledInput(enabled);
        HBoxContainer itemContainer = resourceState.ItemContainer;
        if (itemContainer == null)
            return;
        foreach (Node child in itemContainer.GetChildren())
        {
            if (child is ItemContainer item)
                item.SetProcessInput(enabled);
        }
    }

    private void SetResourceVisibility(bool visible)
    {
        PlayerResourceState resourceState = _map?.PlayerResourceState;
        if (resourceState != null && GodotObject.IsInstanceValid(resourceState))
            resourceState.Visible = visible;
    }

    private string SuspendedVariant(string segment) =>
        $"map_{_suspendMode.ToString().ToLowerInvariant()}_suspended_{segment}";

    private async void FinishBenchmark()
    {
        SetProcess(false);
        if (_battleLayer != null && GodotObject.IsInstanceValid(_battleLayer))
            _battleLayer.QueueFree();
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        if (_map != null && GodotObject.IsInstanceValid(_map))
            _map.QueueFree();
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        GetTree().Quit();
    }

    private static InputEventMouseMotion[] CreateMouseMotionEventPool()
    {
        var events = new InputEventMouseMotion[MouseEventsPerFrame];
        for (int i = 0; i < events.Length; i++)
            events[i] = new InputEventMouseMotion();
        return events;
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

    private static int? GetUserArgumentInt(string prefix)
    {
        foreach (string argument in OS.GetCmdlineUserArgs())
        {
            if (
                argument.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
                && int.TryParse(argument[prefix.Length..], out int value)
            )
            {
                return value;
            }
        }

        return null;
    }

    private static float Percentile(float[] sorted, float percentile)
    {
        int index = Mathf.Clamp(
            Mathf.CeilToInt(sorted.Length * percentile) - 1,
            0,
            sorted.Length - 1
        );
        return sorted[index];
    }

    private static double AverageSlowestPercent(float[] sorted, float percent)
    {
        int count = Math.Max(1, Mathf.CeilToInt(sorted.Length * percent));
        double total = 0.0;
        for (int i = sorted.Length - count; i < sorted.Length; i++)
            total += sorted[i];
        return total / count;
    }
}
