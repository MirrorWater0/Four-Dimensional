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
        string path = item switch
        {
            ItemID.Explosion => "res://shader/Icon/ComsumeItems/ExplosionItem.gdshader",
            ItemID.Health => "res://shader/Icon/BuffIcon/Rebirth.gdshader",
            _ => null,
        };
        var material = new ShaderMaterial() { Shader = GD.Load<Shader>(path) };
        icon.Material = material;
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
            ItemID.Health => "医疗包",
            ItemID.Explosion => "爆裂弹",
            _ => "未知道具",
        };
    }

    public static string GetItemDescription(ItemID itemId)
    {
        return itemId switch
        {
            ItemID.Health => "选择角色，回复15生命。",
            ItemID.Explosion => "选择角色，造成30伤害。",
            _ => string.Empty,
        };
    }
}

public enum ItemID
{
    None,
    Health,
    Explosion,
}
