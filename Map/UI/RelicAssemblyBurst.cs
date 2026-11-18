using System;
using System.Threading.Tasks;
using Godot;

// One GPU-instanced quad per tile. At rest their UV patches tile the original
// artwork exactly; transparent holes belong to the texture, not a generic silhouette.
public partial class RelicAssemblyBurst : MultiMeshInstance2D {
    private const int Grid = 32;
    private const float Duration = 0.90f;
    private const int EchoCount = 6;
    private static readonly Shader EchoShader = GD.Load<Shader>("res://shader/UI/RelicAssemblyEcho.gdshader");
    private static readonly StringName Gather = "gather";
    private static readonly StringName EchoRadius = "echo_radius";
    private static readonly Shader AssemblyShader = GD.Load<Shader>("res://shader/UI/RelicAssembly.gdshader");
    private static readonly Shader ShockShader = GD.Load<Shader>("res://shader/UI/RelicAssemblyShock.gdshader");
    private static readonly StringName Phase = "phase";
    private static readonly StringName Progress = "progress";
    private static readonly StringName Origin = "flight_origin";
    private static readonly StringName PlateOrigin = "plate_origin";
    private static readonly StringName PlateX = "plate_x";
    private static readonly StringName PlateY = "plate_y";
    private static MultiMesh _tiles;

    private readonly TaskCompletionSource<bool> _completion = new();
    private Control _target;
    private TextureRect _art;
    private ScrollContainer _scroll;
    private Color _restTint;
    private Vector2 _sourceInViewport;
    private ShaderMaterial _ink;
    private ColorRect _launchFlash;
    private readonly Sprite2D[] _echoes = new Sprite2D[EchoCount];
    private ShaderMaterial _echoMaterial;
    private Action _release;
    private float _elapsed;
    private bool _positioned;
    private bool _finished;

    private static MultiMesh GetTiles() {
        if (_tiles != null)
            return _tiles;

        var mesh = new MultiMesh {
            TransformFormat = MultiMesh.TransformFormatEnum.Transform2D,
            UseCustomData = true,
            Mesh = new QuadMesh { Size = Vector2.One },
            InstanceCount = Grid * Grid,
        };
        var rng = new Random(4179);
        for (int y = 0; y < Grid; y++) {
            for (int x = 0; x < Grid; x++) {
                int i = y * Grid + x;
                mesh.SetInstanceTransform2D(i, Transform2D.Identity);
                mesh.SetInstanceCustomData(i, new Color((x + 0.5f) / Grid, (y + 0.5f) / Grid,
                    rng.NextSingle(), rng.NextSingle()));
            }
        }
        return _tiles = mesh;
    }

    public Task Start(Control target, TextureRect art, Vector2 source, ScrollContainer scroll, Action release) {
        _target = target;
        _art = art;
        _scroll = scroll;
        _release = release;
        _sourceInViewport = source;
        _restTint = target.Modulate;
        _ink = new ShaderMaterial { Shader = AssemblyShader };
        _ink.SetShaderParameter("tile_size", 1f / Grid);
        Material = _ink;
        Texture = art.Texture;
        Multimesh = GetTiles();
        ZIndex = 100;
        _launchFlash = CreateLaunchFlash();
        _echoMaterial = new ShaderMaterial { Shader = EchoShader };
        for (int i = 0; i < EchoCount; i++) {
            var echo = new Sprite2D { Texture = art.Texture, Material = _echoMaterial,
                ShowBehindParent = true, Visible = false };
            AddChild(echo);
            _echoes[i] = echo;
        }
        ProcessMode = ProcessModeEnum.Always;
        // Suppress the already-inserted icon and count without removing its layout slot.
        _target.Modulate = _restTint with { A = 0f };
        Visible = false;
        return _completion.Task;
    }

    private ColorRect CreateLaunchFlash() {
        var material = new ShaderMaterial { Shader = ShockShader };
        var flash = new ColorRect {
            Material = material,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            ShowBehindParent = true,
            Visible = false,
        };
        AddChild(flash);
        return flash;
    }

    private static void UpdateFlash(ColorRect flash, Vector2 center, float radius, float phase) {
        flash.Visible = phase >= 0f && phase < 1f;
        if (!flash.Visible)
            return;
        flash.Size = Vector2.One * (radius * 2f);
        flash.Position = center - flash.Size * 0.5f;
        ((ShaderMaterial)flash.Material).SetShaderParameter(Phase, phase);
    }

    private void UpdateEchoes(float t, Vector2 center, Vector2 spanX, Vector2 spanY, Vector2 native) {
        // The flying tiles and their complete afterimages share exactly the same six centers.
        float gather = Mathf.SmoothStep(0.54f, 0.84f, t);
        float radius = Mathf.Clamp(Mathf.Max(spanX.Length(), spanY.Length()) * 1.12f, 33f, 82f);
        _ink.SetShaderParameter(Gather, gather);
        _ink.SetShaderParameter(EchoRadius, radius);
        float light = Mathf.SmoothStep(0.46f, 0.59f, t) * (1f - Mathf.SmoothStep(0.76f, 0.89f, t));
        _echoMaterial.SetShaderParameter("strength", light);
        for (int i = 0; i < EchoCount; i++) {
            var echo = _echoes[i];
            echo.Visible = light > 0f;
            if (!echo.Visible)
                continue;
            float angle = Mathf.Tau * i / EchoCount;
            Vector2 position = center + Vector2.FromAngle(angle) * (radius * (1f - gather));
            echo.Transform = new Transform2D(spanX / native.X, spanY / native.Y, position);
        }
    }

    public override void _Process(double delta) {
        if (_finished || _ink == null)
            return;
        if (!GodotObject.IsInstanceValid(_art) || !_art.IsInsideTree() || !_art.IsVisibleInTree()) {
            Finish();
            return;
        }

        // Containers finish sorting newly granted icons after insertion. Track the live
        // rect thereafter as the dock can also move when the reward screen closes.
        if (!_positioned) {
            if (GodotObject.IsInstanceValid(_scroll))
                _scroll.EnsureControlVisible(_target);
            _positioned = true;
        }
        Transform2D fromViewport = GetGlobalTransformWithCanvas().AffineInverse();
        Transform2D toFx = fromViewport * _art.GetGlobalTransformWithCanvas();
        Vector2 native = Texture.GetSize();
        Vector2 size = native * Mathf.Min(_art.Size.X / native.X, _art.Size.Y / native.Y);
        Vector2 corner = toFx * ((_art.Size - size) * 0.5f);
        Vector2 launch = fromViewport * _sourceInViewport;
        Vector2 spanX = toFx.X * size.X;
        Vector2 spanY = toFx.Y * size.Y;
        _ink.SetShaderParameter(Origin, launch);
        _ink.SetShaderParameter(PlateOrigin, corner);
        _ink.SetShaderParameter(PlateX, spanX);
        _ink.SetShaderParameter(PlateY, spanY);
        // The CPU mesh is unit-sized; its shader moves vertices across the HUD.
        RenderingServer.CanvasItemSetCustomRect(GetCanvasItem(), true, fromViewport * GetViewportRect());

        _elapsed += (float)delta;
        float t = Mathf.Clamp(_elapsed / Duration, 0f, 1f);
        _ink.SetShaderParameter(Progress, t);
        UpdateFlash(_launchFlash, launch, 108f, t / 0.24f);
        UpdateEchoes(t, corner + 0.5f * (spanX + spanY), spanX, spanY, native);
        float handoff = Mathf.SmoothStep(0.90f, 1f, t);
        _target.Modulate = _restTint with { A = _restTint.A * handoff };
        SelfModulate = _restTint with { A = _restTint.A * (1f - handoff) };
        Visible = true;
        if (t >= 1f)
            Finish();
    }

    public void Finish() {
        Release();
        if (IsInsideTree() && !IsQueuedForDeletion())
            QueueFree();
    }

    private void Release() {
        if (_finished)
            return;
        _finished = true;
        Visible = false;
        if (GodotObject.IsInstanceValid(_target))
            _target.Modulate = _restTint;
        _release?.Invoke();
        _completion.TrySetResult(true);
    }

    public override void _ExitTree() => Release();
}
