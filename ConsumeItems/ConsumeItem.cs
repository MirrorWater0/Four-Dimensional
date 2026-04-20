using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;

public partial class ConsumeItem
{
    private enum ItemEffectType
    {
        Recover,
        Block,
        PropertyIncrease,
        Damage,
    }

    private readonly record struct ItemConfig(
        string Name,
        ItemEffectType EffectType,
        int Value,
        PropertyType PropertyType = default
    );

    private static readonly Dictionary<ItemID, ItemConfig> ItemConfigs = new()
    {
        [ItemID.Health] = new("治疗道具", ItemEffectType.Recover, 15),
        [ItemID.Guard] = new("脉冲护盾", ItemEffectType.Block, 40),
        [ItemID.Fury] = new("肾上腺素", ItemEffectType.PropertyIncrease, 3, PropertyType.Power),
        [ItemID.Haste] = new("迅捷之翼", ItemEffectType.PropertyIncrease, 5, PropertyType.Speed),
        [ItemID.Vitality] = new(
            "全息装甲",
            ItemEffectType.PropertyIncrease,
            4,
            PropertyType.Survivability
        ),
        [ItemID.Explosion] = new("爆裂弹", ItemEffectType.Damage, 30),
    };

    public static PackedScene IconSence = GD.Load<PackedScene>(
        "res://ConsumeItems/ItemTemplate.tscn"
    );
    private ItemID item;
    private Character target;
    public ColorRect Icon;
    public PlayerResourceState playerResource;
    public ItemID ItemId => item;

    public ConsumeItem(ItemID item)
    {
        this.item = item;
    }

    public static void AddItem(
        PlayerResourceState playerResource,
        ItemID item,
        bool syncGameInfo = true
    )
    {
        if (playerResource == null)
            return;

        GameInfo.Items ??= new List<ItemID>();

        int currentCount = syncGameInfo ? GameInfo.Items.Count : playerResource.Items.Count;
        if (currentCount >= GameInfo.ItemsMaxCount)
            return;

        if (syncGameInfo)
            GameInfo.Items.Add(item);

        var consumeItem = new ConsumeItem(item);
        playerResource.Items.Add(consumeItem);
        var icon = IconSence.Instantiate() as ColorRect;
        consumeItem.Icon = icon;
        consumeItem.playerResource = playerResource;
        ConfigureIcon(icon, item);
        icon.MouseFilter = Control.MouseFilterEnum.Ignore;

        playerResource
            .ItemContainer.GetChildren()
            .First(x => x.GetChildCount() == 0)
            .AddChild(icon);
    }

    public async Task UseEffect(Battle battle)
    {
        target = await AimTarget.AimTargetTask(battle);
        if (target == null)
            return;

        await ApplyItemEffectAsync(target, item);

        playerResource.Items.Remove(this);
        GameInfo.Items?.Remove(item);
        Icon.QueueFree();
    }

    public string BuildTooltipText()
    {
        string name = GetItemName(item);
        string desc = GetItemDescription(item);
        desc = GlobalFunction.ColorizeNumbers(desc);
        return $"[b]{name}[/b]\n{desc}";
    }

    public static string GetItemName(ItemID itemId)
    {
        return TryGetItemConfig(itemId, out var config) ? config.Name : "未知道具";
    }

    public static string GetItemDescription(ItemID itemId)
    {
        return GetItemDescription(itemId, "选择角色");
    }

    public static string GetItemDescription(ItemID itemId, string targetPrefix)
    {
        if (!TryGetItemConfig(itemId, out var config))
            return string.Empty;

        targetPrefix = string.IsNullOrWhiteSpace(targetPrefix) ? "选择角色" : targetPrefix;
        return config.EffectType switch
        {
            ItemEffectType.Recover => $"{targetPrefix}，回复{config.Value}生命。",
            ItemEffectType.Block => $"{targetPrefix}，获得{config.Value}点格挡。",
            ItemEffectType.PropertyIncrease =>
                $"{targetPrefix}，获得{config.Value}点{GetPropertyDisplayName(config.PropertyType)}。",
            ItemEffectType.Damage => $"{targetPrefix}，造成{config.Value}伤害。",
            _ => string.Empty,
        };
    }

    public static void ConfigureIcon(ColorRect icon, ItemID itemId)
    {
        if (icon == null)
            return;

        string shaderPath = GetItemShaderPath(itemId);
        if (string.IsNullOrWhiteSpace(shaderPath))
        {
            icon.Material = null;
            return;
        }

        var shader = GD.Load<Shader>(shaderPath);
        icon.Material = shader == null ? null : new ShaderMaterial { Shader = shader };
    }

    private static string GetItemShaderPath(ItemID itemId)
    {
        return itemId switch
        {
            ItemID.Health => "res://shader/Icon/BuffIcon/Rebirth.gdshader",
            ItemID.Guard => "res://shader/Icon/ComsumeItems/GuardItem.gdshader",
            ItemID.Fury => "res://shader/Icon/ComsumeItems/FuryItem.gdshader",
            ItemID.Haste => "res://shader/Icon/ComsumeItems/HasteItem.gdshader",
            ItemID.Vitality => "res://shader/Icon/ComsumeItems/VitalityItem.gdshader",
            ItemID.Explosion => "res://shader/Icon/ComsumeItems/ExplosionItem.gdshader",
            _ => null,
        };
    }

    public static int GetItemValue(ItemID itemId)
    {
        return TryGetItemConfig(itemId, out var config) ? config.Value : 0;
    }

    private static bool TryGetItemConfig(ItemID itemId, out ItemConfig config)
    {
        return ItemConfigs.TryGetValue(itemId, out config);
    }

    private static string GetPropertyDisplayName(PropertyType propertyType)
    {
        return propertyType switch
        {
            PropertyType.Power => "力量",
            PropertyType.Speed => "速度",
            PropertyType.Survivability => "生存",
            PropertyType.MaxLife => "生命",
            _ => "属性",
        };
    }

    private static async Task ApplyItemEffectAsync(Character target, ItemID itemId)
    {
        if (target == null || !TryGetItemConfig(itemId, out var config))
            return;

        switch (config.EffectType)
        {
            case ItemEffectType.Recover:
                target.Recover(config.Value);
                break;
            case ItemEffectType.Block:
                target.UpdataBlock(config.Value);
                break;
            case ItemEffectType.PropertyIncrease:
                await target.IncreaseProperties(config.PropertyType, config.Value);
                break;
            case ItemEffectType.Damage:
                await target.GetHurt(config.Value);
                break;
        }
    }
}

public enum ItemID
{
    None,
    Health,
    Guard,
    Fury,
    Haste,
    Vitality,
    Explosion,
}
