using Godot;

/// <summary>
/// Scene factories and shared motion parameters for pile card selection overlays.
/// </summary>
public static class CardPileOverlayUi
{
    public static readonly PackedScene OverlayScene = GD.Load<PackedScene>(
        "res://battle/UIScene/BattlePileOverlay.tscn"
    );
    public static readonly PackedScene SkillCardScene = GD.Load<PackedScene>(
        "res://battle/UIScene/Reward/SkillCard.tscn"
    );

    public static readonly Vector2 CardBaseSize = new(240f, 370f);
    public static readonly Vector2 CardScale = new(1f, 1f);
    public static readonly Vector2 CardHolderPadding = new(14f, 18f);
    public static Vector2 CardDisplaySize => CardBaseSize * CardScale;
    public static Vector2 CardHolderSize => CardDisplaySize + CardHolderPadding * 2f;

    public const float ContentWidth = 1412f;
    public const int GridColumns = 5;
    public const int GridHSeparation = 18;
    public const int GridVSeparation = 34;
    public const int SectionSeparation = 48;
    public const float ScrollBottomOffset = 16f;
    public const int ScrollContentBottomMargin = 132;

    public const int MaskZIndex = 0;
    public const int ContentZIndex = 1;
    public const int ActionButtonZIndex = 2;

    public const float MaskMaxAlpha = 0.68f;
    public const float ActionButtonFadeDuration = 0.18f;
    public const float ActionButtonFadeDelay = 0.12f;

    // Keep every pile-style card browser on the same motion profile.
    public const float ContentMoveDuration = 0.18f;
    public const float ContentSlideOffset = 42f;
    public const float ScrollBounceStep = 18f;
    public const float ScrollBounceMaxOffset = 46f;
    public const float ScrollBounceOutDuration = 0.06f;
    public const float ScrollBounceBackDuration = 0.28f;
    public const float SmoothWheelStep = 360f;
    public const float PanGestureMultiplier = 18f;
    public const float SmoothScrollSpring = 210f;
    public const float SmoothScrollDamping = 24f;
    public const float SmoothScrollMaxVelocity = 5200f;
    public const float SmoothScrollSnapDistance = 0.6f;
    public const float SmoothScrollStopSpeed = 12f;

    public const int AnimatedCardCount = 15;
    public const float CardEntryYOffset = 28f;
    public const float CardEntryDuration = 0.26f;
    public const float CardEntryStagger = 0.018f;
    public const float CardEntryBaseDelay = 0.08f;

    public static void ConfigureCancelButton(Button button, string text = "取消")
    {
        if (button == null)
            return;
        button.Text = text;
        button.Visible = false;
    }

    public static void ConfigureConfirmButton(Button button, string text = "确认")
    {
        if (button == null)
            return;
        button.Text = text;
        button.Visible = false;
    }

    public static Control CreateCardHolder(out SkillCard card)
    {
        var holder = GD.Load<PackedScene>("res://battle/UIScene/PileCardHolder.tscn").Instantiate<Control>();
        card = holder.GetNode<SkillCard>("Card");
        holder.Visible = false;
        card.Modulate = Colors.Transparent;
        return holder;
    }

    public static void ApplyPreviewCard(
        SkillCard card,
        SkillID skillId,
        string characterName,
        string characterKey,
        int power,
        int survivability,
        int playerIndex = -1
    )
    {
        Skill skill = Skill.GetSkill(skillId);
        if (skill == null || card == null)
            return;

        skill.SetPreviewStats(power, survivability, 1, playerIndex: playerIndex);
        skill.UpdateDescription();
        card.Name = $"PileCard_{skillId}";
        card.ConfigureDisplayScale(CardScale);
        card.AutoPressEffect = false;
        card.UseDefaultHoverEffect = true;
        card.AutoAdjustDescriptionTextSize = false;
        card.PreviewCharacterName = characterName;
        card.PreviewCharacterKey = characterKey;
        card.Button.ToggleMode = false;
        card.Button.ButtonPressed = false;
        card.Button.Disabled = false;
        card.Button.FocusMode = Control.FocusModeEnum.None;
        card.SetSkill(skill);
        card.CharacterName.Text = characterName ?? string.Empty;
        card.ConfigurePilePreviewVisuals();
    }

    public static void PlayCardEntryAnimation(SkillCard card, int cardIndex)
    {
        if (card == null || !GodotObject.IsInstanceValid(card))
            return;

        Vector2 targetPosition = card.Position;
        if (cardIndex >= AnimatedCardCount)
        {
            card.Position = targetPosition;
            card.Modulate = SkillButton.EnabledModulate;
            card.Scale = Vector2.One;
            return;
        }

        float delay = CardEntryBaseDelay
            + System.Math.Min(cardIndex, AnimatedCardCount - 1) * CardEntryStagger;
        card.Position = targetPosition + new Vector2(0f, CardEntryYOffset);
        Color targetModulate = SkillButton.EnabledModulate;
        card.Modulate = new Color(targetModulate.R, targetModulate.G, targetModulate.B, 0f);
        card.Scale = new Vector2(0.88f, 0.88f);

        Tween tween = card.CreateTween();
        tween.SetParallel(true);
        tween
            .TweenProperty(card, "position", targetPosition, CardEntryDuration)
            .SetDelay(delay)
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.Out);
        tween
            .TweenProperty(card, "modulate", targetModulate, CardEntryDuration)
            .SetDelay(delay)
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.Out);
        tween
            .TweenProperty(card, "scale", Vector2.One, CardEntryDuration)
            .SetDelay(delay)
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.Out);
    }

    public static Label CreateCountBadge(int count)
    {
        var badge = GD.Load<PackedScene>("res://battle/UIScene/PileCountBadge.tscn").Instantiate<Label>();
        badge.Text = $"x{count}";
        return badge;
    }

}
