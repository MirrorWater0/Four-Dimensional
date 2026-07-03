using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;

public partial class CharacterControl
{
    private void EnsurePileOverlayUi()
    {
        if (
            _pileOverlayRoot != null
            && GodotObject.IsInstanceValid(_pileOverlayRoot)
            && _pileOverlaySections != null
            && GodotObject.IsInstanceValid(_pileOverlaySections)
        )
            return;

        Node parent = BattleNode ?? GetParent();
        if (parent == null)
            return;

        _pileOverlayLayer = parent.GetNodeOrNull<CanvasLayer>("BattlePileOverlayLayer");
        if (_pileOverlayLayer == null)
        {
            _pileOverlayLayer = new CanvasLayer
            {
                Name = "BattlePileOverlayLayer",
                Layer = BattlePileOverlayLayer,
            };
            parent.AddChild(_pileOverlayLayer);
        }

        _pileOverlayRoot = _pileOverlayLayer.GetNodeOrNull<Control>("PileOverlayRoot");
        if (
            _pileOverlayRoot != null
            && GodotObject.IsInstanceValid(_pileOverlayRoot)
            && _pileOverlayRoot.GetNodeOrNull<VBoxContainer>("Scroll/Margin/PileSections") == null
        )
        {
            _pileOverlayRoot.Name = "RetiredPileOverlayRoot";
            _pileOverlayRoot.QueueFree();
            _pileOverlayRoot = null;
        }

        if (_pileOverlayRoot == null)
        {
            _pileOverlayRoot =
                BattlePileOverlayScene?.Instantiate<Control>() ?? CreateFallbackPileOverlayRoot();
            _pileOverlayLayer.AddChild(_pileOverlayRoot);
        }
        ConfigurePileOverlayRoot(_pileOverlayRoot);

        var mask = _pileOverlayRoot.GetNodeOrNull<ColorRect>("Mask");
        if (mask == null)
        {
            mask = new ColorRect
            {
                Name = "Mask",
                Color = new Color(0f, 0f, 0f, 0.68f),
                MouseFilter = MouseFilterEnum.Stop,
            };
            _pileOverlayRoot.AddChild(mask);
        }
        ConfigurePileOverlayMask(mask);
        _pileOverlayMask = mask;

        var scroll = _pileOverlayRoot.GetNodeOrNull<ScrollContainer>("Scroll");
        if (scroll == null)
            scroll = CreateFallbackPileOverlayScroll(_pileOverlayRoot);
        if (_pileOverlayScroll != scroll)
        {
            if (
                _pileOverlayVScrollBar != null
                && GodotObject.IsInstanceValid(_pileOverlayVScrollBar)
            )
            {
                _pileOverlayVScrollBar.ValueChanged -= OnPileOverlayScrollChanged;
                _pileOverlayVScrollBar.GuiInput -= OnPileOverlayScrollBarGuiInput;
            }

            if (
                _pileOverlayScrollInputTarget != null
                && GodotObject.IsInstanceValid(_pileOverlayScrollInputTarget)
            )
                _pileOverlayScrollInputTarget.GuiInput -= OnPileOverlayScrollGuiInput;

            _pileOverlayScroll = scroll;
            _pileOverlayScrollInputTarget = scroll;
            _pileOverlayScroll.GuiInput += OnPileOverlayScrollGuiInput;
            _pileOverlayVScrollBar = _pileOverlayScroll.GetVScrollBar();
            if (_pileOverlayVScrollBar != null)
            {
                _pileOverlayVScrollBar.ValueChanged += OnPileOverlayScrollChanged;
                _pileOverlayVScrollBar.GuiInput += OnPileOverlayScrollBarGuiInput;
            }
        }

        if (_pileOverlayScroll != null && GodotObject.IsInstanceValid(_pileOverlayScroll))
        {
            _pileOverlayScrollBaseOffsetTop = _pileOverlayScroll.OffsetTop;
            CardPileOverlayUi.ConfigureCardScrollContainer(_pileOverlayScroll);
        }

        _pileOverlayMargin = scroll.GetNodeOrNull<MarginContainer>("Margin");
        if (_pileOverlayMargin == null)
            _pileOverlayMargin = CreateFallbackPileOverlayMargin(scroll);
        CardPileOverlayUi.ConfigureScrollContentMargin(_pileOverlayMargin);

        _pileOverlaySections = _pileOverlayMargin.GetNodeOrNull<VBoxContainer>("PileSections");
        if (_pileOverlaySections == null)
        {
            _pileOverlaySections = new VBoxContainer
            {
                Name = "PileSections",
                MouseFilter = MouseFilterEnum.Ignore,
                SizeFlagsHorizontal = SizeFlags.ShrinkCenter,
                SizeFlagsVertical = SizeFlags.ShrinkBegin,
            };
            _pileOverlayMargin.AddChild(_pileOverlaySections);
        }
        _pileOverlaySections.CustomMinimumSize = new Vector2(PileOverlayContentWidth, 0f);
        _pileOverlaySections.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        _pileOverlaySections.SizeFlagsVertical = SizeFlags.ShrinkBegin;
        _pileOverlaySections.AddThemeConstantOverride("separation", PileOverlaySectionSeparation);
        EnsurePileOverlaySelectionButtons();
        EnsurePileOverlayDrawOrder();
        SyncPileOverlaySelectionButtons();
    }

    private void EnsurePileOverlayDrawOrder()
    {
        if (_pileOverlayRoot == null || !GodotObject.IsInstanceValid(_pileOverlayRoot))
            return;

        if (_pileOverlayMask != null && GodotObject.IsInstanceValid(_pileOverlayMask))
        {
            _pileOverlayMask.ZIndex = PileOverlayMaskZIndex;
            _pileOverlayRoot.MoveChild(_pileOverlayMask, 0);
        }

        if (_pileOverlayScroll != null && GodotObject.IsInstanceValid(_pileOverlayScroll))
            _pileOverlayScroll.ZIndex = PileOverlayContentZIndex;

        EnsurePileOverlaySelectionButtonsOnTop();
    }

    private void EnsurePileOverlaySelectionButtonsOnTop()
    {
        if (
            _pileOverlayHideButton != null
            && GodotObject.IsInstanceValid(_pileOverlayHideButton)
            && _pileOverlayLayer != null
            && GodotObject.IsInstanceValid(_pileOverlayLayer)
        )
        {
            _pileOverlayHideButton.ZIndex = PileOverlayConfirmZIndex + 1;
            _pileOverlayHideButton.ZAsRelative = true;
            _pileOverlayLayer.MoveChild(
                _pileOverlayHideButton,
                _pileOverlayLayer.GetChildCount() - 1
            );
        }

        if (_pileOverlayRoot == null || !GodotObject.IsInstanceValid(_pileOverlayRoot))
            return;

        if (
            _pileOverlayConfirmButton != null
            && GodotObject.IsInstanceValid(_pileOverlayConfirmButton)
        )
        {
            _pileOverlayConfirmButton.ZIndex = PileOverlayConfirmZIndex;
            _pileOverlayConfirmButton.ZAsRelative = true;
            _pileOverlayRoot.MoveChild(
                _pileOverlayConfirmButton,
                _pileOverlayRoot.GetChildCount() - 1
            );
        }
    }

    private void EnsurePileOverlaySelectionButtons()
    {
        EnsurePileOverlayHideButton();
        EnsurePileOverlayConfirmButton();
    }

    private void EnsurePileOverlayHideButton()
    {
        if (_pileOverlayLayer == null || !GodotObject.IsInstanceValid(_pileOverlayLayer))
            return;

        _pileOverlayHideButton =
            _pileOverlayLayer.GetNodeOrNull<Button>("PileHideButton")
            ?? _pileOverlayRoot?.GetNodeOrNull<Button>("PileHideButton")
            ?? _pileOverlayRoot?.GetNodeOrNull<Button>("PileCancelButton");
        if (_pileOverlayHideButton == null)
        {
            _pileOverlayHideButton = new Button
            {
                Name = "PileHideButton",
                Visible = false,
            };
            _pileOverlayLayer.AddChild(_pileOverlayHideButton);
        }
        else if (_pileOverlayHideButton.GetParent() != _pileOverlayLayer)
        {
            _pileOverlayHideButton.Reparent(_pileOverlayLayer);
        }

        if (_pileOverlayHideButtonInputTarget != _pileOverlayHideButton)
        {
            if (
                _pileOverlayHideButtonInputTarget != null
                && GodotObject.IsInstanceValid(_pileOverlayHideButtonInputTarget)
            )
            {
                _pileOverlayHideButtonInputTarget.Pressed -= OnPileOverlayHidePressed;
            }

            _pileOverlayHideButton.Pressed += OnPileOverlayHidePressed;
            _pileOverlayHideButtonInputTarget = _pileOverlayHideButton;
        }

        ConfigurePileOverlayHideButtonLayout(_pileOverlayHideButton);
        EnsurePileOverlaySelectionButtonsOnTop();
    }

    private static void ConfigurePileOverlayHideButtonLayout(Button button)
    {
        if (button == null)
            return;

        button.CustomMinimumSize = new Vector2(170f, 58f);
        button.SetAnchorsPreset(Control.LayoutPreset.BottomRight);
        button.OffsetLeft = -406f;
        button.OffsetTop = -86f;
        button.OffsetRight = -236f;
        button.OffsetBottom = -28f;
        button.MouseFilter = MouseFilterEnum.Stop;
        button.FocusMode = FocusModeEnum.None;
        ConfigureEndTurnButton(button);
    }

    private void EnsurePileOverlayConfirmButton()
    {
        if (_pileOverlayRoot == null || !GodotObject.IsInstanceValid(_pileOverlayRoot))
            return;

        _pileOverlayConfirmButton = _pileOverlayRoot.GetNodeOrNull<Button>("PileConfirmButton");
        if (_pileOverlayConfirmButton == null)
        {
            _pileOverlayConfirmButton = new Button
            {
                Name = "PileConfirmButton",
                Visible = false,
            };
            _pileOverlayRoot.AddChild(_pileOverlayConfirmButton);
        }

        if (_pileOverlayConfirmButtonInputTarget != _pileOverlayConfirmButton)
        {
            if (
                _pileOverlayConfirmButtonInputTarget != null
                && GodotObject.IsInstanceValid(_pileOverlayConfirmButtonInputTarget)
            )
            {
                _pileOverlayConfirmButtonInputTarget.Pressed -= OnPileOverlayConfirmPressed;
            }

            _pileOverlayConfirmButton.Pressed += OnPileOverlayConfirmPressed;
            _pileOverlayConfirmButtonInputTarget = _pileOverlayConfirmButton;
        }

        _pileOverlayConfirmButton.CustomMinimumSize = new Vector2(170f, 58f);
        _pileOverlayConfirmButton.SetAnchorsPreset(Control.LayoutPreset.BottomRight);
        _pileOverlayConfirmButton.OffsetLeft = -236f;
        _pileOverlayConfirmButton.OffsetTop = -86f;
        _pileOverlayConfirmButton.OffsetRight = -66f;
        _pileOverlayConfirmButton.OffsetBottom = -28f;
        _pileOverlayConfirmButton.MouseFilter = MouseFilterEnum.Stop;
        _pileOverlayConfirmButton.FocusMode = FocusModeEnum.None;
        ConfigureEndTurnButton(_pileOverlayConfirmButton);
        _pileOverlayConfirmButton.Text = "确认";
        EnsurePileOverlaySelectionButtonsOnTop();
    }

    private void SyncPileOverlaySelectionButtons()
    {
        SyncPileOverlayHideButton();
        SyncPileOverlayConfirmButton();
    }

    private void SyncPileOverlayHideButton()
    {
        if (
            _pileOverlayHideButton == null
            || !GodotObject.IsInstanceValid(_pileOverlayHideButton)
        )
        {
            return;
        }

        bool visible = _isPileCardSelectionActive;
        _pileOverlayHideButton.Visible = visible;
        _pileOverlayHideButton.Disabled = !visible;
        if (!visible)
            return;

        _pileOverlayHideButton.Text = _pileOverlayContentTemporarilyHidden ? "返回选牌" : "隐藏";
        EnsurePileOverlaySelectionButtonsOnTop();
        if (_pileOverlayHideButton.Modulate.A < 0.05f)
            _pileOverlayHideButton.Modulate = Colors.White;
    }

    private void SyncPileOverlayConfirmButton()
    {
        if (
            _pileOverlayConfirmButton == null
            || !GodotObject.IsInstanceValid(_pileOverlayConfirmButton)
        )
        {
            return;
        }

        bool overlayOpen =
            _pileOverlayRoot != null
            && GodotObject.IsInstanceValid(_pileOverlayRoot)
            && _pileOverlayRoot.Visible;
        bool inSelection =
            _isPileCardSelectionActive && overlayOpen && !_pileOverlayContentTemporarilyHidden;
        bool selectionReady =
            _pileCardSelectionIndexes.Count >= _pileCardSelectionTargetCount;
        bool visible = inSelection && selectionReady;
        _pileOverlayConfirmButton.Visible = visible;
        _pileOverlayConfirmButton.Disabled = !visible;
        if (!inSelection)
        {
            _pileOverlayConfirmSelectionReady = false;
            return;
        }

        if (!visible)
        {
            _pileOverlayConfirmSelectionReady = false;
            _pileOverlayConfirmButton.Modulate = new Color(1f, 1f, 1f, 0f);
            return;
        }

        EnsurePileOverlaySelectionButtonsOnTop();
        if (!_pileOverlayConfirmSelectionReady)
        {
            _pileOverlayConfirmSelectionReady = true;
            PlayPileOverlayConfirmRevealAnimation();
            return;
        }

        if (_pileOverlayConfirmButton.Modulate.A < 0.05f)
            _pileOverlayConfirmButton.Modulate = Colors.White;
    }

    private void PlayPileOverlayConfirmRevealAnimation()
    {
        if (
            _pileOverlayConfirmButton == null
            || !GodotObject.IsInstanceValid(_pileOverlayConfirmButton)
        )
        {
            return;
        }

        _pileOverlayConfirmButton.Modulate = new Color(1f, 1f, 1f, 0f);
        Tween tween = _pileOverlayConfirmButton.CreateTween();
        tween
            .TweenProperty(
                _pileOverlayConfirmButton,
                "modulate:a",
                1f,
                PileOverlayConfirmFadeDuration
            )
            .SetTrans(Tween.TransitionType.Sine)
            .SetEase(Tween.EaseType.Out);
    }

    private void OnPileOverlayHidePressed()
    {
        if (!_isPileCardSelectionActive)
            return;

        SetPileOverlayContentTemporarilyHidden(!_pileOverlayContentTemporarilyHidden);
    }

    private void SetPileOverlayContentTemporarilyHidden(bool hidden)
    {
        if (!_isPileCardSelectionActive || _pileOverlayContentTemporarilyHidden == hidden)
            return;

        _pileOverlayContentTemporarilyHidden = hidden;
        if (hidden)
            PlayPileOverlayTemporaryHideAnimation();
        else
            PlayPileOverlayTemporaryShowAnimation();

        SyncPileOverlaySelectionButtons();
        RefreshTurnUi();
        if (hidden)
            ScheduleCardHoverRefresh();
    }

    private void PlayPileOverlayTemporaryHideAnimation()
    {
        if (_pileOverlayRoot == null || !GodotObject.IsInstanceValid(_pileOverlayRoot))
            return;

        CancelPileOverlayScrollBounce(resetOffsetTop: false);
        _pileOverlayFadeTween?.Kill();
        _pileOverlayFadeTween = _pileOverlayRoot.CreateTween();
        _pileOverlayFadeTween.SetParallel(true);

        if (_pileOverlayMask != null && GodotObject.IsInstanceValid(_pileOverlayMask))
        {
            _pileOverlayFadeTween
                .TweenProperty(_pileOverlayMask, "color:a", 0f, 0.14f)
                .SetTrans(Tween.TransitionType.Sine)
                .SetEase(Tween.EaseType.In);
        }

        if (_pileOverlayScroll != null && GodotObject.IsInstanceValid(_pileOverlayScroll))
        {
            _pileOverlayFadeTween
                .TweenProperty(_pileOverlayScroll, "modulate:a", 0f, 0.14f)
                .SetTrans(Tween.TransitionType.Cubic)
                .SetEase(Tween.EaseType.In);
        }

        if (
            _pileOverlayConfirmButton != null
            && GodotObject.IsInstanceValid(_pileOverlayConfirmButton)
        )
        {
            _pileOverlayFadeTween
                .TweenProperty(_pileOverlayConfirmButton, "modulate:a", 0f, 0.12f)
                .SetTrans(Tween.TransitionType.Sine)
                .SetEase(Tween.EaseType.In);
        }
    }

    private void PlayPileOverlayTemporaryShowAnimation()
    {
        if (_pileOverlayRoot == null || !GodotObject.IsInstanceValid(_pileOverlayRoot))
            return;

        CancelPileOverlayScrollBounce(resetOffsetTop: false);
        _pileOverlayFadeTween?.Kill();
        _pileOverlayFadeTween = _pileOverlayRoot.CreateTween();
        _pileOverlayFadeTween.SetParallel(true);

        if (_pileOverlayMask != null && GodotObject.IsInstanceValid(_pileOverlayMask))
        {
            _pileOverlayFadeTween
                .TweenProperty(
                    _pileOverlayMask,
                    "color:a",
                    PileOverlayMaskMaxAlpha,
                    PileOverlayContentFadeInDuration
                )
                .SetTrans(Tween.TransitionType.Sine)
                .SetEase(Tween.EaseType.Out);
        }

        if (_pileOverlayScroll != null && GodotObject.IsInstanceValid(_pileOverlayScroll))
        {
            _pileOverlayFadeTween
                .TweenProperty(
                    _pileOverlayScroll,
                    "modulate:a",
                    1f,
                    PileOverlayContentFadeInDuration
                )
                .SetTrans(Tween.TransitionType.Cubic)
                .SetEase(Tween.EaseType.Out);
            _pileOverlayFadeTween
                .TweenProperty(
                    _pileOverlayScroll,
                    "offset_top",
                    _pileOverlayScrollBaseOffsetTop,
                    PileOverlayContentFadeInDuration
                )
                .SetTrans(Tween.TransitionType.Cubic)
                .SetEase(Tween.EaseType.Out);
        }

        SyncPileOverlayConfirmButton();
    }

    private void OnPileOverlayConfirmPressed()
    {
        if (!_isPileCardSelectionActive)
            return;

        _ = CompletePileCardSelectionAsync();
    }

    private void ConfigurePileOverlayRoot(Control root)
    {
        if (root == null)
            return;

        root.SetAnchorsPreset(LayoutPreset.FullRect);
        root.MouseFilter = MouseFilterEnum.Ignore;
        if (
            _pileOverlayRootInputTarget != null
            && GodotObject.IsInstanceValid(_pileOverlayRootInputTarget)
        )
        {
            _pileOverlayRootInputTarget.GuiInput -= OnPileOverlayBackgroundGuiInput;
            _pileOverlayRootInputTarget = null;
        }
    }

    private static Control CreateFallbackPileOverlayRoot()
    {
        return new Control
        {
            Name = "PileOverlayRoot",
            Visible = false,
            MouseFilter = MouseFilterEnum.Stop,
        };
    }

    private static ScrollContainer CreateFallbackPileOverlayScroll(Control root)
    {
        var scroll = new ScrollContainer
        {
            Name = "Scroll",
            MouseFilter = MouseFilterEnum.Pass,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
            VerticalScrollMode = ScrollContainer.ScrollMode.ShowNever,
        };
        scroll.SetAnchorsPreset(LayoutPreset.FullRect);
        scroll.OffsetLeft = 160f;
        scroll.OffsetTop = 86f;
        scroll.OffsetRight = -160f;
        scroll.OffsetBottom = CardPileOverlayUi.ScrollBottomOffset;
        root.AddChild(scroll);
        return scroll;
    }

    private static MarginContainer CreateFallbackPileOverlayMargin(ScrollContainer scroll)
    {
        var margin = new MarginContainer
        {
            Name = "Margin",
            MouseFilter = MouseFilterEnum.Ignore,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ShrinkBegin,
        };
        margin.AddThemeConstantOverride("margin_left", 12);
        margin.AddThemeConstantOverride("margin_top", 12);
        margin.AddThemeConstantOverride("margin_right", 12);
        margin.AddThemeConstantOverride("margin_bottom", 12);
        scroll.AddChild(margin);
        return margin;
    }

    private void ConfigurePileOverlayMask(ColorRect mask)
    {
        if (mask == null)
            return;

        mask.SetAnchorsPreset(LayoutPreset.FullRect);
        mask.Color = new Color(0f, 0f, 0f, 0.68f);
        mask.MouseFilter = MouseFilterEnum.Stop;
        mask.ZIndex = PileOverlayMaskZIndex;
        if (_pileOverlayMaskInputTarget != mask)
        {
            if (
                _pileOverlayMaskInputTarget != null
                && GodotObject.IsInstanceValid(_pileOverlayMaskInputTarget)
            )
                _pileOverlayMaskInputTarget.GuiInput -= OnPileOverlayBackgroundGuiInput;

            mask.GuiInput += OnPileOverlayBackgroundGuiInput;
            _pileOverlayMaskInputTarget = mask;
        }
    }

    private void OnPileOverlayBackgroundGuiInput(InputEvent @event)
    {
        if (
            @event is InputEventMouseButton mouseButton
            && mouseButton.Pressed
            && mouseButton.ButtonIndex == MouseButton.Left
        )
        {
            if (_isPileCardSelectionActive)
            {
                GetViewport().SetInputAsHandled();
                return;
            }

            HidePileOverlay();
            GetViewport().SetInputAsHandled();
        }
    }


}
