using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;

public partial class CharacterControl
{
    private readonly Dictionary<Button, Vector2> _pileButtonIconBaseScales = new();

    private void ConfigurePileButton(Button button, string text)
    {
        if (button == null)
            return;

        if (GetPileButtonIcon(button) is { } icon)
            _pileButtonIconBaseScales.TryAdd(button, icon.Scale);
        button.Text = string.Empty;
        button.TooltipText = text;
        EnsurePileButtonCountLabel(button);
        button.MouseEntered += () => AnimatePileButtonHover(button, true);
        button.MouseExited += () => AnimatePileButtonHover(button, false);
        button.FocusEntered += () => AnimatePileButtonHover(button, true);
        button.FocusExited += () => AnimatePileButtonHover(button, false);
        button.ButtonDown += () => SetPileButtonPressedAmount(button, 1f, 0.08f);
        button.ButtonUp += () => SetPileButtonPressedAmount(button, 0f, 0.12f);
        button.VisibilityChanged += () =>
        {
            if (button == null || !GodotObject.IsInstanceValid(button))
                return;

            if (!button.Visible)
                AnimatePileButtonHover(button, false);
        };
        SyncPileButtonVisualState(button);
    }



    private static Label EnsurePileButtonCountLabel(Button button)
    {
        return button?.GetNodeOrNull<Label>(PileButtonCountLabelName);
    }

    private static void SetPileButtonCount(Button button, int count)
    {
        Label label = EnsurePileButtonCountLabel(button);
        if (label == null)
            return;

        label.Text = Math.Max(0, count).ToString();
        Color modulate = button.Disabled
            ? button.GetThemeColor("pile_count_disabled")
            : button.GetThemeColor("pile_count_enabled");
        label.Modulate = modulate;
        ColorRect background = button.GetNodeOrNull<ColorRect>(PileButtonCountBackgroundName);
        if (background != null)
        {
            background.Modulate = modulate;
            background.Visible = true;
        }
        label.Visible = true;
    }



    private void SyncPileButtonVisualState(Button button)
    {
        Control icon = GetPileButtonIcon(button);
        if (icon == null || !GodotObject.IsInstanceValid(icon))
            return;

        icon.Modulate = button.Disabled
            ? button.GetThemeColor("pile_icon_disabled")
            : button.GetThemeColor("pile_icon_enabled");

        if (icon.Material is not ShaderMaterial shader)
        {
            if (button.Disabled)
                icon.Scale = GetPileButtonBaseScale(button);
            return;
        }

        shader.SetShaderParameter("disabled_amount", button.Disabled ? 1f : 0f);
        if (button.Disabled)
        {
            shader.SetShaderParameter("hover_amount", 0f);
            shader.SetShaderParameter("pressed_amount", 0f);
            shader.SetShaderParameter("receive_amount", 0f);
            icon.Scale = GetPileButtonBaseScale(button);
        }
    }

    private void AnimatePileButtonHover(Button button, bool hovered)
    {
        if (button == null || !GodotObject.IsInstanceValid(button))
            return;

        if (button.Disabled)
            hovered = false;

        Tween hoverTween = GetPileButtonHoverTween(button);
        if (hoverTween != null && GodotObject.IsInstanceValid(hoverTween))
            hoverTween.Kill();

        Tween shaderTween = GetPileButtonShaderTween(button);
        if (shaderTween != null && GodotObject.IsInstanceValid(shaderTween))
            shaderTween.Kill();

        Control icon = GetPileButtonIcon(button);
        if (icon == null || !GodotObject.IsInstanceValid(icon))
            return;

        icon.PivotOffset = icon.Size / 2f;
        Vector2 targetScale = GetPileButtonBaseScale(button) * (hovered ? 1.10f : 1f);
        Color targetModulate = hovered
            ? button.GetThemeColor("pile_icon_hover")
            : button.Disabled
                ? button.GetThemeColor("pile_icon_disabled")
                : button.GetThemeColor("pile_icon_enabled");
        Tween newHoverTween = icon.CreateTween();
        newHoverTween.SetParallel(true);
        newHoverTween
            .TweenProperty(icon, "scale", targetScale, hovered ? 0.18f : 0.14f)
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(hovered ? Tween.EaseType.Out : Tween.EaseType.InOut);
        newHoverTween
            .TweenProperty(icon, "modulate", targetModulate, hovered ? 0.18f : 0.14f)
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(hovered ? Tween.EaseType.Out : Tween.EaseType.InOut);
        SetPileButtonHoverTween(button, newHoverTween);

        if (icon.Material is not ShaderMaterial shader)
            return;

        float from = GetShaderParameterFloat(shader, "hover_amount");
        float to = hovered ? 1f : 0f;
        Tween newShaderTween = icon.CreateTween();
        newShaderTween
            .TweenMethod(
                Callable.From<float>(value =>
                {
                    if (
                        GodotObject.IsInstanceValid(icon)
                        && icon.Material is ShaderMaterial liveShader
                    )
                        liveShader.SetShaderParameter("hover_amount", value);
                }),
                from,
                to,
                hovered ? 0.20f : 0.12f
            )
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(hovered ? Tween.EaseType.Out : Tween.EaseType.InOut);
        SetPileButtonShaderTween(button, newShaderTween);
    }

    private void SetPileButtonPressedAmount(Button button, float amount, float duration)
    {
        Control icon = GetPileButtonIcon(button);
        if (icon == null || !GodotObject.IsInstanceValid(icon) || icon.Material is not ShaderMaterial shader)
            return;

        float from = GetShaderParameterFloat(shader, "pressed_amount");
        icon.CreateTween()
            .TweenMethod(
                Callable.From<float>(value =>
                {
                    if (
                        GodotObject.IsInstanceValid(icon)
                        && icon.Material is ShaderMaterial liveShader
                    )
                        liveShader.SetShaderParameter("pressed_amount", value);
                }),
                from,
                amount,
                duration
            )
            .SetTrans(Tween.TransitionType.Sine)
            .SetEase(Tween.EaseType.Out);
    }

    private void PulsePileButtonReceive(Button button)
    {
        if (!ShouldPlayPileButtonReceivePulse(button))
            return;

        Control icon = GetPileButtonIcon(button);
        if (icon == null || !GodotObject.IsInstanceValid(icon))
            return;

        Tween oldScaleTween = GetPileButtonReceiveScaleTween(button);
        if (oldScaleTween != null && GodotObject.IsInstanceValid(oldScaleTween))
            oldScaleTween.Kill();

        Tween oldShaderTween = GetPileButtonReceiveShaderTween(button);
        if (oldShaderTween != null && GodotObject.IsInstanceValid(oldShaderTween))
            oldShaderTween.Kill();

        icon.PivotOffset = icon.Size / 2f;
        Tween scaleTween = icon.CreateTween();
        scaleTween.SetParallel(false);
        scaleTween
            .TweenProperty(icon, "scale", GetPileButtonBaseScale(button) * 1.07f, PileButtonReceivePulseDuration * 0.38f)
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.Out);
        scaleTween
            .TweenProperty(icon, "scale", GetPileButtonRestScale(button), PileButtonReceivePulseDuration * 0.62f)
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.InOut);
        SetPileButtonReceiveScaleTween(button, scaleTween);

        if (icon.Material is not ShaderMaterial shader)
            return;

        Tween shaderTween = icon.CreateTween();
        shaderTween
            .TweenMethod(
                Callable.From<float>(value =>
                {
                    if (
                        GodotObject.IsInstanceValid(icon)
                        && icon.Material is ShaderMaterial liveShader
                    )
                        liveShader.SetShaderParameter("receive_amount", value);
                }),
                GetShaderParameterFloat(shader, "receive_amount"),
                1f,
                PileButtonReceivePulseDuration * 0.42f
            )
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.Out);
        shaderTween
            .TweenMethod(
                Callable.From<float>(value =>
                {
                    if (
                        GodotObject.IsInstanceValid(icon)
                        && icon.Material is ShaderMaterial liveShader
                    )
                        liveShader.SetShaderParameter("receive_amount", value);
                }),
                1f,
                0f,
                PileButtonReceivePulseDuration * 0.58f
            )
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.Out);
        SetPileButtonReceiveShaderTween(button, shaderTween);
    }

    private void PulsePileButtonOpen(Button button)
    {
        Control icon = GetPileButtonIcon(button);
        if (icon == null || !GodotObject.IsInstanceValid(icon))
            return;

        icon.PivotOffset = icon.Size / 2f;
        Tween scaleTween = icon.CreateTween();
        scaleTween
            .TweenProperty(icon, "scale", GetPileButtonBaseScale(button) * 0.94f, 0.045f)
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.Out);
        scaleTween
            .TweenProperty(icon, "scale", GetPileButtonBaseScale(button) * 1.08f, 0.10f)
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.Out);
        scaleTween
            .TweenProperty(icon, "scale", GetPileButtonRestScale(button), 0.13f)
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.Out);

        if (icon.Material is not ShaderMaterial shader)
            return;

        Tween shaderTween = icon.CreateTween();
        shaderTween
            .TweenMethod(
                Callable.From<float>(value => SetPileButtonOpenShaderPulse(icon, value)),
                GetShaderParameterFloat(shader, "pressed_amount"),
                1f,
                0.075f
            )
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.Out);
        shaderTween
            .TweenMethod(
                Callable.From<float>(value => SetPileButtonOpenShaderPulse(icon, value)),
                1f,
                0f,
                0.18f
            )
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.Out);
    }

    private static void SetPileButtonOpenShaderPulse(Control icon, float value)
    {
        if (
            GodotObject.IsInstanceValid(icon)
            && icon.Material is ShaderMaterial liveShader
        )
        {
            liveShader.SetShaderParameter("pressed_amount", value);
            liveShader.SetShaderParameter("receive_amount", value * 0.82f);
        }
    }

    private static float GetShaderParameterFloat(ShaderMaterial shader, string parameterName)
    {
        if (shader == null)
            return 0f;

        Variant value = shader.GetShaderParameter(parameterName);
        return value.VariantType switch
        {
            Variant.Type.Float => (float)value.AsDouble(),
            Variant.Type.Int => value.AsInt64(),
            _ => 0f,
        };
    }

    private static Control GetPileButtonIcon(Button button)
    {
        return button?.GetNodeOrNull<Control>("PileIcon");
    }

    private static Vector2 GetPileButtonVisualCenter(Button button)
    {
        Control icon = GetPileButtonIcon(button);
        if (
            icon != null
            && GodotObject.IsInstanceValid(icon)
            && icon.IsInsideTree()
            && icon.GetGlobalRect().Size.LengthSquared() > 1f
        )
        {
            return icon.GetGlobalRect().GetCenter();
        }

        return button.GetGlobalRect().GetCenter();
    }

    private Button GetPileButtonForKind(BattlePileKind kind)
    {
        return kind switch
        {
            BattlePileKind.Draw => _drawPileButton,
            BattlePileKind.Discard => _discardPileButton,
            BattlePileKind.Exhausted => _exhaustedPileButton,
            _ => null,
        };
    }

    private Vector2? GetPileOverlayCardEntryStartPosition(
        Control holder,
        Button sourceButton
    )
    {
        if (
            holder == null
            || !GodotObject.IsInstanceValid(holder)
            || !holder.IsInsideTree()
            || sourceButton == null
            || !GodotObject.IsInstanceValid(sourceButton)
            || !sourceButton.IsInsideTree()
        )
        {
            return null;
        }

        Vector2 sourceCenter = GetPileButtonVisualCenter(sourceButton);
        Vector2 cardVisualSize = BattleCardBaseSize * PileCardScale;
        Transform2D holderTransform = holder.GetGlobalTransformWithCanvas();
        return holderTransform.AffineInverse() * sourceCenter - cardVisualSize * 0.5f;
    }

    private Tween GetPileButtonHoverTween(Button button)
    {
        if (button == _drawPileButton)
            return _drawPileHoverTween;
        if (button == _discardPileButton)
            return _discardPileHoverTween;
        return _exhaustedPileHoverTween;
    }

    private void SetPileButtonHoverTween(Button button, Tween tween)
    {
        if (button == _drawPileButton)
            _drawPileHoverTween = tween;
        else if (button == _discardPileButton)
            _discardPileHoverTween = tween;
        else if (button == _exhaustedPileButton)
            _exhaustedPileHoverTween = tween;
    }

    private Tween GetPileButtonShaderTween(Button button)
    {
        if (button == _drawPileButton)
            return _drawPileShaderTween;
        if (button == _discardPileButton)
            return _discardPileShaderTween;
        return _exhaustedPileShaderTween;
    }

    private void SetPileButtonShaderTween(Button button, Tween tween)
    {
        if (button == _drawPileButton)
            _drawPileShaderTween = tween;
        else if (button == _discardPileButton)
            _discardPileShaderTween = tween;
        else if (button == _exhaustedPileButton)
            _exhaustedPileShaderTween = tween;
    }

    private bool ShouldPlayPileButtonReceivePulse(Button button)
    {
        if (button == null || !GodotObject.IsInstanceValid(button) || button.Disabled)
            return false;

        ulong now = Time.GetTicksMsec();
        ulong lastPulse = GetPileButtonLastReceivePulseMsec(button);
        if (lastPulse != 0 && now - lastPulse < PileButtonReceivePulseMinIntervalMsec)
            return false;

        SetPileButtonLastReceivePulseMsec(button, now);
        return true;
    }

    private Vector2 GetPileButtonBaseScale(Button button) =>
        _pileButtonIconBaseScales.TryGetValue(button, out Vector2 scale) ? scale : Vector2.One;

    private Vector2 GetPileButtonRestScale(Button button)
    {
        if (
            button != null
            && GodotObject.IsInstanceValid(button)
            && button.IsInsideTree()
            && button.GetGlobalRect().HasPoint(GetGlobalMousePosition())
        )
        {
            return GetPileButtonBaseScale(button) * 1.10f;
        }

        return GetPileButtonBaseScale(button);
    }

    private Tween GetPileButtonReceiveScaleTween(Button button)
    {
        if (button == _drawPileButton)
            return _drawPileReceiveScaleTween;
        if (button == _discardPileButton)
            return _discardPileReceiveScaleTween;
        return _exhaustedPileReceiveScaleTween;
    }

    private void SetPileButtonReceiveScaleTween(Button button, Tween tween)
    {
        if (button == _drawPileButton)
            _drawPileReceiveScaleTween = tween;
        else if (button == _discardPileButton)
            _discardPileReceiveScaleTween = tween;
        else if (button == _exhaustedPileButton)
            _exhaustedPileReceiveScaleTween = tween;
    }

    private Tween GetPileButtonReceiveShaderTween(Button button)
    {
        if (button == _drawPileButton)
            return _drawPileReceiveShaderTween;
        if (button == _discardPileButton)
            return _discardPileReceiveShaderTween;
        return _exhaustedPileReceiveShaderTween;
    }

    private void SetPileButtonReceiveShaderTween(Button button, Tween tween)
    {
        if (button == _drawPileButton)
            _drawPileReceiveShaderTween = tween;
        else if (button == _discardPileButton)
            _discardPileReceiveShaderTween = tween;
        else if (button == _exhaustedPileButton)
            _exhaustedPileReceiveShaderTween = tween;
    }

    private ulong GetPileButtonLastReceivePulseMsec(Button button)
    {
        if (button == _drawPileButton)
            return _drawPileLastReceivePulseMsec;
        if (button == _discardPileButton)
            return _discardPileLastReceivePulseMsec;
        return _exhaustedPileLastReceivePulseMsec;
    }

    private void SetPileButtonLastReceivePulseMsec(Button button, ulong msec)
    {
        if (button == _drawPileButton)
            _drawPileLastReceivePulseMsec = msec;
        else if (button == _discardPileButton)
            _discardPileLastReceivePulseMsec = msec;
        else if (button == _exhaustedPileButton)
            _exhaustedPileLastReceivePulseMsec = msec;
    }


    private void UpdatePileButtons()
    {
        int drawCount = 0;
        int discardCount = 0;
        int exhaustedCount = 0;
        bool canReadPile =
            _activePlayer != null
            && GodotObject.IsInstanceValid(_activePlayer)
            && _activePlayer.State != Character.CharacterState.Dying
            && BattleNode != null
            && GodotObject.IsInstanceValid(BattleNode);
        bool canOpenPile =
            canReadPile
            && (!IsPileLockedByCardResolution() || CanReadPileWhileCardSelectionIsHidden())
            && !IsManualTargetSelectionPending();

        if (canReadPile)
        {
            drawCount = BattleNode.GetDrawBattleCardPile(_activePlayer).Length;
            discardCount = BattleNode.GetDiscardBattleCardPile(_activePlayer).Length;
            exhaustedCount = BattleNode.GetExhaustedBattleCardPile(_activePlayer).Length;
        }

        if (_drawPileButton != null)
        {
            _drawPileButton.Text = string.Empty;
            _drawPileButton.TooltipText = $"抽牌堆 {drawCount}";
            _drawPileButton.Disabled = !canOpenPile;
            SetPileButtonCount(_drawPileButton, drawCount);
            SyncPileButtonVisualState(_drawPileButton);
        }

        if (_discardPileButton != null)
        {
            _discardPileButton.Text = string.Empty;
            _discardPileButton.TooltipText = $"弃牌堆 {discardCount}";
            _discardPileButton.Disabled = !canOpenPile;
            SetPileButtonCount(_discardPileButton, discardCount);
            SyncPileButtonVisualState(_discardPileButton);
        }

        if (_exhaustedPileButton != null)
        {
            _exhaustedPileButton.Text = string.Empty;
            _exhaustedPileButton.TooltipText = $"消耗牌堆 {exhaustedCount}";
            _exhaustedPileButton.Disabled = !canOpenPile;
            SetPileButtonCount(_exhaustedPileButton, exhaustedCount);
            SyncPileButtonVisualState(_exhaustedPileButton);
        }
    }

    private void OnDrawPilePressed()
    {
        PulsePileButtonOpen(_drawPileButton);
        ShowCurrentPlayerPile(BattlePileKind.Draw);
    }

    private void OnDiscardPilePressed()
    {
        PulsePileButtonOpen(_discardPileButton);
        ShowCurrentPlayerPile(BattlePileKind.Discard);
    }

    private void OnExhaustedPilePressed()
    {
        PulsePileButtonOpen(_exhaustedPileButton);
        ShowCurrentPlayerPile(BattlePileKind.Exhausted);
    }

}
