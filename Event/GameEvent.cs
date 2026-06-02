using System;
using System.Collections.Generic;

public partial class GameEvent
{
    public string EventName { get; private set; }
    public string Text { get; private set; }
    public EventOption[] Options { get; private set; }

    public static readonly GameEvent[] Catalog =
    [
        Event(
            "能量场",
            "你踏入一片不断脉动的蓝白能量场。\n"
                + "终端警告显示：该区域会在短时间内重写体征参数。\n"
                + "你可以让系统随机校准一名队员，也可以手动指定目标。",
            Option(
                "启动随机校准",
                propertyChange: Stats((PropertyType.Power, 5), (PropertyType.Survivability, -1)),
                randomChange: true
            ),
            Option(
                "手动指定校准对象",
                propertyChange: Stats((PropertyType.Speed, 2))
            ),
            Option("稳定场核并回充", transitionEnergyChangeMin: 15, transitionEnergyChangeMax: 25)
        ),
        Event(
            "轨道黑匣",
            "一具漂浮在舱壁缺口旁的黑匣仍在循环广播事故录音。\n"
                + "加密芯片尚未烧毁，只是供能极不稳定。\n"
                + "你可以读取残存记录，也可以直接拆下还能用的模块。",
            Option(
                "随机解码事故航迹",
                propertyChange: Stats(
                    (PropertyType.Speed, 2),
                    (PropertyType.Power, 1),
                    (PropertyType.MaxLife, -3)
                ),
                randomChange: true
            ),
            Option("拆下完整模块", electricityChangeMin: 70, electricityChangeMax: 100),
            Option("接入残余电容回充", transitionEnergyChangeMin: 15, transitionEnergyChangeMax: 25)
        ),
        Event(
            "观测阵列",
            "高空观测阵列仍缓慢转动，镜面上覆盖着薄薄一层霜。\n"
                + "如果重新校准，它可以为一名队员提供精确轨迹预测。\n"
                + "若直接接管阵列，则有机会把整段星图卖给空间站。",
            Option(
                "为一名角色进行轨迹校准",
                propertyChange: Stats((PropertyType.Power, 1), (PropertyType.Survivability, 2))
            ),
            Option(
                "让系统随机分配观测增益",
                propertyChange: Stats((PropertyType.Survivability, 5), (PropertyType.Speed, -1)),
                randomChange: true
            ),
            Option("打包星图数据出售", electricityChangeMin: 70, electricityChangeMax: 100)
        ),
        Event(
            "废弃军械舱",
            "密封门后是一间被炸开的军械舱，备用挂架上只剩一些可拆解零件。\n"
                + "你没有足够时间全部带走，只能在高风险输出和稳态防护之间做选择。\n"
                + "也可以放弃搜刮，把能拆下来的零件直接换成电力币。",
            Option(
                "为一名角色装配高能模块",
                propertyChange: Stats((PropertyType.Power, 4), (PropertyType.Speed, -1))
            ),
            Option(
                "随机接入军械协议",
                propertyChange: Stats(
                    (PropertyType.Power, 3),
                    (PropertyType.Speed, 1),
                    (PropertyType.Survivability, -1)
                ),
                randomChange: true
            ),
            Option("拆解备用电芯回充", transitionEnergyChangeMin: 15, transitionEnergyChangeMax: 25)
        ),
        Event(
            "低温休眠仓",
            "一排休眠仓仍维持着最低功耗，雾气在玻璃内壁来回聚散。\n"
                + "医疗日志显示，这套设备可以执行一次短时组织修补，但代价并不稳定。\n"
                + "你也可以把备用电池抽走，留作航程中的过渡能量。",
            Option(
                "为一名角色执行修补程序",
                propertyChange: Stats((PropertyType.MaxLife, 12), (PropertyType.Speed, -1))
            ),
            Option(
                "随机唤醒一具维修躯体协助强化",
                propertyChange: Stats((PropertyType.Survivability, 4)),
                randomChange: true
            ),
            Option(
                "抽走休眠仓备用电池",
                transitionEnergyChangeMin: 15,
                transitionEnergyChangeMax: 25
            )
        ),
        Event(
            "引力透镜井",
            "你在舰体残骸中央发现了一口微型引力透镜井。\n"
                + "井口附近的空间被拉伸成细长的光带，靠近者会承受不同程度的体征偏转。\n"
                + "若强行收束透镜，也许还能榨出一笔值钱的实验数据。",
            Option(
                "让一名角色接受引力偏转",
                propertyChange: Stats((PropertyType.Survivability, 4), (PropertyType.MaxLife, -3))
            ),
            Option(
                "随机暴露于焦点中心",
                propertyChange: Stats(
                    (PropertyType.Power, 3),
                    (PropertyType.Speed, 1),
                    (PropertyType.MaxLife, -3)
                ),
                randomChange: true
            ),
            Option("收束透镜并导出实验数据", electricityChangeMin: 70, electricityChangeMax: 100)
        ),
        Event(
            "静默圣所",
            "一间被切断外部通信的独立舱室安静得异常，中央只留下简易祭台和存放柜。\n"
                + "柜体里的防护部件已经失效，墙面上则反复滚动着一段静默训练守则。\n"
                + "你可以拆解柜体，也可以短暂停留，恢复航程节奏。",
            Option(
                "让一名角色遵循守则训练",
                propertyChange: Stats((PropertyType.Survivability, 3))
            ),
            Option(
                "随机读取静默训练记录",
                propertyChange: Stats(
                    (PropertyType.Speed, 1),
                    (PropertyType.Survivability, 3),
                    (PropertyType.Power, -1)
                ),
                randomChange: true
            ),
            Option("遵循守则静坐片刻", transitionEnergyChangeMin: 15, transitionEnergyChangeMax: 25)
        ),
    ];

    private static GameEvent Event(string name, string text, params EventOption[] options)
    {
        return new GameEvent
        {
            EventName = name,
            Text = text,
            Options = options ?? Array.Empty<EventOption>(),
        };
    }

    private static EventOption Option(
        string text,
        Dictionary<PropertyType, int> propertyChange = null,
        bool randomChange = false,
        bool exit = true,
        int transitionEnergyChange = 0,
        int transitionEnergyChangeMin = 0,
        int transitionEnergyChangeMax = 0,
        int electricityChange = 0,
        int electricityChangeMin = 0,
        int electricityChangeMax = 0,
        int propertyChangeElectricityCostMin = EventOption.AutoPropertyChangeElectricityCost,
        int propertyChangeElectricityCostMax = EventOption.AutoPropertyChangeElectricityCost
    )
    {
        return new EventOption
        {
            Text = text,
            PropertyChange = propertyChange,
            RandomChange = randomChange,
            Exit = exit,
            TransitionEnergyChange = transitionEnergyChange,
            TransitionEnergyChangeMin = transitionEnergyChangeMin,
            TransitionEnergyChangeMax = transitionEnergyChangeMax,
            ElectricityChange = electricityChange,
            ElectricityChangeMin = electricityChangeMin,
            ElectricityChangeMax = electricityChangeMax,
            PropertyChangeElectricityCostMin = propertyChangeElectricityCostMin,
            PropertyChangeElectricityCostMax = propertyChangeElectricityCostMax,
        };
    }

    private static Dictionary<PropertyType, int> Stats(
        params (PropertyType type, int value)[] pairs
    )
    {
        if (pairs == null || pairs.Length == 0)
            return null;

        var result = new Dictionary<PropertyType, int>(pairs.Length);
        for (int i = 0; i < pairs.Length; i++)
            result[pairs[i].type] = pairs[i].value;
        return result;
    }
}

public class EventOption
{
    public const int AutoPropertyChangeElectricityCost = -1;
    private const int DefaultPropertyChangeElectricityCostMin = 10;
    private const int DefaultPropertyChangeElectricityCostMax = 30;

    public Dictionary<PropertyType, int> PropertyChange;
    public bool RandomChange = false;
    public bool Exit;
    public int TransitionEnergyChange;
    public int TransitionEnergyChangeMin;
    public int TransitionEnergyChangeMax;
    public int ElectricityChange;
    public int ElectricityChangeMin;
    public int ElectricityChangeMax;
    public int PropertyChangeElectricityCostMin = AutoPropertyChangeElectricityCost;
    public int PropertyChangeElectricityCostMax = AutoPropertyChangeElectricityCost;
    public string Text;

    public bool HasTransitionEnergyChange =>
        TransitionEnergyChange != 0
        || TransitionEnergyChangeMin != 0
        || TransitionEnergyChangeMax != 0;

    public bool HasTransitionEnergyRange =>
        TransitionEnergyChangeMin != 0 || TransitionEnergyChangeMax != 0;

    public int RollTransitionEnergyChange(Random rng)
    {
        if (!HasTransitionEnergyRange)
            return TransitionEnergyChange;

        int min = Math.Min(TransitionEnergyChangeMin, TransitionEnergyChangeMax);
        int max = Math.Max(TransitionEnergyChangeMin, TransitionEnergyChangeMax);
        if (min == max)
            return min;

        rng ??= new Random();
        return rng.Next(min, max + 1);
    }

    public bool HasPropertyChangeElectricityCost =>
        HasExplicitPropertyChangeElectricityCost
        || (PropertyChange != null && PropertyChange.Count > 0);

    public int RollPropertyChangeElectricityCost(Random rng)
    {
        if (!HasPropertyChangeElectricityCost)
            return 0;

        if (!HasExplicitPropertyChangeElectricityCost)
            return RollDefaultPropertyChangeElectricityCost(rng);

        int min = Math.Min(PropertyChangeElectricityCostMin, PropertyChangeElectricityCostMax);
        int max = Math.Max(PropertyChangeElectricityCostMin, PropertyChangeElectricityCostMax);
        if (min == max)
            return min;

        rng ??= new Random();
        return rng.Next(min, max + 1);
    }

    private bool HasExplicitPropertyChangeElectricityCost =>
        PropertyChangeElectricityCostMin != AutoPropertyChangeElectricityCost
        || PropertyChangeElectricityCostMax != AutoPropertyChangeElectricityCost;

    private static int RollDefaultPropertyChangeElectricityCost(Random rng)
    {
        rng ??= new Random();
        return rng.Next(
            DefaultPropertyChangeElectricityCostMin,
            DefaultPropertyChangeElectricityCostMax + 1
        );
    }

    public bool HasElectricityChange =>
        ElectricityChange != 0 || ElectricityChangeMin != 0 || ElectricityChangeMax != 0;

    public bool HasElectricityRange => ElectricityChangeMin != 0 || ElectricityChangeMax != 0;

    public int RollElectricityChange(Random rng)
    {
        if (!HasElectricityRange)
            return ElectricityChange;

        int min = Math.Min(ElectricityChangeMin, ElectricityChangeMax);
        int max = Math.Max(ElectricityChangeMin, ElectricityChangeMax);
        if (min == max)
            return min;

        rng ??= new Random();
        return rng.Next(min, max + 1);
    }
}
