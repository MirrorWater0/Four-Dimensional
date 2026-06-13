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
                propertyChange: Stats((PropertyType.Survivability, 2))
            ),
            Option("稳定场核并回充", transitionEnergyChangeMin: 15, transitionEnergyChangeMax: 25)
        ),
        Event(
            "镜像编译器",
            "一台半埋在断层里的编译器仍在执行旧时代的战术模拟。\n"
                + "它可以复制一次既有战术，也可以把一段战术指令重写成同源的新形态。\n"
                + "如果你愿意，还能让它彻底擦除一张不再需要的卡牌。",
            Option("复制一张卡牌", actionType: EventOptionActionType.CopyCard),
            Option("变化一张卡牌", actionType: EventOptionActionType.TransformCard),
            Option("删除一张卡牌", actionType: EventOptionActionType.RemoveCard)
        ),
        Event(
            "遗失补给舱",
            "补给舱卡在一截弯折的外骨架之间，内部的自动分拣臂仍在低速摆动。\n"
                + "一个封存匣保存着未登记遗物，另一侧则是可注入神经接口的训练芯片。\n"
                + "也可以放弃检索，直接抽走剩余维生电池。",
            Option("取得封存遗物", actionType: EventOptionActionType.GainRelic),
            Option(
                "接入训练芯片",
                actionType: EventOptionActionType.GainTalentPoint,
                talentPointAmount: 1
            ),
            Option("抽走维生电池", transitionEnergyChangeMin: 15, transitionEnergyChangeMax: 25)
        ),
        Event(
            "轨道黑匣",
            "一具漂浮在舱壁缺口旁的黑匣仍在循环广播事故录音。\n"
                + "加密芯片尚未烧毁，只是供能极不稳定。\n"
                + "你可以出售残存记录、复制一段战术日志，或把余电导回队伍系统。",
            Option("出售事故航迹", electricityChangeMin: 70, electricityChangeMax: 100),
            Option("复制战术日志", actionType: EventOptionActionType.CopyCard),
            Option("接入残余电容", transitionEnergyChangeMin: 15, transitionEnergyChangeMax: 25)
        ),
        Event(
            "记忆铸炉",
            "铸炉深处漂浮着无数未完成的战术切片，像冷却前的金属片一样轻轻颤动。\n"
                + "系统提示：投入一张卡牌即可重新锻造成新指令，也可以拓印一段完整模型。\n"
                + "若不信任这台设备，炉芯本身也能拆出一笔电力币。",
            Option("重铸一张卡牌", actionType: EventOptionActionType.TransformCard),
            Option("拓印一张卡牌", actionType: EventOptionActionType.CopyCard),
            Option("拆出售电模块", electricityChangeMin: 50, electricityChangeMax: 80)
        ),
        Event(
            "清除协议站",
            "一座维护站仍保持着低频广播，屏幕上只有一句话：冗余会拖慢生还概率。\n"
                + "它可以安全移除一段战术指令，也可以把废料压缩成可交易数据。\n"
                + "旁路电池还残留着一点可以直接导入队伍的能量。",
            Option("执行卡牌清除", actionType: EventOptionActionType.RemoveCard),
            Option("压缩废料数据", electricityChangeMin: 45, electricityChangeMax: 75),
            Option("导入旁路电池", transitionEnergyChangeMin: 12, transitionEnergyChangeMax: 22)
        ),
        Event(
            "静默圣所",
            "一间被切断外部通信的独立舱室安静得异常，中央只留下简易祭台和存放柜。\n"
                + "墙面反复滚动着一段静默训练守则，柜体深处则锁着一枚旧式护符。\n"
                + "这里不适合久留，但短暂停驻也能让队伍恢复航程节奏。",
            Option(
                "研读静默守则",
                actionType: EventOptionActionType.GainTalentPoint,
                talentPointAmount: 1
            ),
            Option("开启祭台存放柜", actionType: EventOptionActionType.GainRelic),
            Option("静坐片刻", transitionEnergyChangeMin: 15, transitionEnergyChangeMax: 25)
        ),
        Event(
            "废弃军械舱",
            "密封门后是一间被炸开的军械舱，备用挂架上只剩一些还能识别用途的残件。\n"
                + "有些残件可以拼成遗物，有些日志记录了未完成的训练程序。\n"
                + "如果你只想快速离开，也能把挂架电芯拆走。",
            Option("拼合残件遗物", actionType: EventOptionActionType.GainRelic),
            Option(
                "读取训练日志",
                actionType: EventOptionActionType.GainTalentPoint,
                talentPointAmount: 1
            ),
            Option("拆走挂架电芯", electricityChangeMin: 60, electricityChangeMax: 90)
        ),
        Event(
            "低温休眠仓",
            "一排休眠仓仍维持着最低功耗，雾气在玻璃内壁来回聚散。\n"
                + "医疗日志已经损毁，但仓体系统还保存着可复制的应急流程。\n"
                + "你也可以把备用电池抽走，留作航程中的过渡能量。",
            Option("复制应急流程", actionType: EventOptionActionType.CopyCard),
            Option("导出医疗账本", electricityChangeMin: 40, electricityChangeMax: 70),
            Option("抽走备用电池", transitionEnergyChangeMin: 18, transitionEnergyChangeMax: 28)
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
        int propertyChangeElectricityCostMax = EventOption.AutoPropertyChangeElectricityCost,
        EventOptionActionType actionType = EventOptionActionType.None,
        RelicID? relicReward = null,
        int talentPointAmount = 0
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
            ActionType = actionType,
            RelicReward = relicReward,
            TalentPointAmount = talentPointAmount,
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

public enum EventOptionActionType
{
    None,
    CopyCard,
    TransformCard,
    RemoveCard,
    GainRelic,
    GainTalentPoint,
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
    public EventOptionActionType ActionType;
    public RelicID? RelicReward;
    public int TalentPointAmount;
    public string Text;

    public bool RequiresCardSelection =>
        ActionType
            is EventOptionActionType.CopyCard
                or EventOptionActionType.TransformCard
                or EventOptionActionType.RemoveCard;

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
