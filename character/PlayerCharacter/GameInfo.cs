using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Text;
using Godot;

public static partial class GameInfo
{
    public static PlayerInfoStructure[] PlayerCharacters;
    public static int Seed = 1223;
    public static int ElectricityCoin;
    public static int TransitionEnergy;
    public static int TransitionEnergyMax;
    public static int CurrentLevel;
    public static int IntentionRandomNum { get; private set; }
    public static int PositionRandomNum { get; private set; }
    public static Dictionary<Vector2I, LevelNode.LevelState> FirstLevelState = new();
    public static Dictionary<RelicID, int> Relic = new();

    public static void InitNewGame()
    {
        ElectricityCoin = 100;
        TransitionEnergy = 6;
        TransitionEnergyMax = 6;
        FirstLevelState.Clear();
        // Map generation logic in LevelProgress will populate this
        GD.Print("InitNewGame");
    }

    public static void RefreshRandomNum(ref int num)
    {
        num = new Random(num).Next();
    }
}

public struct PlayerInfoStructure
{
    public PlayerInfoStructure() { }

    public string CharacterScenePath;
    public int LifeMax;
    public int Power;
    public int Survivability;
    public int Speed;
    public List<SkillID> GainedSkills = new();
    public SkillID[] TakenSkills = new SkillID[3];
    public SkillID[] AllSkills;
    public int PositionIndex;
    public string PortaitPath;
    public string CharacterName;
    public string PassiveName;
    public string PassiveDescription;
    public Equipment[] Equipments = new Equipment[2];
}

public static class GlobalFunction
{
    public static void TweenShader(Control node, string var, float val, float duration)
    {
        node.CreateTween()
            .TweenMethod(
                Callable.From<float>(value =>
                    ((ShaderMaterial)node.Material).SetShaderParameter(var, value)
                ),
                ((ShaderMaterial)node.Material).GetShaderParameter(var),
                val,
                duration
            );
    }

    public static void TweenShader(Node2D node, string var, float val, float duration)
    {
        node.CreateTween()
            .TweenMethod(
                Callable.From<float>(value =>
                    ((ShaderMaterial)node.Material).SetShaderParameter(var, value)
                ),
                ((ShaderMaterial)node.Material).GetShaderParameter(var),
                val,
                duration
            );
    }

    private const string NumberColor = "#ffff00";

    public static string ColorizeNumbers(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        // 仅高亮 BBCode 标签外的数字，避免破坏像 [color=#87CEEB] 这样的标签参数。
        StringBuilder builder = new StringBuilder(input.Length * 2);
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

                builder.Append($"[color={NumberColor}]");
                builder.Append(input, start, i - start);
                builder.Append("[/color]");
                i--;
                continue;
            }

            builder.Append(ch);
        }

        return builder.ToString();
    }

    public static string ColorizeKeywords(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        // Only process outside BBCode tags to avoid corrupting things like [color=#87CEEB].
        // "Cambridge Blue" is commonly represented as #A3C1AD.
        const string red = "#ff0000";
        const string cambridgeBlue = "#9cdacf";
        const string darkBlue = "#4444ef";

        StringBuilder builder = new StringBuilder(input.Length * 2);
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

            if (!inTag)
            {
                if (i + 2 <= input.Length)
                {
                    string two = input.Substring(i, 2);
                    string color = two switch
                    {
                        "伤害" => "#ffc9c9",
                        "格挡" => cambridgeBlue,
                        "能量" => "#c9cdff",
                        _ => null,
                    };

                    if (color != null)
                    {
                        builder.Append($"[color={color}]");
                        builder.Append(two);
                        builder.Append("[/color]");
                        i += 1;
                        continue;
                    }
                }
            }

            builder.Append(ch);
        }

        return builder.ToString();
    }
}

public class ObservableList<T> : List<T>
{
    public event Action<T> ItemAdded; // 新增元素事件
    public event Action<T> ItemRemoved; // 移除元素事件

    public new void Add(T item)
    {
        base.Add(item);
        ItemAdded?.Invoke(item); // 触发新增回调
    }

    public new void Remove(T item)
    {
        base.Remove(item);
        ItemRemoved?.Invoke(item); // 触发移除回调
    }

    public new void RemoveAt(int index)
    {
        if (index < 0 || index >= Count)
            return;
        T item = this[index];
        base.RemoveAt(index);
        ItemRemoved?.Invoke(item);
    }

    // 可扩展其他方法（如 Insert、Clear 等）
}

public static class EnumExtensions
{
    private const string BuffColor = "#87eb91";

    public static string GetDescription(this Enum value)
    {
        FieldInfo fi = value.GetType().GetField(value.ToString());
        DescriptionAttribute[] attributes =
            fi == null
                ? Array.Empty<DescriptionAttribute>()
                : (DescriptionAttribute[])
                    fi.GetCustomAttributes(typeof(DescriptionAttribute), false);

        // 获取特性中写的字符串（作为翻译 Key）
        string key = attributes.Length > 0 ? attributes[0].Description : value.ToString();

        // 【核心】：调用 Godot 的翻译函数 Tr()
        // 如果翻译表中能找到这个 Key，它会返回当前语言的文字；找不到则返回 Key 本身。
        string translated = TranslationServer.Translate(key);

        // Buff 名称默认蓝色高亮，便于在技能描述和提示中快速识别。
        if (value.GetType() == typeof(Buff.BuffName))
            return $"[color={BuffColor}]{translated}[/color]";

        return translated;
        // 在 Godot 4.x 中，通常也可以直接用 GodotObject.Tr(key)
    }
}
