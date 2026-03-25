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

    public static Relic Create(RelicID relicID)
    {
        return relicID switch
        {
            RelicID.Blessing => new Relic(RelicID.Blessing),
            RelicID.Triangle => new Relic(RelicID.Triangle),
            _ => new Relic(RelicID.curse),
        };
    }

    public static int GetAcquireAmount(RelicID relicID)
    {
        return relicID switch
        {
            RelicID.Blessing => 3,
            _ => -1,
        };
    }

    public static string GetIconShaderPath(RelicID relicID)
    {
        return relicID switch
        {
            RelicID.Blessing => "res://shader/Icon/RelicIcon/Point.gdshader",
            RelicID.Triangle => "res://shader/Icon/RelicIcon/Triangle.gdshader",
            _ => null,
        };
    }

    public static string FormatCountLabel(int count)
    {
        return count < 0 ? string.Empty : count.ToString();
    }

    public static void RelicAdd(PlayerResourceState playerResourceState, RelicID relicID)
    {
        Relic relic = Create(relicID);
        int num = GetAcquireAmount(relicID);
        relic.Num = num;
        relic.IconAdd(playerResourceState);
        GameInfo.Relic[relicID] = num;
    }

    public void IconAdd(PlayerResourceState playerResourceState)
    {
        string path = GetIconShaderPath(ID);
        var icon = IconScene.Instantiate() as ColorRect;
        var shader = string.IsNullOrWhiteSpace(path) ? null : GD.Load<Shader>(path);
        icon.Material = shader == null ? null : new ShaderMaterial() { Shader = shader };
        icon.GetNode<Label>("Label").Text = GetIconCountText();
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
            case RelicID.Triangle:

                int survivabilityGain = 2;
                int targetCount = Math.Min(2, battle.PlayersList.Count);
                for (int i = 0; i < targetCount; i++)
                {
                    var player = battle.PlayersList[i];
                    if (player == null || player.State == Character.CharacterState.Dying)
                        continue;

                    await player.IncreaseProperties(PropertyType.Survivability, survivabilityGain);
                }
                break;
            case RelicID.curse:
                break;
        }
        GameInfo.Relic[ID] = Num;
        UpdateIconLabel();
        UpdateRelicTipText();
    }

    private string BuildTooltip()
    {
        string name = Colorize(RelicName, NameColorHex);
        string description = ColorizeNumbers(RelicDescription, NumberColorHex);
        return $"{name}\n{description}";
    }

    public void UpdateIconLabel()
    {
        if (IconNode == null)
            return;

        IconNode.GetNode<Label>("Label").Text = GetIconCountText();
    }

    private static string GetRelicName(RelicID relicID)
    {
        return relicID switch
        {
            RelicID.Blessing => "祝福",
            RelicID.Triangle => "三角",
            _ => "诅咒",
        };
    }

    private static string GetRelicDescription(RelicID relicID)
    {
        return relicID switch
        {
            RelicID.Blessing => $"战斗开始时对所有敌人造成{40}伤害。",
            RelicID.Triangle => $"战斗开始时第一位和第二位角色获得{2}点生存。",
            _ => "暂无效果。",
        };
    }

    private string GetIconCountText()
    {
        return FormatCountLabel(Num);
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
    Triangle,
    curse,
}
