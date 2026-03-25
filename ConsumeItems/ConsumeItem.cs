using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;

public partial class ConsumeItem
{
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
        switch (item)
        {
            case ItemID.Health:
                target.Recover(15);
                break;
            case ItemID.Guard:
                target.UpdataBlock(12);
                break;
            case ItemID.Fury:
                await target.IncreaseProperties(PropertyType.Power, 3);
                break;
            case ItemID.Haste:
                await target.IncreaseProperties(PropertyType.Speed, 2);
                break;
            case ItemID.Vitality:
                await target.IncreaseProperties(PropertyType.Survivability, 3);
                break;
            case ItemID.Explosion:
                await target.GetHurt(30);
                break;
        }
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
        return itemId switch
        {
            ItemID.Health => "治疗道具",
            ItemID.Guard => "护盾道具",
            ItemID.Fury => "狂怒道具",
            ItemID.Haste => "迅捷道具",
            ItemID.Vitality => "坚韧道具",
            ItemID.Explosion => "爆裂弹",
            _ => "未知道具",
        };
    }

    public static string GetItemDescription(ItemID itemId)
    {
        return itemId switch
        {
            ItemID.Health => "选择角色，回复15生命。",
            ItemID.Guard => "选择角色，获得12点格挡。",
            ItemID.Fury => "选择角色，获得3点力量。",
            ItemID.Haste => "选择角色，获得2点速度。",
            ItemID.Vitality => "选择角色，获得3点生存。",
            ItemID.Explosion => "选择角色，造成30伤害。",
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
