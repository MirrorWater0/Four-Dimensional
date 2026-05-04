using System;
using System.Collections.Generic;
using Godot;

public partial class PlayerResourceState : CanvasLayer
{
    private static readonly PackedScene MenuScene = GD.Load<PackedScene>("res://Menu/Menu.tscn");
    private const long MaxDisplayHours = 99;
    private const float TransitionEnergyShakeOffset = 7f;
    private const float TransitionEnergyShakeDuration = 0.07f;
    private const float TransitionEnergyFlashDuration = 0.3f;
    private static readonly Color TransitionEnergyGainColor = new(0.52f, 1f, 0.64f, 1f);
    private static readonly Color TransitionEnergyLossColor = new(1f, 0.46f, 0.46f, 1f);
    private static readonly Color TransitionEnergyBaseColor = Colors.White;
    private static readonly Color TransitionEnergyBaseBorderColor = new(
        0.8983909f,
        0.89839107f,
        0.8983907f,
        1f
    );
    private static readonly Color TransitionEnergyBaseFillColor = new(
        0.5310346f,
        0.76356995f,
        0.8728643f,
        0.9019608f
    );
    private static readonly Color TransitionEnergyBaseGuideColor = new(0.78f, 0.82f, 0.86f, 0.28f);

    private sealed class TransitionEnergySlotState
    {
        public Tween Tween;
        public StyleBoxFlat BackgroundStyle;
        public StyleBoxFlat FillStyle;
        public ColorRect TopGuide;
        public ColorRect BottomGuide;
        public ColorRect RightLink;
    }

    public int ElectricityCoin
    {
        get => GameInfo.ElectricityCoin;
        set
        {
            CreateTween()
                .TweenMethod(
                    Callable.From<float>(value =>
                        ElectricityCoinIcon.GetChild<Label>(0).Text = value.ToString()
                    ),
                    GameInfo.ElectricityCoin,
                    value,
                    0.4f
                );
            GameInfo.ElectricityCoin = value;
        }
    }
    public int TransitionEnergy
    {
        get => GameInfo.TransitionEnergy;
        set
        {
            value = Math.Clamp(value, 0, GameInfo.TransitionEnergyMax);
            GameInfo.TransitionEnergy = value;
            RefreshTransitionEnergyUI(value);
        }
    }

    public int TransistionEnergyMax => GameInfo.TransitionEnergyMax;
    public Control TransitionEnergyControl => field is not null
        && GodotObject.IsInstanceValid(field)
        ? field
        : field = GetNodeOrNull<Control>("TransitionEnergyControl");
    private HBoxContainer TransitionEnergySlots =>
        field is not null && GodotObject.IsInstanceValid(field)
            ? field
            : field = TransitionEnergyControl?.GetNodeOrNull<HBoxContainer>("HBoxContainer");
    public ColorRect ElectricityCoinIcon => field is not null
        && GodotObject.IsInstanceValid(field)
        ? field
        : field = GetNodeOrNull<ColorRect>("ElectricityCoin");
    static PackedScene TransitionEnergySlot = GD.Load<PackedScene>(
        "res://Map/UI/TransitionEnergySlot.tscn"
    );
    private readonly Dictionary<ProgressBar, TransitionEnergySlotState> _transitionEnergySlotStates =
        new();
    private int _lastTransitionEnergy = -1;
    public List<Relic> RelicList = new();
    public VBoxContainer RelicContainer => field is not null
        && GodotObject.IsInstanceValid(field)
        ? field
        : field = GetNodeOrNull<VBoxContainer>("RelicContainer");
    public List<ConsumeItem> Items = new();
    public HBoxContainer ItemContainer => field is not null
        && GodotObject.IsInstanceValid(field)
        ? field
        : field = GetNodeOrNull<HBoxContainer>("ItemsHContainer");
    private Button MenuButton => field is not null
        && GodotObject.IsInstanceValid(field)
        ? field
        : field = GetNodeOrNull<Button>("MenuButton");
    private Label TimerValueLabel => field is not null
        && GodotObject.IsInstanceValid(field)
        ? field
        : field = GetNodeOrNull<Label>("StatusPanel/StatusMargin/StatusRow/TimerBlock/Value");
    private Timer SessionTimer => field is not null
        && GodotObject.IsInstanceValid(field)
        ? field
        : field = GetNodeOrNull<Timer>("SessionTimer");
    private CanvasLayer MenuLayer => field is not null
        && GodotObject.IsInstanceValid(field)
        ? field
        : field = GetNodeOrNull<CanvasLayer>("/root/Map/MenuLayer");
    private CanvasLayer FrontUiLayer => field is not null
        && GodotObject.IsInstanceValid(field)
        ? field
        : field = GetNodeOrNull<CanvasLayer>("/root/Map/BattleReadyLayer");
    private Menu MenuOverlay => field is not null && GodotObject.IsInstanceValid(field)
        ? field
        : field = MenuLayer?.GetNodeOrNull<Menu>("Menu");
    private DebugConsole DebugConsoleNode =>
        field is not null && GodotObject.IsInstanceValid(field)
            ? field
            : field = GetNodeOrNull<DebugConsole>("/root/Map/DebugConsole");

    public override void _Ready()
    {
        EnsureMenuOverlay();
        ElectricityCoin = GameInfo.ElectricityCoin;
        InitTransitionEnergyMax();
        TransitionEnergy = GameInfo.TransitionEnergy;
        InitRelic();
        InitItems();
        RefreshStatusPanel();
        if (MenuButton != null)
            MenuButton.Pressed += OnMenuButtonPressed;
        if (SessionTimer != null)
        {
            SessionTimer.Timeout += OnSessionTimerTimeout;
            SessionTimer.Start();
        }
    }

    public void InitRelic()
    {
        foreach (var relic in GameInfo.Relics)
        {
            Relic relicInst = Relic.Create(relic.Key);
            relicInst.Num = relic.Value;
            relicInst.IconAdd(this);
            RelicList.Add(relicInst);
        }
    }

    public void InitItems()
    {
        Items.Clear();
        if (GameInfo.Items == null || GameInfo.Items.Count == 0)
            return;

        foreach (var item in GameInfo.Items)
            ConsumeItem.AddItem(this, item, syncGameInfo: false);
    }

    public void SetItemsEnabled(bool enabled)
    {
        var itemContainer = ItemContainer;
        if (
            itemContainer == null
            || !GodotObject.IsInstanceValid(itemContainer)
            || !itemContainer.IsInsideTree()
        )
            return;

        for (int i = itemContainer.GetChildCount() - 1; i >= 0; i--)
        {
            if (itemContainer.GetChild(i) is ItemContainer container)
                container.SetEnabled(enabled);
        }
    }

    public void InitTransitionEnergyMax()
    {
        var transitionEnergySlots = TransitionEnergySlots;
        if (
            transitionEnergySlots == null
            || !GodotObject.IsInstanceValid(transitionEnergySlots)
            || !transitionEnergySlots.IsInsideTree()
        )
            return;

        while (transitionEnergySlots.GetChildCount() > 0)
        {
            var child = transitionEnergySlots.GetChild(0);
            transitionEnergySlots.RemoveChild(child);
            child.QueueFree();
        }

        _transitionEnergySlotStates.Clear();

        for (int i = 0; i < GameInfo.TransitionEnergyMax; i++)
        {
            var slot = TransitionEnergySlot.Instantiate<ProgressBar>();
            slot.SelfModulate = TransitionEnergyBaseColor;
            transitionEnergySlots.AddChild(slot);
            _transitionEnergySlotStates[slot] = BuildTransitionEnergySlotState(slot);
        }

        _lastTransitionEnergy = -1;
    }

    private void RefreshTransitionEnergyUI(int value)
    {
        var slots = TransitionEnergySlots;
        if (slots == null || !GodotObject.IsInstanceValid(slots) || !slots.IsInsideTree())
            return;

        int previousValue = _lastTransitionEnergy;
        for (int i = 0; i < slots.GetChildCount(); i++)
        {
            if (slots.GetChild(i) is ProgressBar slot)
                slot.Value = i < value ? 100 : 0;
        }

        if (previousValue >= 0 && previousValue != value)
            AnimateTransitionEnergyChange(slots, previousValue, value);

        _lastTransitionEnergy = value;
    }

    public void RefreshDebugView()
    {
        ElectricityCoinIcon.GetChild<Label>(0).Text = GameInfo.ElectricityCoin.ToString();
        InitTransitionEnergyMax();
        TransitionEnergy = GameInfo.TransitionEnergy;
        ClearRelicIcons();
        InitRelic();
        ClearItemIcons();
        InitItems();
        RefreshStatusPanel();
    }

    private void OnMenuButtonPressed()
    {
        MenuOverlay?.Toggle();
    }

    private void EnsureMenuOverlay()
    {
        if (MenuLayer == null || MenuOverlay != null)
            return;

        var menu = MenuScene?.Instantiate<Menu>();
        if (menu == null)
            return;

        menu.Name = "Menu";
        MenuLayer.AddChild(menu);
    }

    private void ClearRelicIcons()
    {
        RelicList.Clear();
        var relicContainer = RelicContainer;
        if (
            relicContainer == null
            || !GodotObject.IsInstanceValid(relicContainer)
            || !relicContainer.IsInsideTree()
        )
            return;

        for (int i = relicContainer.GetChildCount() - 1; i >= 0; i--)
        {
            Node child = relicContainer.GetChild(i);
            if (child is Label)
                continue;
            relicContainer.RemoveChild(child);
            child.QueueFree();
        }
    }

    private void ClearItemIcons()
    {
        Items.Clear();
        var itemContainer = ItemContainer;
        if (
            itemContainer == null
            || !GodotObject.IsInstanceValid(itemContainer)
            || !itemContainer.IsInsideTree()
        )
            return;

        for (int i = 0; i < itemContainer.GetChildCount(); i++)
        {
            if (itemContainer.GetChild(i) is not ItemContainer container)
                continue;

            container.ClearItemIcon();
        }
    }

    private void OnSessionTimerTimeout()
    {
        GameInfo.SessionPlaySeconds += 1;
        RefreshTimerDisplay();
    }

    private void RefreshStatusPanel()
    {
        RefreshTimerDisplay();
    }

    private void RefreshTimerDisplay()
    {
        if (TimerValueLabel == null)
            return;

        long totalSeconds = Math.Max(0, GameInfo.SessionPlaySeconds);
        long hours = Math.Min(MaxDisplayHours, totalSeconds / 3600);
        long minutes = (totalSeconds % 3600) / 60;
        long seconds = totalSeconds % 60;

        TimerValueLabel.Text = $"{hours:00}:{minutes:00}:{seconds:00}";
        TimerValueLabel.Modulate = new Color(0.92f, 0.96f, 1f, 1f);
    }

    private void AnimateTransitionEnergyChange(HBoxContainer slots, int previousValue, int value)
    {
        int startIndex = Math.Min(previousValue, value);
        int endIndex = Math.Max(previousValue, value);
        bool gained = value > previousValue;
        Color flashColor = gained ? TransitionEnergyGainColor : TransitionEnergyLossColor;

        for (int i = startIndex; i < endIndex; i++)
        {
            if (slots.GetChild(i) is ProgressBar slot)
                AnimateTransitionEnergySlot(slot, flashColor);
        }
    }

    private void AnimateTransitionEnergySlot(ProgressBar slot, Color flashColor)
    {
        if (
            slot == null
            || !GodotObject.IsInstanceValid(slot)
            || !_transitionEnergySlotStates.TryGetValue(slot, out var state)
        )
            return;

        state.Tween?.Kill();
        slot.SelfModulate = flashColor;
        Vector2 basePosition = slot.Position;

        var tween = CreateTween();
        state.Tween = tween;
        tween.SetEase(Tween.EaseType.Out);
        tween.SetTrans(Tween.TransitionType.Cubic);
        tween.SetParallel(true);
        tween.TweenProperty(slot, "self_modulate", TransitionEnergyBaseColor, TransitionEnergyFlashDuration);
        TweenSlotColor(tween, state.BackgroundStyle, "border_color", TransitionEnergyBaseBorderColor, flashColor);
        TweenSlotColor(tween, state.FillStyle, "bg_color", TransitionEnergyBaseFillColor, flashColor);
        TweenSlotColor(tween, state.TopGuide, "color", TransitionEnergyBaseGuideColor, flashColor);
        TweenSlotColor(
            tween,
            state.BottomGuide,
            "color",
            new Color(flashColor.R, flashColor.G, flashColor.B, 0.24f),
            flashColor
        );
        TweenSlotColor(
            tween,
            state.RightLink,
            "color",
            new Color(flashColor.R, flashColor.G, flashColor.B, 0.5f),
            flashColor
        );

        tween.SetParallel(false);
        tween.TweenProperty(
            slot,
            "position",
            basePosition + new Vector2(-TransitionEnergyShakeOffset, 0f),
            TransitionEnergyShakeDuration
        );
        tween.TweenProperty(
            slot,
            "position",
            basePosition + new Vector2(TransitionEnergyShakeOffset, 0f),
            TransitionEnergyShakeDuration
        );
        tween.TweenProperty(
            slot,
            "position",
            basePosition + new Vector2(-TransitionEnergyShakeOffset * 0.55f, 0f),
            TransitionEnergyShakeDuration
        );
        tween.TweenProperty(slot, "position", basePosition, TransitionEnergyShakeDuration);
        tween.Finished += () =>
        {
            if (slot != null && GodotObject.IsInstanceValid(slot))
            {
                slot.Position = basePosition;
                slot.SelfModulate = TransitionEnergyBaseColor;
            }
        };
    }

    private TransitionEnergySlotState BuildTransitionEnergySlotState(ProgressBar slot)
    {
        var state = new TransitionEnergySlotState
        {
            BackgroundStyle = DuplicateSlotStyle(slot, "background"),
            FillStyle = DuplicateSlotStyle(slot, "fill"),
            TopGuide = slot.GetNodeOrNull<ColorRect>("TopGuide"),
            BottomGuide = slot.GetNodeOrNull<ColorRect>("BottomGuide"),
            RightLink = slot.GetNodeOrNull<ColorRect>("RightLink"),
        };

        ResetTransitionEnergySlotVisuals(state);
        return state;
    }

    private static StyleBoxFlat DuplicateSlotStyle(ProgressBar slot, string styleName)
    {
        if (slot?.GetThemeStylebox(styleName) is not StyleBoxFlat style)
            return null;

        var duplicate = style.Duplicate() as StyleBoxFlat;
        if (duplicate == null)
            return null;

        slot.AddThemeStyleboxOverride(styleName, duplicate);
        return duplicate;
    }

    private static void TweenSlotColor(
        Tween tween,
        GodotObject target,
        string property,
        Color baseColor,
        Color flashColor
    )
    {
        if (tween == null || target == null)
            return;

        switch (target)
        {
            case StyleBoxFlat style:
                style.Set(property, flashColor);
                break;
            case CanvasItem item:
                item.Set(property, flashColor);
                break;
        }

        tween.Parallel().TweenProperty(target, property, baseColor, TransitionEnergyFlashDuration);
    }

    private static void ResetTransitionEnergySlotVisuals(TransitionEnergySlotState state)
    {
        if (state == null)
            return;

        if (state.BackgroundStyle != null)
            state.BackgroundStyle.BorderColor = TransitionEnergyBaseBorderColor;
        if (state.FillStyle != null)
            state.FillStyle.BgColor = TransitionEnergyBaseFillColor;
        if (state.TopGuide != null)
            state.TopGuide.Color = TransitionEnergyBaseGuideColor;
        if (state.BottomGuide != null)
            state.BottomGuide.Color = new Color(0.68f, 0.72f, 0.76f, 0.24f);
        if (state.RightLink != null)
            state.RightLink.Color = new Color(0.72f, 0.76f, 0.8f, 0.5f);
    }
}
