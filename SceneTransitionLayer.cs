using System.Threading.Tasks;
using Godot;

public partial class SceneTransitionLayer : CanvasLayer
{
    private const string LayerNodeName = "SceneTransitionLayer";
    private const string StartInterfaceScenePath = "res://BeginGame/StartInterface.tscn";
    private const float DefaultFadeDuration = 0.24f;
    private const int StartInterfaceReadyFrameLimit = 90;

    private ColorRect _mask;
    private VBoxContainer _loadingOverlay;
    private Label _loadingStatus;
    private ProgressBar _loadingProgress;
    private Control _loadingSquareFrame;
    private double _loadingSquareRotationStartSeconds;
    private bool _loadingSpinnerEnabled = true;
    private Tween _fadeTween;
    private bool _isTransitioning;
    private ulong _fadeOperationId;

    private ColorRect Mask => _mask ??= GetNodeOrNull<ColorRect>("Mask");

    public override void _Notification(int what)
    {
        if (what == NotificationWMCloseRequest)
            PreloadeScene.ReleaseCachedResources();
    }

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

    public void ShowLoadingProgress(float progress, string status)
    {
        BuildOverlay();
        BuildLoadingOverlay();
        if (_loadingOverlay == null)
            return;

        _loadingOverlay.Visible = true;
        bool wasSquareVisible = _loadingSquareFrame.Visible;
        _loadingSquareFrame.Visible = _loadingSpinnerEnabled;
        if (_loadingSpinnerEnabled && !wasSquareVisible)
            _loadingSquareRotationStartSeconds = GetMonotonicSeconds();
        _loadingProgress.Value = Mathf.Clamp(progress, 0f, 1f) * 100f;
        _loadingStatus.Text = string.IsNullOrWhiteSpace(status) ? "正在准备游戏资源…" : status;
    }

    public void HideLoadingProgress()
    {
        if (_loadingOverlay == null)
            return;

        _loadingOverlay.Visible = false;
        if (_loadingSquareFrame != null)
            _loadingSquareFrame.Visible = false;
    }

    public void SetLoadingSpinnerEnabled(bool enabled)
    {
        _loadingSpinnerEnabled = enabled;
        if (_loadingSquareFrame == null)
            return;

        bool shouldBeVisible = enabled && _loadingOverlay?.Visible == true;
        if (shouldBeVisible && !_loadingSquareFrame.Visible)
            _loadingSquareRotationStartSeconds = GetMonotonicSeconds();
        _loadingSquareFrame.Visible = shouldBeVisible;
    }

    public override void _Process(double delta)
    {
        if (_loadingSquareFrame?.Visible == true)
        {
            double elapsed = GetMonotonicSeconds() - _loadingSquareRotationStartSeconds;
            _loadingSquareFrame.Rotation = (float)(elapsed * 0.9d);
        }
    }

    private static double GetMonotonicSeconds()
    {
        return Time.GetTicksMsec() * 0.001d;
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
            Color = new Color(0.09f, 0.1f, 0.11f, 1f),
            MouseFilter = Control.MouseFilterEnum.Ignore,
            Visible = false,
            Modulate = new Color(1, 1, 1, 0),
        };
        _mask.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        AddChild(_mask);
    }

    private void BuildLoadingOverlay()
    {
        if (_loadingOverlay != null)
            return;

        _loadingOverlay = new VBoxContainer
        {
            Name = "LoadingOverlay",
            MouseFilter = Control.MouseFilterEnum.Ignore,
            Visible = false,
            CustomMinimumSize = new Vector2(360f, 0f),
        };
        _loadingOverlay.SetAnchorsPreset(Control.LayoutPreset.CenterBottom);
        _loadingOverlay.OffsetLeft = -180f;
        _loadingOverlay.OffsetTop = -132f;
        _loadingOverlay.OffsetRight = 180f;
        _loadingOverlay.OffsetBottom = -72f;
        _loadingOverlay.AddThemeConstantOverride("separation", 8);

        var title = new Label
        {
            Text = "正在准备游戏资源",
            HorizontalAlignment = HorizontalAlignment.Center,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        title.AddThemeFontSizeOverride("font_size", 22);
        title.AddThemeColorOverride("font_color", new Color(0.9f, 0.91f, 0.94f, 1f));
        _loadingOverlay.AddChild(title);

        _loadingStatus = new Label
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        _loadingStatus.AddThemeFontSizeOverride("font_size", 16);
        _loadingStatus.AddThemeColorOverride("font_color", new Color(0.65f, 0.67f, 0.72f, 1f));
        _loadingOverlay.AddChild(_loadingStatus);

        _loadingProgress = new ProgressBar
        {
            MinValue = 0,
            MaxValue = 100,
            ShowPercentage = false,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            CustomMinimumSize = new Vector2(360f, 8f),
        };
        _loadingProgress.AddThemeStyleboxOverride(
            "background",
            new StyleBoxFlat
            {
                BgColor = new Color(0.16f, 0.17f, 0.19f, 1f),
                BorderWidthLeft = 1,
                BorderWidthTop = 1,
                BorderWidthRight = 1,
                BorderWidthBottom = 1,
                BorderColor = new Color(0.48f, 0.5f, 0.54f, 1f),
            }
        );
        _loadingProgress.AddThemeStyleboxOverride(
            "fill",
            new StyleBoxFlat
            {
                BgColor = new Color(0.84f, 0.85f, 0.88f, 1f),
            }
        );
        _loadingOverlay.AddChild(_loadingProgress);
        AddChild(_loadingOverlay);

        _loadingSquareFrame = new Control
        {
            Name = "LoadingSquareFrame",
            MouseFilter = Control.MouseFilterEnum.Ignore,
            CustomMinimumSize = new Vector2(112f, 112f),
            PivotOffset = new Vector2(56f, 56f),
            Visible = false,
        };
        _loadingSquareFrame.SetAnchorsPreset(Control.LayoutPreset.Center);
        _loadingSquareFrame.OffsetLeft = -56f;
        _loadingSquareFrame.OffsetTop = -56f;
        _loadingSquareFrame.OffsetRight = 56f;
        _loadingSquareFrame.OffsetBottom = 56f;

        Vector2[] squarePoints =
        [
            new Vector2(20.5f, 20.5f),
            new Vector2(91.5f, 20.5f),
            new Vector2(91.5f, 91.5f),
            new Vector2(20.5f, 91.5f),
            new Vector2(20.5f, 20.5f),
        ];

        // The soft outer stroke hides the staircase pattern that becomes visible when
        // a one-pixel outline is rotated, while the inner stroke keeps the shape crisp.
        var squareSoftEdge = new Line2D
        {
            Width = 4.5f,
            DefaultColor = new Color(0.84f, 0.85f, 0.89f, 0.16f),
            Antialiased = true,
            JointMode = Line2D.LineJointMode.Round,
            Points = squarePoints,
        };
        _loadingSquareFrame.AddChild(squareSoftEdge);

        var squareLine = new Line2D
        {
            Width = 1.5f,
            DefaultColor = new Color(0.84f, 0.85f, 0.89f, 0.9f),
            Antialiased = true,
            JointMode = Line2D.LineJointMode.Round,
            Points = squarePoints,
        };
        _loadingSquareFrame.AddChild(squareLine);
        AddChild(_loadingSquareFrame);
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

            await WaitForStartInterfacePresentationAsync(scenePath);
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

    private async Task WaitForStartInterfacePresentationAsync(string scenePath)
    {
        if (!string.Equals(scenePath, StartInterfaceScenePath, System.StringComparison.OrdinalIgnoreCase))
            return;

        SceneTree tree = GetTree();
        if (tree == null)
            return;

        for (int frame = 0; frame < StartInterfaceReadyFrameLimit; frame++)
        {
            if (tree.CurrentScene is StartInterface startInterface
                && startInterface.IsPresentationReady)
            {
                // Let the SubViewport submit one fully built frame before the
                // black mask is removed. This prevents an uninitialised render
                // target from briefly showing old GPU contents as random art.
                await ToSignal(tree, SceneTree.SignalName.ProcessFrame);
                return;
            }

            await ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        }

        GD.PushWarning("SceneTransitionLayer: title background was not ready before the transition timeout.");
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
