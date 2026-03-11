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
            if (value < GameInfo.TransitionEnergy)
            {
                for (int i = GameInfo.TransitionEnergy; i > value; i--)
                {
                    TransitionEnergyControl
                        .GetNode<HBoxContainer>("HBoxContainer")
                        .GetChild<ProgressBar>(TransistionEnergyMax - i - 1)
                        .Value = 0;
                }
            }
            else
            {
                for (int i = GameInfo.TransitionEnergy; i < value; i++)
                {
                    TransitionEnergyControl
                        .GetNode<HBoxContainer>("HBoxContainer")
                        .GetChild<ProgressBar>(TransistionEnergyMax - i - 1)
                        .Value = 100;
                }
            }
            GameInfo.TransitionEnergy = value;
            if (value > GameInfo.TransitionEnergyMax)
            {
                GD.Print("Energy overflow");
            }
        }
    }

    public int TransistionEnergyMax => GameInfo.TransitionEnergyMax;
    public Control TransitionEnergyControl => field ??= GetNode<Control>("TransitionEnergyControl");
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
        TransitionEnergy = GameInfo.TransitionEnergy;
        InitTransitionEnergyMax();
        InitRelic();
        InitItems();
    }

    public void InitRelic()
    {
        foreach (var relic in GameInfo.Relic)
        {
            Relic relicInst = relic.Key switch
            {
                RelicID.Blessing => new Relic(RelicID.Blessing),
                _ => new Relic(RelicID.curse),
            };
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
        for (int i = 0; i < GameInfo.TransitionEnergyMax; i++)
        {
            TransitionEnergyControl
                .GetNode<HBoxContainer>("HBoxContainer")
                .AddChild(TransitionEnergySlot.Instantiate());
        }
    }
}
