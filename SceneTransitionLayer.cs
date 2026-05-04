using System.Threading.Tasks;
using Godot;

public partial class SceneTransitionLayer : CanvasLayer
{
    private const string LayerNodeName = "SceneTransitionLayer";
    private const float DefaultFadeDuration = 0.24f;

    private ColorRect _mask;
    private Tween _fadeTween;
    private bool _isTransitioning;

    private ColorRect Mask => _mask ??= GetNodeOrNull<ColorRect>("Mask");

    public static SceneTransitionLayer Ensure(Node caller)
    {
        var root = caller?.GetTree()?.Root;
        if (root == null)
            return null;

        var existing = root.GetNodeOrNull<SceneTransitionLayer>(LayerNodeName);
        if (existing != null)
            return existing;

        var layer = new SceneTransitionLayer
        {
            Name = LayerNodeName,
            Layer = 1000,
        };
        layer.BuildOverlay();
        root.AddChild(layer);
        return layer;
    }

    public void SwitchScene(string scenePath, float fadeOutDuration = DefaultFadeDuration, float fadeInDuration = DefaultFadeDuration)
    {
        if (string.IsNullOrWhiteSpace(scenePath))
            return;

        _ = RunSwitchSceneAsync(scenePath, fadeOutDuration, fadeInDuration);
    }

    private void BuildOverlay()
    {
        if (Mask != null)
            return;

        _mask = new ColorRect
        {
            Name = "Mask",
            Color = Colors.Black,
            MouseFilter = Control.MouseFilterEnum.Stop,
            Visible = false,
            Modulate = new Color(1, 1, 1, 0),
        };
        _mask.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        AddChild(_mask);
    }

    private async Task RunSwitchSceneAsync(
        string scenePath,
        float fadeOutDuration,
        float fadeInDuration
    )
    {
        if (_isTransitioning)
            return;

        _isTransitioning = true;
        try
        {
            await FadeToBlackAsync(fadeOutDuration);

            var err = GetTree().ChangeSceneToFile(scenePath);
            if (err != Error.Ok)
            {
                GD.PushError($"SceneTransitionLayer: failed to change scene to {scenePath}: {err}");
                await FadeFromBlackAsync(fadeInDuration);
                return;
            }

            await FadeFromBlackAsync(fadeInDuration);
        }
        finally
        {
            _isTransitioning = false;
        }
    }

    private async Task FadeToBlackAsync(float duration)
    {
        BuildOverlay();
        if (Mask == null)
            return;

        _fadeTween?.Kill();
        Mask.Visible = true;
        _fadeTween = CreateTween();

        if (duration <= 0.0f)
        {
            SetMaskAlpha(1.0f);
            return;
        }

        _fadeTween.TweenProperty(Mask, "modulate:a", 1.0f, duration);
        await ToSignal(_fadeTween, Tween.SignalName.Finished);
        SetMaskAlpha(1.0f);
    }

    private async Task FadeFromBlackAsync(float duration)
    {
        BuildOverlay();
        if (Mask == null)
            return;

        _fadeTween?.Kill();
        Mask.Visible = true;
        _fadeTween = CreateTween();

        if (duration <= 0.0f)
        {
            SetMaskAlpha(0.0f);
            Mask.Visible = false;
            return;
        }

        _fadeTween.TweenProperty(Mask, "modulate:a", 0.0f, duration);
        await ToSignal(_fadeTween, Tween.SignalName.Finished);
        SetMaskAlpha(0.0f);
        Mask.Visible = false;
    }

    private void SetMaskAlpha(float alpha)
    {
        if (Mask == null)
            return;

        Color color = Mask.Modulate;
        color.A = alpha;
        Mask.Modulate = color;
    }
}
