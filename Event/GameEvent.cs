using System;
using System.Collections.Generic;
using System.ComponentModel.Design.Serialization;
using System.IO;
using System.Runtime.CompilerServices;
using Godot;

public partial class GameEvent
{
    public string EventName { get; private set; }
    public string Text { get; private set; }
    public EventOption[] Options { get; private set; }
    public static readonly GameEvent[] Catalog =
    [
        new GameEvent()
        {
            EventName = "能量场",
            Text =
                "你踏入一片不断脉动的蓝白能量场。\n"
                + "终端警告显示：该区域会在短时间内重写体征参数。\n"
                + "你可以让系统随机校准一名队员，也可以手动指定目标。",
            Options =
            [
                new EventOption()
                {
                    Text = "启动随机校准",
                    PropertyChange = new Dictionary<PropertyType, int>()
                    {
                        { PropertyType.Power, 2 },
                        { PropertyType.Survivability, -2 },
                    },
                    RandomChange = true,
                    Exit = true,
                },
                new EventOption()
                {
                    Text = "手动指定校准对象",
                    PropertyChange = new Dictionary<PropertyType, int>()
                    {
                        { PropertyType.Power, -2 },
                        { PropertyType.Survivability, 2 },
                    },
                    RandomChange = false,
                    Exit = true,
                },
            ],
        },
    ];
}

public class EventOption
{
    public Dictionary<PropertyType, int> PropertyChange;
    public bool RandomChange = false;
    public bool Exit;
    public List<Equipment> EquipmentReward;
    public int TransitionEnergyChange;
    public int ElectricityChange;
    public string Text;
}
