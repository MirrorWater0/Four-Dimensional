using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;

public partial class CharacterControl
{
    private void LogPileOverlayLayoutTrace(string label)
    {
        if (!PileOverlayLayoutTraceEnabled)
            return;

        var parts = new List<string>
        {
            $"[PileLayout] t={Time.GetTicksMsec()} label={label}",
            FormatPileOverlayViewportTrace(),
            FormatPileOverlayScrollTrace(),
            FormatPileOverlayNodeTrace("root", _pileOverlayRoot),
            FormatPileOverlayNodeTrace("scroll", _pileOverlayScroll),
            FormatPileOverlayNodeTrace("margin", _pileOverlayMargin),
            FormatPileOverlayNodeTrace("sections", _pileOverlaySections),
            FormatPileOverlayVirtualTrace(),
        };

        GD.Print(string.Join(" | ", parts));
    }

    private string FormatPileOverlayViewportTrace()
    {
        Vector2 viewportSize = GetViewport()?.GetVisibleRect().Size ?? Vector2.Zero;
        return $"viewport[w={viewportSize.X:F1},h={viewportSize.Y:F1}]";
    }

    private string FormatPileOverlayScrollTrace()
    {
        if (_pileOverlayScroll == null || !GodotObject.IsInstanceValid(_pileOverlayScroll))
            return "scrollState=null";

        string bar = "bar=null";
        if (_pileOverlayVScrollBar != null && GodotObject.IsInstanceValid(_pileOverlayVScrollBar))
        {
            bar =
                $"bar[v={_pileOverlayVScrollBar.Value:F1},min={_pileOverlayVScrollBar.MinValue:F1},max={_pileOverlayVScrollBar.MaxValue:F1},page={_pileOverlayVScrollBar.Page:F1},vis={_pileOverlayVScrollBar.Visible}]";
        }

        return
            $"scrollState[v={_pileOverlayScroll.ScrollVertical},modeH={(int)_pileOverlayScroll.HorizontalScrollMode},modeV={(int)_pileOverlayScroll.VerticalScrollMode},baseTop={_pileOverlayScrollBaseOffsetTop:F1},offsetTop={_pileOverlayScroll.OffsetTop:F1},virtual={_pileOverlayVirtualGrids.Count}] {bar}";
    }

    private string FormatPileOverlayNodeTrace(string label, Control control)
    {
        if (control == null || !GodotObject.IsInstanceValid(control))
            return $"{label}=null";

        Rect2 rect = control.GetGlobalRect();
        float globalX = rect.Position.X;
        float dx = 0f;
        if (_pileOverlayLayoutTraceLastGlobalX.TryGetValue(label, out float lastX))
            dx = globalX - lastX;
        _pileOverlayLayoutTraceLastGlobalX[label] = globalX;

        return
            $"{label}[vis={control.Visible},gx={globalX:F1},dx={dx:F1},x={control.Position.X:F1},sx={rect.Size.X:F1},size={control.Size.X:F1},min={control.CustomMinimumSize.X:F1},hf={(int)control.SizeFlagsHorizontal},child={control.GetChildCount()}]";
    }

    private string FormatPileOverlayVirtualTrace()
    {
        if (
            _pileOverlayVirtualGrids.Count == 0
            || _pileOverlayScroll == null
            || !GodotObject.IsInstanceValid(_pileOverlayScroll)
        )
        {
            return "virtual=none";
        }

        Rect2 visibleRect = _pileOverlayScroll.GetGlobalRect();
        var parts = new List<string>();
        foreach (PileOverlayVirtualGrid virtualGrid in _pileOverlayVirtualGrids.Take(2))
        {
            if (
                virtualGrid?.Grid == null
                || !GodotObject.IsInstanceValid(virtualGrid.Grid)
                || virtualGrid.Entries == null
            )
            {
                continue;
            }

            float rowHeight = PileCardHolderSize.Y + PileOverlayGridVSeparation;
            Rect2 gridRect = virtualGrid.Grid.GetGlobalRect();
            float visibleTop = Mathf.Max(0f, visibleRect.Position.Y - gridRect.Position.Y);
            float visibleBottom = Mathf.Min(
                virtualGrid.Grid.CustomMinimumSize.Y,
                visibleRect.End.Y - gridRect.Position.Y
            );
            CalculatePileOverlayVirtualWindow(
                virtualGrid.Entries.Length,
                rowHeight,
                visibleTop,
                visibleBottom,
                out int firstIndex,
                out int visibleSlotCount
            );
            int firstRow = firstIndex / PileOverlayGridColumns;
            int lastRow = visibleSlotCount <= 0
                ? firstRow
                : (firstIndex + visibleSlotCount - 1) / PileOverlayGridColumns;
            parts.Add(
                $"{virtualGrid.Kind}[cards={virtualGrid.Entries.Length},holders={virtualGrid.Holders.Count},window={firstIndex}+{visibleSlotCount},rows={firstRow}-{lastRow},gridY={gridRect.Position.Y:F1},vis={visibleTop:F1}-{visibleBottom:F1},h={virtualGrid.Grid.CustomMinimumSize.Y:F1}]"
            );
        }

        return parts.Count == 0 ? "virtual=invalid" : $"virtual={string.Join(",", parts)}";
    }

    private bool IsPileOverlayVisible()
    {
        return _pileOverlayRoot != null
            && GodotObject.IsInstanceValid(_pileOverlayRoot)
            && _pileOverlayRoot.Visible
            && (
                (_pileOverlayMask != null
                    && GodotObject.IsInstanceValid(_pileOverlayMask)
                    && _pileOverlayMask.Color.A > 0.01f)
                || (_pileOverlayScroll != null
                    && GodotObject.IsInstanceValid(_pileOverlayScroll)
                    && _pileOverlayScroll.Modulate.A > 0.01f)
            );
    }

    private void ResetPileOverlayPresentation()
    {
        CancelPileOverlayScrollBounce(resetOffsetTop: false);

        if (_pileOverlayRoot != null && GodotObject.IsInstanceValid(_pileOverlayRoot))
            _pileOverlayRoot.Modulate = Colors.White;

        if (_pileOverlayMask != null && GodotObject.IsInstanceValid(_pileOverlayMask))
        {
            Color maskColor = _pileOverlayMask.Color;
            maskColor.A = 0f;
            _pileOverlayMask.Color = maskColor;
        }

        if (_pileOverlayScroll != null && GodotObject.IsInstanceValid(_pileOverlayScroll))
        {
            _pileOverlayScroll.Modulate = Colors.White;
            _pileOverlayScroll.OffsetTop = _pileOverlayScrollBaseOffsetTop + PileOverlayContentSlideOffset;
        }

        if (_pileOverlayConfirmButton != null && GodotObject.IsInstanceValid(_pileOverlayConfirmButton))
            _pileOverlayConfirmButton.Modulate = Colors.White;

        if (_pileOverlayHideButton != null && GodotObject.IsInstanceValid(_pileOverlayHideButton))
            _pileOverlayHideButton.Modulate = Colors.White;

        _pileOverlayConfirmSelectionReady = false;
        _pileOverlayContentTemporarilyHidden = false;
    }

    private void SetPileOverlayPresentationFullyVisible()
    {
        CancelPileOverlayScrollBounce(resetOffsetTop: false);

        if (_pileOverlayMask != null && GodotObject.IsInstanceValid(_pileOverlayMask))
        {
            Color maskColor = _pileOverlayMask.Color;
            maskColor.A = PileOverlayMaskMaxAlpha;
            _pileOverlayMask.Color = maskColor;
        }

        if (_pileOverlayScroll != null && GodotObject.IsInstanceValid(_pileOverlayScroll))
        {
            _pileOverlayScroll.Modulate = Colors.White;
            _pileOverlayScroll.OffsetTop = _pileOverlayScrollBaseOffsetTop;
        }

        if (_pileOverlayConfirmButton != null && GodotObject.IsInstanceValid(_pileOverlayConfirmButton))
            _pileOverlayConfirmButton.Modulate = Colors.White;
    }

    private void PlayPileOverlayIntroAnimation()
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

        if (
            _pileOverlayHideButton != null
            && GodotObject.IsInstanceValid(_pileOverlayHideButton)
            && _isPileCardSelectionActive
        )
        {
            _pileOverlayHideButton.Visible = true;
            _pileOverlayHideButton.Modulate = Colors.White;
            EnsurePileOverlaySelectionButtonsOnTop();
        }
    }

    private void PlayPileOverlayOutroAnimation(int hideVersion, Action onFinished)
    {
        if (_pileOverlayRoot == null || !GodotObject.IsInstanceValid(_pileOverlayRoot))
        {
            onFinished?.Invoke();
            return;
        }

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

        if (_pileOverlayConfirmButton != null && GodotObject.IsInstanceValid(_pileOverlayConfirmButton))
        {
            _pileOverlayFadeTween
                .TweenProperty(_pileOverlayConfirmButton, "modulate:a", 0f, 0.12f)
                .SetTrans(Tween.TransitionType.Sine)
                .SetEase(Tween.EaseType.In);
        }

        if (_pileOverlayHideButton != null && GodotObject.IsInstanceValid(_pileOverlayHideButton))
        {
            _pileOverlayFadeTween
                .TweenProperty(_pileOverlayHideButton, "modulate:a", 0f, 0.12f)
                .SetTrans(Tween.TransitionType.Sine)
                .SetEase(Tween.EaseType.In);
        }

        _pileOverlayFadeTween.SetParallel(false);
        _pileOverlayFadeTween.TweenCallback(
            Callable.From(() =>
            {
                if (hideVersion != _pileOverlayBuildVersion)
                    return;

                onFinished?.Invoke();
            })
        );
    }

    private void ResetPileOverlayScroll()
    {
        _pileOverlayScrollRefreshPending = false;
        CancelPileOverlaySmoothScroll();
        bool resetBounceOffset =
            _pileOverlayScroll != null
            && GodotObject.IsInstanceValid(_pileOverlayScroll)
            && _pileOverlayScroll.Modulate.A > 0.95f;
        CancelPileOverlayScrollBounce(resetOffsetTop: resetBounceOffset);
        if (_pileOverlayScroll == null || !GodotObject.IsInstanceValid(_pileOverlayScroll))
            return;

        _pileOverlayScroll.ScrollVertical = 0;
        if (_pileOverlayVScrollBar != null && GodotObject.IsInstanceValid(_pileOverlayVScrollBar))
            _pileOverlayVScrollBar.Value = _pileOverlayVScrollBar.MinValue;
    }

    private void OnPileOverlayScrollGuiInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouseButton)
        {
            if (
                !mouseButton.Pressed
                || mouseButton.ButtonIndex is not MouseButton.WheelUp and not MouseButton.WheelDown
            )
            {
                return;
            }

            float visualDirection = mouseButton.ButtonIndex == MouseButton.WheelUp ? 1f : -1f;
            float scrollDirection = mouseButton.ButtonIndex == MouseButton.WheelUp ? -1f : 1f;
            float wheelFactor = Math.Max(0.35f, Math.Abs(mouseButton.Factor));
            if (
                QueuePileOverlaySmoothScrollByDelta(
                    scrollDirection * PileOverlaySmoothWheelStep * wheelFactor,
                    visualDirection,
                    wheelFactor
                )
            )
                GetViewport().SetInputAsHandled();
            return;
        }

        if (@event is InputEventPanGesture panGesture)
        {
            float scrollDelta = panGesture.Delta.Y * PileOverlayPanGestureMultiplier;
            if (Mathf.Abs(scrollDelta) <= 0.01f)
                return;

            float visualDirection = scrollDelta < 0f ? 1f : -1f;
            float bounceStrength = Mathf.Clamp(
                Mathf.Abs(scrollDelta) / PileOverlaySmoothWheelStep,
                0.5f,
                1.4f
            );
            if (QueuePileOverlaySmoothScrollByDelta(scrollDelta, visualDirection, bounceStrength))
                GetViewport().SetInputAsHandled();
            return;
        }

        if (MobilePlatform.TryGetTouchScrollDelta(@event, out float touchDelta, 1.15f))
        {
            float visualDirection = touchDelta < 0f ? 1f : -1f;
            float bounceStrength = Mathf.Clamp(
                Mathf.Abs(touchDelta) / PileOverlaySmoothWheelStep,
                0.35f,
                1.1f
            );
            if (QueuePileOverlaySmoothScrollByDelta(touchDelta, visualDirection, bounceStrength))
                GetViewport().SetInputAsHandled();
        }
    }

    private void OnPileOverlayScrollBarGuiInput(InputEvent @event)
    {
        if (
            _pileOverlaySmoothScrollActive
            && @event is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left }
        )
            CancelPileOverlaySmoothScroll();
    }

    private bool QueuePileOverlaySmoothScrollByDelta(
        float scrollDelta,
        float visualDirection,
        float bounceStrength
    )
    {
        if (!CanPileOverlayReceiveSmoothScroll())
            return false;

        double minValue = _pileOverlayVScrollBar.MinValue;
        double maxValue = GetPileOverlayScrollMaxValue();
        double currentValue = _pileOverlayVScrollBar.Value;
        if (!_pileOverlaySmoothScrollActive)
        {
            _pileOverlaySmoothScrollPosition = currentValue;
            _pileOverlaySmoothScrollTarget = currentValue;
            _pileOverlaySmoothScrollVelocity = 0d;
        }

        double sourceTarget = _pileOverlaySmoothScrollTarget;
        double requestedTarget = sourceTarget + scrollDelta;
        double clampedTarget = Math.Clamp(requestedTarget, minValue, maxValue);
        bool hitEdge = !Mathf.IsEqualApprox((float)requestedTarget, (float)clampedTarget);

        _pileOverlaySmoothScrollTarget = clampedTarget;
        _pileOverlaySmoothScrollActive =
            Math.Abs(_pileOverlaySmoothScrollTarget - _pileOverlaySmoothScrollPosition)
                > PileOverlaySmoothScrollSnapDistance
            || Math.Abs(_pileOverlaySmoothScrollVelocity) > PileOverlaySmoothScrollStopSpeed;
        _pileOverlaySmoothScrollTracePending = _pileOverlaySmoothScrollActive;
        LogPileOverlayLayoutTrace(
            $"scroll-input delta={scrollDelta:F1} current={currentValue:F1} target={_pileOverlaySmoothScrollTarget:F1} requested={requestedTarget:F1} clamped={clampedTarget:F1}"
        );

        if (hitEdge)
            QueuePileOverlayScrollBounceCheck(visualDirection, bounceStrength);

        UpdateProcessState();
        return true;
    }

    private void UpdatePileOverlaySmoothScroll(float delta)
    {
        if (!_pileOverlaySmoothScrollActive)
            return;

        if (!CanPileOverlayReceiveSmoothScroll())
        {
            CancelPileOverlaySmoothScroll();
            return;
        }

        float frameDelta = Mathf.Clamp(delta, 0f, 0.05f);
        if (frameDelta <= 0f)
            return;

        double minValue = _pileOverlayVScrollBar.MinValue;
        double maxValue = GetPileOverlayScrollMaxValue();
        _pileOverlaySmoothScrollTarget = Math.Clamp(
            _pileOverlaySmoothScrollTarget,
            minValue,
            maxValue
        );

        _pileOverlaySmoothScrollPosition = Math.Clamp(
            _pileOverlaySmoothScrollPosition,
            minValue,
            maxValue
        );

        double distance = _pileOverlaySmoothScrollTarget - _pileOverlaySmoothScrollPosition;
        if (
            Math.Abs(distance) <= PileOverlaySmoothScrollSnapDistance
            && Math.Abs(_pileOverlaySmoothScrollVelocity) <= PileOverlaySmoothScrollStopSpeed
        )
        {
            SetPileOverlayScrollValue(_pileOverlaySmoothScrollTarget);
            CancelPileOverlaySmoothScroll();
            return;
        }

        _pileOverlaySmoothScrollVelocity += distance * PileOverlaySmoothScrollSpring * frameDelta;
        _pileOverlaySmoothScrollVelocity *= Math.Exp(
            -PileOverlaySmoothScrollDamping * frameDelta
        );
        _pileOverlaySmoothScrollVelocity = Math.Clamp(
            _pileOverlaySmoothScrollVelocity,
            -PileOverlaySmoothScrollMaxVelocity,
            PileOverlaySmoothScrollMaxVelocity
        );

        double nextValue =
            _pileOverlaySmoothScrollPosition + _pileOverlaySmoothScrollVelocity * frameDelta;
        if (nextValue <= minValue || nextValue >= maxValue)
        {
            nextValue = Math.Clamp(nextValue, minValue, maxValue);
            _pileOverlaySmoothScrollTarget = Math.Clamp(
                _pileOverlaySmoothScrollTarget,
                minValue,
                maxValue
            );
            _pileOverlaySmoothScrollVelocity = 0d;
        }

        _pileOverlaySmoothScrollPosition = nextValue;
        SetPileOverlayScrollValue(_pileOverlaySmoothScrollPosition);
        if (_pileOverlaySmoothScrollTracePending)
        {
            _pileOverlaySmoothScrollTracePending = false;
            LogPileOverlayLayoutTrace(
                $"smooth-step-first pos={_pileOverlaySmoothScrollPosition:F1} target={_pileOverlaySmoothScrollTarget:F1} velocity={_pileOverlaySmoothScrollVelocity:F1}"
            );
        }
    }

    private bool CanPileOverlayReceiveSmoothScroll()
    {
        return _pileOverlayRoot != null
            && GodotObject.IsInstanceValid(_pileOverlayRoot)
            && _pileOverlayRoot.Visible
            && !_pileOverlayContentTemporarilyHidden
            && _pileOverlayScroll != null
            && GodotObject.IsInstanceValid(_pileOverlayScroll)
            && _pileOverlayScroll.Modulate.A > 0.05f
            && _pileOverlayVScrollBar != null
            && GodotObject.IsInstanceValid(_pileOverlayVScrollBar)
            && GetPileOverlayScrollMaxValue() > _pileOverlayVScrollBar.MinValue + 0.5d;
    }

    private void SetPileOverlayScrollValue(double value)
    {
        if (_pileOverlayScroll == null || !GodotObject.IsInstanceValid(_pileOverlayScroll))
            return;

        _pileOverlayScroll.ScrollVertical = Mathf.RoundToInt((float)value);
    }

    private void CancelPileOverlaySmoothScroll()
    {
        _pileOverlaySmoothScrollActive = false;
        _pileOverlaySmoothScrollTracePending = false;
        _pileOverlaySmoothScrollPosition =
            _pileOverlayVScrollBar != null && GodotObject.IsInstanceValid(_pileOverlayVScrollBar)
                ? _pileOverlayVScrollBar.Value
                : 0d;
        _pileOverlaySmoothScrollTarget = _pileOverlaySmoothScrollPosition;
        _pileOverlaySmoothScrollVelocity = 0d;
    }

    private void QueuePileOverlayScrollBounceCheck(float visualDirection, float strength = 1f)
    {
        if (!CanPileOverlayReceiveScrollBounce())
            return;

        _pileOverlayPendingBounceDirection = visualDirection >= 0f ? 1f : -1f;
        _pileOverlayPendingBounceStrength = Math.Max(
            _pileOverlayPendingBounceStrength,
            Math.Max(0.5f, strength)
        );

        if (_pileOverlayScrollBounceCheckPending)
            return;

        _pileOverlayScrollBounceCheckPending = true;
        CallDeferred(MethodName.DeferredApplyPileOverlayScrollBounce);
    }

    private void DeferredApplyPileOverlayScrollBounce()
    {
        _pileOverlayScrollBounceCheckPending = false;
        float visualDirection = _pileOverlayPendingBounceDirection;
        float strength = _pileOverlayPendingBounceStrength;
        _pileOverlayPendingBounceDirection = 0f;
        _pileOverlayPendingBounceStrength = 0f;

        if (
            visualDirection == 0f
            || !CanPileOverlayReceiveScrollBounce()
            || !IsPileOverlayAtScrollEdge(visualDirection)
        )
        {
            return;
        }

        PlayPileOverlayScrollBounce(visualDirection, strength);
    }

    private bool CanPileOverlayReceiveScrollBounce()
    {
        return _pileOverlayRoot != null
            && GodotObject.IsInstanceValid(_pileOverlayRoot)
            && _pileOverlayRoot.Visible
            && !_pileOverlayContentTemporarilyHidden
            && _pileOverlayScroll != null
            && GodotObject.IsInstanceValid(_pileOverlayScroll)
            && _pileOverlayScroll.Modulate.A > 0.05f
            && _pileOverlayVScrollBar != null
            && GodotObject.IsInstanceValid(_pileOverlayVScrollBar)
            && GetPileOverlayScrollMaxValue() > _pileOverlayVScrollBar.MinValue + 0.5d;
    }

    private bool IsPileOverlayAtScrollEdge(float visualDirection)
    {
        if (_pileOverlayVScrollBar == null || !GodotObject.IsInstanceValid(_pileOverlayVScrollBar))
            return false;

        double value = _pileOverlayVScrollBar.Value;
        double minValue = _pileOverlayVScrollBar.MinValue;
        double maxValue = GetPileOverlayScrollMaxValue();
        return visualDirection > 0f
            ? value <= minValue + 0.5d
            : value >= maxValue - 0.5d;
    }

    private double GetPileOverlayScrollMaxValue()
    {
        if (_pileOverlayVScrollBar == null || !GodotObject.IsInstanceValid(_pileOverlayVScrollBar))
            return 0d;

        return Math.Max(
            _pileOverlayVScrollBar.MinValue,
            _pileOverlayVScrollBar.MaxValue - _pileOverlayVScrollBar.Page
        );
    }

    private void PlayPileOverlayScrollBounce(float visualDirection, float strength)
    {
        if (_pileOverlayScroll == null || !GodotObject.IsInstanceValid(_pileOverlayScroll))
            return;

        float direction = visualDirection >= 0f ? 1f : -1f;
        float currentOffset = _pileOverlayScroll.OffsetTop - _pileOverlayScrollBaseOffsetTop;
        if (Math.Sign(currentOffset) != Math.Sign(direction))
            currentOffset = 0f;

        float targetOffset = Mathf.Clamp(
            currentOffset
                + direction * PileOverlayScrollBounceStep * Mathf.Clamp(strength, 0.5f, 2f),
            -PileOverlayScrollBounceMaxOffset,
            PileOverlayScrollBounceMaxOffset
        );

        _pileOverlayScrollBounceTween?.Kill();
        _pileOverlayScrollBounceTween = _pileOverlayScroll.CreateTween();
        _pileOverlayScrollBounceTween.SetParallel(false);
        _pileOverlayScrollBounceTween
            .TweenProperty(
                _pileOverlayScroll,
                "offset_top",
                _pileOverlayScrollBaseOffsetTop + targetOffset,
                PileOverlayScrollBounceOutDuration
            )
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.Out);
        _pileOverlayScrollBounceTween
            .TweenProperty(
                _pileOverlayScroll,
                "offset_top",
                _pileOverlayScrollBaseOffsetTop,
                PileOverlayScrollBounceBackDuration
            )
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.Out);
        _pileOverlayScrollBounceTween.TweenCallback(
            Callable.From(() => _pileOverlayScrollBounceTween = null)
        );
    }

    private void CancelPileOverlayScrollBounce(bool resetOffsetTop)
    {
        _pileOverlayScrollBounceCheckPending = false;
        _pileOverlayPendingBounceDirection = 0f;
        _pileOverlayPendingBounceStrength = 0f;
        _pileOverlayScrollBounceTween?.Kill();
        _pileOverlayScrollBounceTween = null;

        if (
            resetOffsetTop
            && _pileOverlayScroll != null
            && GodotObject.IsInstanceValid(_pileOverlayScroll)
        )
        {
            _pileOverlayScroll.OffsetTop = _pileOverlayScrollBaseOffsetTop;
        }
    }

    private void HidePileOverlay()
    {
        if (_isPileCardSelectionActive)
            CancelPileCardSelection();

        _pileOverlayContentTemporarilyHidden = false;
        int hideVersion = ++_pileOverlayBuildVersion;
        if (_pileOverlayRoot == null || !GodotObject.IsInstanceValid(_pileOverlayRoot))
            return;

        _pileOverlayRoot.MouseFilter = MouseFilterEnum.Ignore;
        SetPileOverlayContentInputEnabled(false);
        SyncPileOverlaySelectionButtons();
        PlayPileOverlayOutroAnimation(
            hideVersion,
            () =>
            {
                if (
                    hideVersion != _pileOverlayBuildVersion
                    || _pileOverlayRoot == null
                    || !GodotObject.IsInstanceValid(_pileOverlayRoot)
                )
                {
                    return;
                }

                _pileOverlayRoot.Visible = false;
                ResetPileOverlayScroll();
                ResetPileOverlayPresentation();
                ClearPileOverlayCards();
                RequestTurnUiRefresh(refreshHover: true);
            }
        );
    }


}
