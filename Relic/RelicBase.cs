using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

public partial class Relic
{
    public RelicID ID;
    public string RelicName;
    public string RelicDescription;
    public int Num = -1;
    public Control IconNode;
    public static PackedScene IconScene = GD.Load<PackedScene>("res://Relic/RelicIcon.tscn");

    public Relic(RelicID relicID)
    {
        ID = relicID;
        RelicName = GetRelicName(relicID);
        RelicDescription = GetRelicDescription(relicID);
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
        relic.Num = num;
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
        WireRelicTip(icon, playerResourceState);
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
                    list.Add(battle.EnemiesList[i].GetHurt(40));
                }
                await Task.WhenAll(list);
                Num--;
                break;
            case RelicID.curse:
                break;
        }
        GameInfo.Relic[ID] = Num;
        IconNode.GetNode<Label>("Label").Text = Num.ToString();
        UpdateRelicTipText();
    }

    private string BuildTooltip()
    {
        string countText = Num >= 0 ? $"\n次数: {Num}" : string.Empty;
        string name = Colorize(RelicName, NameColorHex);
        string description = ColorizeNumbers($"{RelicDescription}{countText}", NumberColorHex);
        return $"{name}\n{description}";
    }

    private static string GetRelicName(RelicID relicID)
    {
        return relicID switch
        {
            RelicID.Blessing => "祝福",
            _ => "诅咒",
        };
    }

    private static string GetRelicDescription(RelicID relicID)
    {
        return relicID switch
        {
            RelicID.Blessing => $"战斗开始时对所有敌人造成{40}伤害。",
            _ => "负面遗物，暂无效果。",
        };
    }

    private void WireRelicTip(Control icon, PlayerResourceState playerResourceState)
    {
        if (icon == null || playerResourceState == null)
            return;

        var tip = GetOrCreateRelicTip(playerResourceState);
        if (tip == null)
            return;

        icon.MouseEntered += () =>
        {
            tip.FollowMouse = true;
            tip.Description.Text = BuildTooltip();
            tip.Visible = true;
        };
        icon.MouseExited += () =>
        {
            if (tip != null)
                tip.Visible = false;
        };
    }

    private void UpdateRelicTipText()
    {
        if (IconNode == null)
            return;
        var tip = GetOrCreateRelicTip(IconNode);
        if (tip == null || !tip.Visible)
            return;
        tip.Description.Text = BuildTooltip();
    }

    private static Tip GetOrCreateRelicTip(Node context)
    {
        var root = context?.GetTree()?.Root;
        if (root == null)
            return null;

        var layer = root.GetNodeOrNull<CanvasLayer>("TipLayer");
        if (layer == null)
        {
            layer = new CanvasLayer { Layer = 6, Name = "TipLayer" };
            if (root.IsInsideTree())
                root.AddChild(layer);
            else
                root.CallDeferred(Node.MethodName.AddChild, layer);
        }

        var existing = layer.GetNodeOrNull<Tip>("RelicTip");
        if (existing != null)
            return existing;

        var tipScene = GD.Load<PackedScene>("res://battle/UIScene/Tip.tscn");
        if (tipScene == null)
            return null;

        var tip = tipScene.Instantiate<Tip>();
        tip.Name = "RelicTip";
        tip.FollowMouse = true;
        tip.AnchorOffset = new Vector2(20f, 20f);
        layer.AddChild(tip);
        return tip;
    }

    private const string NameColorHex = "#b78cff";
    private const string NumberColorHex = "#ffd24a";

    private static string Colorize(string text, string colorHex)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;
        return $"[color={colorHex}]{text}[/color]";
    }

    private static string ColorizeNumbers(string input, string colorHex)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        var builder = new System.Text.StringBuilder(input.Length * 2);
        bool inTag = false;

        for (int i = 0; i < input.Length; i++)
        {
            char ch = input[i];

            if (ch == '[')
            {
                inTag = true;
                builder.Append(ch);
                continue;
            }

            if (ch == ']')
            {
                inTag = false;
                builder.Append(ch);
                continue;
            }

            if (!inTag && char.IsDigit(ch))
            {
                int start = i;
                while (i < input.Length && char.IsDigit(input[i]))
                    i++;

                builder.Append($"[color={colorHex}]");
                builder.Append(input, start, i - start);
                builder.Append("[/color]");
                i--;
                continue;
            }

            builder.Append(ch);
        }

        return builder.ToString();
    }
}

public enum RelicID
{
    Blessing,
    curse,
}
