using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

public partial class Relic
{
    public RelicID ID;
    public string RelicDescription;
    public int Num = -1;
    public Control IconNode;
    public static PackedScene IconScene = GD.Load<PackedScene>("res://Relic/RelicIcon.tscn");

    public Relic(RelicID relicID)
    {
        ID = relicID;
    }

    public static void RelicAdd(PlayerResourceState playerResourceState, RelicID relicID)
    {
        Relic relic = relicID switch
        {
            RelicID.Blessing => new Relic(RelicID.Blessing),
            _ => new Relic(RelicID.curse),
        };
        int num = relicID switch
        {
            RelicID.Blessing => 3,
            _ => -1,
        };
        relic.IconAdd(playerResourceState);
        GameInfo.Relic.Add(relicID, num);
    }

    public void IconAdd(PlayerResourceState playerResourceState)
    {
        string path = ID switch
        {
            RelicID.Blessing => "res://shader/Icon/RelicIcon/Point.gdshader",
            _ => null,
        };
        var icon = IconScene.Instantiate() as ColorRect;
        var shader = GD.Load<Shader>(path);
        var material = new ShaderMaterial() { Shader = shader };
        icon.Material = material;
        icon.GetNode<Label>("Label").Text = Num.ToString();
        playerResourceState.RelicContainer.AddChild(icon);
        IconNode = icon;
    }

    public async Task BattleEffect(Battle battle)
    {
        switch (ID)
        {
            case RelicID.Blessing:
                if (Num <= 0)
                    return;
                List<Task> list = new();
                for (int i = 0; i < battle.EnemiesList.Count; i++)
                {
                    list.Add(battle.EnemiesList[i].GetHurt(20));
                }
                await Task.WhenAll(list);
                Num--;
                break;
            case RelicID.curse:
                break;
        }
        GameInfo.Relic[ID] = Num;
        IconNode.GetNode<Label>("Label").Text = Num.ToString();
    }
}

public enum RelicID
{
    Blessing,
    curse,
}
