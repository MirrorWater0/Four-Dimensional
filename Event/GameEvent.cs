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
                propertyChange: Stats((PropertyType.Power, 4), (PropertyType.Survivability, -1)),
                randomChange: true
            ),
            Option(
                "手动指定校准对象",
                propertyChange: Stats((PropertyType.Power, -1), (PropertyType.Survivability, 3))
            )
        ),
        Event(
            "轨道黑匣",
            "一具漂浮在舱壁缺口旁的黑匣仍在循环广播事故录音。\n"
                + "加密芯片尚未烧毁，只是供能极不稳定。\n"
                + "你可以读取残存记录，也可以直接拆下还能用的模块。",
            Option("解码事故航迹", electricityChange: 90, transitionEnergyChange: -1),
            Option("拆下完整模块", electricityChange: 90),
            Option("写入伪装日志后离开", electricityChange: 30)
        ),
        Event(
            "观测阵列",
            "高空观测阵列仍缓慢转动，镜面上覆盖着薄薄一层霜。\n"
                + "如果重新校准，它可以为一名队员提供精确轨迹预测。\n"
                + "若直接接管阵列，则有机会把整段星图卖给空间站。",
            Option(
                "为一名角色进行轨迹校准",
                propertyChange: Stats((PropertyType.Speed, 3), (PropertyType.Power, 1))
            ),
            Option(
                "让系统随机分配观测增益",
                propertyChange: Stats(
                    (PropertyType.Survivability, 4),
                    (PropertyType.MaxLife, 5),
                    (PropertyType.Speed, -1)
                ),
                randomChange: true
            ),
            Option("打包星图数据出售", electricityChange: 90)
        ),
        Event(
            "废弃军械舱",
            "密封门后是一间被炸开的军械舱，备用挂架上只剩一些可拆解零件。\n"
                + "你没有足够时间全部带走，只能在高风险输出和稳态防护之间做选择。\n"
                + "也可以放弃搜刮，把能拆下来的零件直接换成电力币。",
            Option("回收高能模块", electricityChange: 90),
            Option("回收防护模块", electricityChange: 90),
            Option("拆解剩余零件", electricityChange: 90)
        ),
        Event(
            "低温休眠仓",
            "一排休眠仓仍维持着最低功耗，雾气在玻璃内壁来回聚散。\n"
                + "医疗日志显示，这套设备可以执行一次短时组织修补，但代价并不稳定。\n"
                + "你也可以把备用电池抽走，留作航程中的过渡能量。",
            Option(
                "为一名角色执行修补程序",
                propertyChange: Stats((PropertyType.MaxLife, 15), (PropertyType.Speed, -1))
            ),
            Option(
                "随机唤醒一具维修躯体协助强化",
                propertyChange: Stats((PropertyType.Survivability, 3), (PropertyType.Power, 1)),
                randomChange: true
            ),
            Option("抽走休眠仓备用电池", transitionEnergyChange: 20, electricityChange: -20)
        ),
        Event(
            "引力透镜井",
            "你在舰体残骸中央发现了一口微型引力透镜井。\n"
                + "井口附近的空间被拉伸成细长的光带，靠近者会承受不同程度的体征偏转。\n"
                + "若强行收束透镜，也许还能榨出一笔值钱的实验数据。",
            Option(
                "让一名角色接受引力偏转",
                propertyChange: Stats((PropertyType.Survivability, 3), (PropertyType.Speed, -1))
            ),
            Option(
                "随机暴露于焦点中心",
                propertyChange: Stats(
                    (PropertyType.Power, 3),
                    (PropertyType.Speed, 3),
                    (PropertyType.MaxLife, -4)
                ),
                randomChange: true
            ),
            Option("收束透镜并导出实验数据", electricityChange: 90, transitionEnergyChange: -1)
        ),
        Event(
            "静默圣所",
            "一间被切断外部通信的独立舱室安静得异常，中央只留下简易祭台和存放柜。\n"
                + "柜体里的防护部件已经失效，墙面上则反复滚动着一段静默训练守则。\n"
                + "你可以拆解柜体，也可以短暂停留，恢复航程节奏。",
            Option("拆解存放柜", electricityChange: 90),
            Option("展开折叠护壁并拆解", electricityChange: 90),
            Option("遵循守则静坐片刻", transitionEnergyChange: 20)
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
        int electricityChange = 0
    )
    {
        return new EventOption
        {
            Text = text,
            PropertyChange = propertyChange,
            RandomChange = randomChange,
            Exit = exit,
            TransitionEnergyChange = transitionEnergyChange,
            ElectricityChange = electricityChange,
        };
    }

    private static Dictionary<PropertyType, int> Stats(params (PropertyType type, int value)[] pairs)
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
    public Dictionary<PropertyType, int> PropertyChange;
    public bool RandomChange = false;
    public bool Exit;
    public int TransitionEnergyChange;
    public int ElectricityChange;
    public string Text;
}
