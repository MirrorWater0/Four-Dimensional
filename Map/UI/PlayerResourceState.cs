using System;
using System.Collections.Generic;
using Godot;

public partial class PlayerResourceState : CanvasLayer
{
    private static readonly PackedScene MenuScene = GD.Load<PackedScene>("res://Menu/Menu.tscn");

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
    public Control TransitionEnergyControl => field ??= GetNode<Control>("TransitionEnergyControl");
    private HBoxContainer TransitionEnergySlots =>
        field ??= TransitionEnergyControl.GetNode<HBoxContainer>("HBoxContainer");
    public ColorRect ElectricityCoinIcon => field ??= GetNode<ColorRect>("ElectricityCoin");
    static PackedScene TransitionEnergySlot = GD.Load<PackedScene>(
        "res://Map/UI/TransitionEnergySlot.tscn"
    );
    public List<Relic> RelicList = new();
    public VBoxContainer RelicContainer => field ??= GetNode<VBoxContainer>("RelicContainer");
    public List<ConsumeItem> Items = new();
    public HBoxContainer ItemContainer => field ??= GetNode<HBoxContainer>("ItemsHContainer");
    private Button MenuButton => field ??= GetNodeOrNull<Button>("MenuButton");
    private CanvasLayer FrontUiLayer => field ??= GetNodeOrNull<CanvasLayer>("/root/Map/BattleReadyLayer");
    private Menu MenuOverlay => field ??= FrontUiLayer?.GetNodeOrNull<Menu>("Menu");
    private DebugConsole DebugConsoleNode =>
        field ??= GetNodeOrNull<DebugConsole>("/root/Map/DebugConsole");

    public override void _Ready()
    {
        EnsureMenuOverlay();
        ElectricityCoin = GameInfo.ElectricityCoin;
        InitTransitionEnergyMax();
        TransitionEnergy = GameInfo.TransitionEnergy;
        InitRelic();
        InitItems();
        if (MenuButton != null)
            MenuButton.Pressed += OnMenuButtonPressed;
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
        if (ItemContainer == null)
            return;

        for (int i = 0; i < ItemContainer.GetChildCount(); i++)
        {
            if (ItemContainer.GetChild(i) is ItemContainer container)
                container.SetEnabled(enabled);
        }
    }

    public void InitTransitionEnergyMax()
    {
        while (TransitionEnergySlots.GetChildCount() > 0)
        {
            var child = TransitionEnergySlots.GetChild(0);
            TransitionEnergySlots.RemoveChild(child);
            child.QueueFree();
        }

        for (int i = 0; i < GameInfo.TransitionEnergyMax; i++)
        {
            TransitionEnergySlots.AddChild(TransitionEnergySlot.Instantiate());
        }
    }

    private void RefreshTransitionEnergyUI(int value)
    {
        var slots = TransitionEnergySlots;
        for (int i = 0; i < slots.GetChildCount(); i++)
        {
            if (slots.GetChild(i) is ProgressBar slot)
                slot.Value = i < value ? 100 : 0;
        }
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
    }

    private void OnMenuButtonPressed()
    {
        MenuOverlay?.Toggle();
    }

    private void EnsureMenuOverlay()
    {
        if (FrontUiLayer == null || MenuOverlay != null)
            return;

        var menu = MenuScene?.Instantiate<Menu>();
        if (menu == null)
            return;

        menu.Name = "Menu";
        FrontUiLayer.AddChild(menu);
    }

    private void ClearRelicIcons()
    {
        RelicList.Clear();
        if (RelicContainer == null)
            return;

        for (int i = RelicContainer.GetChildCount() - 1; i >= 0; i--)
        {
            Node child = RelicContainer.GetChild(i);
            if (child is Label)
                continue;
            RelicContainer.RemoveChild(child);
            child.QueueFree();
        }
    }

    private void ClearItemIcons()
    {
        Items.Clear();
        if (ItemContainer == null)
            return;

        for (int i = 0; i < ItemContainer.GetChildCount(); i++)
        {
            if (ItemContainer.GetChild(i) is not ItemContainer container)
                continue;

            for (int j = container.GetChildCount() - 1; j >= 0; j--)
            {
                Node child = container.GetChild(j);
                container.RemoveChild(child);
                child.QueueFree();
            }
        }
    }
}
