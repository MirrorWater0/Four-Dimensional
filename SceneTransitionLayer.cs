using System.Threading.Tasks;
using Godot;

public partial class SceneTransitionLayer : CanvasLayer
{
    private const string LayerNodeName = "SceneTransitionLayer";
    private const float DefaultFadeDuration = 0.24f;

    private ColorRect _mask;
    private Tween _fadeTween;
    private bool _isTransitioning;
    private ulong _fadeOperationId;

    private ColorRect Mask => _mask ??= GetNodeOrNull<ColorRect>("Mask");

    public static SceneTransitionLayer Ensure(Node caller, bool deferAddToRoot = false)
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
        if (deferAddToRoot)
            root.CallDeferred(Node.MethodName.AddChild, layer);
        else
            root.AddChild(layer);
        return layer;
    }

    public void SwitchScene(string scenePath, float fadeOutDuration = DefaultFadeDuration, float fadeInDuration = DefaultFadeDuration)
    {
        if (string.IsNullOrWhiteSpace(scenePath))
            return;

        _ = RunSwitchSceneAsync(scenePath, fadeOutDuration, fadeInDuration);
    }

    public void ShowBlackImmediate()
    {
        BuildOverlay();
        if (Mask == null)
            return;

        BeginFadeOperation();
        Mask.Visible = true;
        SetMaskInteractive(true);
        SetMaskAlpha(1.0f);
    }

    public Tween PulseBlack(float duration, bool hideAfter = true)
    {
        BuildOverlay();
        if (Mask == null)
            return null;

        ulong operationId = BeginFadeOperation();
        Mask.Visible = true;
        SetMaskInteractive(true);

        if (duration <= 0.0f)
        {
            SetMaskAlpha(1.0f);
            if (hideAfter)
            {
                SetMaskAlpha(0.0f);
                Mask.Visible = false;
                SetMaskInteractive(false);
            }
            return null;
        }

        _fadeTween = CreateTween();
        _fadeTween.TweenProperty(Mask, "modulate:a", 1.0f, duration);
        if (hideAfter)
        {
            _fadeTween.Chain().TweenProperty(Mask, "modulate:a", 0.0f, duration);
            _fadeTween.TweenCallback(
                Callable.From(() =>
                {
                    if (operationId != _fadeOperationId)
                        return;

                    SetMaskAlpha(0.0f);
                    Mask.Visible = false;
                    SetMaskInteractive(false);
                    _fadeTween = null;
                })
            );
            _ = CompletePulseFallbackAsync(operationId, duration * 2.0f);
        }

        return _fadeTween;
    }

    private void BuildOverlay()
    {
        if (Mask != null)
            return;

        _mask = new ColorRect
        {
            Name = "Mask",
            Color = Colors.Black,
            MouseFilter = Control.MouseFilterEnum.Ignore,
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
            bool fadedToBlack = await FadeToBlackCoreAsync(fadeOutDuration);
            if (!fadedToBlack)
                return;

            var err = GetTree().ChangeSceneToFile(scenePath);
            if (err != Error.Ok)
            {
                GD.PushError($"SceneTransitionLayer: failed to change scene to {scenePath}: {err}");
                await FadeFromBlackAsync(fadeInDuration);
                return;
            }

            await FadeFromBlackAsync(fadeInDuration);
        }
        catch (System.Exception e)
        {
            GD.PushError($"SceneTransitionLayer: transition failed: {e.Message}");
            HideImmediate();
        }
        finally
        {
            _isTransitioning = false;
        }
    }

    public async Task FadeToBlackAsync(float duration = DefaultFadeDuration)
    {
        await FadeToBlackCoreAsync(duration);
    }

    private async Task<bool> FadeToBlackCoreAsync(float duration = DefaultFadeDuration)
    {
        BuildOverlay();
        if (Mask == null)
            return false;

        ulong operationId = BeginFadeOperation();
        Mask.Visible = true;
        SetMaskInteractive(true);

        if (duration <= 0.0f)
        {
            SetMaskAlpha(1.0f);
            return true;
        }

        _fadeTween = CreateTween();
        _fadeTween.TweenProperty(Mask, "modulate:a", 1.0f, duration);
        await WaitForFadeDurationAsync(duration);

        if (operationId != _fadeOperationId)
            return false;

        SetMaskAlpha(1.0f);
        _fadeTween = null;
        return true;
    }

    public async Task FadeFromBlackAsync(float duration = DefaultFadeDuration)
    {
        BuildOverlay();
        if (Mask == null)
            return;

        ulong operationId = BeginFadeOperation();
        Mask.Visible = true;
        SetMaskInteractive(true);

        if (duration <= 0.0f)
        {
            SetMaskAlpha(0.0f);
            Mask.Visible = false;
            SetMaskInteractive(false);
            return;
        }

        _fadeTween = CreateTween();
        _fadeTween.TweenProperty(Mask, "modulate:a", 0.0f, duration);
        await WaitForFadeDurationAsync(duration);

        if (operationId != _fadeOperationId)
            return;

        SetMaskAlpha(0.0f);
        Mask.Visible = false;
        SetMaskInteractive(false);
        _fadeTween = null;
    }

    public void HideImmediate()
    {
        BuildOverlay();
        if (Mask == null)
            return;

        BeginFadeOperation();
        SetMaskAlpha(0.0f);
        Mask.Visible = false;
        SetMaskInteractive(false);
    }

    private ulong BeginFadeOperation()
    {
        _fadeOperationId++;
        _fadeTween?.Kill();
        _fadeTween = null;
        return _fadeOperationId;
    }

    private async Task WaitForFadeDurationAsync(float duration)
    {
        if (duration <= 0.0f)
            return;

        var tree = GetTree();
        if (tree == null)
            return;

        await ToSignal(
            tree.CreateTimer(duration + 0.05f),
            SceneTreeTimer.SignalName.Timeout
        );
    }

    private async Task CompletePulseFallbackAsync(ulong operationId, float duration)
    {
        await WaitForFadeDurationAsync(duration);
        if (operationId != _fadeOperationId)
            return;

        SetMaskAlpha(0.0f);
        if (Mask != null)
            Mask.Visible = false;
        SetMaskInteractive(false);
        _fadeTween = null;
    }

    private void SetMaskAlpha(float alpha)
    {
        if (Mask == null)
            return;

        Color color = Mask.Modulate;
        color.A = alpha;
        Mask.Modulate = color;
    }

    private void SetMaskInteractive(bool shouldBlockInput)
    {
        if (Mask == null)
            return;

        Mask.MouseFilter = shouldBlockInput
            ? Control.MouseFilterEnum.Stop
            : Control.MouseFilterEnum.Ignore;
    }
}
