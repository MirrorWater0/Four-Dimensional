using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

public partial class Relic
{
    public RelicName RelicName;
    public string RelicDescription;
    public int Num;

    public Relic()
    {
        Num = 3;
    }

    public Relic(RelicName relicName)
    {
        RelicName = relicName;
        Num = 3;
    }

    public void IconAdd(PlayerResourceState playerResourceState)
    {
        switch (RelicName)
        {
            case RelicName.Blessing:
                break;
            case RelicName.curse:
                break;
        }
    }

    public async Task Effect(Battle battle)
    {
        switch (RelicName)
        {
            case RelicName.Blessing:
                List<Task> list = new();
                for (int i = 0; i < battle.EnemiesList.Count; i++)
                {
                    list.Add(battle.EnemiesList[i].GetHurt(10));
                }
                await Task.WhenAll(list);
                Num--;
                break;
            case RelicName.curse:
                break;
        }
    }
}

public enum RelicName
{
    Blessing,
    curse,
}
