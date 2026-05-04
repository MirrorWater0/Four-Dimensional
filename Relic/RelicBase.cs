using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

public partial class Relic
{
    private const int BlessingDamage = 50;

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

    public static RelicID[] GetUnownedOfferPool()
    {
        var ownedRelics = GameInfo.Relics;
        List<RelicID> result = new();
        RelicID[] pool =
        {
            RelicID.Blessing,
            RelicID.Triangle,
            RelicID.Square,
            RelicID.Pentagon,
            RelicID.Hexagon,
            RelicID.Heptagon,
            RelicID.Octagon,
            RelicID.CompressionCore,
        };

        for (int i = 0; i < pool.Length; i++)
        {
            var relicId = pool[i];
            if (ownedRelics == null || !ownedRelics.ContainsKey(relicId))
                result.Add(relicId);
        }

        return result.ToArray();
    }

    public static Relic Create(RelicID relicID)
    {
        return relicID switch
        {
            RelicID.Blessing => new Relic(RelicID.Blessing),
            RelicID.Triangle => new Relic(RelicID.Triangle),
            RelicID.Square => new Relic(RelicID.Square),
            RelicID.Pentagon => new Relic(RelicID.Pentagon),
            RelicID.Hexagon => new Relic(RelicID.Hexagon),
            RelicID.Heptagon => new Relic(RelicID.Heptagon),
            RelicID.Octagon => new Relic(RelicID.Octagon),
            RelicID.CompressionCore => new Relic(RelicID.CompressionCore),
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
            RelicID.Square => "res://shader/Icon/RelicIcon/Square.gdshader",
            RelicID.Pentagon => "res://shader/Icon/RelicIcon/Pentagon.gdshader",
            RelicID.Hexagon => "res://shader/Icon/RelicIcon/Hexagon.gdshader",
            RelicID.Heptagon => "res://shader/Icon/RelicIcon/Heptagon.gdshader",
            RelicID.Octagon => "res://shader/Icon/RelicIcon/Octagon.gdshader",
            RelicID.CompressionCore => "res://shader/Icon/RelicIcon/CompressionCore.gdshader",
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
        playerResourceState.RelicList ??= new List<Relic>();
        playerResourceState.RelicList.Add(relic);
        GameInfo.Relics[relicID] = num;
    }

    public static int ApplyElectricityCoinBonus(int baseAmount)
    {
        if (baseAmount <= 0)
            return 0;

        if (GameInfo.Relics == null || !GameInfo.Relics.ContainsKey(RelicID.CompressionCore))
            return baseAmount;

        return Mathf.CeilToInt(baseAmount * 1.2f);
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
                    list.Add(battle.EnemiesList[i].GetHurt(BlessingDamage));
                await Task.WhenAll(list);
                Num--;
                break;
            case RelicID.Triangle:
                await ApplyEffectToFrontPlayers(battle, PropertyType.Survivability, 2);
                break;
            case RelicID.Square:
                await ApplyEffectToFrontPlayers(battle, PropertyType.Power, 2);
                break;
            case RelicID.Pentagon:
                await ApplyEffectToFrontPlayers(battle, PropertyType.Speed, 2);
                break;
            case RelicID.Hexagon:
                await ApplyHexagonEffect(battle);
                break;
            case RelicID.Heptagon:
                ApplyDebuffToEnemies(battle, Buff.BuffName.Vulnerable, 1);
                break;
            case RelicID.Octagon:
                ApplyDebuffToEnemies(battle, Buff.BuffName.Weaken, 1);
                break;
            case RelicID.CompressionCore:
            case RelicID.curse:
                break;
        }

        GameInfo.Relics[ID] = Num;
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
            RelicID.Triangle => "三角形",
            RelicID.Square => "正方形",
            RelicID.Pentagon => "正五边形",
            RelicID.Hexagon => "正六边形",
            RelicID.Heptagon => "七边形",
            RelicID.Octagon => "八边形",
            RelicID.CompressionCore => "压缩核心",
            _ => "诅咒",
        };
    }

    private static string GetRelicDescription(RelicID relicID)
    {
        return relicID switch
        {
            RelicID.Blessing => $"战斗开始时对所有敌人造成{BlessingDamage}伤害。",
            RelicID.Triangle => "战斗开始时第一位和第二位角色获得2点生存。",
            RelicID.Square => "战斗开始时第一位和第二位角色获得2点力量。",
            RelicID.Pentagon => "战斗开始时第一位和第二位角色获得2点速度。",
            RelicID.Hexagon => "战斗开始时第一位和第二位角色获得8点血量上限,并回复0点。",
            RelicID.Heptagon => "战斗开始时，敌方全阵获得1层易伤。",
            RelicID.Octagon => "战斗开始时，敌方全阵获得1层虚弱。",
            RelicID.CompressionCore => "获得的电力币增加20%。",
            _ => "暂无效果。",
        };
    }

    private static async Task ApplyEffectToFrontPlayers(
        Battle battle,
        PropertyType propertyType,
        int amount
    )
    {
        int targetCount = Math.Min(2, battle.PlayersList.Count);
        for (int i = 0; i < targetCount; i++)
        {
            var player = battle.PlayersList[i];
            if (player == null || player.State == Character.CharacterState.Dying)
                continue;

            await player.IncreaseProperties(propertyType, amount);
        }
    }

    private static async Task ApplyHexagonEffect(Battle battle)
    {
        int targetCount = Math.Min(2, battle.PlayersList.Count);
        for (int i = 0; i < targetCount; i++)
        {
            var player = battle.PlayersList[i];
            if (player == null || player.State == Character.CharacterState.Dying)
                continue;

            await player.IncreaseProperties(PropertyType.MaxLife, 8);
            player.Recover(0);
        }
    }

    private static void ApplyDebuffToEnemies(Battle battle, Buff.BuffName buffName, int stacks)
    {
        if (battle?.EnemiesList == null)
            return;

        for (int i = 0; i < battle.EnemiesList.Count; i++)
        {
            var enemy = battle.EnemiesList[i];
            if (enemy == null || enemy.State == Character.CharacterState.Dying)
                continue;

            switch (buffName)
            {
                case Buff.BuffName.Vulnerable:
                    HurtBuff.BuffAdd(buffName, enemy, stacks);
                    break;
                case Buff.BuffName.Weaken:
                    AttackBuff.BuffAdd(buffName, enemy, stacks);
                    break;
            }
        }
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
    Square,
    Pentagon,
    Hexagon,
    Heptagon,
    Octagon,
    CompressionCore,
    curse,
}
