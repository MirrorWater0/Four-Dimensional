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

        _pileOverlayLayer = parent.GetNode<CanvasLayer>("BattlePileOverlayLayer");
        _pileOverlayRoot = _pileOverlayLayer.GetNode<Control>("PileOverlayRoot");
        _pileOverlayMask = _pileOverlayRoot.GetNode<ColorRect>("Mask");
        PileOverlayMaskMaxAlpha = _pileOverlayMask.Color.A;
        ConfigurePileOverlayMask(_pileOverlayMask);
        var scroll = _pileOverlayRoot.GetNode<ScrollContainer>("Scroll");
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
            _pileOverlayScrollMouseFilter = scroll.MouseFilter;
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
        }

        _pileOverlayMargin = scroll.GetNode<MarginContainer>("Margin");
        _pileOverlaySections = _pileOverlayMargin.GetNode<VBoxContainer>("PileSections");
        GridContainer sceneGrid = _pileOverlaySections.GetNode<GridContainer>("DrawSection/Grid");
        PileOverlayContentWidth = sceneGrid.CustomMinimumSize.X;
        PileOverlayGridColumns = sceneGrid.Columns;
        PileOverlayGridHSeparation = sceneGrid.GetThemeConstant("h_separation");
        PileOverlayGridVSeparation = sceneGrid.GetThemeConstant("v_separation");
        using (Control template = GD.Load<PackedScene>("res://battle/UIScene/PileCardHolder.tscn").Instantiate<Control>())
        {
            PileCardHolderSize = template.CustomMinimumSize;
            SkillCard templateCard = template.GetNode<SkillCard>("Card");
            _pileCardRestPosition = templateCard.Position;
            PileCardScale = templateCard.Scale;
            template.Free();
        }
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
            _pileOverlayRoot.MoveChild(_pileOverlayMask, 0);
        }

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

        _pileOverlayHideButton = _pileOverlayLayer.GetNode<Button>("PileHideButton");
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

        EnsurePileOverlaySelectionButtonsOnTop();
    }

    private void EnsurePileOverlayConfirmButton()
    {
        if (_pileOverlayRoot == null || !GodotObject.IsInstanceValid(_pileOverlayRoot))
            return;

        _pileOverlayConfirmButton = _pileOverlayRoot.GetNodeOrNull<Button>("PileConfirmButton");
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
            _pileCardSelectionAllowsFewer
            || _pileCardSelectionIndexes.Count >= _pileCardSelectionTargetCount;
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

        if (!hidden)
        {
            RestorePileCardSelectionOverlay();
            return;
        }

        _pileOverlayContentTemporarilyHidden = hidden;
        SetPileOverlayContentInputEnabled(!hidden);
        PlayPileOverlayTemporaryHideAnimation();

        SyncPileOverlaySelectionButtons();
        RequestTurnUiRefresh(refreshHover: true);
    }

    private void SetPileOverlayContentInputEnabled(bool enabled)
    {
        if (_pileOverlayMask != null && GodotObject.IsInstanceValid(_pileOverlayMask))
            _pileOverlayMask.MouseFilter = enabled ? MouseFilterEnum.Stop : MouseFilterEnum.Ignore;

        if (_pileOverlayScroll != null && GodotObject.IsInstanceValid(_pileOverlayScroll))
        {
            _pileOverlayScroll.MouseFilter = enabled
                ? _pileOverlayScrollMouseFilter
                : MouseFilterEnum.Ignore;
        }

        SetPileSelectionCardPointerInputEnabled(enabled);
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
                .TweenProperty(
                    _pileOverlayMask,
                    "color:a",
                    0f,
                    PileOverlayContentMoveDuration
                )
                .SetTrans(Tween.TransitionType.Sine)
                .SetEase(Tween.EaseType.In);
        }

        if (_pileOverlayScroll != null && GodotObject.IsInstanceValid(_pileOverlayScroll))
        {
            _pileOverlayScroll.Modulate = Colors.White;
            _pileOverlayFadeTween
                .TweenProperty(
                    _pileOverlayScroll,
                    "modulate:a",
                    0f,
                    PileOverlayContentMoveDuration
                )
                .SetTrans(Tween.TransitionType.Sine)
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

        _pileOverlayFadeTween.SetParallel(false);
        _pileOverlayFadeTween.TweenCallback(
            Callable.From(() =>
            {
                if (
                    !_pileOverlayContentTemporarilyHidden
                    || _pileOverlayRoot == null
                    || !GodotObject.IsInstanceValid(_pileOverlayRoot)
                )
                    return;

                _pileOverlayRoot.Visible = false;
            })
        );
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
            Color maskColor = _pileOverlayMask.Color;
            maskColor.A = PileOverlayMaskMaxAlpha;
            _pileOverlayMask.Color = maskColor;
        }

        if (_pileOverlayScroll != null && GodotObject.IsInstanceValid(_pileOverlayScroll))
        {
            _pileOverlayScroll.Modulate = Colors.White;
            _pileOverlayScroll.OffsetTop =
                _pileOverlayScrollBaseOffsetTop + PileOverlayContentSlideOffset;
            _pileOverlayFadeTween
                .TweenProperty(
                    _pileOverlayScroll,
                    "offset_top",
                    _pileOverlayScrollBaseOffsetTop,
                    PileOverlayContentMoveDuration
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

    private void ConfigurePileOverlayMask(ColorRect mask)
    {
        if (mask == null)
            return;

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
