using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
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

    public String CharacterScenePath;
    public int LifeMax;
    public int Power;
    public int Survivability;
    public int Speed;
    public List<SkillID> GainedSkills = new();
    public SkillID[] TakenSkills = new SkillID[3];
    public int PositionIndex;
    public string PortaitPath;
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
    public static string GetDescription(this Enum value)
    {
        FieldInfo fi = value.GetType().GetField(value.ToString());
        DescriptionAttribute[] attributes = (DescriptionAttribute[])
            fi.GetCustomAttributes(typeof(DescriptionAttribute), false);

        // 获取特性中写的字符串（作为翻译 Key）
        string key = attributes.Length > 0 ? attributes[0].Description : value.ToString();

        // 【核心】：调用 Godot 的翻译函数 Tr()
        // 如果翻译表中能找到这个 Key，它会返回当前语言的文字；找不到则返回 Key 本身。
        return TranslationServer.Translate(key);
        // 在 Godot 4.x 中，通常也可以直接用 GodotObject.Tr(key)
    }
}
