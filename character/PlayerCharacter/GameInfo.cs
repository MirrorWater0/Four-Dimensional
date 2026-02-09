using System;
using System.Collections.Generic;
using Godot;

public static partial class GameInfo
{
    public static PlayerInfoStructure[] PlayerCharacters;
    public static int Seed = 1113;
    public static Random IntentionRandom = new Random(Seed);
    public static Dictionary<Vector2I, LevelNode.LevelState> FirstLevelState = new();

    public static void InitNewGame()
    {
        FirstLevelState.Clear();
        for (int y = 0; y < 3; y++)
        {
            for (int x = 0; x < 7; x++)
            {
                FirstLevelState[new Vector2I(x, y)] = LevelNode.LevelState.Locked;
            }
        }
        FirstLevelState[new Vector2I(0, 0)] = LevelNode.LevelState.Unlocked;
        FirstLevelState[new Vector2I(0, 1)] = LevelNode.LevelState.Unlocked;
        FirstLevelState[new Vector2I(0, 2)] = LevelNode.LevelState.Unlocked;
        GD.Print("InitNewGame");
    }
}

public struct PlayerInfoStructure
{
    public PlayerInfoStructure() { }

    public PackedScene CharacterScene;
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
        if (index < 0 || index >= Count) return;
        T item = this[index];
        base.RemoveAt(index);
        ItemRemoved?.Invoke(item);
    }

    // 可扩展其他方法（如 Insert、Clear 等）
}
