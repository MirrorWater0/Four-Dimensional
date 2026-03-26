using System;
using System.Collections.Generic;
using Godot;

public partial class PlayerResourceState : CanvasLayer
{
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

    public override void _Ready()
    {
        ElectricityCoin = GameInfo.ElectricityCoin;
        InitTransitionEnergyMax();
        TransitionEnergy = GameInfo.TransitionEnergy;
        InitRelic();
        InitItems();
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
}
