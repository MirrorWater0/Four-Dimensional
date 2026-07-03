using System.Linq;
using System.Threading.Tasks;
using Godot;

public partial class ExhaustVfx : Node2D
{
    private const string ScenePath = "res://battle/Effect/ExhaustVfx.tscn";
    private const float ReferenceCardDuration = 0.5f;
    private const float AshesBaseLifetime = 1.2f;
    private const float CloudsBaseLifetime = 2.0f;
    private const float AshesBaseSpeedScale = 1.0f;
    private const float CloudsBaseSpeedScale = 1.5f;
    private const float VfxLingerPadding = 0.22f;

    private static PackedScene _scene;
    private float _selfDestructDelay = 0.55f;

    public static ExhaustVfx SpawnAt(Control anchor, float scale, float cardDuration)
    {
        if (anchor == null || !GodotObject.IsInstanceValid(anchor) || !anchor.IsInsideTree())
            return null;

        PackedScene scene = GetScene();
        if (scene == null)
            return null;

        var vfx = scene.Instantiate<ExhaustVfx>();
        vfx.Scale = Vector2.One * scale;
        vfx.ConfigureForCardDuration(cardDuration);

        Node parent = FindAttachParent(anchor);
        parent.AddChild(vfx);
        vfx.GlobalPosition = anchor.GetGlobalRect().GetCenter();
        if (anchor is CanvasItem canvasItem)
            vfx.ZIndex = canvasItem.ZIndex + 8;
        return vfx;
    }

    public static ExhaustVfx SpawnAt(Control anchor, float scale = 0.8f) =>
        SpawnAt(anchor, scale, ReferenceCardDuration);

    public static void Prewarm()
    {
        _ = GetScene();
    }

    private static PackedScene GetScene()
    {
        _scene ??= GD.Load<PackedScene>(ScenePath);
        return _scene;
    }

    private static Node FindAttachParent(Control anchor)
    {
        for (Node node = anchor; node != null; node = node.GetParent())
        {
            if (node is CanvasLayer layer && layer.Name == "CardPlayOverlay")
                return layer;
        }

        for (Node node = anchor; node != null; node = node.GetParent())
        {
            if (node is CanvasLayer layer)
                return layer;
        }

        return anchor.GetTree()?.CurrentScene ?? anchor;
    }

    public void ConfigureForCardDuration(float cardDuration)
    {
        GpuParticles2D ashes = GetNodeOrNull<GpuParticles2D>("Ashes");
        GpuParticles2D clouds = GetNodeOrNull<GpuParticles2D>("Clouds");

        // 灰烬和烟雾需要残留感，不跟着卡牌消失时长压缩。
        float ashesLifetime = ConfigureLayer(
            ashes,
            AshesBaseLifetime,
            AshesBaseSpeedScale,
            1f
        );

        // 烟雾比卡牌消失慢，不跟着卡牌时长压缩，避免序列帧闪得太快。
        float cloudsLifetime = ConfigureLayer(
            clouds,
            CloudsBaseLifetime,
            CloudsBaseSpeedScale,
            1f
        );
        _selfDestructDelay = Mathf.Max(ashesLifetime, cloudsLifetime) + VfxLingerPadding;
    }

    private static float ConfigureLayer(
        GpuParticles2D particles,
        float baseLifetime,
        float baseSpeedScale,
        float timeScale
    )
    {
        if (particles == null)
            return 0f;

        float lifetime = baseLifetime / timeScale;
        particles.Lifetime = lifetime;
        particles.SpeedScale = baseSpeedScale * timeScale;
        return lifetime;
    }

    public override void _Ready()
    {
        foreach (GpuParticles2D particles in GetChildren().OfType<GpuParticles2D>())
        {
            particles.OneShot = true;
            particles.Restart();
            particles.Emitting = true;
        }

        _ = SelfDestructAsync();
    }

    private async Task SelfDestructAsync()
    {
        await ToSignal(
            GetTree().CreateTimer(_selfDestructDelay),
            SceneTreeTimer.SignalName.Timeout
        );
        if (GodotObject.IsInstanceValid(this))
            QueueFree();
    }
}
