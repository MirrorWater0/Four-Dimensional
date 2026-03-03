public partial class Equipment
{
    public enum EquipmentName
    {
        RiftBlade,
        PhaseShoulder,
        EchoCore,
        LumenBadge,
        SilentPendant,
        FoldedBulwark,
    }

    // Keep these as public fields so SaveSystem can serialize/deserialize them via reflection.
    public EquipmentName Name;
    public string DisplayName;
    public string TypeLabel;
    public int Power;
    public int Survivability;
    public int Speed;
    public int MaxLife;
    public string Description;

    public static readonly Equipment[] Catalog =
    [
        new Equipment
        {
            Name = EquipmentName.RiftBlade,
            DisplayName = "裂隙短刃",
            TypeLabel = "输出",
            Power = 3,
            Survivability = -2,
            Speed = 0,
            MaxLife = 0,
            Description = "高频切割型武装，强化先手与追击。",
        },
        new Equipment
        {
            Name = EquipmentName.PhaseShoulder,
            DisplayName = "相位肩甲",
            TypeLabel = "生存",
            Power = 0,
            Survivability = 2,
            Speed = 0,
            MaxLife = 5,
            Description = "重构受击姿态，显著提升承伤稳定性。",
        },
        new Equipment
        {
            Name = EquipmentName.EchoCore,
            DisplayName = "回响核心",
            TypeLabel = "均衡",
            Power = 1,
            Survivability = 1,
            Speed = 1,
            MaxLife = 0,
            Description = "共振回路稳定，提供均衡型攻防提升。",
        },
        new Equipment
        {
            Name = EquipmentName.LumenBadge,
            DisplayName = "流光徽章",
            TypeLabel = "速度",
            Power = 0,
            Survivability = 0,
            Speed = 2,
            MaxLife = 5,
            Description = "压缩行动间隔，强化循环速度与续战能力。",
        },
        new Equipment
        {
            Name = EquipmentName.SilentPendant,
            DisplayName = "缄默吊坠",
            TypeLabel = "速度 · 生存",
            Power = 0,
            Survivability = 1,
            Speed = 2,
            MaxLife = -5,
            Description = "轻量防护组件，兼顾机动与生存。",
        },
        new Equipment
        {
            Name = EquipmentName.FoldedBulwark,
            DisplayName = "折叠护壁",
            TypeLabel = "生命",
            Power = 0,
            Survivability = 0,
            Speed = 0,
            MaxLife = 15,
            Description = "展开后形成缓冲屏障，直接抬升生命上限。",
        },
    ];

    public static Equipment Create(EquipmentName name)
    {
        for (int i = 0; i < Catalog.Length; i++)
        {
            if (Catalog[i].Name == name)
                return Clone(Catalog[i]);
        }
        return null;
    }

    public static Equipment[] GetCatalogClones()
    {
        Equipment[] result = new Equipment[Catalog.Length];
        for (int i = 0; i < Catalog.Length; i++)
            result[i] = Clone(Catalog[i]);
        return result;
    }

    public static Equipment Clone(Equipment source)
    {
        if (source == null)
            return null;

        return new Equipment
        {
            Name = source.Name,
            DisplayName = source.DisplayName,
            TypeLabel = source.TypeLabel,
            Power = source.Power,
            Survivability = source.Survivability,
            Speed = source.Speed,
            MaxLife = source.MaxLife,
            Description = source.Description,
        };
    }
}
