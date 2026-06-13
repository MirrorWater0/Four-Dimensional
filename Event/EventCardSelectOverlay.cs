using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public readonly struct EventCardSelection(int playerIndex, SkillID skillId, SkillCard sourceCard)
{
    public int PlayerIndex { get; } = playerIndex;
    public SkillID SkillId { get; } = skillId;
    public SkillCard SourceCard { get; } = sourceCard;
}

public readonly struct EventCardSelectionEntry(
    int playerIndex,
    SkillID skillId,
    int count,
    string characterName,
    string characterKey,
    int power,
    int survivability
)
{
    public int PlayerIndex { get; } = playerIndex;
    public SkillID SkillId { get; } = skillId;
    public int Count { get; } = count;
    public string CharacterName { get; } = characterName;
    public string CharacterKey { get; } = characterKey;
    public int Power { get; } = power;
    public int Survivability { get; } = survivability;
}

public partial class EventCardSelectOverlay : Control
{
    private static readonly PackedScene SkillCardScene = GD.Load<PackedScene>(
        "res://battle/UIScene/Reward/SkillCard.tscn"
    );
    private static readonly Vector2 CardHolderSize = new(164f, 254f);
    private static readonly Vector2 CardScale = new(0.66f, 0.66f);

    public event Action<EventCardSelection> CardSelected;
    public event Action SelectionCanceled;

    private ColorRect _mask;
    private Panel _panel;
    private Label _titleLabel;
    private Label _hintLabel;
    private ScrollContainer _scroll;
    private GridContainer _cardGrid;
    private Button _cancelButton;
    private Tween _activeTween;
    private bool _isAnimating;
    private readonly List<EventCardSelectionEntry> _entries = new();

    public override void _Ready()
    {
        BuildUi();
        Visible = false;
    }

    public void ShowSelection(IEnumerable<EventCardSelectionEntry> entries, string hintText)
    {
        BuildUi();
        _entries.Clear();
        if (entries != null)
            _entries.AddRange(entries);

        _hintLabel.Text = string.IsNullOrWhiteSpace(hintText)
            ? "请选择一张卡牌"
            : hintText;
        _cancelButton.Disabled = false;
        RefreshCards();
        PlayAssembleAnimation();
    }

    public void HideSelection()
    {
        if (!Visible && !_isAnimating)
            return;

        PlayDisassembleAnimation();
    }

    private void BuildUi()
    {
        if (_mask != null && GodotObject.IsInstanceValid(_mask))
            return;

        SetAnchorsPreset(LayoutPreset.FullRect);
        MouseFilter = MouseFilterEnum.Stop;

        _mask = new ColorRect
        {
            Name = "Mask",
            Color = new Color(0f, 0f, 0f, 0.64f),
            MouseFilter = MouseFilterEnum.Stop,
        };
        _mask.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(_mask);

        _panel = new Panel
        {
            Name = "Panel",
            OffsetLeft = 180f,
            OffsetTop = 104f,
            OffsetRight = 1460f,
            OffsetBottom = 760f,
            MouseFilter = MouseFilterEnum.Stop,
        };
        _panel.AddThemeStyleboxOverride("panel", CreatePanelStyle());
        AddChild(_panel);

        _titleLabel = new Label
        {
            Name = "Title",
            Text = "选择卡牌",
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            OffsetLeft = 28f,
            OffsetTop = 18f,
            OffsetRight = 1252f,
            OffsetBottom = 62f,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        _titleLabel.AddThemeFontSizeOverride("font_size", 30);
        _titleLabel.AddThemeColorOverride("font_color", new Color(1f, 0.94f, 0.72f, 1f));
        _titleLabel.AddThemeColorOverride("font_outline_color", new Color(0.01f, 0.02f, 0.04f, 0.95f));
        _titleLabel.AddThemeConstantOverride("outline_size", 4);
        _panel.AddChild(_titleLabel);

        _hintLabel = new Label
        {
            Name = "Hint",
            Text = "请选择一张卡牌",
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            OffsetLeft = 28f,
            OffsetTop = 62f,
            OffsetRight = 1252f,
            OffsetBottom = 98f,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        _hintLabel.AddThemeFontSizeOverride("font_size", 18);
        _hintLabel.AddThemeColorOverride("font_color", new Color(0.76f, 0.86f, 1f, 0.86f));
        _panel.AddChild(_hintLabel);

        _scroll = new ScrollContainer
        {
            Name = "Scroll",
            OffsetLeft = 42f,
            OffsetTop = 116f,
            OffsetRight = 1238f,
            OffsetBottom = 574f,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
            MouseFilter = MouseFilterEnum.Stop,
        };
        _panel.AddChild(_scroll);

        _cardGrid = new GridContainer
        {
            Name = "CardGrid",
            Columns = 6,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        _cardGrid.AddThemeConstantOverride("h_separation", 24);
        _cardGrid.AddThemeConstantOverride("v_separation", 22);
        _scroll.AddChild(_cardGrid);

        _cancelButton = new Button
        {
            Name = "Cancel",
            Text = "取消",
            OffsetLeft = 42f,
            OffsetTop = 594f,
            OffsetRight = 1238f,
            OffsetBottom = 636f,
            FocusMode = FocusModeEnum.None,
        };
        _cancelButton.AddThemeFontSizeOverride("font_size", 20);
        _cancelButton.Pressed += OnCancelPressed;
        _panel.AddChild(_cancelButton);
    }

    private void RefreshCards()
    {
        foreach (Node child in _cardGrid.GetChildren())
            child.QueueFree();

        if (_entries.Count == 0)
        {
            var empty = new Label
            {
                Text = "没有可选择的卡牌",
                CustomMinimumSize = new Vector2(1120f, 120f),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            empty.AddThemeFontSizeOverride("font_size", 24);
            empty.AddThemeColorOverride("font_color", new Color(0.82f, 0.9f, 1f, 0.8f));
            _cardGrid.AddChild(empty);
            return;
        }

        foreach (var entry in _entries)
            _cardGrid.AddChild(CreateCardHolder(entry));
    }

    private Control CreateCardHolder(EventCardSelectionEntry entry)
    {
        var holder = new Control
        {
            CustomMinimumSize = CardHolderSize,
            MouseFilter = MouseFilterEnum.Ignore,
        };

        var card = SkillCardScene.Instantiate<SkillCard>();
        holder.AddChild(card);
        card.PreviewCharacterName = entry.CharacterName;
        card.PreviewCharacterKey = entry.CharacterKey;
        card.ConfigureDisplayScale(CardScale);
        var skill = Skill.GetSkill(entry.SkillId);
        if (skill != null)
        {
            skill.SetPreviewStats(entry.Power, entry.Survivability, 1);
            card.SetSkill(skill);
        }
        card.Button.Pressed += () => OnCardPressed(entry, card);
        card.CallDeferred(nameof(SkillCard.RestoreDisplayState));

        if (entry.Count > 1)
            holder.AddChild(CreateCountBadge(entry.Count));

        return holder;
    }

    private static Label CreateCountBadge(int count)
    {
        var badge = new Label
        {
            Text = $"x{count}",
            OffsetLeft = 104f,
            OffsetTop = 210f,
            OffsetRight = 156f,
            OffsetBottom = 244f,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        badge.AddThemeFontSizeOverride("font_size", 22);
        badge.AddThemeColorOverride("font_color", new Color(1f, 0.94f, 0.45f, 1f));
        badge.AddThemeColorOverride("font_outline_color", new Color(0.01f, 0.02f, 0.04f, 1f));
        badge.AddThemeConstantOverride("outline_size", 4);
        return badge;
    }

    private void OnCardPressed(EventCardSelectionEntry entry, SkillCard card = null)
    {
        if (!Visible || _isAnimating)
            return;

        DisableSelectionInput();
        CardSelected?.Invoke(new EventCardSelection(entry.PlayerIndex, entry.SkillId, card));
    }

    private void OnCancelPressed()
    {
        if (!Visible || _isAnimating)
            return;

        HideSelection();
        SelectionCanceled?.Invoke();
    }

    private void DisableSelectionInput()
    {
        _cancelButton.Disabled = true;
        foreach (Node child in _cardGrid.GetChildren())
        {
            if (child is not Control holder)
                continue;

            foreach (Node nested in holder.GetChildren())
            {
                if (nested is SkillCard card)
                    card.Button.Disabled = true;
            }
        }
    }

    private void PlayAssembleAnimation()
    {
        CancelActiveTween();
        _isAnimating = true;
        Visible = true;
        Modulate = new Color(1f, 1f, 1f, 0f);
        _panel.Position += new Vector2(0f, 20f);
        _panel.Scale = new Vector2(0.98f, 0.98f);

        _activeTween = CreateTween();
        _activeTween.SetParallel(true);
        _activeTween.TweenProperty(this, "modulate:a", 1f, 0.18f);
        _activeTween
            .TweenProperty(_panel, "position", _panel.Position - new Vector2(0f, 20f), 0.24f)
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.Out);
        _activeTween
            .TweenProperty(_panel, "scale", Vector2.One, 0.24f)
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.Out);
        _activeTween.Finished += OnAssembleFinished;
    }

    private void PlayDisassembleAnimation()
    {
        CancelActiveTween();
        if (!Visible)
            return;

        _isAnimating = true;
        _activeTween = CreateTween();
        _activeTween.SetParallel(true);
        _activeTween.TweenProperty(this, "modulate:a", 0f, 0.14f);
        _activeTween
            .TweenProperty(_panel, "position", _panel.Position + new Vector2(0f, 16f), 0.16f)
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.In);
        _activeTween
            .TweenProperty(_panel, "scale", new Vector2(0.98f, 0.98f), 0.16f)
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.In);
        _activeTween.Finished += OnDisassembleFinished;
    }

    private void OnAssembleFinished()
    {
        _activeTween = null;
        _isAnimating = false;
        _panel.Scale = Vector2.One;
    }

    private void OnDisassembleFinished()
    {
        _activeTween = null;
        _isAnimating = false;
        Visible = false;
        Modulate = Colors.White;
        _panel.Scale = Vector2.One;
    }

    private void CancelActiveTween()
    {
        if (_activeTween == null)
            return;

        if (GodotObject.IsInstanceValid(_activeTween))
            _activeTween.Kill();
        _activeTween = null;
        _isAnimating = false;
    }

    private static StyleBoxFlat CreatePanelStyle()
    {
        return new StyleBoxFlat
        {
            BgColor = new Color(0.04f, 0.065f, 0.105f, 0.98f),
            BorderColor = new Color(0.5f, 0.68f, 0.88f, 0.62f),
            BorderWidthLeft = 1,
            BorderWidthTop = 1,
            BorderWidthRight = 1,
            BorderWidthBottom = 1,
            CornerRadiusTopLeft = 8,
            CornerRadiusTopRight = 8,
            CornerRadiusBottomRight = 8,
            CornerRadiusBottomLeft = 8,
            ShadowColor = new Color(0.01f, 0.025f, 0.055f, 0.56f),
            ShadowSize = 24,
            ShadowOffset = new Vector2(0f, 8f),
        };
    }
}
