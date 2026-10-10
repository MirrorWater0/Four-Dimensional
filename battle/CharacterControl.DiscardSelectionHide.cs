using Godot;

public partial class CharacterControl
{
    private Color _discardSelectionScreenMaskColor;
    private const float DiscardSelectionContentFadeDuration = 0.14f;

    private bool _discardSelectionContentTemporarilyHidden;
    private Button _discardSelectionHideButton;
    private Button _discardSelectionHideButtonInputTarget;
    private Tween _discardSelectionFadeTween;

    private void ResetDiscardSelectionTemporaryHideState()
    {
        _discardSelectionFadeTween?.Kill();
        _discardSelectionFadeTween = null;
        _discardSelectionContentTemporarilyHidden = false;
        if (
            _discardSelectionHideButton != null
            && GodotObject.IsInstanceValid(_discardSelectionHideButton)
        )
        {
            _discardSelectionHideButton.Visible = false;
        }
    }

    private void EnsureDiscardSelectionHideButton()
    {
        CanvasLayer overlay = EnsureCardPlayOverlay();
        if (overlay == null || !GodotObject.IsInstanceValid(overlay))
            return;

        _discardSelectionHideButton = overlay.GetNodeOrNull<Button>("DiscardSelectionHideButton");
        if (_discardSelectionHideButtonInputTarget != _discardSelectionHideButton)
        {
            if (
                _discardSelectionHideButtonInputTarget != null
                && GodotObject.IsInstanceValid(_discardSelectionHideButtonInputTarget)
            )
            {
                _discardSelectionHideButtonInputTarget.Pressed -= OnDiscardSelectionHidePressed;
            }

            _discardSelectionHideButton.Pressed += OnDiscardSelectionHidePressed;
            _discardSelectionHideButtonInputTarget = _discardSelectionHideButton;
        }

    }


    private void SyncDiscardSelectionHideButton()
    {
        if (
            _discardSelectionHideButton == null
            || !GodotObject.IsInstanceValid(_discardSelectionHideButton)
        )
        {
            return;
        }

        bool visible = _isDiscardSelectionActive && !_isDiscardSelectionCompleting;
        _discardSelectionHideButton.Visible = visible;
        _discardSelectionHideButton.Disabled = !visible;
        if (!visible)
            return;

        _discardSelectionHideButton.Text = _discardSelectionContentTemporarilyHidden
            ? "返回选牌"
            : "隐藏";
        EnsureDiscardSelectionHideButtonOnTop();
        if (_discardSelectionHideButton.Modulate.A < 0.05f)
            _discardSelectionHideButton.Modulate = Colors.White;
    }

    private void EnsureDiscardSelectionHideButtonOnTop()
    {
        if (
            _discardSelectionHideButton == null
            || !GodotObject.IsInstanceValid(_discardSelectionHideButton)
        )
        {
            return;
        }

        Node parent = _discardSelectionHideButton.GetParent();
        if (parent == null)
            return;

        parent.MoveChild(_discardSelectionHideButton, parent.GetChildCount() - 1);
    }

    private void OnDiscardSelectionHidePressed()
    {
        if (!_isDiscardSelectionActive || _isDiscardSelectionCompleting)
            return;

        SetDiscardSelectionContentTemporarilyHidden(!_discardSelectionContentTemporarilyHidden);
    }

    private void SetDiscardSelectionContentTemporarilyHidden(bool hidden)
    {
        if (
            !_isDiscardSelectionActive
            || _isDiscardSelectionCompleting
            || _discardSelectionContentTemporarilyHidden == hidden
        )
        {
            return;
        }

        _discardSelectionContentTemporarilyHidden = hidden;
        if (hidden)
            PlayDiscardSelectionTemporaryHideAnimation();
        else
            PlayDiscardSelectionTemporaryShowAnimation();

        SyncDiscardSelectionHideButton();
        RequestTurnUiRefresh(refreshHover: hidden);
    }

    private void PlayDiscardSelectionTemporaryHideAnimation()
    {
        if (
            _discardSelectionScreenMask == null
            || !GodotObject.IsInstanceValid(_discardSelectionScreenMask)
        )
        {
            return;
        }

        _discardSelectionFadeTween?.Kill();
        _discardSelectionScreenMask.MouseFilter = MouseFilterEnum.Ignore;
        _discardSelectionFadeTween = _discardSelectionScreenMask.CreateTween();
        _discardSelectionFadeTween
            .TweenProperty(
                _discardSelectionScreenMask,
                "color:a",
                0f,
                DiscardSelectionContentFadeDuration
            )
            .SetTrans(Tween.TransitionType.Sine)
            .SetEase(Tween.EaseType.In);
    }

    private void PlayDiscardSelectionTemporaryShowAnimation()
    {
        if (
            _discardSelectionScreenMask == null
            || !GodotObject.IsInstanceValid(_discardSelectionScreenMask)
        )
        {
            return;
        }

        _discardSelectionFadeTween?.Kill();
        _discardSelectionScreenMask.Visible = true;
        _discardSelectionScreenMask.MouseFilter = MouseFilterEnum.Stop;
        _discardSelectionFadeTween = _discardSelectionScreenMask.CreateTween();
        _discardSelectionFadeTween
            .TweenProperty(
                _discardSelectionScreenMask,
                "color:a",
                _discardSelectionScreenMaskColor.A,
                DiscardSelectionContentFadeDuration
            )
            .SetTrans(Tween.TransitionType.Sine)
            .SetEase(Tween.EaseType.Out);
    }

    private void ApplyDiscardSelectionScreenMaskVisibleState()
    {
        if (
            _discardSelectionScreenMask == null
            || !GodotObject.IsInstanceValid(_discardSelectionScreenMask)
        )
        {
            return;
        }

        _discardSelectionScreenMask.Visible = true;
        if (_discardSelectionContentTemporarilyHidden)
        {
            Color transparent = _discardSelectionScreenMaskColor;
            transparent.A = 0f;
            _discardSelectionScreenMask.Color = transparent;
            _discardSelectionScreenMask.MouseFilter = MouseFilterEnum.Ignore;
            return;
        }

        _discardSelectionScreenMask.Color = _discardSelectionScreenMaskColor;
        _discardSelectionScreenMask.MouseFilter = MouseFilterEnum.Stop;
    }
}
