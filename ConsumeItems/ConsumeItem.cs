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
        Buff,
        DrawCards,
        Energy,
    }

    private readonly record struct ItemConfig(
        string Name,
        ItemEffectType EffectType,
        int Value,
        PropertyType PropertyType = default,
        Buff.BuffName BuffName = default
    );

    private static readonly Dictionary<ItemID, ItemConfig> ItemConfigs = new()
    {
        [ItemID.Health] = new("治疗道具", ItemEffectType.Buff, 1, BuffName: Buff.BuffName.RebirthI),
        [ItemID.Guard] = new("脉冲护盾", ItemEffectType.Block, 60),
        [ItemID.Fury] = new("肾上腺素", ItemEffectType.PropertyIncrease, 6, PropertyType.Power),
        [ItemID.Haste] = new("迅捷之翼", ItemEffectType.PropertyIncrease, 5, PropertyType.Speed),
        [ItemID.Vitality] = new(
            "全息装甲",
            ItemEffectType.PropertyIncrease,
            6,
            PropertyType.Survivability
        ),
        [ItemID.Explosion] = new("爆裂弹", ItemEffectType.Damage, 40),
        [ItemID.ElectromagneticInterference] = new(
            "电磁干扰",
            ItemEffectType.Buff,
            6,
            BuffName: Buff.BuffName.Weaken
        ),
        [ItemID.SpaceOscillation] = new(
            "空间震荡",
            ItemEffectType.Buff,
            6,
            BuffName: Buff.BuffName.Vulnerable
        ),
        [ItemID.StreamingTransmission] = new("流式传输", ItemEffectType.DrawCards, 3),
        [ItemID.Battery] = new("电池", ItemEffectType.Energy, 3),
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
        TryAddItem(playerResource, item, syncGameInfo);
    }

    public static bool TryAddItem(
        PlayerResourceState playerResource,
        ItemID item,
        bool syncGameInfo = true
    )
    {
        if (playerResource == null)
            return false;

        GameInfo.Items ??= new List<ItemID>();

        int currentCount = syncGameInfo ? GameInfo.Items.Count : playerResource.Items.Count;
        if (currentCount >= GameInfo.ItemsMaxCount)
            return false;

        var slot = FindEmptyItemSlot(playerResource);
        if (slot == null)
            return false;

        if (syncGameInfo)
            GameInfo.Items.Add(item);

        var consumeItem = new ConsumeItem(item);
        playerResource.Items.Add(consumeItem);
        var icon = IconSence.Instantiate() as ColorRect;
        consumeItem.Icon = icon;
        consumeItem.playerResource = playerResource;
        ConfigureIcon(icon, item);
        icon.MouseFilter = Control.MouseFilterEnum.Ignore;

        slot.AddItemIcon(icon);
        return true;
    }

    public async Task UseEffect(Battle battle, Control sourceControl = null)
    {
        target =
            battle?.CharacterControl != null && GodotObject.IsInstanceValid(battle.CharacterControl)
                ? await battle.CharacterControl.PickItemTargetAsync(sourceControl)
                : await AimTarget.AimTargetTask(battle);
        if (target == null)
            return;

        await ApplyItemEffectAsync(target, item);
        RefreshBattleUiAfterItemEffect(battle);

        RemoveFromInventory();
        await RefreshBattleUiAfterItemEffectDeferred(battle);
    }

    private static void RefreshBattleUiAfterItemEffect(Battle battle)
    {
        battle?.CharacterControl?.RefreshCurrentTurnUi();
    }

    private static async Task RefreshBattleUiAfterItemEffectDeferred(Battle battle)
    {
        if (battle == null || !GodotObject.IsInstanceValid(battle) || !battle.IsInsideTree())
            return;

        await battle.ToSignal(battle.GetTree(), SceneTree.SignalName.ProcessFrame);
        RefreshBattleUiAfterItemEffect(battle);
    }

    public void Discard()
    {
        RemoveFromInventory();
    }

    private void RemoveFromInventory()
    {
        playerResource?.Items.Remove(this);
        GameInfo.Items?.Remove(item);
        if (Icon != null && GodotObject.IsInstanceValid(Icon))
            Icon.QueueFree();
        Icon = null;
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
            ItemEffectType.Buff =>
                $"{targetPrefix}，给予{config.Value}层{Buff.GetBuffDisplayName(config.BuffName)}。",
            ItemEffectType.DrawCards => $"{targetPrefix}，抽{config.Value}张牌。",
            ItemEffectType.Energy => $"{targetPrefix}，获得{config.Value}点能量。",
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
            ItemID.ElectromagneticInterference =>
                "res://shader/Icon/ComsumeItems/ElectromagneticInterferenceItem.gdshader",
            ItemID.SpaceOscillation =>
                "res://shader/Icon/ComsumeItems/SpaceOscillationItem.gdshader",
            ItemID.StreamingTransmission =>
                "res://shader/Icon/ComsumeItems/StreamingTransmissionItem.gdshader",
            ItemID.Battery => "res://shader/Icon/ComsumeItems/BatteryItem.gdshader",
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

    private static ItemContainer FindEmptyItemSlot(PlayerResourceState playerResource)
    {
        if (playerResource?.ItemContainer == null)
            return null;

        return playerResource
            .ItemContainer.GetChildren()
            .OfType<ItemContainer>()
            .FirstOrDefault(slot => !slot.HasItemIcon());
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
            case ItemEffectType.Buff:
                ApplyBuffItemEffect(target, config);
                break;
            case ItemEffectType.DrawCards:
                if (target is PlayerCharacter playerCharacter)
                    playerCharacter.TryDrawBattleCards(config.Value);
                break;
            case ItemEffectType.Energy:
                target.UpdataEnergy(config.Value, target);
                break;
        }
    }

    private static void ApplyBuffItemEffect(Character target, ItemConfig config)
    {
        switch (config.BuffName)
        {
            case Buff.BuffName.Weaken:
                AttackBuff.BuffAdd(config.BuffName, target, config.Value);
                break;
            case Buff.BuffName.Vulnerable:
                HurtBuff.BuffAdd(config.BuffName, target, config.Value);
                break;
            case Buff.BuffName.RebirthI:
                DyingBuff.BuffAdd(config.BuffName, target, config.Value);
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
    ElectromagneticInterference,
    SpaceOscillation,
    StreamingTransmission,
    Battery,
}
