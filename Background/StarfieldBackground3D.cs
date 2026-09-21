using System;
using System.Collections.Generic;
using Godot;

public partial class StarfieldBackground3D : Control
{
    [Export] public int NearStarCount = 420;
    [Export] public int FarStarCount = 760;
    [Export] public float FieldRadius = 46f;
    [Export] public float TravelSpeed = 3.8f;
    [Export] public float DriftSpeed = 0.08f;
    [Export(PropertyHint.Range, "0.03,0.4,0.01")]
    public float StarBaseSize = 0.13f;
    [Export] public Vector2 NearStarSizeRange = new(0.8f, 2.4f);
    [Export] public Vector2 FarStarSizeRange = new(0.45f, 1.25f);
    [Export(PropertyHint.Range, "40,95,1")]
    public float CameraFov = 66f;
    [Export(PropertyHint.Range, "0.2,4.0,0.05")]
    public float NearStarBrightness = 1.35f;
    [Export(PropertyHint.Range, "0.2,4.0,0.05")]
    public float FarStarBrightness = 0.85f;
    [Export(PropertyHint.Range, "0.2,4.0,0.05")]
    public float StarSharpness = 1.65f;
    [Export(PropertyHint.Range, "0.0,2.0,0.05")]
    public float NebulaBrightness = 0.72f;
    [Export(PropertyHint.Range, "0.0,1.0,0.01")]
    public float CameraPathStrength = 0.0f;
    [Export(PropertyHint.Range, "0.0,0.5,0.01")]
    public float CameraPathSpeed = 0.055f;
    [Export(PropertyHint.Range, "0.0,1.0,0.01")]
    public float CameraLookAhead = 0.18f;
    [Export(PropertyHint.Range, "0.0,0.16,0.005")]
    public float CameraRollAmount = 0.045f;
    [Export(PropertyHint.Range, "0.0,0.2,0.005")]
    public float CameraOrbitSpeed = 0.0f;
    [Export(PropertyHint.Range, "0.0,0.6,0.01")]
    public float NebulaExpansionAmount = 0.0f;
    [Export(PropertyHint.Range, "0.0,3.0,0.05")]
    public float NebulaSpreadDistance = 0.9f;
    [Export(PropertyHint.Range, "0.0,1.0,0.01")]
    public float StellarisBattleLayerStrength = 0.0f;
    [Export(PropertyHint.Range, "0.0,1.5,0.01")]
    public float HyperlaneStrength = 0.9f;
    [Export] public bool BattleComposition = false;
    private float _presentationMotionScale = 1f;

    public void SetPresentationMotionScale(float scale) {
        _presentationMotionScale = Mathf.Clamp(scale, 0f, 1f);
    }
    [Export(PropertyHint.Range, "0.0,1.0,0.01")]
    public float DeepSpaceContrastStrength = 0.0f;
    [Export(PropertyHint.Range, "0.0,1.0,0.01")]
    public float SkyBrightness = 0.0f;
    [Export(PropertyHint.Range, "0.0,1.0,0.01")]
    public float ForegroundDecorationStrength = 0.62f;
    [Export(PropertyHint.Range, "0,24,1")]
    public int ForegroundShardCount = 12;
    [Export] public bool DeferredBuild = true;
    [Export(PropertyHint.Range, "16,128,16")]
    public int StarTextureSize = 48;
    [Export(PropertyHint.Range, "64,512,64")]
    public int NebulaTextureSize = 256;
    [Export(PropertyHint.Range, "8,60,1")]
    public float StarUpdateRate = 24f;
    [Export(PropertyHint.Range, "8,60,1")]
    public float OverlayUpdateRate = 20f;
    [Export(PropertyHint.Range, "8,60,1")]
    public float ForegroundUpdateRate = 30f;
    [Export] public Color NebulaColorA = new(0.22f, 0.52f, 0.88f, 0.18f);
    [Export] public Color NebulaColorB = new(0.86f, 0.48f, 0.78f, 0.13f);

    private const float NearStarMinRadius = 13f;
    private const float FarStarMinRadius = 28f;
    private const int RandomSeed = 43019;

    private static readonly Dictionary<(int Size, float Sharpness), Texture2D> StarTextureCache =
        new();
    private static readonly Dictionary<(int Size, uint ColorHash, int Seed), Texture2D> NebulaTextureCache =
        new();
    private static Godot.Environment _cachedStarfieldEnvironment;
    private static Texture2D _cachedFlowHeadTexture;

    // Keep these values aligned with the StarfieldBackground3D instance in Battle.tscn.
    private static readonly (Color Color, int Seed)[] BattleNebulaDefaults =
    {
        (new Color(0.21960784f, 0.52156866f, 0.8784314f, 0.81960785f), 1409),
        (new Color(0.85882354f, 0.47843137f, 0.78039217f, 0.79607844f), 2197),
        (new Color(0.35f, 0.95f, 0.82f, 0.13f), 3541),
        (new Color(0.18f, 0.44f, 0.95f, 0.11f), 5081),
        (new Color(0.72f, 0.24f, 0.82f, 0.09f), 6827),
        (new Color(0.12f, 0.72f, 0.92f, 0.10f), 7901),
        (new Color(0.94f, 0.38f, 0.58f, 0.08f), 9059)
    };

    private readonly RandomNumberGenerator _random = new();
    private readonly List<StarLayer> _layers = new();
    private readonly List<NebulaPlaneState> _nebulaPlanes = new();
    private readonly List<ForegroundShardState> _foregroundShards = new();
    private readonly List<FlowRingState> _flowRings = new();

    private Texture2D _starTexture;
    private Node3D _world;
    private Camera3D _camera;
    private Node3D _sphericalFlowRoot;
    private MeshInstance3D _sphericalSkyShell;
    private ShaderMaterial _skyMaterial;
    private Node3D _foregroundDecorationRoot;
    private CanvasLayer _battleForegroundLayer;
    private BattleForegroundOverlay _battleForegroundOverlay;
    private SpaceBattleOverlay _spaceBattleOverlay;
    private SubViewport _viewport;
    private bool _visualsBuilt;
    private bool _buildStarted;
    private bool _applicationFocused = true;
    private Vector3 _baseCameraPosition = new(0f, 0f, 9f);
    private Vector3 _baseCameraRotation = Vector3.Zero;
    private float _time;
    private float _overlayUpdateAccumulator;
    private float _foregroundUpdateAccumulator;

    // A scene transition can safely reveal this background only after the
    // SubViewport has been populated with the complete set of 3D visuals.
    // Before then the render target is deliberately disabled while the
    // deferred builder is adding its meshes and textures.
    public bool IsPresentationReady => _visualsBuilt;

    private static readonly Vector3[] CameraPathPoints =
    {
        new(0.0f, 0.7f, 9.2f),
        new(4.8f, 1.4f, 7.6f),
        new(8.2f, 1.0f, 3.5f),
        new(8.8f, -0.2f, -2.2f),
        new(5.8f, -1.2f, -7.0f),
        new(0.6f, -0.7f, -9.1f),
        new(-4.9f, 0.6f, -7.5f),
        new(-8.5f, 1.2f, -3.1f),
        new(-8.6f, 0.2f, 2.7f),
        new(-5.2f, -0.9f, 7.4f)
    };

    private static readonly Vector3[] CameraLookPathPoints =
    {
        new(0.0f, 0.2f, 0.0f),
        new(-0.5f, 0.8f, 0.4f),
        new(-0.9f, 0.3f, -0.2f),
        new(-0.4f, -0.5f, -0.7f),
        new(0.4f, -0.8f, -0.5f),
        new(0.8f, -0.2f, 0.2f),
        new(0.5f, 0.7f, 0.7f),
        new(-0.2f, 0.9f, 0.4f),
        new(-0.7f, 0.1f, -0.4f),
        new(-0.3f, -0.5f, -0.2f)
    };

    private sealed class StarLayer
    {
        public MultiMesh MultiMesh;
        public float Speed;
        public float TwinkleStrength;
        public Vector3[] BasePositions;
        public float[] Phase;
        public float[] Size;
        public Color[] BaseColors;
        public int NextUpdateIndex;
        public float UpdateBudget;
    }

    private sealed class NebulaPlaneState
    {
        public MeshInstance3D Plane;
        public StandardMaterial3D Material;
        public Vector3 BasePosition;
        public float BaseRoll;
        public Vector3 BaseScale;
        public Color BaseAlbedoColor;
        public float BaseEmissionEnergy;
        public float PhaseOffset;
    }

    private sealed class ForegroundShardState
    {
        public Node3D Node;
        public Vector3 BasePosition;
        public Vector3 RotationAxis;
        public float RotationSpeed;
        public float DriftPhase;
    }

    private sealed class FlowRingState
    {
        public Vector3 Axis;
        public float Latitude;
        public float Radius;
        public float Speed;
        public float Phase;
        public ShaderMaterial CoreMaterial;
        public ShaderMaterial HaloMaterial;
        public Node3D[] Heads;
    }

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        ProcessMode = ProcessModeEnum.Inherit;
        _applicationFocused = GetWindow()?.HasFocus() ?? true;
        SetProcess(_applicationFocused);
        ApplyMobileQualityProfile();
        _random.Seed = RandomSeed;
        _starTexture = GetCachedStarTexture(Math.Max(16, StarTextureSize), StarSharpness);
        _layers.Clear();
        _nebulaPlanes.Clear();
        _foregroundShards.Clear();
        _flowRings.Clear();
        _celestialBodies.Clear();

        _viewport = GetNodeOrNull<SubViewport>(
            "StarfieldViewportContainer/StarfieldViewport"
        );
        if (_viewport != null)
        {
            _viewport.OwnWorld3D = true;
            _viewport.TransparentBg = true;
            _viewport.RenderTargetClearMode = SubViewport.ClearMode.Always;
            if (MobilePlatform.IsMobile)
            {
                _viewport.Size = new Vector2I(960, 540);
                _viewport.Msaa3D = Viewport.Msaa.Disabled;
                _viewport.ScreenSpaceAA = Viewport.ScreenSpaceAAEnum.Disabled;
            }
            if (DeferredBuild || !_applicationFocused)
                _viewport.RenderTargetUpdateMode = SubViewport.UpdateMode.Disabled;
        }

        ColorRect skyClear = GetNodeOrNull<ColorRect>("SkyClear");
        if (skyClear != null)
        {
            float sky = Mathf.Clamp(SkyBrightness, 0f, 1f);
            skyClear.Color = new Color(
                0.0015f + 0.035f * sky,
                0.0025f + 0.04f * sky,
                0.006f + 0.06f * sky,
                1f
            );
        }

        _world = GetNodeOrNull<Node3D>("StarfieldViewportContainer/StarfieldViewport/World");
        if (_world == null)
            return;

        _camera = _world.GetNodeOrNull<Camera3D>("Camera3D");
        if (_camera != null)
        {
            _camera.Fov = CameraFov;
            _baseCameraPosition = _camera.Position;
            _baseCameraRotation = _camera.Rotation;
        }

        WorldEnvironment worldEnvironment = _world.GetNodeOrNull<WorldEnvironment>("WorldEnvironment");
        if (worldEnvironment != null)
        {
            Godot.Environment environment = GetCachedStarfieldEnvironment();
            if (MobilePlatform.IsMobile)
                environment.GlowEnabled = false;
            worldEnvironment.Environment = environment;
        }

        SetupBattleForegroundOverlay();

        if (DeferredBuild)
        {
            StartDeferredBuild();
            return;
        }

        BuildVisuals();
    }

    private void ApplyMobileQualityProfile()
    {
        if (!MobilePlatform.IsMobile)
            return;

        NearStarCount = Math.Min(NearStarCount, 260);
        FarStarCount = Math.Min(FarStarCount, 440);
        ForegroundShardCount = Math.Min(ForegroundShardCount, 6);
        NebulaTextureSize = Math.Min(NebulaTextureSize, 128);
        StarUpdateRate = Math.Min(StarUpdateRate, 18f);
        OverlayUpdateRate = Math.Min(OverlayUpdateRate, 24f);
        ForegroundUpdateRate = Math.Min(ForegroundUpdateRate, 20f);
        ForegroundDecorationStrength *= 0.72f;
        StellarisBattleLayerStrength *= 0.82f;
    }

    public override void _Notification(int what)
    {
        if (what == MainLoop.NotificationApplicationFocusOut)
        {
            SetApplicationFocused(false);
        }
        else if (what == MainLoop.NotificationApplicationFocusIn)
        {
            SetApplicationFocused(true);
        }
    }

    private void SetApplicationFocused(bool focused)
    {
        if (_applicationFocused == focused)
            return;

        _applicationFocused = focused;
        SetProcess(focused);

        if (_viewport != null && GodotObject.IsInstanceValid(_viewport))
        {
            _viewport.RenderTargetUpdateMode = focused && _visualsBuilt
                ? SubViewport.UpdateMode.Always
                : SubViewport.UpdateMode.Disabled;
        }

        if (!focused)
            return;

        // Do not let the first frame after Alt+Tab catch up all animations at once.
        foreach (StarLayer layer in _layers)
            layer.UpdateBudget = 0f;
        _overlayUpdateAccumulator = 0f;
        _foregroundUpdateAccumulator = 0f;
    }

    private void BuildVisuals()
    {
        BuildSphericalSkyShell();
        BuildCelestialBodies();
        BuildForegroundDecorations();
        BuildStarLayer(
            GetNodeOrNull<MultiMeshInstance3D>(
                "StarfieldViewportContainer/StarfieldViewport/World/NearStars"
            ),
            NearStarCount,
            1.0f,
            0.52f
        );
        BuildStarLayer(
            GetNodeOrNull<MultiMeshInstance3D>(
                "StarfieldViewportContainer/StarfieldViewport/World/FarStars"
            ),
            FarStarCount,
            0.34f,
            0.32f
        );
        BuildNebulaPlanes();
        BuildSphericalFlowCurves();
        SetupSpaceBattleOverlay();
        _visualsBuilt = true;
        if (_viewport != null && GodotObject.IsInstanceValid(_viewport))
        {
            _viewport.RenderTargetUpdateMode = _applicationFocused
                ? SubViewport.UpdateMode.Always
                : SubViewport.UpdateMode.Disabled;
        }
    }

    private void StartDeferredBuild()
    {
        if (_buildStarted)
            return;

        _buildStarted = true;
        _ = BuildVisualsDeferredAsync();
    }

    private async System.Threading.Tasks.Task BuildVisualsDeferredAsync()
    {
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        if (!CanBuildVisuals())
            return;

        BuildSphericalSkyShell();
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        if (!CanBuildVisuals())
            return;

        BuildCelestialBodies();
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        if (!CanBuildVisuals())
            return;

        BuildForegroundDecorations();
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        if (!CanBuildVisuals())
            return;

        BuildStarLayer(
            GetNodeOrNull<MultiMeshInstance3D>(
                "StarfieldViewportContainer/StarfieldViewport/World/FarStars"
            ),
            FarStarCount,
            0.34f,
            0.32f
        );
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        if (!CanBuildVisuals())
            return;

        BuildStarLayer(
            GetNodeOrNull<MultiMeshInstance3D>(
                "StarfieldViewportContainer/StarfieldViewport/World/NearStars"
            ),
            NearStarCount,
            1.0f,
            0.52f
        );
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        if (!CanBuildVisuals())
            return;

        await BuildNebulaPlanesDeferredAsync();
        if (!CanBuildVisuals())
            return;

        BuildSphericalFlowCurves();
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        if (!CanBuildVisuals())
            return;

        SetupSpaceBattleOverlay();
        if (
            _applicationFocused
            && _viewport != null
            && GodotObject.IsInstanceValid(_viewport)
        )
        {
            _viewport.RenderTargetUpdateMode = SubViewport.UpdateMode.Always;
        }
        _visualsBuilt = true;
    }

    private bool CanBuildVisuals()
    {
        return GodotObject.IsInstanceValid(this)
            && IsInsideTree()
            && _world != null
            && GodotObject.IsInstanceValid(_world);
    }

    public override void _Process(double delta)
    {
        float dt = (float)delta;
        _time += dt * _presentationMotionScale;
        _skyMaterial?.SetShaderParameter("sky_time", _time);

        if (_world != null && GodotObject.IsInstanceValid(_world))
        {
            _world.Rotation = new Vector3(
                Mathf.Sin(_time * 0.09f) * 0.018f,
                _time * DriftSpeed,
                Mathf.Cos(_time * 0.07f) * 0.012f
            );
        }

        UpdateCameraPath();
        UpdateSphericalFlowCurves();
        UpdateForegroundDecorations(dt);
        UpdateCelestialBodies();
        UpdateBattleForegroundOverlayThrottled(dt);
        if (_visualsBuilt)
        {
            UpdateStarLayersThrottled(dt);
            UpdateNebulaPlanes();
            UpdateSpaceBattleOverlayThrottled(dt);
        }
    }

    private void BuildStarLayer(
        MultiMeshInstance3D instance,
        int count,
        float speed,
        float twinkleStrength
    )
    {
        if (count <= 0 || instance == null)
            return;

        var mesh = new QuadMesh { Size = new Vector2(StarBaseSize, StarBaseSize) };
        var material = new StandardMaterial3D
        {
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
            BillboardMode = BaseMaterial3D.BillboardModeEnum.Enabled,
            NoDepthTest = true,
            AlbedoColor = Colors.White,
            AlbedoTexture = _starTexture,
            VertexColorUseAsAlbedo = true,
            EmissionEnabled = true,
            Emission = Colors.White,
            EmissionTexture = _starTexture,
            EmissionEnergyMultiplier = speed > 0.5f ? NearStarBrightness : FarStarBrightness,
            TextureFilter = BaseMaterial3D.TextureFilterEnum.Nearest
        };
        mesh.Material = material;

        var multiMesh = new MultiMesh
        {
            TransformFormat = MultiMesh.TransformFormatEnum.Transform3D,
            UseColors = true,
            Mesh = mesh,
            InstanceCount = count
        };

        var layer = new StarLayer
        {
            MultiMesh = multiMesh,
            Speed = speed,
            TwinkleStrength = twinkleStrength,
            BasePositions = new Vector3[count],
            Phase = new float[count],
            Size = new float[count],
            BaseColors = new Color[count]
        };

        for (int i = 0; i < count; i++)
        {
            float minRadius = speed > 0.5f ? NearStarMinRadius : FarStarMinRadius;
            float maxRadius = speed > 0.5f ? FieldRadius * 0.72f : FieldRadius;
            layer.BasePositions[i] = RandomStarPosition(minRadius, maxRadius);
            layer.Phase[i] = _random.RandfRange(0f, Mathf.Tau);
            Vector2 sizeRange = speed > 0.5f ? NearStarSizeRange : FarStarSizeRange;
            layer.Size[i] = _random.RandfRange(sizeRange.X, sizeRange.Y);
            layer.BaseColors[i] = PickStarColor(speed);
            SetStarInstance(layer, i, 1f);
        }

        instance.Multimesh = multiMesh;
        _layers.Add(layer);
    }

    private void BuildNebulaPlanes()
    {
        AddNebulaPlane("NebulaA", NebulaColorA, new Vector2(58f, 22f), 1409, new Vector3(-10f, 5f, -34f), -0.18f);
        AddNebulaPlane("NebulaB", NebulaColorB, new Vector2(54f, 24f), 2197, new Vector3(24f, -3f, -27f), 0.24f);
        AddNebulaPlane("NebulaC", new Color(0.35f, 0.95f, 0.82f, 0.13f), new Vector2(62f, 20f), 3541, new Vector3(-31f, 10f, -5f), -0.04f);
        AddNebulaPlane("NebulaD", new Color(0.18f, 0.44f, 0.95f, 0.11f), new Vector2(58f, 26f), 5081, new Vector3(23f, -11f, 23f), 0.34f);
        AddNebulaPlane("NebulaE", new Color(0.72f, 0.24f, 0.82f, 0.09f), new Vector2(50f, 18f), 6827, new Vector3(-20f, -8f, 26f), 0.02f);
        AddNebulaPlane("NebulaF", new Color(0.12f, 0.72f, 0.92f, 0.10f), new Vector2(52f, 20f), 7901, new Vector3(8f, 25f, 24f), -0.28f);
        AddNebulaPlane("NebulaG", new Color(0.94f, 0.38f, 0.58f, 0.08f), new Vector2(48f, 19f), 9059, new Vector3(-25f, 18f, 18f), 0.18f);
    }

    private async System.Threading.Tasks.Task BuildNebulaPlanesDeferredAsync()
    {
        AddNebulaPlane("NebulaA", NebulaColorA, new Vector2(58f, 22f), 1409, new Vector3(-10f, 5f, -34f), -0.18f);
        if (!await YieldBuildFrame())
            return;

        AddNebulaPlane("NebulaB", NebulaColorB, new Vector2(54f, 24f), 2197, new Vector3(24f, -3f, -27f), 0.24f);
        if (!await YieldBuildFrame())
            return;

        AddNebulaPlane("NebulaC", new Color(0.35f, 0.95f, 0.82f, 0.13f), new Vector2(62f, 20f), 3541, new Vector3(-31f, 10f, -5f), -0.04f);
        if (!await YieldBuildFrame())
            return;

        AddNebulaPlane("NebulaD", new Color(0.18f, 0.44f, 0.95f, 0.11f), new Vector2(58f, 26f), 5081, new Vector3(23f, -11f, 23f), 0.34f);
        if (!await YieldBuildFrame())
            return;

        AddNebulaPlane("NebulaE", new Color(0.72f, 0.24f, 0.82f, 0.09f), new Vector2(50f, 18f), 6827, new Vector3(-20f, -8f, 26f), 0.02f);
        if (!await YieldBuildFrame())
            return;

        AddNebulaPlane("NebulaF", new Color(0.12f, 0.72f, 0.92f, 0.10f), new Vector2(52f, 20f), 7901, new Vector3(8f, 25f, 24f), -0.28f);
        if (!await YieldBuildFrame())
            return;

        AddNebulaPlane("NebulaG", new Color(0.94f, 0.38f, 0.58f, 0.08f), new Vector2(48f, 19f), 9059, new Vector3(-25f, 18f, 18f), 0.18f);
    }

    private async System.Threading.Tasks.Task<bool> YieldBuildFrame()
    {
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        return CanBuildVisuals();
    }

    private void BuildSphericalFlowCurves()
    {
        if (_world == null || !GodotObject.IsInstanceValid(_world))
            return;

        _sphericalFlowRoot = _world.GetNodeOrNull<Node3D>("SphericalFlowCurves");
        if (_sphericalFlowRoot != null && GodotObject.IsInstanceValid(_sphericalFlowRoot))
            return;

        _sphericalFlowRoot = new Node3D { Name = "SphericalFlowCurves" };
        _world.AddChild(_sphericalFlowRoot, false, InternalMode.Disabled);

        Shader flowShader = CreateSphericalFlowShader();
        float flowRadius = GetSphericalShellRadius() - 0.72f;
        var rings = new (
            Vector3 Axis,
            float Latitude,
            float Width,
            Color Color,
            float Speed,
            float Phase
        )[]
        {
            (new(0.0f, 1.0f, 0.0f), 0.08f, 0.032f, new Color(1f, 1f, 1f, 0.46f), 0.17f, 0.08f),
            (new(0.72f, 0.20f, 0.66f), -0.18f, 0.028f, new Color(1f, 1f, 1f, 0.40f), 0.13f, 0.31f),
            (new(-0.45f, 0.82f, 0.35f), 0.23f, 0.035f, new Color(1f, 1f, 1f, 0.44f), 0.15f, 0.53f),
            (new(0.20f, 0.55f, -0.81f), -0.32f, 0.030f, new Color(1f, 1f, 1f, 0.39f), 0.11f, 0.72f)
        };

        float strength = Mathf.Clamp(
            StellarisBattleLayerStrength * (0.75f + HyperlaneStrength * 0.65f),
            0f,
            1.5f
        );
        for (int i = 0; i < rings.Length; i++)
        {
            var ring = rings[i];
            ImmediateMesh haloTube = CreateSphericalFlowRing(
                ring.Axis,
                ring.Latitude,
                flowRadius,
                ring.Width * 4.0f
            );
            Color haloColor = ring.Color with { A = ring.Color.A * 0.55f };
            ShaderMaterial haloMaterial = CreateFlowMaterial(
                flowShader,
                haloColor,
                ring.Speed,
                ring.Phase,
                strength,
                1f
            );
            haloMaterial.SetShaderParameter("battle_composition", BattleComposition);
            var halo = new MeshInstance3D
            {
                Name = $"FlowRingHalo{i + 1}",
                Mesh = haloTube,
                MaterialOverride = haloMaterial,
                CastShadow = GeometryInstance3D.ShadowCastingSetting.Off
            };
            _sphericalFlowRoot.AddChild(halo, false, InternalMode.Disabled);

            ImmediateMesh coreTube = CreateSphericalFlowRing(
                ring.Axis,
                ring.Latitude,
                flowRadius - 0.025f,
                ring.Width
            );
            ShaderMaterial coreMaterial = CreateFlowMaterial(
                flowShader,
                ring.Color,
                ring.Speed,
                ring.Phase,
                strength,
                0f
            );
            coreMaterial.SetShaderParameter("battle_composition", BattleComposition);
            var core = new MeshInstance3D
            {
                Name = $"FlowRingCore{i + 1}",
                Mesh = coreTube,
                MaterialOverride = coreMaterial,
                CastShadow = GeometryInstance3D.ShadowCastingSetting.Off
            };
            _sphericalFlowRoot.AddChild(core, false, InternalMode.Disabled);

            var heads = new Node3D[2];
            for (int headIndex = 0; headIndex < heads.Length; headIndex++)
            {
                heads[headIndex] = CreateSphericalFlowHead(
                    $"FlowRingHead{i + 1}_{headIndex + 1}",
                    ring.Width,
                    ring.Color,
                    strength
                );
                heads[headIndex].Visible = !BattleComposition;
                _sphericalFlowRoot.AddChild(heads[headIndex], false, InternalMode.Disabled);
            }

            _flowRings.Add(
                new FlowRingState
                {
                    Axis = ring.Axis,
                    Latitude = ring.Latitude,
                    Radius = flowRadius - 0.04f,
                    Speed = ring.Speed,
                    Phase = ring.Phase,
                    CoreMaterial = coreMaterial,
                    HaloMaterial = haloMaterial,
                    Heads = heads
                }
            );
        }

        UpdateSphericalFlowCurves();
    }

    private static Node3D CreateSphericalFlowHead(
        string name,
        float lineWidth,
        Color color,
        float strength
    )
    {
        var root = new Node3D { Name = name };
        float visibleStrength = Mathf.Clamp(strength, 0f, 1.5f);
        Texture2D headTexture = GetFlowHeadTexture();

        float haloSize = lineWidth * 13.5f;
        var haloMesh = new QuadMesh
        {
            Size = new Vector2(haloSize, haloSize)
        };
        var haloMaterial = new StandardMaterial3D
        {
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
            BlendMode = BaseMaterial3D.BlendModeEnum.Add,
            CullMode = BaseMaterial3D.CullModeEnum.Disabled,
            BillboardMode = BaseMaterial3D.BillboardModeEnum.Enabled,
            AlbedoTexture = headTexture,
            AlbedoColor = color with { A = color.A * visibleStrength * 0.22f },
            EmissionEnabled = true,
            Emission = color with { A = 1f },
            EmissionTexture = headTexture,
            EmissionEnergyMultiplier = 1.8f * visibleStrength
        };
        haloMesh.Material = haloMaterial;
        root.AddChild(
            new MeshInstance3D
            {
                Name = "Halo",
                Mesh = haloMesh,
                CastShadow = GeometryInstance3D.ShadowCastingSetting.Off
            },
            false,
            InternalMode.Disabled
        );

        float coreSize = lineWidth * 6.2f;
        var coreMesh = new QuadMesh
        {
            Size = new Vector2(coreSize, coreSize)
        };
        Color coreColor = new(
            Mathf.Lerp(color.R, 1f, 0.82f),
            Mathf.Lerp(color.G, 1f, 0.82f),
            Mathf.Lerp(color.B, 1f, 0.82f),
            Mathf.Clamp(color.A * visibleStrength * 1.65f, 0f, 1f)
        );
        var coreMaterial = new StandardMaterial3D
        {
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
            BlendMode = BaseMaterial3D.BlendModeEnum.Add,
            CullMode = BaseMaterial3D.CullModeEnum.Disabled,
            BillboardMode = BaseMaterial3D.BillboardModeEnum.Enabled,
            AlbedoTexture = headTexture,
            AlbedoColor = coreColor,
            EmissionEnabled = true,
            Emission = coreColor with { A = 1f },
            EmissionTexture = headTexture,
            EmissionEnergyMultiplier = 4.8f * visibleStrength
        };
        coreMesh.Material = coreMaterial;
        root.AddChild(
            new MeshInstance3D
            {
                Name = "Core",
                Mesh = coreMesh,
                CastShadow = GeometryInstance3D.ShadowCastingSetting.Off
            },
            false,
            InternalMode.Disabled
        );

        return root;
    }

    private static Texture2D GetFlowHeadTexture()
    {
        if (_cachedFlowHeadTexture != null && GodotObject.IsInstanceValid(_cachedFlowHeadTexture))
            return _cachedFlowHeadTexture;

        const int TextureSize = 64;
        Image image = Image.CreateEmpty(
            TextureSize,
            TextureSize,
            true,
            Image.Format.Rgba8
        );
        for (int y = 0; y < TextureSize; y++)
        {
            for (int x = 0; x < TextureSize; x++)
            {
                Vector2 uv = new(
                    (x + 0.5f) / TextureSize * 2f - 1f,
                    (y + 0.5f) / TextureSize * 2f - 1f
                );
                float distance = uv.Length();
                float alpha = 1f - Mathf.SmoothStep(0.34f, 0.98f, distance);
                alpha *= 1f - Mathf.SmoothStep(0.84f, 1f, distance);
                image.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        image.GenerateMipmaps();
        _cachedFlowHeadTexture = ImageTexture.CreateFromImage(image);
        return _cachedFlowHeadTexture;
    }

    private void UpdateSphericalFlowCurves()
    {
        const float HeadPosition = 0.08f;
        for (int ringIndex = 0; ringIndex < _flowRings.Count; ringIndex++)
        {
            FlowRingState ring = _flowRings[ringIndex];
            if (ring.CoreMaterial != null && GodotObject.IsInstanceValid(ring.CoreMaterial))
                ring.CoreMaterial.SetShaderParameter("flow_time", _time);
            if (ring.HaloMaterial != null && GodotObject.IsInstanceValid(ring.HaloMaterial))
                ring.HaloMaterial.SetShaderParameter("flow_time", _time);

            float firstHeadT = Mathf.PosMod(
                (_time * ring.Speed - ring.Phase + HeadPosition) * 0.5f,
                1f
            );
            for (int headIndex = 0; headIndex < ring.Heads.Length; headIndex++)
            {
                Node3D head = ring.Heads[headIndex];
                if (head == null || !GodotObject.IsInstanceValid(head))
                    continue;

                float t = Mathf.PosMod(firstHeadT + headIndex * 0.5f, 1f);
                head.Position = SampleSphericalRingPosition(
                    ring.Axis,
                    ring.Latitude,
                    ring.Radius,
                    t
                );
                float pulse = 0.94f + Mathf.Sin(
                    _time * 5.2f + ringIndex * 1.7f + headIndex * 2.4f
                ) * 0.06f;
                head.Scale = Vector3.One * pulse;
            }
        }
    }

    private static Vector3 SampleSphericalRingPosition(
        Vector3 axis,
        float latitude,
        float radius,
        float t
    )
    {
        Vector3 ringAxis = axis.Normalized();
        Vector3 reference = Mathf.Abs(ringAxis.Dot(Vector3.Up)) > 0.92f
            ? Vector3.Right
            : Vector3.Up;
        Vector3 basisA = ringAxis.Cross(reference).Normalized();
        Vector3 basisB = ringAxis.Cross(basisA).Normalized();
        float clampedLatitude = Mathf.Clamp(latitude, -0.82f, 0.82f);
        float ringRadius = Mathf.Sqrt(1f - clampedLatitude * clampedLatitude);
        float angle = Mathf.Tau * t;
        Vector3 direction = (
            (basisA * Mathf.Cos(angle) + basisB * Mathf.Sin(angle)) * ringRadius
            + ringAxis * clampedLatitude
        ).Normalized();
        return direction * radius;
    }

    private static ShaderMaterial CreateFlowMaterial(
        Shader shader,
        Color color,
        float speed,
        float phase,
        float strength,
        float softLayer
    )
    {
        var material = new ShaderMaterial { Shader = shader };
        material.SetShaderParameter("flow_color", color);
        material.SetShaderParameter("flow_speed", speed);
        material.SetShaderParameter("flow_phase", phase);
        material.SetShaderParameter("flow_strength", strength);
        material.SetShaderParameter("soft_layer", softLayer);
        return material;
    }

    private void BuildSphericalSkyShell()
    {
        if (_world == null || !GodotObject.IsInstanceValid(_world))
            return;

        _sphericalSkyShell = _world.GetNodeOrNull<MeshInstance3D>("SphericalSkyShell");
        if (_sphericalSkyShell != null && GodotObject.IsInstanceValid(_sphericalSkyShell))
            return;

        float radius = GetSphericalShellRadius();
        var sphere = new SphereMesh
        {
            Radius = radius,
            Height = radius * 2f,
            RadialSegments = 96,
            Rings = 64
        };
        var shader = new Shader
        {
            Code = """
                shader_type spatial;
                render_mode unshaded, cull_front, depth_draw_opaque;

                uniform float sky_strength = 1.0;
                uniform float contrast_strength = 0.5;
                uniform float sky_time = 0.0;
                varying vec3 sphere_direction;

                void vertex() {
                    sphere_direction = normalize(VERTEX);
                }

                void fragment() {
                    vec3 d = normalize(sphere_direction);
                    float slow_time = sky_time * 0.012;
                    float broad_wave = sin(d.x * 6.0 + d.z * 3.5 + slow_time)
                        + sin(d.y * 9.0 - d.x * 4.0 - slow_time * 0.7) * 0.55;
                    float fine_wave = sin(d.z * 17.0 + d.y * 11.0 + slow_time * 1.4)
                        * sin(d.x * 13.0 - d.z * 7.0 - slow_time) * 0.5 + 0.5;
                    float equatorial_band = exp(-abs(d.y + sin(d.x * 4.2 + slow_time) * 0.18) * 5.2);
                    float diagonal_band = exp(-abs(d.y + d.x * 0.32 - sin(d.z * 3.1) * 0.20) * 6.4);
                    float cyan_cloud = smoothstep(0.12, 1.25, broad_wave * 0.5 + 0.5)
                        * equatorial_band;
                    float violet_cloud = smoothstep(0.42, 0.96, fine_wave)
                        * diagonal_band;
                    float dark_rift = smoothstep(0.20, 0.88,
                        sin(d.x * 8.0 - d.y * 5.0 + d.z * 6.0 + slow_time * 0.4) * 0.5 + 0.5);

                    vec3 color = vec3(0.0015, 0.0030, 0.0090);
                    color += vec3(0.012, 0.085, 0.135) * cyan_cloud * sky_strength;
                    color += vec3(0.082, 0.018, 0.120) * violet_cloud * sky_strength;
                    color += vec3(0.025, 0.045, 0.095) * equatorial_band * fine_wave * 0.65 * sky_strength;
                    color *= mix(0.48 + contrast_strength * 0.12, 1.0, dark_rift);
                    ALBEDO = color;
                    EMISSION = color * 1.15;
                }
                """
        };
        var material = new ShaderMaterial { Shader = shader };
        _skyMaterial = material;
        material.SetShaderParameter("sky_strength", 0.82f + SkyBrightness * 1.4f);
        material.SetShaderParameter(
            "contrast_strength",
            Mathf.Clamp(DeepSpaceContrastStrength, 0f, 1f)
        );

        _sphericalSkyShell = new MeshInstance3D
        {
            Name = "SphericalSkyShell",
            Mesh = sphere,
            MaterialOverride = material,
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off
        };
        _world.AddChild(_sphericalSkyShell, false, InternalMode.Disabled);
        _world.MoveChild(_sphericalSkyShell, 0);
    }

    private float GetSphericalShellRadius()
    {
        return Mathf.Max(FieldRadius * 1.24f, 52f);
    }

    private void BuildForegroundDecorations()
    {
        if (
            _world == null
            || !GodotObject.IsInstanceValid(_world)
            || ForegroundDecorationStrength <= 0.001f
            || ForegroundShardCount <= 0
        )
        {
            return;
        }

        _foregroundDecorationRoot = _world.GetNodeOrNull<Node3D>("ForegroundDecorations");
        if (
            _foregroundDecorationRoot != null
            && GodotObject.IsInstanceValid(_foregroundDecorationRoot)
        )
        {
            return;
        }

        _foregroundDecorationRoot = new Node3D { Name = "ForegroundDecorations" };
        _world.AddChild(_foregroundDecorationRoot, false, InternalMode.Disabled);
        Shader shader = CreateForegroundShardShader();
        ImmediateMesh shardMesh = CreateForegroundShardMesh();
        Color[] palette =
        {
            new(0.22f, 0.78f, 1f, 0.24f),
            new(0.38f, 1f, 0.82f, 0.20f),
            new(0.70f, 0.38f, 1f, 0.18f),
            new(0.96f, 0.34f, 0.74f, 0.16f)
        };

        int count = Mathf.Clamp(ForegroundShardCount, 0, 24);
        for (int i = 0; i < count; i++)
        {
            float y = _random.RandfRange(-0.82f, 0.82f);
            float angle = _random.RandfRange(0f, Mathf.Tau);
            float radial = Mathf.Sqrt(Mathf.Max(0f, 1f - y * y));
            Vector3 direction = new(
                radial * Mathf.Cos(angle),
                y,
                radial * Mathf.Sin(angle)
            );
            Vector3 position = direction * _random.RandfRange(4.2f, 7.1f);
            var pivot = new Node3D
            {
                Name = $"ForegroundShard{i + 1}",
                Position = position,
                Rotation = new Vector3(
                    _random.RandfRange(0f, Mathf.Tau),
                    _random.RandfRange(0f, Mathf.Tau),
                    _random.RandfRange(0f, Mathf.Tau)
                ),
                Scale = new Vector3(
                    _random.RandfRange(0.18f, 0.38f),
                    _random.RandfRange(0.42f, 0.92f),
                    _random.RandfRange(0.14f, 0.30f)
                )
            };
            _foregroundDecorationRoot.AddChild(pivot, false, InternalMode.Disabled);

            Color tint = palette[i % palette.Length];
            var material = new ShaderMaterial { Shader = shader };
            material.SetShaderParameter("shard_tint", tint);
            material.SetShaderParameter(
                "decoration_strength",
                Mathf.Clamp(ForegroundDecorationStrength, 0f, 1f)
            );
            var shard = new MeshInstance3D
            {
                Name = "GlassShard",
                Mesh = shardMesh,
                MaterialOverride = material,
                CastShadow = GeometryInstance3D.ShadowCastingSetting.Off
            };
            pivot.AddChild(shard, false, InternalMode.Disabled);

            _foregroundShards.Add(
                new ForegroundShardState
                {
                    Node = pivot,
                    BasePosition = position,
                    RotationAxis = new Vector3(
                        _random.RandfRange(-0.8f, 0.8f),
                        _random.RandfRange(-0.8f, 0.8f),
                        _random.RandfRange(-0.8f, 0.8f)
                    ).Normalized(),
                    RotationSpeed = _random.RandfRange(0.045f, 0.12f),
                    DriftPhase = _random.RandfRange(0f, Mathf.Tau)
                }
            );
        }
    }

    private void UpdateForegroundDecorations(float dt)
    {
        if (
            _foregroundDecorationRoot == null
            || !GodotObject.IsInstanceValid(_foregroundDecorationRoot)
        )
        {
            return;
        }

        _foregroundDecorationRoot.Rotation = new Vector3(
            Mathf.Sin(_time * 0.037f) * 0.035f,
            -_time * DriftSpeed * 0.22f,
            Mathf.Cos(_time * 0.031f) * 0.022f
        );
        foreach (ForegroundShardState state in _foregroundShards)
        {
            if (state.Node == null || !GodotObject.IsInstanceValid(state.Node))
                continue;

            state.Node.RotateObjectLocal(state.RotationAxis, state.RotationSpeed * dt);
            float drift = Mathf.Sin(_time * 0.21f + state.DriftPhase) * 0.16f;
            state.Node.Position = state.BasePosition + state.BasePosition.Normalized() * drift;
        }
    }

    private void SetupBattleForegroundOverlay()
    {
        if (ForegroundDecorationStrength <= 0.001f)
            return;

        _battleForegroundLayer = GetNodeOrNull<CanvasLayer>("BattleForegroundLayer");
        if (_battleForegroundLayer == null)
        {
            _battleForegroundLayer = new CanvasLayer
            {
                Name = "BattleForegroundLayer",
                Layer = 2
            };
            AddChild(_battleForegroundLayer, false, InternalMode.Disabled);
        }

        _battleForegroundOverlay = _battleForegroundLayer.GetNodeOrNull<BattleForegroundOverlay>(
            "ForegroundGlassOverlay"
        );
        if (_battleForegroundOverlay == null)
        {
            _battleForegroundOverlay = new BattleForegroundOverlay
            {
                Name = "ForegroundGlassOverlay",
                MouseFilter = MouseFilterEnum.Ignore,
                ProcessMode = ProcessModeEnum.Always
            };
            _battleForegroundLayer.AddChild(
                _battleForegroundOverlay,
                false,
                InternalMode.Disabled
            );
            _battleForegroundOverlay.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        }
    }

    private void UpdateBattleForegroundOverlay()
    {
        if (
            _battleForegroundOverlay == null
            || !GodotObject.IsInstanceValid(_battleForegroundOverlay)
        )
        {
            return;
        }

        _battleForegroundOverlay.Configure(
            _time,
            Mathf.Clamp(ForegroundDecorationStrength, 0f, 1f)
        );
    }

    private void UpdateBattleForegroundOverlayThrottled(float dt)
    {
        float rate = Mathf.Max(ForegroundUpdateRate, 1f);
        float interval = 1f / rate;
        _foregroundUpdateAccumulator += dt;

        if (_foregroundUpdateAccumulator < interval)
            return;

        _foregroundUpdateAccumulator = 0f;
        UpdateBattleForegroundOverlay();
    }

    private static Shader CreateForegroundShardShader()
    {
        return new Shader
        {
            Code = """
                shader_type spatial;
                render_mode unshaded, blend_add, cull_disabled, depth_draw_never;

                uniform vec4 shard_tint : source_color = vec4(0.2, 0.8, 1.0, 0.2);
                uniform float decoration_strength = 0.6;

                void fragment() {
                    float facing = abs(dot(normalize(NORMAL), normalize(VIEW)));
                    float rim = pow(1.0 - facing, 2.4);
                    float facet = 0.08 + (1.0 - facing) * 0.18;
                    ALBEDO = shard_tint.rgb;
                    EMISSION = shard_tint.rgb * (facet + rim * 1.15);
                    ALPHA = shard_tint.a * decoration_strength * (0.12 + rim * 0.88);
                }
                """
        };
    }

    private static ImmediateMesh CreateForegroundShardMesh()
    {
        Vector3 top = new(0f, 0.72f, 0f);
        Vector3 bottom = new(0f, -0.72f, 0f);
        Vector3[] ring =
        {
            new(0.52f, 0f, 0f),
            new(0f, 0f, 0.34f),
            new(-0.52f, 0f, 0f),
            new(0f, 0f, -0.34f)
        };
        var mesh = new ImmediateMesh();
        mesh.SurfaceBegin(Mesh.PrimitiveType.Triangles);
        for (int i = 0; i < ring.Length; i++)
        {
            Vector3 current = ring[i];
            Vector3 next = ring[(i + 1) % ring.Length];
            AddShardFacet(mesh, top, current, next);
            AddShardFacet(mesh, bottom, next, current);
        }
        mesh.SurfaceEnd();
        return mesh;
    }

    private static void AddShardFacet(
        ImmediateMesh mesh,
        Vector3 a,
        Vector3 b,
        Vector3 c
    )
    {
        Vector3 normal = (b - a).Cross(c - a).Normalized();
        mesh.SurfaceSetNormal(normal);
        mesh.SurfaceAddVertex(a);
        mesh.SurfaceSetNormal(normal);
        mesh.SurfaceAddVertex(b);
        mesh.SurfaceSetNormal(normal);
        mesh.SurfaceAddVertex(c);
    }

    private static Shader CreateSphericalFlowShader()
    {
        var shader = new Shader
        {
            Code = """
                shader_type spatial;
                render_mode unshaded, blend_add, cull_disabled, depth_draw_never;

                uniform vec4 flow_color : source_color = vec4(1.0, 1.0, 1.0, 0.45);
                uniform float flow_speed = 0.15;
                uniform float flow_phase = 0.0;
                uniform float flow_time = 0.0;
                uniform float flow_strength = 1.0;
                uniform float soft_layer = 0.0;
                uniform bool battle_composition = false;

                void fragment() {
                    float flow_position = fract(UV.x * 2.0 - flow_time * flow_speed + flow_phase);
                    const float head_position = 0.08;
                    float trail_distance = fract(head_position - flow_position + 1.0);
                    float trail_aa = max(fwidth(UV.x * 2.0) * 1.5, 0.00075);
                    float front_cutoff = 1.0 - smoothstep(
                        0.012 - trail_aa,
                        0.028 + trail_aa,
                        trail_distance
                    );
                    float tail_cutoff = 1.0 - smoothstep(
                        0.40 - trail_aa,
                        0.66 + trail_aa,
                        trail_distance
                    );
                    float directional_trail = exp(-trail_distance * (battle_composition ? 21.0 : 7.1)) * tail_cutoff;
                    float neck = exp(-trail_distance * 24.0) * front_cutoff;
                    vec3 color = mix(flow_color.rgb, vec3(1.0), neck * 0.72);
                    float base_level = mix(0.012, 0.055, soft_layer);
                    float composition_mask = 1.0;
                    if (battle_composition) {
                        base_level *= 0.10;
                        float side = smoothstep(0.25, 0.49, abs(SCREEN_UV.x - 0.5));
                        float top = 1.0 - smoothstep(0.10, 0.28, SCREEN_UV.y);
                        composition_mask = mix(0.025, 0.40, max(side, top));
                    }
                    float tail_level = mix(0.82, 0.92, soft_layer);
                    float view_facing = clamp(
                        abs(dot(normalize(NORMAL), normalize(VIEW))),
                        0.0,
                        1.0
                    );
                    float silhouette_aa = max(fwidth(view_facing) * 1.35, 0.025);
                    float silhouette_coverage = smoothstep(0.0, silhouette_aa, view_facing);
                    ALBEDO = color;
                    EMISSION = color * mix(
                        0.24 + directional_trail * 2.5,
                        0.38 + directional_trail * 2.8,
                        soft_layer
                    );
                    ALPHA = flow_color.a * flow_strength
                        * (
                            base_level
                            + directional_trail * tail_level
                        )
                        * silhouette_coverage * composition_mask;
                }
                """
        };
        return shader;
    }

    private static ImmediateMesh CreateSphericalFlowRing(
        Vector3 axis,
        float latitude,
        float radius,
        float width
    )
    {
        const int SegmentCount = 384;
        const int SideCount = 24;
        var centers = new Vector3[SegmentCount + 1];
        var normals = new Vector3[SegmentCount + 1];
        var binormals = new Vector3[SegmentCount + 1];

        Vector3 ringAxis = axis.Normalized();
        Vector3 reference = Mathf.Abs(ringAxis.Dot(Vector3.Up)) > 0.92f
            ? Vector3.Right
            : Vector3.Up;
        Vector3 basisA = ringAxis.Cross(reference).Normalized();
        Vector3 basisB = ringAxis.Cross(basisA).Normalized();
        float clampedLatitude = Mathf.Clamp(latitude, -0.82f, 0.82f);
        float ringRadius = Mathf.Sqrt(1f - clampedLatitude * clampedLatitude);
        for (int i = 0; i <= SegmentCount; i++)
        {
            float angle = Mathf.Tau * i / SegmentCount;
            Vector3 direction = (
                (basisA * Mathf.Cos(angle) + basisB * Mathf.Sin(angle)) * ringRadius
                + ringAxis * clampedLatitude
            ).Normalized();
            centers[i] = direction * radius;
        }

        for (int i = 0; i <= SegmentCount; i++)
        {
            int wrappedIndex = i == SegmentCount ? 0 : i;
            int previousIndex = (wrappedIndex - 1 + SegmentCount) % SegmentCount;
            int nextIndex = (wrappedIndex + 1) % SegmentCount;
            Vector3 previous = centers[previousIndex];
            Vector3 next = centers[nextIndex];
            Vector3 tangent = (next - previous).Normalized();
            Vector3 radial = centers[i].Normalized();
            Vector3 normal = tangent.Cross(radial).Normalized();
            if (normal.LengthSquared() < 0.001f)
                normal = tangent.Cross(Vector3.Up).Normalized();
            normals[i] = normal;
            binormals[i] = tangent.Cross(normal).Normalized();
        }

        var mesh = new ImmediateMesh();
        mesh.SurfaceBegin(Mesh.PrimitiveType.Triangles);
        for (int i = 0; i < SegmentCount; i++)
        {
            float t0 = (float)i / SegmentCount;
            float t1 = (float)(i + 1) / SegmentCount;
            for (int side = 0; side < SideCount; side++)
            {
                int nextSide = (side + 1) % SideCount;
                Vector3 a = TubePoint(centers[i], normals[i], binormals[i], side, SideCount, width);
                Vector3 b = TubePoint(centers[i + 1], normals[i + 1], binormals[i + 1], side, SideCount, width);
                Vector3 c = TubePoint(centers[i + 1], normals[i + 1], binormals[i + 1], nextSide, SideCount, width);
                Vector3 d = TubePoint(centers[i], normals[i], binormals[i], nextSide, SideCount, width);
                float v0 = (float)side / SideCount;
                float v1 = (float)(side + 1) / SideCount;
                Vector3 normalA = (a - centers[i]).Normalized();
                Vector3 normalB = (b - centers[i + 1]).Normalized();
                Vector3 normalC = (c - centers[i + 1]).Normalized();
                Vector3 normalD = (d - centers[i]).Normalized();

                AddFlowVertex(mesh, a, normalA, new Vector2(t0, v0));
                AddFlowVertex(mesh, b, normalB, new Vector2(t1, v0));
                AddFlowVertex(mesh, c, normalC, new Vector2(t1, v1));
                AddFlowVertex(mesh, a, normalA, new Vector2(t0, v0));
                AddFlowVertex(mesh, c, normalC, new Vector2(t1, v1));
                AddFlowVertex(mesh, d, normalD, new Vector2(t0, v1));
            }
        }
        mesh.SurfaceEnd();
        return mesh;
    }

    private static Vector3 TubePoint(
        Vector3 center,
        Vector3 normal,
        Vector3 binormal,
        int side,
        int sideCount,
        float width
    )
    {
        float angle = Mathf.Tau * side / sideCount;
        return center + (normal * Mathf.Cos(angle) + binormal * Mathf.Sin(angle)) * width;
    }

    private static void AddFlowVertex(
        ImmediateMesh mesh,
        Vector3 position,
        Vector3 normal,
        Vector2 uv
    )
    {
        mesh.SurfaceSetNormal(normal);
        mesh.SurfaceSetUV(uv);
        mesh.SurfaceAddVertex(position);
    }

    private void AddNebulaPlane(
        string name,
        Color color,
        Vector2 size,
        int seed,
        Vector3? positionOverride = null,
        float? rollOverride = null
    )
    {
        MeshInstance3D plane = GetNodeOrNull<MeshInstance3D>(
            $"StarfieldViewportContainer/StarfieldViewport/World/{name}"
        );
        if (plane == null)
        {
            plane = new MeshInstance3D { Name = name };
            _world.AddChild(plane, false, InternalMode.Disabled);
        }

        var mesh = new QuadMesh { Size = size };
        Color nebulaColor = new(
            Mathf.Clamp(color.R * NebulaBrightness, 0f, 1f),
            Mathf.Clamp(color.G * NebulaBrightness, 0f, 1f),
            Mathf.Clamp(color.B * NebulaBrightness, 0f, 1f),
            Mathf.Clamp(color.A * NebulaBrightness, 0f, 1f)
        );
        Texture2D texture = GetCachedNebulaTexture(Math.Max(64, NebulaTextureSize), nebulaColor, seed);
        var material = new StandardMaterial3D
        {
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
            BillboardMode = BaseMaterial3D.BillboardModeEnum.Enabled,
            NoDepthTest = true,
            AlbedoColor = Colors.White,
            AlbedoTexture = texture,
            EmissionEnabled = true,
            Emission = new Color(nebulaColor.R, nebulaColor.G, nebulaColor.B, 1f),
            EmissionTexture = texture,
            EmissionEnergyMultiplier = 0.38f * NebulaBrightness
        };
        mesh.Material = material;

        plane.Mesh = mesh;
        if (positionOverride.HasValue)
            plane.Position = positionOverride.Value;
        if (rollOverride.HasValue)
            plane.Rotation = new Vector3(0f, 0f, rollOverride.Value);

        _nebulaPlanes.Add(
            new NebulaPlaneState
            {
                Plane = plane,
                Material = material,
                BasePosition = plane.Position,
                BaseRoll = plane.Rotation.Z,
                BaseScale = plane.Scale,
                BaseAlbedoColor = material.AlbedoColor,
                BaseEmissionEnergy = material.EmissionEnergyMultiplier,
                PhaseOffset = seed * 0.013f
            }
        );
    }

    private void UpdateCameraPath()
    {
        if (_camera == null || !GodotObject.IsInstanceValid(_camera))
            return;

        if (CameraOrbitSpeed > 0.0001f)
        {
            // Slow panoramic orbit: pivot on a small circle around the origin
            // and look outward, so bodies placed at different azimuths drift
            // through the frame one after another instead of crowding one view.
            // Everything is expressed in _world-local space: LookAt would mix in
            // the world's own drift rotation, so set the rotation directly.
            float yaw = _time * CameraOrbitSpeed;
            var outward = new Vector3(Mathf.Sin(yaw), 0f, -Mathf.Cos(yaw));
            _camera.Position = -outward * 2.2f
                + Vector3.Up * (Mathf.Sin(_time * 0.05f) * 0.4f);
            _camera.Rotation = new Vector3(0f, -yaw, 0f);
            return;
        }

        float strength = Mathf.Clamp(CameraPathStrength, 0f, 1f);
        if (strength <= 0.001f)
        {
            _camera.Position = _baseCameraPosition;
            _camera.Rotation = _baseCameraRotation;
            return;
        }

        float pathT = _time * CameraPathSpeed;
        Vector3 pathPosition = SampleClosedCatmullRom(CameraPathPoints, pathT);
        Vector3 cameraPosition = _baseCameraPosition.Lerp(pathPosition, strength);
        Vector3 lookAt = SampleClosedCatmullRom(CameraLookPathPoints, pathT + CameraLookAhead);
        _camera.Position = cameraPosition;
        _camera.LookAt(lookAt, Vector3.Up);
        _camera.RotateObjectLocal(Vector3.Forward, Mathf.Sin(_time * CameraPathSpeed * Mathf.Tau) * CameraRollAmount * strength);
    }

    private void SetupSpaceBattleOverlay()
    {
        _spaceBattleOverlay = GetNodeOrNull<SpaceBattleOverlay>("SpaceBattleOverlay");
        if (_spaceBattleOverlay == null)
        {
            _spaceBattleOverlay = new SpaceBattleOverlay { Name = "SpaceBattleOverlay" };
            AddChild(_spaceBattleOverlay, false, InternalMode.Disabled);
        }

        _spaceBattleOverlay.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        _spaceBattleOverlay.MouseFilter = MouseFilterEnum.Ignore;
        _spaceBattleOverlay.ProcessMode = ProcessModeEnum.Always;
        _spaceBattleOverlay.ZIndex = 8;
        UpdateSpaceBattleOverlay();
    }

    private void UpdateSpaceBattleOverlay()
    {
        if (_spaceBattleOverlay == null || !GodotObject.IsInstanceValid(_spaceBattleOverlay))
            return;

        _spaceBattleOverlay.Configure(
            Mathf.Clamp(StellarisBattleLayerStrength, 0f, 1f),
            Mathf.Max(HyperlaneStrength, 0f),
            Mathf.Clamp(DeepSpaceContrastStrength, 0f, 1f),
            _time,
            Mathf.Clamp(CameraPathStrength, 0f, 1f),
            Mathf.Max(CameraPathSpeed, 0f),
            Mathf.Max(DriftSpeed, 0f)
        );
    }

    private void UpdateSpaceBattleOverlayThrottled(float dt)
    {
        float rate = Mathf.Max(OverlayUpdateRate, 1f);
        float interval = 1f / rate;
        _overlayUpdateAccumulator += dt;

        if (_overlayUpdateAccumulator < interval)
            return;

        _overlayUpdateAccumulator = 0f;
        UpdateSpaceBattleOverlay();
    }

    private void UpdateStarLayersThrottled(float dt)
    {
        float rate = Mathf.Max(StarUpdateRate, 1f);
        float interval = 1f / rate;
        foreach (StarLayer layer in _layers)
        {
            int count = layer.BasePositions.Length;
            if (count == 0)
                continue;

            // Spread the same amount of work across render frames instead of updating the
            // entire MultiMesh every 1 / StarUpdateRate seconds in one large burst.
            layer.UpdateBudget = Mathf.Min(
                count,
                layer.UpdateBudget + count * rate * Mathf.Min(dt, 0.1f)
            );
            int updateCount = Mathf.FloorToInt(layer.UpdateBudget);
            layer.UpdateBudget -= updateCount;

            for (int update = 0; update < updateCount; update++)
            {
                int i = layer.NextUpdateIndex;
                layer.NextUpdateIndex = (i + 1) % count;
                Vector3 position = layer.BasePositions[i];
                float orbitSpeed = TravelSpeed * layer.Speed * 0.00038f;
                Vector3 axis = new(
                    0.18f + Mathf.Sin(layer.Phase[i]) * 0.12f,
                    1f,
                    Mathf.Cos(layer.Phase[i]) * 0.16f
                );
                position = position.Rotated(axis.Normalized(), orbitSpeed * interval * _presentationMotionScale);

                layer.BasePositions[i] = position;

                float twinkle =
                    1f + Mathf.Sin(_time * 1.7f + layer.Phase[i]) * layer.TwinkleStrength;
                SetStarInstance(layer, i, Mathf.Clamp(twinkle, 0.35f, 1.8f));
            }
        }
    }

    private void UpdateNebulaPlanes()
    {
        for (int i = 0; i < _nebulaPlanes.Count; i++)
        {
            NebulaPlaneState state = _nebulaPlanes[i];
            MeshInstance3D plane = state.Plane;
            if (!GodotObject.IsInstanceValid(plane))
                continue;

            float phase = _time * (0.055f + i * 0.012f) + i * 1.7f;
            float effectStrength = Mathf.Clamp(NebulaExpansionAmount / 0.18f, 0f, 1f);
            float expansionWave = 0.5f + 0.5f * Mathf.Sin(_time * (0.19f + i * 0.025f) + state.PhaseOffset);
            float softNoise = Mathf.Sin(_time * (0.047f + i * 0.01f) + state.PhaseOffset * 0.37f) * 0.5f + 0.5f;
            float expansion = 1f + NebulaExpansionAmount * Mathf.Lerp(expansionWave, softNoise, 0.28f);
            Vector2 radial = new(state.BasePosition.X, state.BasePosition.Y);
            Vector2 spreadDirection = radial.LengthSquared() > 0.001f ? radial.Normalized() : Vector2.Right;
            float spread = effectStrength > 0f
                ? NebulaSpreadDistance * (expansion - 1f) / Mathf.Max(NebulaExpansionAmount, 0.001f)
                : 0f;

            plane.Position = state.BasePosition with
            {
                X = state.BasePosition.X + Mathf.Sin(phase) * 0.34f + spreadDirection.X * spread,
                Y = state.BasePosition.Y + Mathf.Cos(phase * 0.8f) * 0.24f + spreadDirection.Y * spread * 0.62f
            };
            plane.Rotation = new Vector3(
                0f,
                0f,
                state.BaseRoll + Mathf.Sin(_time * 0.03f + i) * 0.045f + Mathf.Sin(phase * 0.55f) * 0.018f
            );
            plane.Scale = new Vector3(
                state.BaseScale.X * expansion * (1f + Mathf.Sin(phase * 0.7f) * 0.035f),
                state.BaseScale.Y * (1f + (expansion - 1f) * 0.72f + Mathf.Cos(phase * 0.63f) * 0.03f),
                state.BaseScale.Z
            );

            if (state.Material != null)
            {
                float fade = Mathf.Lerp(1f, Mathf.Lerp(1.08f, 0.82f, expansionWave), effectStrength);
                float emissionPulse = Mathf.Lerp(1f, Mathf.Lerp(0.86f, 1.18f, softNoise), effectStrength);
                Color albedo = state.BaseAlbedoColor;
                albedo.A = Mathf.Clamp(state.BaseAlbedoColor.A * fade, 0f, 1f);
                state.Material.AlbedoColor = albedo;
                state.Material.EmissionEnergyMultiplier = state.BaseEmissionEnergy * emissionPulse;
            }
        }
    }

    private static Vector3 SampleClosedCatmullRom(Vector3[] points, float t)
    {
        if (points == null || points.Length == 0)
            return Vector3.Zero;

        if (points.Length == 1)
            return points[0];

        float wrapped = Mathf.PosMod(t, 1f) * points.Length;
        int i1 = Mathf.FloorToInt(wrapped);
        float localT = wrapped - i1;
        int i0 = PosMod(i1 - 1, points.Length);
        int i2 = PosMod(i1 + 1, points.Length);
        int i3 = PosMod(i1 + 2, points.Length);

        return CatmullRom(points[i0], points[i1], points[i2], points[i3], localT);
    }

    private static Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        float t2 = t * t;
        float t3 = t2 * t;
        return 0.5f * (
            2f * p1
            + (-p0 + p2) * t
            + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2
            + (-p0 + 3f * p1 - 3f * p2 + p3) * t3
        );
    }

    private static int PosMod(int value, int divisor)
    {
        int result = value % divisor;
        return result < 0 ? result + divisor : result;
    }

    private void SetStarInstance(StarLayer layer, int index, float brightness)
    {
        float size = layer.Size[index] * Mathf.Lerp(0.72f, 1.26f, brightness / 1.8f);
        Transform3D transform = Transform3D.Identity
            .ScaledLocal(new Vector3(size, size, size))
            .TranslatedLocal(layer.BasePositions[index]);

        layer.MultiMesh.SetInstanceTransform(index, transform);
        Color color = layer.BaseColors[index] * brightness;
        color.A = Mathf.Clamp(0.58f + brightness * 0.26f, 0.45f, 1f);
        layer.MultiMesh.SetInstanceColor(index, color);
    }

    private Vector3 RandomStarPosition(float minRadius, float maxRadius)
    {
        float y = _random.RandfRange(-1f, 1f);
        float azimuth = _random.RandfRange(0f, Mathf.Tau);
        float radial = Mathf.Sqrt(Mathf.Max(0f, 1f - y * y));
        Vector3 direction = new(
            radial * Mathf.Cos(azimuth),
            y,
            radial * Mathf.Sin(azimuth)
        );
        float radius = _random.RandfRange(minRadius, Mathf.Max(minRadius, maxRadius));
        return direction * radius;
    }

    private Color PickStarColor(float speed)
    {
        float warmth = _random.Randf();
        Color cold = new(0.7f, 0.86f, 1f, 1f);
        Color warm = new(1f, 0.86f, 0.62f, 1f);
        Color violet = new(0.82f, 0.74f, 1f, 1f);
        Color baseColor = warmth < 0.18f ? violet : cold.Lerp(warm, warmth * 0.34f);
        return baseColor * (speed > 0.5f ? _random.RandfRange(0.82f, 1.18f) : _random.RandfRange(0.60f, 1.00f));
    }

    public static void ReleaseCachedResources()
    {
        StarTextureCache.Clear();
        NebulaTextureCache.Clear();
        _cachedStarfieldEnvironment = null;
    }

    public static async System.Threading.Tasks.Task<bool> PrewarmBattleBackgroundTexturesAsync(Node owner)
    {
        if (owner == null || !GodotObject.IsInstanceValid(owner) || !owner.IsInsideTree())
            return false;

        GetCachedStarTexture(48, 3.2f);
        await owner.ToSignal(owner.GetTree(), SceneTree.SignalName.ProcessFrame);
        if (owner == null || !GodotObject.IsInstanceValid(owner) || !owner.IsInsideTree())
            return false;

        int textureSize = MobilePlatform.IsMobile ? 128 : 256;
        const float battleNebulaBrightness = 1.55f;
        foreach (var entry in BattleNebulaDefaults)
        {
            Color color = new(
                Mathf.Clamp(entry.Color.R * battleNebulaBrightness, 0f, 1f),
                Mathf.Clamp(entry.Color.G * battleNebulaBrightness, 0f, 1f),
                Mathf.Clamp(entry.Color.B * battleNebulaBrightness, 0f, 1f),
                Mathf.Clamp(entry.Color.A * battleNebulaBrightness, 0f, 1f)
            );
            GetCachedNebulaTexture(textureSize, color, entry.Seed);

            await owner.ToSignal(owner.GetTree(), SceneTree.SignalName.ProcessFrame);
            if (owner == null || !GodotObject.IsInstanceValid(owner) || !owner.IsInsideTree())
                return false;
        }

        return true;
    }

    private static Godot.Environment GetCachedStarfieldEnvironment()
    {
        if (_cachedStarfieldEnvironment != null && GodotObject.IsInstanceValid(_cachedStarfieldEnvironment))
            return _cachedStarfieldEnvironment;

        _cachedStarfieldEnvironment = new Godot.Environment
        {
            BackgroundMode = Godot.Environment.BGMode.Color,
            BackgroundColor = new Color(0.0015f, 0.0025f, 0.006f, 1f),
            GlowEnabled = true,
            GlowNormalized = true,
            GlowIntensity = 0.52f,
            GlowStrength = 0.46f,
            GlowHdrThreshold = 1.15f,
            AdjustmentEnabled = true,
            AdjustmentBrightness = 0.78f,
            AdjustmentContrast = 1.22f,
            AdjustmentSaturation = 1.08f
        };
        return _cachedStarfieldEnvironment;
    }

    private static Texture2D GetCachedStarTexture(int size, float sharpness)
    {
        var key = (size, sharpness);
        if (
            StarTextureCache.TryGetValue(key, out Texture2D cached)
            && GodotObject.IsInstanceValid(cached)
        )
        {
            return cached;
        }

        cached = CreateStarTexture(size, sharpness);
        StarTextureCache[key] = cached;
        return cached;
    }

    private static Texture2D GetCachedNebulaTexture(int size, Color color, int seed)
    {
        var key = (size, color.ToArgb32(), seed);
        if (
            NebulaTextureCache.TryGetValue(key, out Texture2D cached)
            && GodotObject.IsInstanceValid(cached)
        )
        {
            return cached;
        }

        cached = CreateNebulaTexture(size, color, seed);
        NebulaTextureCache[key] = cached;
        return cached;
    }

    private static Texture2D CreateStarTexture(int size, float sharpness)
    {
        Image image = Image.CreateEmpty(size, size, false, Image.Format.Rgba8);
        Vector2 center = new((size - 1) * 0.5f, (size - 1) * 0.5f);
        float radius = size * 0.5f;
        float coreRadius = Mathf.Lerp(0.22f, 0.08f, Mathf.Clamp((sharpness - 0.2f) / 3.8f, 0f, 1f));
        float haloPower = Mathf.Lerp(2.2f, 7.5f, Mathf.Clamp((sharpness - 0.2f) / 3.8f, 0f, 1f));

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = center.DistanceTo(new Vector2(x, y)) / radius;
                float core = 1f - Mathf.SmoothStep(coreRadius, coreRadius + 0.035f, distance);
                float halo = Mathf.Pow(Mathf.Clamp(1f - distance, 0f, 1f), haloPower) * 0.36f;
                float alpha = Mathf.Clamp(core + halo, 0f, 1f);
                image.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        return ImageTexture.CreateFromImage(image);
    }

    private static Texture2D CreateNebulaTexture(int size, Color color, int seed)
    {
        Image image = Image.CreateEmpty(size, size, false, Image.Format.Rgba8);
        float seedOffset = seed * 0.0137f;
        float tilt = Mathf.Sin(seed * 0.037f) * 0.38f;
        float majorWidth = 0.28f + 0.08f * Mathf.Sin(seed * 0.021f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 uv = new((float)x / (size - 1), (float)y / (size - 1));
                Vector2 p = uv * 2f - Vector2.One;
                p.Y += p.X * tilt;

                float warpA = Fbm(p * 1.6f + new Vector2(seedOffset, -seedOffset * 0.47f));
                float warpB = Fbm(new Vector2(p.Y, p.X) * 2.2f + new Vector2(seedOffset * 0.21f, seedOffset * 0.83f));
                Vector2 warped = p + new Vector2(warpA - 0.5f, warpB - 0.5f) * 0.62f;

                float centerLine =
                    Mathf.Sin(warped.X * 2.35f + seedOffset) * 0.22f +
                    Mathf.Sin(warped.X * 5.15f - seedOffset * 0.41f) * 0.075f +
                    (Fbm(new Vector2(warped.X * 1.15f + seedOffset, seedOffset * 0.31f)) - 0.5f) * 0.32f;

                float dist = Mathf.Abs(warped.Y - centerLine);
                float broadFlow = Mathf.Exp(-dist * dist / (majorWidth * majorWidth));
                float filamentA = Mathf.Exp(-Mathf.Pow(Mathf.Abs(warped.Y - centerLine - 0.18f * Mathf.Sin(warped.X * 4.2f + seedOffset)), 1.35f) / 0.075f);
                float filamentB = Mathf.Exp(-Mathf.Pow(Mathf.Abs(warped.Y - centerLine + 0.24f * Mathf.Cos(warped.X * 3.4f - seedOffset)), 1.25f) / 0.11f);

                float turbulence = Fbm(warped * 4.4f + new Vector2(seedOffset * 0.3f, seedOffset * 0.9f));
                float fineTurbulence = Fbm(warped * 11.0f + new Vector2(-seedOffset, seedOffset * 0.5f));
                float voidMask = Mathf.SmoothStep(0.18f, 0.76f, Fbm(warped * 2.7f + new Vector2(seedOffset * 1.7f, seedOffset * 0.11f)));
                float broadLight = Fbm(warped * 0.72f + new Vector2(seedOffset * 0.5f, -seedOffset * 0.24f));
                float darkRift = Fbm(new Vector2(warped.X * 0.9f + warped.Y * 0.26f, warped.Y * 1.25f) + new Vector2(-seedOffset * 0.62f, seedOffset * 0.18f));
                float edgeFadeX = Mathf.SmoothStep(1.08f, 0.18f, Mathf.Abs(p.X));
                float edgeFadeY = Mathf.SmoothStep(1.0f, 0.35f, Mathf.Abs(p.Y));

                float cloud = broadFlow * 0.44f + filamentA * 0.28f + filamentB * 0.22f;
                cloud *= Mathf.Lerp(0.34f, 1.25f, turbulence);
                cloud += Mathf.Max(fineTurbulence - 0.62f, 0f) * 0.28f;
                cloud *= Mathf.Lerp(0.34f, 1.0f, voidMask);
                cloud *= Mathf.Lerp(0.42f, 1.58f, broadLight);
                cloud *= Mathf.Lerp(0.24f, 1.0f, Mathf.SmoothStep(0.32f, 0.88f, darkRift));
                cloud *= edgeFadeX * edgeFadeY;

                float alpha = Mathf.Clamp(cloud, 0f, 1f) * color.A;
                float highlight = Mathf.Clamp(filamentA * 0.36f + turbulence * 0.22f + broadLight * 0.16f, 0f, 0.72f);
                Color pixel = new(
                    Mathf.Clamp(color.R + highlight * 0.18f, 0f, 1f),
                    Mathf.Clamp(color.G + highlight * 0.20f, 0f, 1f),
                    Mathf.Clamp(color.B + highlight * 0.28f, 0f, 1f),
                    alpha
                );
                image.SetPixel(x, y, pixel);
            }
        }

        return ImageTexture.CreateFromImage(image);
    }

    private static float Fbm(Vector2 p)
    {
        float value = 0f;
        float amplitude = 0.5f;
        float frequency = 1f;
        for (int i = 0; i < 5; i++)
        {
            value += ValueNoise(p * frequency) * amplitude;
            frequency *= 2.03f;
            amplitude *= 0.52f;
        }

        return Mathf.Clamp(value, 0f, 1f);
    }

    private static float ValueNoise(Vector2 p)
    {
        Vector2 i = new(Mathf.Floor(p.X), Mathf.Floor(p.Y));
        Vector2 f = p - i;
        f = f * f * (new Vector2(3f, 3f) - 2f * f);

        float a = Hash21(i);
        float b = Hash21(i + Vector2.Right);
        float c = Hash21(i + Vector2.Down);
        float d = Hash21(i + Vector2.One);
        return Mathf.Lerp(Mathf.Lerp(a, b, f.X), Mathf.Lerp(c, d, f.X), f.Y);
    }

    private static float Hash21(Vector2 p)
    {
        float h = p.Dot(new Vector2(127.1f, 311.7f));
        return Mathf.PosMod(Mathf.Sin(h) * 43758.5453f, 1f);
    }

    private sealed partial class BattleForegroundOverlay : Control
    {
        private const int ShardPointCount = 6;

        private readonly struct GlassShardSpec
        {
            public GlassShardSpec(
                Vector2 center,
                Vector2 size,
                float rotation,
                float drift,
                float phase,
                Color color
            )
            {
                Center = center;
                Size = size;
                Rotation = rotation;
                Drift = drift;
                Phase = phase;
                Color = color;
            }

            public readonly Vector2 Center;
            public readonly Vector2 Size;
            public readonly float Rotation;
            public readonly float Drift;
            public readonly float Phase;
            public readonly Color Color;
        }

        private static readonly GlassShardSpec[] Shards =
        {
            new(new Vector2(0.025f, 0.36f), new Vector2(150f, 300f), -0.16f, 18f, 0.2f, new Color(0.18f, 0.76f, 1f, 0.095f)),
            new(new Vector2(0.975f, 0.39f), new Vector2(170f, 320f), 0.13f, 22f, 1.6f, new Color(0.42f, 0.94f, 0.86f, 0.085f)),
            new(new Vector2(0.245f, 0.43f), new Vector2(72f, 145f), -0.32f, 13f, 2.8f, new Color(0.42f, 0.72f, 1f, 0.052f)),
            new(new Vector2(0.72f, 0.15f), new Vector2(96f, 180f), 0.38f, 16f, 4.1f, new Color(0.72f, 0.40f, 1f, 0.060f)),
            new(new Vector2(0.52f, 0.68f), new Vector2(78f, 138f), -0.18f, 11f, 5.3f, new Color(0.20f, 0.90f, 0.82f, 0.050f))
        };

        private readonly Vector2[][] _polygonBuffers = CreatePointBuffers(
            Shards.Length,
            ShardPointCount
        );
        private readonly Vector2[][] _glowPolygonBuffers = CreatePointBuffers(
            Shards.Length,
            ShardPointCount
        );
        private readonly Vector2[][] _outlineBuffers = CreatePointBuffers(
            Shards.Length,
            ShardPointCount + 1
        );

        private float _time;
        private float _strength;

        public void Configure(float time, float strength)
        {
            _time = time;
            _strength = strength;
            Visible = _strength > 0.001f;
            QueueRedraw();
        }

        public override void _Draw()
        {
            if (_strength <= 0.001f || Size.X <= 1f || Size.Y <= 1f)
                return;

            float scale = Mathf.Min(Size.X / 1920f, Size.Y / 1080f);
            for (int i = 0; i < Shards.Length; i++)
                DrawGlassShard(i, Shards[i], scale);
        }

        private void DrawGlassShard(int shardIndex, GlassShardSpec shard, float scale)
        {
            Vector2 center = new(Size.X * shard.Center.X, Size.Y * shard.Center.Y);
            center += new Vector2(
                Mathf.Sin(_time * 0.17f + shard.Phase),
                Mathf.Cos(_time * 0.13f + shard.Phase * 1.3f)
            ) * shard.Drift * scale;
            float rotation = shard.Rotation
                + Mathf.Sin(_time * 0.075f + shard.Phase) * 0.085f;
            Vector2 halfSize = shard.Size * scale * 0.5f;
            Vector2[] polygon = _polygonBuffers[shardIndex];
            Vector2[] glowPolygon = _glowPolygonBuffers[shardIndex];
            FillShardPolygon(polygon, halfSize, center, rotation, 1f);
            FillShardPolygon(glowPolygon, halfSize, center, rotation, 1.16f);
            float pulse = 0.88f + Mathf.Sin(_time * 0.22f + shard.Phase) * 0.12f;
            Color color = shard.Color with
            {
                A = shard.Color.A * _strength * pulse
            };

            DrawColoredPolygon(glowPolygon, color with { A = color.A * 0.24f });
            DrawColoredPolygon(polygon, color with { A = color.A * 0.48f });
            Vector2[] outline = _outlineBuffers[shardIndex];
            for (int i = 0; i < polygon.Length; i++)
                outline[i] = polygon[i];
            outline[^1] = polygon[0];
            DrawPolyline(outline, color with { A = color.A * 1.45f }, 1.0f * scale, true);
            DrawLine(
                polygon[1],
                polygon[4],
                color with { A = color.A * 0.42f },
                0.7f * scale,
                true
            );
        }

        private static void FillShardPolygon(
            Vector2[] result,
            Vector2 halfSize,
            Vector2 center,
            float rotation,
            float scale
        )
        {
            result[0] = center + (new Vector2(-0.44f * halfSize.X, -0.12f * halfSize.Y) * scale).Rotated(rotation);
            result[1] = center + (new Vector2(-0.10f * halfSize.X, -0.52f * halfSize.Y) * scale).Rotated(rotation);
            result[2] = center + (new Vector2(0.38f * halfSize.X, -0.30f * halfSize.Y) * scale).Rotated(rotation);
            result[3] = center + (new Vector2(0.52f * halfSize.X, 0.16f * halfSize.Y) * scale).Rotated(rotation);
            result[4] = center + (new Vector2(0.08f * halfSize.X, 0.52f * halfSize.Y) * scale).Rotated(rotation);
            result[5] = center + (new Vector2(-0.40f * halfSize.X, 0.28f * halfSize.Y) * scale).Rotated(rotation);
        }

        private static Vector2[][] CreatePointBuffers(int bufferCount, int pointCount)
        {
            var buffers = new Vector2[bufferCount][];
            for (int i = 0; i < bufferCount; i++)
                buffers[i] = new Vector2[pointCount];
            return buffers;
        }
    }

    private sealed partial class SpaceBattleOverlay : Control
    {
        private readonly struct HyperlaneMark
        {
            public HyperlaneMark(Vector2 from, Vector2 to, float phase, float width)
            {
                From = from;
                To = to;
                Phase = phase;
                Width = width;
            }

            public readonly Vector2 From;
            public readonly Vector2 To;
            public readonly float Phase;
            public readonly float Width;
        }

        private static readonly HyperlaneMark[] Hyperlanes =
        {
            new(new Vector2(0.64f, 0.58f), new Vector2(0.95f, 0.39f), 0.2f, 1.1f),
            new(new Vector2(0.60f, 0.48f), new Vector2(0.84f, 0.73f), 1.1f, 0.85f),
            new(new Vector2(0.53f, 0.64f), new Vector2(0.78f, 0.42f), 2.0f, 0.65f),
            new(new Vector2(0.72f, 0.30f), new Vector2(0.59f, 0.97f), 2.9f, 1.2f),
            new(new Vector2(0.40f, 0.61f), new Vector2(0.73f, 0.52f), 3.8f, 0.55f)
        };

        private float _strength;
        private float _hyperlaneStrength;
        private float _contrastStrength;
        private float _time;
        private float _cameraPathStrength;
        private float _cameraPathSpeed;
        private float _driftSpeed;

        public void Configure(
            float strength,
            float hyperlaneStrength,
            float contrastStrength,
            float time,
            float cameraPathStrength,
            float cameraPathSpeed,
            float driftSpeed
        )
        {
            bool previousVisible = Visible;
            float previousStrength = _strength;
            float previousContrastStrength = _contrastStrength;
            _strength = strength;
            _hyperlaneStrength = hyperlaneStrength;
            _contrastStrength = contrastStrength;
            _time = time;
            _cameraPathStrength = cameraPathStrength;
            _cameraPathSpeed = cameraPathSpeed;
            _driftSpeed = driftSpeed;
            Visible = _strength > 0.001f || _contrastStrength > 0.001f;
            if (
                previousVisible != Visible
                || !Mathf.IsEqualApprox(previousStrength, _strength)
                || !Mathf.IsEqualApprox(previousContrastStrength, _contrastStrength)
            )
            {
                QueueRedraw();
            }
        }

        public override void _Draw()
        {
            if ((_strength <= 0.001f && _contrastStrength <= 0.001f) || Size.X <= 1f || Size.Y <= 1f)
                return;

            DrawEdgeDarkening(Mathf.Clamp(_contrastStrength, 0f, 1f));
        }

        private void DrawDeepSpaceShading(float scale)
        {
            float strength = Mathf.Clamp(_contrastStrength, 0f, 1f);
            if (strength <= 0.001f)
                return;

            DrawLine(
                Uv(-0.08f, 0.14f),
                Uv(1.08f, 0.54f),
                new Color(0f, 0.006f, 0.016f, 0.32f * strength),
                360f * scale,
                true
            );
            DrawLine(
                Uv(0.22f, 1.08f),
                Uv(1.08f, 0.18f),
                new Color(0f, 0.004f, 0.012f, 0.24f * strength),
                300f * scale,
                true
            );
            DrawLine(
                Uv(-0.08f, 0.78f),
                Uv(0.68f, 0.18f),
                new Color(0.0f, 0.03f, 0.06f, 0.17f * strength),
                230f * scale,
                true
            );
            DrawLine(
                Uv(0.34f, 0.04f),
                Uv(1.08f, 0.10f),
                new Color(0.20f, 0.54f, 0.48f, 0.09f * strength),
                260f * scale,
                true
            );
            DrawLine(
                Uv(-0.06f, 0.52f),
                Uv(0.62f, 0.42f),
                new Color(0.18f, 0.26f, 0.62f, 0.075f * strength),
                220f * scale,
                true
            );

            DrawEdgeDarkening(strength);
        }

        private void DrawRegionalLighting(float scale)
        {
            DrawSoftGlow(Uv(0.34f, 0.27f), 330f * scale, new Color(0.10f, 0.28f, 0.74f), 0.16f * _strength);
            DrawSoftGlow(Uv(0.63f, 0.49f), 430f * scale, new Color(0.07f, 0.52f, 0.68f), 0.21f * _strength);
            DrawSoftGlow(Uv(0.86f, 0.38f), 290f * scale, new Color(0.58f, 0.13f, 0.72f), 0.15f * _strength);
            DrawSoftGlow(Uv(0.57f, 0.42f), 155f * scale, new Color(0.20f, 0.72f, 0.95f), 0.18f * _strength);
            DrawSoftGlow(Uv(0.75f, 0.51f), 130f * scale, new Color(0.88f, 0.24f, 0.74f), 0.13f * _strength);
            DrawSoftShadow(Uv(0.48f, 0.12f), 360f * scale, 0.32f * _contrastStrength);
            DrawSoftShadow(Uv(0.30f, 0.77f), 390f * scale, 0.25f * _contrastStrength);
            DrawSoftShadow(Uv(0.88f, 0.72f), 310f * scale, 0.22f * _contrastStrength);
        }

        private void DrawEdgeDarkening(float strength)
        {
            const int StripCount = 18;
            for (int i = 0; i < StripCount; i++)
            {
                float t = (float)i / StripCount;
                float alpha = Mathf.Pow(1f - t, 2.0f) * 0.035f * strength;
                float topHeight = Size.Y * 0.012f;
                float sideWidth = Size.X * 0.012f;
                DrawRect(new Rect2(0f, i * topHeight, Size.X, topHeight + 1f), new Color(0f, 0f, 0f, alpha));
                DrawRect(new Rect2(0f, Size.Y - (i + 1f) * topHeight, Size.X, topHeight + 1f), new Color(0f, 0f, 0f, alpha * 1.25f));
                DrawRect(new Rect2(i * sideWidth, 0f, sideWidth + 1f, Size.Y), new Color(0f, 0f, 0f, alpha * 0.82f));
                DrawRect(new Rect2(Size.X - (i + 1f) * sideWidth, 0f, sideWidth + 1f, Size.Y), new Color(0f, 0f, 0f, alpha * 1.1f));
            }
        }

        private void DrawOrbitalGrid(float scale)
        {
            float overlayRotation = GetOverlayRotation();
            Color orbitColor = new(0.18f, 0.92f, 0.68f, 0.17f * _strength);
            DrawEllipseArc(Uv(-0.07f, 0.36f), new Vector2(Size.X * 0.33f, Size.Y * 0.12f), -0.06f + overlayRotation, -0.22f, 5.78f, orbitColor, 1.0f * scale);
            DrawEllipseArc(Uv(0.19f, 0.35f), new Vector2(Size.X * 0.55f, Size.Y * 0.17f), -0.08f + overlayRotation, -0.14f, 5.62f, orbitColor with { A = 0.13f * _strength }, 1.0f * scale);
            DrawEllipseArc(Uv(0.47f, 0.24f), new Vector2(Size.X * 0.74f, Size.Y * 0.22f), -0.12f + overlayRotation, -0.4f, 4.86f, orbitColor with { A = 0.095f * _strength }, 1.0f * scale);
            DrawEllipseArc(Uv(0.81f, 0.07f), new Vector2(Size.X * 0.68f, Size.Y * 0.19f), 0.14f + overlayRotation, 2.52f, 5.92f, orbitColor with { A = 0.07f * _strength }, 1.0f * scale);

            DrawOrbitFlow(Uv(-0.07f, 0.36f), new Vector2(Size.X * 0.33f, Size.Y * 0.12f), -0.06f + overlayRotation, -0.22f, 5.78f, 0.08f, scale);
            DrawOrbitFlow(Uv(0.19f, 0.35f), new Vector2(Size.X * 0.55f, Size.Y * 0.17f), -0.08f + overlayRotation, -0.14f, 5.62f, 0.41f, scale);
            DrawOrbitFlow(Uv(0.47f, 0.24f), new Vector2(Size.X * 0.74f, Size.Y * 0.22f), -0.12f + overlayRotation, -0.4f, 4.86f, 0.69f, scale);
            DrawOrbitFlow(Uv(0.81f, 0.07f), new Vector2(Size.X * 0.68f, Size.Y * 0.19f), 0.14f + overlayRotation, 2.52f, 5.92f, 0.87f, scale);
        }

        private void DrawOrbitFlow(
            Vector2 center,
            Vector2 radius,
            float rotation,
            float startAngle,
            float endAngle,
            float phase,
            float scale
        )
        {
            float travel = Mathf.PosMod(_time * (0.032f + phase * 0.012f) + phase, 1f);
            float edgeFade = PathEdgeFade(travel);
            float angle = Mathf.Lerp(startAngle, endAngle, travel);
            float segmentLength = 0.16f + phase * 0.05f;
            Color highlight = new(0.44f, 1f, 0.80f, 0.42f * _strength * edgeFade);
            DrawEllipseArc(center, radius, rotation, angle - segmentLength, angle, highlight, 1.8f * scale);
            Vector2 point = EllipsePoint(center, radius, rotation, angle);
            DrawCircle(point, 3.8f * scale, new Color(0.18f, 0.92f, 0.70f, 0.10f * _strength * edgeFade));
            DrawCircle(point, 1.3f * scale, new Color(0.72f, 1f, 0.90f, 0.78f * _strength * edgeFade));
        }

        private void DrawNebulaRibbons(float scale)
        {
            Vector2[] cyanRibbon = BuildRibbonPoints(0.47f, 0.055f, -0.09f, 0.8f);
            Vector2[] violetRibbon = BuildRibbonPoints(0.55f, 0.072f, 0.12f, 2.4f);

            DrawPolyline(cyanRibbon, new Color(0.01f, 0.08f, 0.15f, 0.20f * _strength), 92f * scale, true);
            DrawPolyline(cyanRibbon, new Color(0.02f, 0.27f, 0.42f, 0.075f * _strength), 46f * scale, true);
            DrawPolyline(cyanRibbon, new Color(0.14f, 0.74f, 0.9f, 0.03f * _strength), 16f * scale, true);

            DrawPolyline(violetRibbon, new Color(0.08f, 0.02f, 0.17f, 0.18f * _strength), 108f * scale, true);
            DrawPolyline(violetRibbon, new Color(0.43f, 0.10f, 0.62f, 0.065f * _strength), 56f * scale, true);
            DrawPolyline(violetRibbon, new Color(0.94f, 0.34f, 0.82f, 0.025f * _strength), 18f * scale, true);

            DrawFlowPulses(cyanRibbon, 0.8f, new Color(0.40f, 0.90f, 1f), 0.18f * _strength, scale);
            DrawFlowPulses(violetRibbon, 2.4f, new Color(0.94f, 0.46f, 1f), 0.13f * _strength, scale);

            DrawSoftGlow(Uv(0.54f, 0.48f), 170f * scale, new Color(0.14f, 0.58f, 0.92f), 0.095f * _strength);
            DrawSoftGlow(Uv(0.67f, 0.43f), 210f * scale, new Color(0.64f, 0.19f, 0.84f), 0.075f * _strength);
        }

        private Vector2[] BuildRibbonPoints(float baseY, float amplitude, float slope, float phase)
        {
            const int PointCount = 28;
            var points = new Vector2[PointCount];
            float animatedPhase = phase + _time * 0.075f;
            float verticalDrift = Mathf.Sin(_time * 0.045f + phase) * 0.012f;
            for (int i = 0; i < PointCount; i++)
            {
                float t = (float)i / (PointCount - 1);
                float wave = Mathf.Sin(t * 5.2f + animatedPhase) * amplitude
                    + Mathf.Sin(t * 11.7f + animatedPhase * 1.9f) * amplitude * 0.24f;
                points[i] = Uv(t, baseY + verticalDrift + (t - 0.5f) * slope + wave);
            }

            return points;
        }

        private void DrawPlanet(float scale)
        {
            Vector2 center = Uv(0.095f, 0.245f);
            float radius = 116f * scale;
            float overlayRotation = GetOverlayRotation();
            DrawSoftGlow(center, radius * 1.62f, new Color(0.12f, 0.56f, 0.86f), 0.12f * _strength);
            DrawCircle(center, radius * 1.08f, new Color(0.34f, 0.75f, 0.9f, 0.12f * _strength));
            DrawCircle(center, radius, new Color(0.004f, 0.012f, 0.022f, 0.96f * _strength));
            DrawCircle(center + new Vector2(-radius * 0.18f, -radius * 0.12f), radius * 0.82f, new Color(0.018f, 0.11f, 0.18f, 0.74f * _strength));
            DrawCircle(center + new Vector2(radius * 0.24f, -radius * 0.19f), radius * 0.32f, new Color(0.11f, 0.30f, 0.34f, 0.34f * _strength));
            DrawCircle(center + new Vector2(-radius * 0.2f, radius * 0.23f), radius * 0.26f, new Color(0.07f, 0.34f, 0.21f, 0.28f * _strength));
            DrawCircle(center + new Vector2(radius * 0.44f, radius * 0.08f), radius * 0.72f, new Color(0f, 0f, 0f, 0.44f * _strength));
            DrawEllipseArc(center, new Vector2(radius * 1.50f, radius * 0.29f), 0.08f + overlayRotation, 0.16f, 5.95f, new Color(0.46f, 0.86f, 1f, 0.34f * _strength), 1.5f * scale);
            DrawEllipseArc(center, new Vector2(radius * 1.43f, radius * 0.25f), 0.08f + overlayRotation, 0.28f, 5.82f, new Color(0.30f, 0.66f, 0.92f, 0.13f * _strength), 4.2f * scale);
        }

        private void DrawDistantSun(float scale)
        {
            Vector2 center = Uv(0.805f, 0.19f);
            float pulse = 0.94f + Mathf.Sin(_time * 0.38f) * 0.06f;
            float coreRadius = 3.4f * scale * pulse;
            DrawSoftGlow(center, 120f * scale, new Color(0.18f, 0.58f, 1f), 0.11f * _strength);
            DrawLine(center - Vector2.Right * 128f * scale, center + Vector2.Right * 128f * scale, new Color(0.24f, 0.68f, 1f, 0.075f * _strength), 0.8f * scale, true);
            DrawLine(center - Vector2.Down * 38f * scale, center + Vector2.Down * 38f * scale, new Color(0.42f, 0.82f, 1f, 0.09f * _strength), 0.7f * scale, true);
            Vector2 diagonal = new Vector2(1f, 1f).Normalized();
            DrawLine(center - diagonal * 18f * scale, center + diagonal * 18f * scale, new Color(0.62f, 0.90f, 1f, 0.13f * _strength), 0.65f * scale, true);
            DrawLine(center - new Vector2(diagonal.X, -diagonal.Y) * 18f * scale, center + new Vector2(diagonal.X, -diagonal.Y) * 18f * scale, new Color(0.62f, 0.90f, 1f, 0.13f * _strength), 0.65f * scale, true);
            DrawCircle(center, coreRadius * 2.2f, new Color(0.32f, 0.72f, 1f, 0.19f * _strength));
            DrawCircle(center, coreRadius, new Color(0.88f, 0.98f, 1f, 0.88f * _strength));
        }

        private void DrawHyperlanes(float scale)
        {
            foreach (HyperlaneMark lane in Hyperlanes)
            {
                float pulse = 0.66f + 0.34f * Mathf.Sin(_time * 0.9f + lane.Phase);
                Vector2 from = Uv(lane.From.X, lane.From.Y);
                Vector2 to = Uv(lane.To.X, lane.To.Y);
                Vector2[] curve = BuildHyperlaneCurve(from, to, lane.Phase);
                float alpha = _strength * _hyperlaneStrength * pulse;
                DrawPolyline(curve, new Color(0.02f, 0.18f, 0.78f, 0.14f * alpha), 4.2f * lane.Width * scale, true);
                DrawPolyline(curve, new Color(0.10f, 0.62f, 1f, 0.24f * alpha), 1.15f * lane.Width * scale, true);
                DrawPolyline(curve, new Color(0.72f, 0.97f, 1f, 0.28f * alpha), 0.42f * lane.Width * scale, true);
                DrawFlowPulses(curve, lane.Phase, new Color(0.62f, 0.94f, 1f), 0.52f * alpha, scale);
            }
        }

        private Vector2[] BuildHyperlaneCurve(Vector2 from, Vector2 to, float phase)
        {
            const int SegmentCount = 28;
            var points = new Vector2[SegmentCount + 1];
            Vector2 direction = to - from;
            Vector2 perpendicular = direction.LengthSquared() > 0.001f
                ? new Vector2(-direction.Y, direction.X).Normalized()
                : Vector2.Up;
            float bend = Size.Y * (0.024f + 0.009f * Mathf.Sin(phase * 1.31f));
            if (Mathf.Sin(phase * 2.17f) < 0f)
                bend = -bend;
            Vector2 control = (from + to) * 0.5f + perpendicular * bend;

            for (int i = 0; i <= SegmentCount; i++)
            {
                float t = (float)i / SegmentCount;
                float oneMinusT = 1f - t;
                points[i] = oneMinusT * oneMinusT * from
                    + 2f * oneMinusT * t * control
                    + t * t * to;
            }

            return points;
        }

        private void DrawFlowPulses(Vector2[] path, float phase, Color color, float alpha, float scale)
        {
            const int PulseCount = 4;
            for (int i = 0; i < PulseCount; i++)
            {
                float t = Mathf.PosMod(_time * 0.085f + phase * 0.11f + (float)i / PulseCount, 1f);
                float edgeFade = PathEdgeFade(t);
                Vector2 position = SamplePath(path, t);
                Vector2 previous = SamplePath(path, Mathf.Max(t - 0.018f, 0f));
                Vector2 direction = (position - previous).Normalized();
                float pulse = 0.68f + 0.32f * Mathf.Sin(_time * 1.1f + phase + i);
                DrawLine(position - direction * 22f * scale, position, color with { A = alpha * 0.28f * pulse * edgeFade }, 2.6f * scale, true);
                DrawLine(position - direction * 8f * scale, position, color with { A = alpha * 0.72f * pulse * edgeFade }, 0.9f * scale, true);
                DrawCircle(position, 1.6f * scale, color with { A = alpha * pulse * edgeFade });
            }
        }

        private static float PathEdgeFade(float t)
        {
            float fadeIn = Mathf.SmoothStep(0f, 0.10f, t);
            float fadeOut = 1f - Mathf.SmoothStep(0.90f, 1f, t);
            return fadeIn * fadeOut;
        }

        private static Vector2 SamplePath(Vector2[] path, float t)
        {
            if (path == null || path.Length == 0)
                return Vector2.Zero;
            if (path.Length == 1)
                return path[0];

            float scaled = Mathf.Clamp(t, 0f, 1f) * (path.Length - 1);
            int index = Mathf.Min(Mathf.FloorToInt(scaled), path.Length - 2);
            return path[index].Lerp(path[index + 1], scaled - index);
        }

        private void DrawStellarClusters(float scale)
        {
            DrawStarCluster(Uv(0.34f, 0.18f), 52f * scale, 0.3f);
            DrawStarCluster(Uv(0.49f, 0.72f), 68f * scale, 1.7f);
            DrawStarCluster(Uv(0.91f, 0.56f), 46f * scale, 3.1f);
        }

        private void DrawStarCluster(Vector2 center, float radius, float phase)
        {
            for (int i = 0; i < 9; i++)
            {
                float angle = phase + i * 2.39996f;
                float distance = radius * (0.18f + 0.78f * ((i * 37 % 11) / 10f));
                Vector2 position = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle) * 0.62f) * distance;
                float size = 0.8f + (i % 3) * 0.55f;
                float twinkle = 0.72f + 0.28f * Mathf.Sin(_time * (0.7f + i * 0.03f) + phase + i);
                Color color = i % 4 == 0
                    ? new Color(1f, 0.74f, 0.46f, 0.62f * _strength * twinkle)
                    : new Color(0.62f, 0.88f, 1f, 0.58f * _strength * twinkle);
                DrawCircle(position, size, color);
                if (i == 2 || i == 7)
                    DrawCrossSpark(position, size * 6f, color with { A = color.A * 0.5f });
            }
        }

        private void DrawCrossSpark(Vector2 center, float radius, Color color)
        {
            DrawLine(center - Vector2.Right * radius, center + Vector2.Right * radius, color, 0.8f, true);
            DrawLine(center - Vector2.Down * radius, center + Vector2.Down * radius, color, 0.8f, true);
        }

        private void DrawDistantSignals(float scale)
        {
            DrawSignal(Uv(0.58f, 0.43f), 12f * scale, 0.1f);
            DrawSignal(Uv(0.70f, 0.29f), 24f * scale, 1.4f);
            DrawSignal(Uv(0.77f, 0.30f), 28f * scale, 2.6f);
            DrawSignal(Uv(0.62f, 0.52f), 10f * scale, 3.2f);
        }

        private void DrawSignal(Vector2 center, float radius, float phase)
        {
            float pulse = 0.55f + 0.45f * Mathf.Sin(_time * 1.4f + phase);
            DrawCircle(center, radius * 1.8f, new Color(0.0f, 0.82f, 1f, 0.045f * _strength * pulse));
            DrawCircle(center, radius, new Color(0.42f, 0.94f, 1f, 0.09f * _strength * pulse));
            DrawCircle(center, radius * 0.18f, new Color(1f, 1f, 1f, 0.28f * _strength * pulse));
        }

        private void DrawSoftGlow(Vector2 center, float radius, Color color, float alpha)
        {
            const int RingCount = 26;
            for (int i = RingCount; i >= 1; i--)
            {
                float t = (float)i / RingCount;
                float ringAlpha = alpha * Mathf.Pow(1f - t, 1.7f) * 0.095f;
                DrawCircle(center, radius * t, color with { A = ringAlpha });
            }
        }

        private void DrawSoftShadow(Vector2 center, float radius, float alpha)
        {
            const int RingCount = 22;
            for (int i = RingCount; i >= 1; i--)
            {
                float t = (float)i / RingCount;
                float ringAlpha = alpha * Mathf.Pow(1f - t, 1.45f) * 0.085f;
                DrawCircle(center, radius * t, new Color(0f, 0.002f, 0.008f, ringAlpha));
            }
        }

        private void DrawEllipseArc(Vector2 center, Vector2 radius, float rotation, float startAngle, float endAngle, Color color, float width)
        {
            int segmentCount = GetEllipseArcSegmentCount(radius, startAngle, endAngle);
            var points = new Vector2[segmentCount + 1];
            for (int i = 0; i <= segmentCount; i++)
            {
                float t = (float)i / segmentCount;
                float angle = Mathf.Lerp(startAngle, endAngle, t);
                points[i] = EllipsePoint(center, radius, rotation, angle);
            }

            DrawPolyline(points, color, width, true);
        }

        private static int GetEllipseArcSegmentCount(
            Vector2 radius,
            float startAngle,
            float endAngle
        )
        {
            float radiusX = Mathf.Abs(radius.X);
            float radiusY = Mathf.Abs(radius.Y);
            float perimeter = Mathf.Pi * (
                3f * (radiusX + radiusY)
                - Mathf.Sqrt((3f * radiusX + radiusY) * (radiusX + 3f * radiusY))
            );
            float sweepRatio = Mathf.Abs(endAngle - startAngle) / Mathf.Tau;
            float arcLength = perimeter * sweepRatio;

            // Keep each straight section shorter than a few screen pixels. Canvas AA then
            // only has to soften the silhouette instead of trying to hide visible facets.
            return Mathf.Clamp(Mathf.CeilToInt(arcLength / 3f), 64, 768);
        }

        private static Vector2 EllipsePoint(Vector2 center, Vector2 radius, float rotation, float angle)
        {
            Vector2 local = new(Mathf.Cos(angle) * radius.X, Mathf.Sin(angle) * radius.Y);
            float sinRot = Mathf.Sin(rotation);
            float cosRot = Mathf.Cos(rotation);
            return center + new Vector2(
                local.X * cosRot - local.Y * sinRot,
                local.X * sinRot + local.Y * cosRot
            );
        }

        private Vector2 Uv(float x, float y)
        {
            Vector2 point = new(Size.X * x, Size.Y * y);
            Vector2 pivot = Size * 0.5f;
            Vector2 offset = GetOverlayOffset();
            return pivot + (point - pivot).Rotated(GetOverlayRotation()) + offset;
        }

        private Vector2 GetOverlayOffset()
        {
            float pathPhase = _time * _cameraPathSpeed * Mathf.Tau;
            float driftPhase = _time * _driftSpeed;
            return new Vector2(
                Size.X * (
                    Mathf.Sin(pathPhase) * 0.018f * _cameraPathStrength
                    + Mathf.Sin(driftPhase * 0.64f) * 0.006f
                ),
                Size.Y * (
                    Mathf.Cos(pathPhase * 0.83f + 0.7f) * 0.014f * _cameraPathStrength
                    + Mathf.Sin(driftPhase * 0.47f) * 0.004f
                )
            );
        }

        private float GetOverlayRotation()
        {
            float pathPhase = _time * _cameraPathSpeed * Mathf.Tau;
            float pathRotation = -pathPhase * 0.008f * _cameraPathStrength;
            float driftRotation = Mathf.Sin(_time * _driftSpeed * 0.38f) * 0.006f;
            return pathRotation + driftRotation;
        }
    }
}
