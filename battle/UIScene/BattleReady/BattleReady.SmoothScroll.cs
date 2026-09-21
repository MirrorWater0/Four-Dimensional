using System;
using Godot;

public partial class BattleReady
{
    private ScrollContainer _skillSmoothScrollInputTarget;
    private VScrollBar _skillSmoothScrollBar;
    private Tween _skillScrollBounceTween;
    private float _skillScrollBaseOffsetTop;
    private bool _skillScrollBounceCheckPending;
    private float _skillPendingBounceDirection;
    private float _skillPendingBounceStrength;
    private bool _skillSmoothScrollActive;
    private double _skillSmoothScrollPosition;
    private double _skillSmoothScrollTarget;
    private double _skillSmoothScrollVelocity;

    private void ConfigureSkillSmoothScroll()
    {
        var scroll = SkillContainer;
        if (_skillSmoothScrollInputTarget == scroll)
            return;

        if (
            _skillSmoothScrollInputTarget != null
            && GodotObject.IsInstanceValid(_skillSmoothScrollInputTarget)
        )
        {
            _skillSmoothScrollInputTarget.GuiInput -= OnSkillScrollGuiInput;
        }

        if (_skillSmoothScrollBar != null && GodotObject.IsInstanceValid(_skillSmoothScrollBar))
            _skillSmoothScrollBar.GuiInput -= OnSkillScrollBarGuiInput;

        // The pile overlay helper also changes OffsetBottom and disables clipping for its
        // full-screen layout. This embedded container keeps its authored bounds and clipping,
        // while sharing the same motion profile and input behavior.
        scroll.ClipContents = true;
        scroll.HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled;
        scroll.VerticalScrollMode = ScrollContainer.ScrollMode.ShowNever;
        _skillSmoothScrollInputTarget = scroll;
        _skillSmoothScrollBar = scroll.GetVScrollBar();
        scroll.GuiInput += OnSkillScrollGuiInput;
        if (_skillSmoothScrollBar != null)
            _skillSmoothScrollBar.GuiInput += OnSkillScrollBarGuiInput;

        CancelSkillSmoothScroll();
    }

    private void ResetSkillSmoothScroll()
    {
        CancelSkillSmoothScroll();
        if (SkillContainer != null && GodotObject.IsInstanceValid(SkillContainer))
            SkillContainer.ScrollVertical = 0;
    }

    private void OnSkillScrollGuiInput(InputEvent @event)
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
                QueueSkillSmoothScrollByDelta(
                    scrollDirection * CardPileOverlayUi.SmoothWheelStep * wheelFactor,
                    visualDirection,
                    wheelFactor
                )
            )
            {
                GetViewport().SetInputAsHandled();
            }
            return;
        }

        if (@event is InputEventPanGesture panGesture)
        {
            float scrollDelta = panGesture.Delta.Y * CardPileOverlayUi.PanGestureMultiplier;
            if (Mathf.Abs(scrollDelta) <= 0.01f)
                return;

            float visualDirection = scrollDelta < 0f ? 1f : -1f;
            float bounceStrength = Mathf.Clamp(
                Mathf.Abs(scrollDelta) / CardPileOverlayUi.SmoothWheelStep,
                0.5f,
                1.4f
            );
            if (QueueSkillSmoothScrollByDelta(scrollDelta, visualDirection, bounceStrength))
                GetViewport().SetInputAsHandled();
            return;
        }

        if (MobilePlatform.TryGetTouchScrollDelta(@event, out float touchDelta, 1.15f))
        {
            float visualDirection = touchDelta < 0f ? 1f : -1f;
            float bounceStrength = Mathf.Clamp(
                Mathf.Abs(touchDelta) / CardPileOverlayUi.SmoothWheelStep,
                0.35f,
                1.1f
            );
            if (QueueSkillSmoothScrollByDelta(touchDelta, visualDirection, bounceStrength))
                GetViewport().SetInputAsHandled();
        }
    }

    private void OnSkillScrollBarGuiInput(InputEvent @event)
    {
        if (
            _skillSmoothScrollActive
            && @event is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left }
        )
        {
            CancelSkillSmoothScroll();
        }
    }

    private bool QueueSkillSmoothScrollByDelta(
        float scrollDelta,
        float visualDirection,
        float bounceStrength
    )
    {
        if (!CanReceiveSkillSmoothScroll())
            return false;

        double minValue = _skillSmoothScrollBar.MinValue;
        double maxValue = GetSkillScrollMaxValue();
        double currentValue = _skillSmoothScrollBar.Value;
        if (!_skillSmoothScrollActive)
        {
            _skillSmoothScrollPosition = currentValue;
            _skillSmoothScrollTarget = currentValue;
            _skillSmoothScrollVelocity = 0d;
        }

        double requestedTarget = _skillSmoothScrollTarget + scrollDelta;
        double clampedTarget = Math.Clamp(requestedTarget, minValue, maxValue);
        _skillSmoothScrollTarget = clampedTarget;
        _skillSmoothScrollActive =
            Math.Abs(_skillSmoothScrollTarget - _skillSmoothScrollPosition)
                > CardPileOverlayUi.SmoothScrollSnapDistance
            || Math.Abs(_skillSmoothScrollVelocity) > CardPileOverlayUi.SmoothScrollStopSpeed;

        return true;
    }

    private void UpdateSkillSmoothScroll(float delta)
    {
        if (!_skillSmoothScrollActive)
            return;

        if (!CanReceiveSkillSmoothScroll())
        {
            CancelSkillSmoothScroll();
            return;
        }

        float frameDelta = Mathf.Clamp(delta, 0f, 0.05f);
        if (frameDelta <= 0f)
            return;

        double minValue = _skillSmoothScrollBar.MinValue;
        double maxValue = GetSkillScrollMaxValue();
        _skillSmoothScrollTarget = Math.Clamp(_skillSmoothScrollTarget, minValue, maxValue);
        _skillSmoothScrollPosition = Math.Clamp(_skillSmoothScrollPosition, minValue, maxValue);

        double distance = _skillSmoothScrollTarget - _skillSmoothScrollPosition;
        if (
            Math.Abs(distance) <= CardPileOverlayUi.SmoothScrollSnapDistance
            && Math.Abs(_skillSmoothScrollVelocity) <= CardPileOverlayUi.SmoothScrollStopSpeed
        )
        {
            SetSkillScrollValue(_skillSmoothScrollTarget);
            CancelSkillSmoothScroll();
            return;
        }

        _skillSmoothScrollVelocity +=
            distance * CardPileOverlayUi.SmoothScrollSpring * frameDelta;
        _skillSmoothScrollVelocity *= Math.Exp(-CardPileOverlayUi.SmoothScrollDamping * frameDelta);
        _skillSmoothScrollVelocity = Math.Clamp(
            _skillSmoothScrollVelocity,
            -CardPileOverlayUi.SmoothScrollMaxVelocity,
            CardPileOverlayUi.SmoothScrollMaxVelocity
        );

        double nextValue = _skillSmoothScrollPosition + _skillSmoothScrollVelocity * frameDelta;
        if (nextValue <= minValue || nextValue >= maxValue)
        {
            nextValue = Math.Clamp(nextValue, minValue, maxValue);
            _skillSmoothScrollTarget = Math.Clamp(_skillSmoothScrollTarget, minValue, maxValue);
            _skillSmoothScrollVelocity = 0d;
        }

        _skillSmoothScrollPosition = nextValue;
        SetSkillScrollValue(_skillSmoothScrollPosition);
    }

    private bool CanReceiveSkillSmoothScroll()
    {
        return TacticsModeRoot.Visible
            && SkillContainer.Visible
            && SkillContainer.Modulate.A > 0.05f
            && _skillSmoothScrollBar != null
            && GodotObject.IsInstanceValid(_skillSmoothScrollBar)
            && GetSkillScrollMaxValue() > _skillSmoothScrollBar.MinValue + 0.5d;
    }

    private double GetSkillScrollMaxValue()
    {
        return _skillSmoothScrollBar == null || !GodotObject.IsInstanceValid(_skillSmoothScrollBar)
            ? 0d
            : Math.Max(
                _skillSmoothScrollBar.MinValue,
                _skillSmoothScrollBar.MaxValue - _skillSmoothScrollBar.Page
            );
    }

    private void SetSkillScrollValue(double value)
    {
        if (SkillContainer != null && GodotObject.IsInstanceValid(SkillContainer))
            SkillContainer.ScrollVertical = Mathf.RoundToInt((float)value);
    }

    private void CancelSkillSmoothScroll()
    {
        _skillSmoothScrollActive = false;
        _skillSmoothScrollPosition =
            _skillSmoothScrollBar != null && GodotObject.IsInstanceValid(_skillSmoothScrollBar)
                ? _skillSmoothScrollBar.Value
                : 0d;
        _skillSmoothScrollTarget = _skillSmoothScrollPosition;
        _skillSmoothScrollVelocity = 0d;
    }

    private void QueueSkillScrollBounceCheck(float visualDirection, float strength)
    {
        if (!CanReceiveSkillSmoothScroll())
            return;

        _skillPendingBounceDirection = visualDirection >= 0f ? 1f : -1f;
        _skillPendingBounceStrength = Math.Max(
            _skillPendingBounceStrength,
            Math.Max(0.5f, strength)
        );

        if (_skillScrollBounceCheckPending)
            return;

        _skillScrollBounceCheckPending = true;
        CallDeferred(nameof(DeferredApplySkillScrollBounce));
    }

    private void DeferredApplySkillScrollBounce()
    {
        _skillScrollBounceCheckPending = false;
        float visualDirection = _skillPendingBounceDirection;
        float strength = _skillPendingBounceStrength;
        _skillPendingBounceDirection = 0f;
        _skillPendingBounceStrength = 0f;

        if (
            visualDirection == 0f
            || !CanReceiveSkillSmoothScroll()
            || !IsAtSkillScrollEdge(visualDirection)
        )
        {
            return;
        }

        PlaySkillScrollBounce(visualDirection, strength);
    }

    private bool IsAtSkillScrollEdge(float visualDirection)
    {
        if (_skillSmoothScrollBar == null || !GodotObject.IsInstanceValid(_skillSmoothScrollBar))
            return false;

        double value = _skillSmoothScrollBar.Value;
        double minValue = _skillSmoothScrollBar.MinValue;
        double maxValue = GetSkillScrollMaxValue();
        return visualDirection > 0f
            ? value <= minValue + 0.5d
            : value >= maxValue - 0.5d;
    }

    private void PlaySkillScrollBounce(float visualDirection, float strength)
    {
        if (SkillContainer == null || !GodotObject.IsInstanceValid(SkillContainer))
            return;

        float direction = visualDirection >= 0f ? 1f : -1f;
        float currentOffset = SkillContainer.OffsetTop - _skillScrollBaseOffsetTop;
        if (Math.Sign(currentOffset) != Math.Sign(direction))
            currentOffset = 0f;

        float targetOffset = Mathf.Clamp(
            currentOffset
                + direction
                    * CardPileOverlayUi.ScrollBounceStep
                    * Mathf.Clamp(strength, 0.5f, 2f),
            -CardPileOverlayUi.ScrollBounceMaxOffset,
            CardPileOverlayUi.ScrollBounceMaxOffset
        );

        _skillScrollBounceTween?.Kill();
        _skillScrollBounceTween = SkillContainer.CreateTween();
        _skillScrollBounceTween
            .TweenProperty(
                SkillContainer,
                "offset_top",
                _skillScrollBaseOffsetTop + targetOffset,
                CardPileOverlayUi.ScrollBounceOutDuration
            )
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.Out);
        _skillScrollBounceTween
            .TweenProperty(
                SkillContainer,
                "offset_top",
                _skillScrollBaseOffsetTop,
                CardPileOverlayUi.ScrollBounceBackDuration
            )
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.Out);
        _skillScrollBounceTween.TweenCallback(
            Callable.From(() => _skillScrollBounceTween = null)
        );
    }
}
