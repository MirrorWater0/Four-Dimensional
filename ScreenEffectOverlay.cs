using Godot;

/// <summary>
/// The two persistent screen pulse layers authored on the map scene.
/// </summary>
public partial class ScreenEffectOverlay : CanvasLayer
{
    private static ScreenEffectOverlay _activeInstance;
    private const string LogPrefix = "[ScreenEffectOverlay]";

    private NinePatchRect _damageTint;
    private NinePatchRect _healTint;
    private Tween _damageTintTween;
    private Tween _healTintTween;

    private static readonly Color DamageColor = new(1f, 0.16f, 0.12f, 1f);
    private static readonly Color BlockColor = new(0.46f, 0.84f, 1f, 1f);
    private const float HealPulseFadeInDuration = 0.16f;
    private const float HealPulseHoldDuration = 0.10f;
    private const float HealPulseFadeOutDuration = 0.52f;

    public override void _Ready()
    {
        _activeInstance = this;
        Layer = Mathf.Max(Layer, 900);
        _damageTint = GetNodeOrNull<NinePatchRect>("DamageTint");
        _healTint = GetNodeOrNull<NinePatchRect>("HealTint");
        GD.Print(
            $"{LogPrefix} READY path={GetPath()} layer={Layer} "
                + $"damage={DescribeTint(_damageTint)} heal={DescribeTint(_healTint)}"
        );
        ResetPulses();
    }

    public override void _ExitTree()
    {
        GD.Print($"{LogPrefix} EXIT path={GetPath()} activeIsSelf={_activeInstance == this}");
        if (_activeInstance == this)
            _activeInstance = null;
    }

    /// <summary>
    /// Returns the pre-authored map overlay. It is never created dynamically or
    /// discovered through a scene-tree path; the map's overlay registers itself.
    /// </summary>
    public static ScreenEffectOverlay EnsureMounted(Node caller)
    {
        if (IsLiveMapOverlay(_activeInstance))
        {
            GD.Print($"{LogPrefix} RESOLVE active path={_activeInstance.GetPath()}");
            return _activeInstance;
        }

        // Warmup instantiates a Map under PreloadeScene. Its overlay also calls
        // _Ready, but it must not receive feedback intended for the live /root/Map.
        _activeInstance = null;

        if (caller is Map callerMap)
        {
            ScreenEffectOverlay overlay = callerMap.GetNodeOrNull<ScreenEffectOverlay>(
                "ScreenEffectOverlay"
            );
            if (IsLiveMapOverlay(overlay))
                _activeInstance = overlay;
            GD.Print(
                $"{LogPrefix} RESOLVE via Map path={callerMap.GetPath()} "
                    + $"overlay={(IsLiveMapOverlay(overlay) ? overlay.GetPath() : "NULL")}"
            );
            return IsLiveMapOverlay(overlay) ? overlay : null;
        }

        ScreenEffectOverlay liveOverlay = GetLiveMapOverlay();
        if (liveOverlay != null)
        {
            _activeInstance = liveOverlay;
            GD.Print($"{LogPrefix} RESOLVE root map path={liveOverlay.GetPath()}");
            return liveOverlay;
        }

        // Map feedback can be requested while a new run is still on the start
        // screen. There is no visible overlay to animate at that point, so this
        // is an expected no-op rather than an engine error.
        if (caller == null)
            return null;

        GD.PushError("ScreenEffectOverlay: no active Map/ScreenEffectOverlay instance is registered.");
        return null;
    }

    private static ScreenEffectOverlay GetLiveMapOverlay()
    {
        SceneTree tree = Engine.GetMainLoop() as SceneTree;
        Map map = tree?.Root?.GetNodeOrNull<Map>("/root/Map");
        return map?.GetNodeOrNull<ScreenEffectOverlay>("ScreenEffectOverlay");
    }

    private static bool IsLiveMapOverlay(ScreenEffectOverlay overlay)
    {
        return overlay != null
            && GodotObject.IsInstanceValid(overlay)
            && overlay.IsInsideTree()
            && GetLiveMapOverlay() == overlay;
    }

    public static void PlayHit(Node caller, float impact, int actualDamage, int blockedDamage)
    {
        if (actualDamage <= 0 && blockedDamage <= 0)
            return;

        float peakAlpha = Mathf.Clamp(0.34f * impact, 0.34f, 0.78f);
        EnsureMounted(caller)?.PlayDamagePulse(
            actualDamage > 0 ? DamageColor : BlockColor,
            peakAlpha,
            0.02f + impact * 0.025f,
            0.09f + impact * 0.04f
        );
    }

    public static void PlayHeal(Node caller, int amount)
    {
        float peakAlpha = GetHealPeakAlpha(amount);
        GD.Print(
            $"{LogPrefix} HEAL_REQUEST caller={caller?.GetPath()} amount={amount} peak={peakAlpha:F2}"
        );
        ScreenEffectOverlay overlay = EnsureMounted(caller);
        if (overlay == null)
        {
            GD.PushError($"{LogPrefix} HEAL_ABORT no overlay instance.");
            return;
        }

        overlay.PlayHealPulse(peakAlpha);
    }

    /// <summary>
    /// Plays the map heal feedback for a healing action, even when every party
    /// member is already at full life and the numerical result is zero.
    /// </summary>
    public static void PlayMapHeal(int requestedAmount = 0)
    {
        float peakAlpha = GetHealPeakAlpha(requestedAmount);
        GD.Print(
            $"{LogPrefix} MAP_HEAL_ACTION requested={requestedAmount} peak={peakAlpha:F2}"
        );

        ScreenEffectOverlay overlay = EnsureMounted(null);
        if (overlay == null)
        {
            GD.Print($"{LogPrefix} MAP_HEAL_SKIP map scene is not mounted yet.");
            return;
        }

        overlay.PlayHealPulse(peakAlpha);
    }

    public static void PlayMapPartyLifeChange(int delta)
    {
        GD.Print($"{LogPrefix} MAP_LIFE_CHANGE_RECEIVED delta={delta}");
        if (delta == 0)
        {
            GD.Print($"{LogPrefix} MAP_LIFE_SKIP actual healing/damage was zero.");
            return;
        }

        GD.Print($"{LogPrefix} MAP_LIFE_CHANGE delta={delta}");

        ScreenEffectOverlay overlay = EnsureMounted(null);
        if (overlay == null)
        {
            GD.Print($"{LogPrefix} MAP_LIFE_SKIP map scene is not mounted yet.");
            return;
        }

        if (delta > 0)
        {
            PlayMapHeal(delta);
            return;
        }

        overlay.PlayDamagePulse(DamageColor, 0.46f, 0.05f, 0.22f);
    }

    public void ResetPulses()
    {
        _damageTintTween?.Kill();
        _healTintTween?.Kill();
        _damageTintTween = null;
        _healTintTween = null;
        ResetTint(DamageTint);
        ResetTint(HealTint);
    }

    private void PlayDamagePulse(
        Color color,
        float peakAlpha,
        float holdDuration,
        float fadeDuration
    )
    {
        NinePatchRect tint = DamageTint;
        if (tint == null || !GodotObject.IsInstanceValid(tint))
            return;

        PlayPulse(tint, color, peakAlpha, holdDuration, fadeDuration, isHealPulse: false);
    }

    private void PlayHealPulse(float peakAlpha)
    {
        NinePatchRect tint = HealTint;
        if (tint == null || !GodotObject.IsInstanceValid(tint))
        {
            GD.PushError($"{LogPrefix} HEAL_ABORT HealTint is null or invalid.");
            return;
        }

        GD.Print(
            $"{LogPrefix} HEAL_PULSE tint={DescribeTint(tint)} peak={peakAlpha:F2} "
                + $"fadeIn={HealPulseFadeInDuration:F2} hold={HealPulseHoldDuration:F2} "
                + $"fadeOut={HealPulseFadeOutDuration:F2}"
        );
        PlayPulse(
            tint,
            Colors.White,
            peakAlpha,
            HealPulseHoldDuration,
            HealPulseFadeOutDuration,
            isHealPulse: true,
            fadeInDuration: HealPulseFadeInDuration
        );
    }

    private void PlayPulse(
        NinePatchRect tint,
        Color color,
        float peakAlpha,
        float holdDuration,
        float fadeDuration,
        bool isHealPulse,
        float fadeInDuration = 0.045f
    )
    {
        Tween activeTween = isHealPulse ? _healTintTween : _damageTintTween;
        activeTween?.Kill();
        // Scene instances may inherit a hidden state. Always explicitly enable
        // the target tint at the point a damage/heal pulse is actually requested.
        tint.Visible = true;
        tint.SelfModulate = color with { A = 0f };
        GD.Print(
            $"{LogPrefix} PULSE_START kind={(isHealPulse ? "HEAL" : "DAMAGE")} "
                + $"tint={tint.GetPath()} visible={tint.Visible} "
                + $"modulate={tint.SelfModulate} targetAlpha={peakAlpha:F2}"
        );

        activeTween = CreateTween();
        if (isHealPulse)
            _healTintTween = activeTween;
        else
            _damageTintTween = activeTween;

        activeTween
            .TweenProperty(tint, "self_modulate:a", peakAlpha, fadeInDuration)
            .SetTrans(isHealPulse ? Tween.TransitionType.Sine : Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.Out);
        activeTween.TweenInterval(holdDuration);
        activeTween
            .TweenProperty(tint, "self_modulate:a", 0f, fadeDuration)
            .SetTrans(isHealPulse ? Tween.TransitionType.Sine : Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.InOut);

        Tween tween = activeTween;
        activeTween.TweenCallback(
            Callable.From(() =>
            {
                Tween currentTween = isHealPulse ? _healTintTween : _damageTintTween;
                if (currentTween != tween)
                    return;

                if (isHealPulse)
                    _healTintTween = null;
                else
                    _damageTintTween = null;

                GD.Print(
                    $"{LogPrefix} PULSE_END kind={(isHealPulse ? "HEAL" : "DAMAGE")} "
                        + $"visible={tint.Visible} modulate={tint.SelfModulate}"
                );
            })
        );
    }

    private NinePatchRect DamageTint =>
        _damageTint ??= GetNodeOrNull<NinePatchRect>("DamageTint");

    private NinePatchRect HealTint =>
        _healTint ??= GetNodeOrNull<NinePatchRect>("HealTint");

    private static float GetHealPeakAlpha(int amount) =>
        Mathf.Clamp(0.32f + Mathf.Max(amount, 0) / 360f, 0.32f, 0.50f);

    private static void ResetTint(NinePatchRect tint)
    {
        if (tint == null || !GodotObject.IsInstanceValid(tint))
            return;

        tint.SelfModulate = tint.SelfModulate with { A = 0f };
    }

    private static string DescribeTint(NinePatchRect tint)
    {
        if (tint == null)
            return "NULL";
        if (!GodotObject.IsInstanceValid(tint))
            return "INVALID";

        return $"path={tint.GetPath()} visible={tint.Visible} modulate={tint.SelfModulate}";
    }
}
