using System;
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

    public override void _Ready()
    {
        ElectricityCoin = GameInfo.ElectricityCoin;
        TransitionEnergy = GameInfo.TransitionEnergy;
        InitTransitionEnergyMax();
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
