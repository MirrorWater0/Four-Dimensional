using Godot;

/// <summary>
/// Shared layout and styling constants for the battle pile card selection overlay.
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

    public static void ConfigureSectionLabel(Label label)
    {
        if (label == null)
            return;

        label.CustomMinimumSize = new Vector2(ContentWidth, 0f);
        label.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        label.HorizontalAlignment = HorizontalAlignment.Center;
        label.MouseFilter = Control.MouseFilterEnum.Ignore;
    }

    public static void ConfigureCardGrid(GridContainer grid)
    {
        if (grid == null)
            return;

        grid.CustomMinimumSize = new Vector2(ContentWidth, 0f);
        grid.SizeFlagsHorizontal = Control.SizeFlags.ShrinkBegin;
        grid.SizeFlagsVertical = Control.SizeFlags.ShrinkBegin;
        grid.MouseFilter = Control.MouseFilterEnum.Ignore;
        grid.ClipContents = false;
        grid.Columns = GridColumns;
        grid.AddThemeConstantOverride("h_separation", GridHSeparation);
        grid.AddThemeConstantOverride("v_separation", GridVSeparation);
    }

    public static void ConfigureCardScrollContainer(ScrollContainer scroll)
    {
        if (scroll == null)
            return;

        scroll.ClipContents = false;
        scroll.HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled;
        scroll.VerticalScrollMode = ScrollContainer.ScrollMode.ShowNever;
        scroll.OffsetBottom = ScrollBottomOffset;
    }

    public static void ConfigureScrollContentMargin(MarginContainer margin)
    {
        if (margin == null)
            return;

        margin.AddThemeConstantOverride("margin_bottom", ScrollContentBottomMargin);
    }

    public static void ConfigureOverlayRoot(Control root)
    {
        if (root == null)
            return;

        root.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        root.ClipContents = false;
        root.MouseFilter = Control.MouseFilterEnum.Ignore;
    }

    public static void ConfigureMask(ColorRect mask)
    {
        if (mask == null)
            return;

        mask.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        mask.Color = new Color(0f, 0f, 0f, MaskMaxAlpha);
        mask.MouseFilter = Control.MouseFilterEnum.Stop;
        mask.ZIndex = MaskZIndex;
    }

    public static void ConfigureCancelButton(Button button, string text = "取消")
    {
        if (button == null)
            return;

        button.Text = text;
        button.Visible = false;
        button.CustomMinimumSize = new Vector2(170f, 58f);
        button.SetAnchorsPreset(Control.LayoutPreset.BottomRight);
        button.OffsetLeft = -406f;
        button.OffsetTop = -86f;
        button.OffsetRight = -236f;
        button.OffsetBottom = -28f;
        button.MouseFilter = Control.MouseFilterEnum.Stop;
        button.FocusMode = Control.FocusModeEnum.None;
        button.ZIndex = ActionButtonZIndex;
        button.ZAsRelative = true;
        button.AddThemeFontSizeOverride("font_size", 22);
        button.AddThemeColorOverride("font_color", new Color(0.96f, 0.98f, 1f, 1f));
        button.AddThemeColorOverride("font_hover_color", Colors.White);
        button.AddThemeColorOverride("font_pressed_color", new Color(1f, 0.95f, 0.78f, 1f));
        button.AddThemeColorOverride("font_disabled_color", new Color(0.68f, 0.74f, 0.78f, 0.62f));
        button.AddThemeColorOverride("font_outline_color", new Color(0.02f, 0.05f, 0.08f, 0.9f));
        button.AddThemeConstantOverride("outline_size", 3);
        button.AddThemeStyleboxOverride(
            "normal",
            CreateButtonStyle(new Color(0.07f, 0.16f, 0.22f, 0.9f), new Color(0.35f, 0.82f, 0.92f, 0.88f))
        );
        button.AddThemeStyleboxOverride(
            "hover",
            CreateButtonStyle(new Color(0.10f, 0.24f, 0.30f, 0.95f), new Color(0.65f, 0.94f, 1f, 1f), 3)
        );
        button.AddThemeStyleboxOverride(
            "pressed",
            CreateButtonStyle(new Color(0.04f, 0.11f, 0.16f, 0.98f), new Color(1f, 0.78f, 0.32f, 1f), 3)
        );
        button.AddThemeStyleboxOverride(
            "disabled",
            CreateButtonStyle(new Color(0.05f, 0.08f, 0.1f, 0.58f), new Color(0.25f, 0.36f, 0.42f, 0.62f))
        );
        button.AddThemeStyleboxOverride(
            "focus",
            CreateButtonStyle(new Color(0.10f, 0.24f, 0.30f, 0.95f), new Color(0.65f, 0.94f, 1f, 1f), 3)
        );
    }

    public static void ConfigureConfirmButton(Button button, string text = "确认")
    {
        if (button == null)
            return;

        button.Text = text;
        button.Visible = false;
        button.CustomMinimumSize = new Vector2(170f, 58f);
        button.SetAnchorsPreset(Control.LayoutPreset.BottomRight);
        button.OffsetLeft = -236f;
        button.OffsetTop = -86f;
        button.OffsetRight = -66f;
        button.OffsetBottom = -28f;
        button.MouseFilter = Control.MouseFilterEnum.Stop;
        button.FocusMode = Control.FocusModeEnum.None;
        button.ZIndex = ActionButtonZIndex;
        button.ZAsRelative = true;
        button.AddThemeFontSizeOverride("font_size", 22);
        button.AddThemeColorOverride("font_color", new Color(0.96f, 0.98f, 1f, 1f));
        button.AddThemeColorOverride("font_hover_color", Colors.White);
        button.AddThemeColorOverride("font_pressed_color", new Color(1f, 0.95f, 0.78f, 1f));
        button.AddThemeColorOverride("font_disabled_color", new Color(0.68f, 0.74f, 0.78f, 0.62f));
        button.AddThemeColorOverride("font_outline_color", new Color(0.02f, 0.05f, 0.08f, 0.9f));
        button.AddThemeConstantOverride("outline_size", 3);
        button.AddThemeStyleboxOverride(
            "normal",
            CreateButtonStyle(new Color(0.07f, 0.16f, 0.22f, 0.9f), new Color(0.35f, 0.82f, 0.92f, 0.88f))
        );
        button.AddThemeStyleboxOverride(
            "hover",
            CreateButtonStyle(new Color(0.10f, 0.24f, 0.30f, 0.95f), new Color(0.65f, 0.94f, 1f, 1f), 3)
        );
        button.AddThemeStyleboxOverride(
            "pressed",
            CreateButtonStyle(new Color(0.04f, 0.11f, 0.16f, 0.98f), new Color(1f, 0.78f, 0.32f, 1f), 3)
        );
        button.AddThemeStyleboxOverride(
            "disabled",
            CreateButtonStyle(new Color(0.05f, 0.08f, 0.1f, 0.58f), new Color(0.25f, 0.36f, 0.42f, 0.62f))
        );
        button.AddThemeStyleboxOverride(
            "focus",
            CreateButtonStyle(new Color(0.10f, 0.24f, 0.30f, 0.95f), new Color(0.65f, 0.94f, 1f, 1f), 3)
        );
    }

    public static Control CreateCardHolder(out SkillCard card)
    {
        var holder = new Control
        {
            Name = "PileCardHolder",
            CustomMinimumSize = CardHolderSize,
            Size = CardHolderSize,
            SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter,
            SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            ClipContents = false,
            Visible = false,
        };

        card = SkillCardScene.Instantiate<SkillCard>();
        holder.AddChild(card);
        card.Visible = true;
        card.Modulate = Colors.Transparent;
        card.Scale = Vector2.One;
        card.Position = CardHolderPadding - 0.5f * (Vector2.One - CardScale) * CardBaseSize;
        card.PivotOffset = CardBaseSize * 0.5f;
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
        var badge = new Label
        {
            Text = $"x{count}",
            OffsetLeft = 196f,
            OffsetTop = 318f,
            OffsetRight = 248f,
            OffsetBottom = 352f,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        badge.AddThemeFontSizeOverride("font_size", 22);
        badge.AddThemeColorOverride("font_color", new Color(1f, 0.94f, 0.45f, 1f));
        badge.AddThemeColorOverride("font_outline_color", new Color(0.01f, 0.02f, 0.04f, 1f));
        badge.AddThemeConstantOverride("outline_size", 4);
        return badge;
    }

    private static StyleBoxFlat CreateButtonStyle(Color background, Color border, int borderWidth = 2)
    {
        return new StyleBoxFlat
        {
            BgColor = background,
            BorderColor = border,
            BorderWidthLeft = borderWidth,
            BorderWidthTop = borderWidth,
            BorderWidthRight = borderWidth,
            BorderWidthBottom = borderWidth,
            CornerRadiusTopLeft = 6,
            CornerRadiusTopRight = 6,
            CornerRadiusBottomRight = 6,
            CornerRadiusBottomLeft = 6,
            ContentMarginLeft = 18,
            ContentMarginTop = 12,
            ContentMarginRight = 18,
            ContentMarginBottom = 12,
        };
    }
}
