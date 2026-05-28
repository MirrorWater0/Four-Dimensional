using System;
using System.Collections.Generic;
using Godot;

public partial class PlayerResourceState : CanvasLayer
{
    private static readonly PackedScene MenuScene = GD.Load<PackedScene>("res://Menu/Menu.tscn");
    private const long MaxDisplayHours = 99;
    private const float CoreEnergyShakeOffset = 7f;
    private const float CoreEnergyShakeDuration = 0.07f;
    private const float CoreEnergyFlashDuration = 0.3f;
    private static readonly Color CoreEnergyGainColor = new(0.52f, 1f, 0.64f, 1f);
    private static readonly Color CoreEnergyLossColor = new(1f, 0.46f, 0.46f, 1f);
    private static readonly Color CoreEnergyBaseColor = Colors.White;
    private static readonly Color CoreEnergyBaseBorderColor = new(
        0.46666667f,
        0.85490197f,
        1f,
        0.72f
    );
    private static readonly Color CoreEnergyBaseFillColor = new(0.5310346f, 0.76356995f, 0.8728643f, 0.9f);

    private Tween _coreEnergyTween;
    private StyleBoxFlat _coreEnergyBackgroundStyle;
    private StyleBoxFlat _coreEnergyFillStyle;
    private int _lastTransitionEnergy = -1;
    private readonly List<ResourceVisibilitySnapshot> _mapPeekHiddenResources = new();

    private sealed class ResourceVisibilitySnapshot
    {
        public CanvasItem Item;
        public bool Visible;
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

    public int CoreEnergy
    {
        get => TransitionEnergy;
        set => TransitionEnergy = value;
    }

    public int TransistionEnergyMax => GameInfo.TransitionEnergyMax;
    public int CoreEnergyMax => GameInfo.TransitionEnergyMax;

    public Control TransitionEnergyControl => field is not null
        && GodotObject.IsInstanceValid(field)
        ? field
        : field = GetNodeOrNull<Control>("TransitionEnergyControl");

    private ProgressBar CoreEnergyBar => field is not null
        && GodotObject.IsInstanceValid(field)
        ? field
        : field = TransitionEnergyControl?.GetNodeOrNull<ProgressBar>("CoreEnergyBar");

    private Label CoreEnergyValueLabel => field is not null
        && GodotObject.IsInstanceValid(field)
        ? field
        : field = TransitionEnergyControl?.GetNodeOrNull<Label>("CoreEnergyValue");

    private Label CoreEnergyNameLabel => field is not null
        && GodotObject.IsInstanceValid(field)
        ? field
        : field = TransitionEnergyControl?.GetNodeOrNull<Label>("Label");

    public ColorRect ElectricityCoinIcon => field is not null
        && GodotObject.IsInstanceValid(field)
        ? field
        : field = GetNodeOrNull<ColorRect>("ElectricityCoin");

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
    private Button MapPeekButton => field is not null
        && GodotObject.IsInstanceValid(field)
        ? field
        : field = GetNodeOrNull<Button>("MapPeekButton");
    private ColorRect MapPeekIcon => field is not null
        && GodotObject.IsInstanceValid(field)
        ? field
        : field = MapPeekButton?.GetNodeOrNull<ColorRect>("Icon");
    private Map MapNode => field is not null && GodotObject.IsInstanceValid(field)
        ? field
        : field = GetNodeOrNull<Map>("/root/Map");
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
        if (MapPeekButton != null)
        {
            MapPeekButton.Pressed += OnMapPeekButtonPressed;
            RefreshMapPeekButton();
        }
        if (SessionTimer != null)
        {
            SessionTimer.Timeout += OnSessionTimerTimeout;
            SessionTimer.Start();
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (
            MapNode?.IsMapPeekModeActive != true
            || @event is not InputEventKey keyEvent
            || !keyEvent.Pressed
            || keyEvent.Echo
            || keyEvent.Keycode != Key.Escape
        )
        {
            return;
        }

        GetViewport().SetInputAsHandled();
        CloseMapPeekMode();
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
        var bar = CoreEnergyBar;
        if (bar == null || !GodotObject.IsInstanceValid(bar) || !bar.IsInsideTree())
            return;

        bar.MinValue = 0;
        bar.MaxValue = Math.Max(1, GameInfo.TransitionEnergyMax);
        bar.ShowPercentage = false;
        bar.SelfModulate = CoreEnergyBaseColor;
        _coreEnergyBackgroundStyle = DuplicateBarStyle(bar, "background");
        _coreEnergyFillStyle = DuplicateBarStyle(bar, "fill");
        ResetCoreEnergyBarVisuals();
        if (CoreEnergyNameLabel != null)
            CoreEnergyNameLabel.Text = I18n.Tr("ui.common.core_energy", "核心能源");
        _lastTransitionEnergy = -1;
    }

    private void RefreshTransitionEnergyUI(int value)
    {
        var bar = CoreEnergyBar;
        if (bar == null || !GodotObject.IsInstanceValid(bar) || !bar.IsInsideTree())
            return;

        int previousValue = _lastTransitionEnergy;
        bar.MaxValue = Math.Max(1, GameInfo.TransitionEnergyMax);
        bar.Value = Math.Clamp(value, 0, GameInfo.TransitionEnergyMax);
        if (CoreEnergyValueLabel != null)
            CoreEnergyValueLabel.Text = $"{value}/{GameInfo.TransitionEnergyMax}";

        if (previousValue >= 0 && previousValue != value)
            AnimateCoreEnergyChange(previousValue, value);

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

    private void OnMapPeekButtonPressed()
    {
        var map = MapNode;
        if (map == null || !GodotObject.IsInstanceValid(map))
            return;

        if (map.IsMapPeekModeActive)
            CloseMapPeekMode();
        else
            OpenMapPeekMode();
    }

    private void OpenMapPeekMode()
    {
        MapNode?.EnterMapPeekMode();
        SetResourceChromeVisibleForMapPeek(false);
        RefreshMapPeekButton();
    }

    private void CloseMapPeekMode()
    {
        MapNode?.ExitMapPeekMode();
        SetResourceChromeVisibleForMapPeek(true);
        RefreshMapPeekButton();
    }

    private void RefreshMapPeekButton()
    {
        if (MapPeekButton == null)
            return;

        bool active = MapNode?.IsMapPeekModeActive == true;
        MapPeekButton.Text = string.Empty;
        MapPeekButton.TooltipText = active
            ? I18n.Tr("ui.map.peek_close_tip", "返回当前界面")
            : I18n.Tr("ui.map.peek_open_tip", "查看地图");

        if (MapPeekIcon?.Material is ShaderMaterial material)
            material.SetShaderParameter("active", active ? 1.0f : 0.0f);
    }

    private void SetResourceChromeVisibleForMapPeek(bool visible)
    {
        if (visible)
        {
            RestoreMapPeekResourceVisibility();
            return;
        }

        HideResourceForMapPeek(GetNodeOrNull<CanvasItem>("StatusPanel"));
        HideResourceForMapPeek(TransitionEnergyControl);
        HideResourceForMapPeek(ElectricityCoinIcon);
        HideResourceForMapPeek(RelicContainer);
        HideResourceForMapPeek(ItemContainer);
        HideResourceForMapPeek(MenuButton);
    }

    private void HideResourceForMapPeek(CanvasItem item)
    {
        if (
            item == null
            || !GodotObject.IsInstanceValid(item)
            || item.IsQueuedForDeletion()
            || item == MapPeekButton
            || !item.Visible
        )
        {
            return;
        }

        _mapPeekHiddenResources.Add(
            new ResourceVisibilitySnapshot { Item = item, Visible = item.Visible }
        );
        item.Visible = false;
    }

    private void RestoreMapPeekResourceVisibility()
    {
        for (int i = _mapPeekHiddenResources.Count - 1; i >= 0; i--)
        {
            var snapshot = _mapPeekHiddenResources[i];
            if (
                snapshot?.Item == null
                || !GodotObject.IsInstanceValid(snapshot.Item)
                || snapshot.Item.IsQueuedForDeletion()
            )
            {
                continue;
            }

            snapshot.Item.Visible = snapshot.Visible;
        }

        _mapPeekHiddenResources.Clear();
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

    private void AnimateCoreEnergyChange(int previousValue, int value)
    {
        var bar = CoreEnergyBar;
        if (bar == null || !GodotObject.IsInstanceValid(bar))
            return;

        bool gained = value > previousValue;
        Color flashColor = gained ? CoreEnergyGainColor : CoreEnergyLossColor;

        _coreEnergyTween?.Kill();
        bar.SelfModulate = flashColor;
        Vector2 basePosition = TransitionEnergyControl?.Position ?? bar.Position;
        Node targetPositionNode = TransitionEnergyControl ?? bar;

        var tween = CreateTween();
        _coreEnergyTween = tween;
        tween.SetEase(Tween.EaseType.Out);
        tween.SetTrans(Tween.TransitionType.Cubic);
        tween.SetParallel(true);
        tween.TweenProperty(bar, "self_modulate", CoreEnergyBaseColor, CoreEnergyFlashDuration);
        TweenStyleColor(tween, _coreEnergyBackgroundStyle, "border_color", CoreEnergyBaseBorderColor, flashColor);
        TweenStyleColor(tween, _coreEnergyFillStyle, "bg_color", CoreEnergyBaseFillColor, flashColor);

        tween.SetParallel(false);
        tween.TweenProperty(
            targetPositionNode,
            "position",
            basePosition + new Vector2(-CoreEnergyShakeOffset, 0f),
            CoreEnergyShakeDuration
        );
        tween.TweenProperty(
            targetPositionNode,
            "position",
            basePosition + new Vector2(CoreEnergyShakeOffset, 0f),
            CoreEnergyShakeDuration
        );
        tween.TweenProperty(
            targetPositionNode,
            "position",
            basePosition + new Vector2(-CoreEnergyShakeOffset * 0.55f, 0f),
            CoreEnergyShakeDuration
        );
        tween.TweenProperty(targetPositionNode, "position", basePosition, CoreEnergyShakeDuration);
        tween.Finished += () =>
        {
            if (bar != null && GodotObject.IsInstanceValid(bar))
                bar.SelfModulate = CoreEnergyBaseColor;
            if (targetPositionNode != null && GodotObject.IsInstanceValid(targetPositionNode))
                targetPositionNode.Set("position", basePosition);
            ResetCoreEnergyBarVisuals();
        };
    }

    private static StyleBoxFlat DuplicateBarStyle(ProgressBar bar, string styleName)
    {
        if (bar?.GetThemeStylebox(styleName) is not StyleBoxFlat style)
            return null;

        var duplicate = style.Duplicate() as StyleBoxFlat;
        if (duplicate == null)
            return null;

        bar.AddThemeStyleboxOverride(styleName, duplicate);
        return duplicate;
    }

    private static void TweenStyleColor(
        Tween tween,
        StyleBoxFlat style,
        string property,
        Color baseColor,
        Color flashColor
    )
    {
        if (tween == null || style == null)
            return;

        style.Set(property, flashColor);
        tween.Parallel().TweenProperty(style, property, baseColor, CoreEnergyFlashDuration);
    }

    private void ResetCoreEnergyBarVisuals()
    {
        if (_coreEnergyBackgroundStyle != null)
            _coreEnergyBackgroundStyle.BorderColor = CoreEnergyBaseBorderColor;
        if (_coreEnergyFillStyle != null)
            _coreEnergyFillStyle.BgColor = CoreEnergyBaseFillColor;
    }
}
