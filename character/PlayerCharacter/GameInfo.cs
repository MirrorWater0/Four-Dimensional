using System;
using System.Collections.Generic;
using Godot;

public static partial class GameInfo
{
    public static PlayerInfoStructure[] PlayerCharacters;
    public static int Seed = 1223;
    public static int IntentionRandomNum { get; private set; }
    public static int PositionRandomNum { get; private set; }
    public static Dictionary<Vector2I, LevelNode.LevelState> FirstLevelState = new();

    public static void InitNewGame()
    {
        IntentionRandomNum = new Random(Seed).Next();
        PositionRandomNum = new Random(Seed).Next();
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
    public List<Skill> GainedSkills = new List<Skill>();
    public Skill[] TakenSkills = new Skill[3];
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
