using System;
using System.Threading.Tasks;
using Godot;

public partial class ConsumeItem
{
    ItemEnum item;
    private Character target;

    public async Task UseEffect(Battle battle)
    {
        switch (item)
        {
            case ItemEnum.Health:
                break;
            case ItemEnum.Explosion:
                break;
        }
    }
}

public enum ItemEnum
{
    None,
    Health,
    Explosion,
}
