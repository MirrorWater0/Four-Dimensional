using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

public partial class BattlePreviewTutorialOverlay : CanvasLayer
{
    private const string TutorialSavePath = "user://tutorial.cfg";
    private const string TutorialSection = "Tutorial";
    private const string BattlePreviewTutorialSeenKey = "BattlePreviewTutorialSeenV2";

    private readonly List<Control> _decorations = [];
    private TaskCompletionSource<bool> _completion;
    private BattlePreview _preview;
    private Control _root;
    private ColorRect _scrim;
    private PanelContainer _messageCard;

    public static bool HasSeenTutorial()
    {
        var config = new ConfigFile();
        if (config.Load(TutorialSavePath) != Error.Ok)
            return false;

        return config.GetValue(TutorialSection, BattlePreviewTutorialSeenKey, false).AsBool();
    }

    public static void MarkTutorialSeen()
    {
        var config = new ConfigFile();
        config.Load(TutorialSavePath);
        config.SetValue(TutorialSection, BattlePreviewTutorialSeenKey, true);
        config.Save(TutorialSavePath);
    }

    public static async Task ShowIfNeededAsync(BattlePreview preview)
    {
        if (
            preview == null
            || !GodotObject.IsInstanceValid(preview)
            || !preview.IsInsideTree()
            || HasSeenTutorial()
        )
            return;

        var root = preview.GetTree()?.Root;
        if (root == null)
            return;

        var overlay = new BattlePreviewTutorialOverlay();
        root.AddChild(overlay);
        await overlay.RunAsync(preview);
    }

    private async Task RunAsync(BattlePreview preview)
    {
        _preview = preview;
        _completion = new TaskCompletionSource<bool>();

        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

        if (_preview == null || !GodotObject.IsInstanceValid(_preview) || !_preview.IsInsideTree())
        {
            QueueFree();
            return;
        }

        BuildUi();
        BuildHighlights();
        PlayIntroAnimation();
        await _completion.Task;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (_completion == null || _completion.Task.IsCompleted)
            return;

        switch (@event)
        {
            case InputEventMouseButton mouseButton when mouseButton.Pressed:
                Dismiss();
                GetViewport().SetInputAsHandled();
                break;
            case InputEventKey key when key.Pressed && !key.Echo:
                Dismiss();
                GetViewport().SetInputAsHandled();
                break;
        }
    }

    public override void _ExitTree()
    {
        _completion?.TrySetResult(true);
    }

    private void BuildUi()
    {
        Layer = 20;
        Name = "BattlePreviewTutorialOverlay";

        _root = new Control
        {
            Name = "Root",
            MouseFilter = Control.MouseFilterEnum.Stop,
        };
        _root.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        AddChild(_root);

        _scrim = new ColorRect
        {
            Color = new Color(0.01f, 0.02f, 0.05f, 0.78f),
            MouseFilter = Control.MouseFilterEnum.Stop,
        };
        _scrim.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        _scrim.GuiInput += OnRootGuiInput;
        _root.AddChild(_scrim);

        _messageCard = BuildMessageCard();
        _root.AddChild(_messageCard);
    }

    private void BuildHighlights()
    {
        AddFormationFrame(_preview.PlayerFormation, true);
        AddFormationFrame(_preview.EnemyFormation, false);
        AddReadyButtonHighlight();
        PositionMessageCard();
    }

    private void PlayIntroAnimation()
    {
        _root.Modulate = _root.Modulate with { A = 0f };

        foreach (var decoration in _decorations)
            decoration.Modulate = decoration.Modulate with { A = 0f };

        if (_messageCard != null)
        {
            _messageCard.Modulate = _messageCard.Modulate with { A = 0f };
            _messageCard.Position += new Vector2(0f, 20f);
        }

        var tween = CreateTween();
        tween.SetParallel(true);
        tween.SetTrans(Tween.TransitionType.Cubic);
        tween.SetEase(Tween.EaseType.Out);
        tween.TweenProperty(_root, "modulate:a", 1f, 0.2f);

        float delay = 0.04f;
        for (int i = 0; i < _decorations.Count; i++)
            tween.TweenProperty(_decorations[i], "modulate:a", 1f, 0.22f).SetDelay(i * delay);

        if (_messageCard != null)
        {
            tween.TweenProperty(_messageCard, "modulate:a", 1f, 0.24f).SetDelay(0.18f);
            tween.TweenProperty(
                    _messageCard,
                    "position",
                    _messageCard.Position - new Vector2(0f, 20f),
                    0.24f
                )
                .SetDelay(0.18f);
        }
    }

    private PanelContainer BuildMessageCard()
    {
        var card = new PanelContainer
        {
            MouseFilter = Control.MouseFilterEnum.Stop,
            CustomMinimumSize = new Vector2(560f, 0f),
        };

        var cardStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.05f, 0.09f, 0.16f, 0.96f),
            BorderColor = new Color(0.92f, 0.8f, 0.55f, 0.9f),
            BorderWidthLeft = 2,
            BorderWidthTop = 2,
            BorderWidthRight = 2,
            BorderWidthBottom = 2,
            CornerRadiusTopLeft = 18,
            CornerRadiusTopRight = 18,
            CornerRadiusBottomLeft = 18,
            CornerRadiusBottomRight = 18,
            ContentMarginLeft = 24,
            ContentMarginRight = 24,
            ContentMarginTop = 20,
            ContentMarginBottom = 18,
            ShadowColor = new Color(0f, 0f, 0f, 0.35f),
            ShadowSize = 12,
            ShadowOffset = new Vector2(0f, 6f),
        };
        card.AddThemeStyleboxOverride("panel", cardStyle);

        var layout = new VBoxContainer();
        layout.AddThemeConstantOverride("separation", 10);
        card.AddChild(layout);

        var title = new Label
        {
            Text = "站位决定出手顺序",
        };
        title.AddThemeFontSizeOverride("font_size", 32);
        title.AddThemeColorOverride("font_color", new Color(0.92f, 0.97f, 1f, 1f));
        layout.AddChild(title);
        title.Text = "布阵与出手顺序";

        var body = new RichTextLabel
        {
            BbcodeEnabled = true,
            FitContent = true,
            ScrollActive = false,
            CustomMinimumSize = new Vector2(0f, 132f),
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        body.AddThemeFontSizeOverride("normal_font_size", 21);
        body.AddThemeColorOverride("default_color", new Color(0.86f, 0.92f, 1f, 0.96f));
        body.Text =
            "同一阵营会按图中的 [color=#f0d28a]1 -> 9[/color] 顺序轮流出手，行动后会重新排到队尾。\n\n"
            + "如果想改角色站位和对应战术，点击右下角的 [color=#f0d28a]战术[/color] 就能调整。\n\n"
            + "[color=#7fc7ff]点击任意位置继续[/color]";
        layout.AddChild(body);

        body.Text =
            "同一阵营会按图中的 [color=#f0d28a]1 -> 9[/color] 顺序轮流出手，行动后会重新排到队尾。\n\n"
            + "可以拖拽我方头像调整站位；悬停双方头像可以查看属性、被动和持有技能。总速度越高，行动点数积累越快。\n\n"
            + "[color=#7fc7ff]点击任意位置继续[/color]";

        body.Text =
            "\u540c\u4e00\u9635\u8425\u4f1a\u6309\u56fe\u4e2d\u7684 [color=#f0d28a]1 -> 9[/color] \u987a\u5e8f\u8f6e\u6d41\u51fa\u624b\uff0c\u884c\u52a8\u540e\u4f1a\u91cd\u65b0\u6392\u5230\u961f\u5c3e\u3002\n\n"
            + "\u53ef\u4ee5\u62d6\u62fd\u6211\u65b9\u5934\u50cf\u8c03\u6574\u7ad9\u4f4d\uff1b\u60ac\u505c\u53cc\u65b9\u5934\u50cf\u53ef\u4ee5\u67e5\u770b\u5c5e\u6027\u3001\u88ab\u52a8\u548c\u6301\u6709\u6280\u80fd\u3002\n\n"
            + "\u603b\u901f\u5ea6\u4f1a\u51b3\u5b9a\u884c\u52a8\u70b9\u7d2f\u79ef\u901f\u5ea6\uff1a\u603b\u901f\u5ea6\u8d8a\u9ad8\uff0c\u8d8a\u5bb9\u6613\u5728\u6b63\u5e38\u8f6e\u6d41\u4e4b\u5916\u62a2\u5230\u989d\u5916\u51fa\u624b\u3002\n\n"
            + "[color=#7fc7ff]\u70b9\u51fb\u4efb\u610f\u4f4d\u7f6e\u7ee7\u7eed[/color]";

        return card;
    }

    private void AddFormationFrame(GridContainer formation, bool isPlayerFormation)
    {
        if (formation == null || !GodotObject.IsInstanceValid(formation))
            return;

        Rect2 rect = formation.GetGlobalRect().Grow(18f);
        var frame = CreateOutlinePanel(rect, new Color(0.5f, 0.78f, 1f, 0.95f), 16, 3);
        _root.AddChild(frame);
        _decorations.Add(frame);

        for (int positionIndex = 1; positionIndex <= 9; positionIndex++)
        {
            int childIndex = ResolveSlotChildIndex(positionIndex, isPlayerFormation);
            if (childIndex < 0 || childIndex >= formation.GetChildCount())
                continue;

            if (formation.GetChild(childIndex) is not Control slot)
                continue;

            AddOrderBadge(slot.GetGlobalRect(), positionIndex);
        }
    }

    private void AddOrderBadge(Rect2 slotRect, int order)
    {
        var badgeRoot = new Control
        {
            MouseFilter = Control.MouseFilterEnum.Ignore,
            Position = slotRect.Position,
            Size = slotRect.Size,
        };

        var halo = new ColorRect
        {
            MouseFilter = Control.MouseFilterEnum.Ignore,
            Color = new Color(1f, 0.83f, 0.5f, 0.16f),
            Position = new Vector2(slotRect.Size.X * 0.5f - 22f, slotRect.Size.Y * 0.5f - 22f),
            Size = new Vector2(44f, 44f),
        };
        badgeRoot.AddChild(halo);

        var label = new Label
        {
            Text = order.ToString(),
            MouseFilter = Control.MouseFilterEnum.Ignore,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Position = new Vector2(slotRect.Size.X * 0.5f - 34f, slotRect.Size.Y * 0.5f - 28f),
            Size = new Vector2(68f, 56f),
        };
        label.AddThemeFontSizeOverride("font_size", 34);
        label.AddThemeColorOverride("font_color", new Color(1f, 0.92f, 0.72f, 1f));
        label.AddThemeColorOverride("font_outline_color", new Color(0.04f, 0.06f, 0.1f, 0.92f));
        label.AddThemeConstantOverride("outline_size", 5);
        badgeRoot.AddChild(label);

        _root.AddChild(badgeRoot);
        _decorations.Add(badgeRoot);
    }

    private void AddReadyButtonHighlight()
    {
        var readyButton = _preview.GetTree()?.Root.GetNodeOrNull<Control>("/root/Map/UI/ReadyButton");
        if (readyButton == null || !GodotObject.IsInstanceValid(readyButton))
            return;

        Rect2 rect = readyButton.GetGlobalRect().Grow(14f);
        var frame = CreateOutlinePanel(rect, new Color(0.94f, 0.82f, 0.58f, 0.98f), 28, 3);
        _root.AddChild(frame);
        _decorations.Add(frame);
    }

    private void PositionMessageCard()
    {
        if (_messageCard == null)
            return;

        const float bottomMargin = 76f;
        _messageCard.Position = new Vector2(
            GetViewport().GetVisibleRect().Size.X * 0.5f - _messageCard.CustomMinimumSize.X * 0.5f,
            GetViewport().GetVisibleRect().Size.Y - bottomMargin - 210f
        );
    }

    private static int ResolveSlotChildIndex(int positionIndex, bool isPlayerFormation)
    {
        if (isPlayerFormation)
        {
            if (BattleReady.remap.TryGetValue(positionIndex, out int mappedIndex))
                return mappedIndex - 1;
            return -1;
        }

        if (BattlePreview.remapEnemy.TryGetValue(positionIndex, out int enemyIndex))
            return enemyIndex - 1;

        return -1;
    }

    private static Panel CreateOutlinePanel(
        Rect2 rect,
        Color borderColor,
        int radius,
        int borderWidth
    )
    {
        var panel = new Panel
        {
            MouseFilter = Control.MouseFilterEnum.Ignore,
            Position = rect.Position,
            Size = rect.Size,
        };

        var style = new StyleBoxFlat
        {
            BgColor = new Color(borderColor.R, borderColor.G, borderColor.B, 0.07f),
            BorderColor = borderColor,
            BorderWidthLeft = borderWidth,
            BorderWidthTop = borderWidth,
            BorderWidthRight = borderWidth,
            BorderWidthBottom = borderWidth,
            CornerRadiusTopLeft = radius,
            CornerRadiusTopRight = radius,
            CornerRadiusBottomLeft = radius,
            CornerRadiusBottomRight = radius,
            ShadowColor = new Color(0f, 0f, 0f, 0.2f),
            ShadowSize = 6,
            ShadowOffset = new Vector2(0f, 2f),
        };
        panel.AddThemeStyleboxOverride("panel", style);
        return panel;
    }

    private void OnRootGuiInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouseButton && mouseButton.Pressed)
            Dismiss();
    }

    private void Dismiss()
    {
        if (_completion == null || _completion.Task.IsCompleted)
            return;

        MarkTutorialSeen();

        var tween = CreateTween();
        tween.SetTrans(Tween.TransitionType.Cubic);
        tween.SetEase(Tween.EaseType.In);
        tween.TweenProperty(_root, "modulate:a", 0f, 0.16f);
        tween.Finished += () =>
        {
            _completion.TrySetResult(true);
            QueueFree();
        };
    }
}
