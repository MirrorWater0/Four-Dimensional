using Godot;

public partial class ScreenEffectOverlay : CanvasLayer
{
    public const string OverlayNodeName = "ScreenEffectOverlay";

    private const int OverlayCanvasLayer = 8;
    private const float HealPeakAlpha = 0.28f;
    private const float DamagePeakAlpha = 0.34f;
    private const float FlashInDuration = 0.08f;
    private const float FlashOutDuration = 0.36f;

    private static readonly Color HealFlashColor = new(0.49f, 1f, 0.69f, 1f);
    private static readonly Color DamageFlashColor = new(1f, 0.32f, 0.32f, 1f);

    private static ScreenEffectOverlay _mapOverlay;

    private ColorRect _flash;
    private Tween _flashTween;

    private ColorRect Flash => _flash ??= GetNodeOrNull<ColorRect>("Flash");

    public override void _EnterTree()
    {
        if (ShouldRegisterAsMapOverlay())
            RegisterMapOverlay(this);
    }

    public override void _Ready()
    {
        BuildFlashRect();
    }

    public override void _ExitTree()
    {
        _flashTween?.Kill();
        _flashTween = null;
        if (ReferenceEquals(_mapOverlay, this))
            _mapOverlay = null;
    }

    public static ScreenEffectOverlay EnsureMounted(Node caller)
    {
        if (IsUsableOverlay(_mapOverlay))
            return _mapOverlay;

        var map = FindActiveMap(caller);
        if (map == null)
            return null;

        var existing = FindOverlayOnMap(map);
        if (existing != null)
        {
            RegisterMapOverlay(existing);
            existing.BuildFlashRect();
            return existing;
        }

        var overlay = new ScreenEffectOverlay
        {
            Name = OverlayNodeName,
            Layer = OverlayCanvasLayer,
        };
        overlay.BuildFlashRect();
        map.AddChild(overlay);
        RegisterMapOverlay(overlay);
        return overlay;
    }

    public static void PlayMapPartyLifeChange(int delta)
    {
        if (delta == 0)
            return;

        var overlay = EnsureMounted((Engine.GetMainLoop() as SceneTree)?.Root);
        if (!IsUsableOverlay(overlay))
            return;

        overlay.PlayPartyLifeFlash(delta);
    }

    private void PlayPartyLifeFlash(int delta)
    {
        BuildFlashRect();
        if (Flash == null || !GodotObject.IsInstanceValid(Flash))
            return;

        _flashTween?.Kill();
        bool heal = delta > 0;
        Flash.Color = heal ? HealFlashColor : DamageFlashColor;
        Flash.Visible = true;
        Flash.Modulate = new Color(1f, 1f, 1f, 0f);

        float peak = heal ? HealPeakAlpha : DamagePeakAlpha;
        _flashTween = CreateTween();
        _flashTween
            .TweenProperty(Flash, "modulate:a", peak, FlashInDuration)
            .SetEase(Tween.EaseType.Out);
        _flashTween
            .TweenProperty(Flash, "modulate:a", 0f, FlashOutDuration)
            .SetEase(Tween.EaseType.In);
    }

    private void BuildFlashRect()
    {
        if (Flash != null && GodotObject.IsInstanceValid(Flash))
            return;

        _flash = new ColorRect
        {
            Name = "Flash",
            Color = Colors.White,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            Visible = true,
            Modulate = new Color(1f, 1f, 1f, 0f),
        };
        _flash.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        AddChild(_flash);
    }

    private bool ShouldRegisterAsMapOverlay()
    {
        return GetParent() is Map map && !map.WarmupMode && map.IsInsideTree();
    }

    private static void RegisterMapOverlay(ScreenEffectOverlay overlay)
    {
        if (overlay == null || !GodotObject.IsInstanceValid(overlay))
            return;

        _mapOverlay = overlay;
    }

    private static bool IsUsableOverlay(ScreenEffectOverlay overlay)
    {
        return overlay != null
            && GodotObject.IsInstanceValid(overlay)
            && overlay.IsInsideTree();
    }

    private static Map FindActiveMap(Node caller)
    {
        SceneTree tree = null;
        if (caller != null && GodotObject.IsInstanceValid(caller))
            tree = caller.GetTree();
        tree ??= Engine.GetMainLoop() as SceneTree;

        var root = tree?.Root;
        Map map = caller as Map;
        if (map == null && caller != null && GodotObject.IsInstanceValid(caller))
            map = caller.GetNodeOrNull<Map>("/root/Map");

        map ??= root?.GetNodeOrNull<Map>("/root/Map") ?? root?.GetNodeOrNull<Map>("Map");
        if (map == null || !GodotObject.IsInstanceValid(map) || !map.IsInsideTree() || map.WarmupMode)
            return null;

        return map;
    }

    private static ScreenEffectOverlay FindOverlayOnMap(Map map)
    {
        if (map == null || !GodotObject.IsInstanceValid(map))
            return null;

        var named = map.GetNodeOrNull<ScreenEffectOverlay>(OverlayNodeName);
        if (named != null && GodotObject.IsInstanceValid(named))
            return named;

        foreach (Node child in map.GetChildren())
        {
            if (child is ScreenEffectOverlay overlay && GodotObject.IsInstanceValid(overlay))
                return overlay;
        }

        return null;
    }
}
