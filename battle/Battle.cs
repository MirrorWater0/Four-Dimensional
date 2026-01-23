using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Godot;

public partial class Battle : Node2D
{
    public static bool Istest = true;

    [Signal]
    public delegate void NextEventHandler();

    PackedScene _test1 = (PackedScene)
        ResourceLoader.Load("res://character/EnemyCharacter/Demon.tscn");
    public List<PlayerCharacter> Players = new List<PlayerCharacter>();
    public List<EnemyTemplate> Enemies;
    public Node2D Right => field ??= GetNode("Right") as Node2D;
    public Node2D Left => field ??= GetNode("Left") as Node2D;

    public AnimationPlayer BattlePlayer => field ??= GetNode("BattlePlayer") as AnimationPlayer;
    private int _turn;
    public CharaterControl CharaterControl =>
        field ??= GetNode("CharaterControl") as CharaterControl;

    public int PlayerDyingNum;
    public int EnemiesDyingNum;

    public ObservableList<Skill> UsedSkills = new ObservableList<Skill>();
    public Button RetreatButton;

    public int PlayerSpeed;
    public int EnemySpeed;
    public Label PlayerSpeedLabel => field ??= GetNode("PlayerSpeed/Label") as Label;
    public Label EnemySpeedLabel => field ??= GetNode("EnemySpeed/Label") as Label;
    public override async void _Ready()
    {
        RetreatButton = GetNode("Retreat") as Button;
        RetreatButton.ButtonDown += Retreat;
        UsedSkills.Clear();

        for (int i = 0; i < GameInfo.PlayerCharacters.Length; i++)
        {
            PlayerCharacter character = GameInfo
                .PlayerCharacters[i]
                .CharacterScene.Instantiate<PlayerCharacter>();
            character.CharacterIndex = i;
            character.BattleNode = this;
            character.Initialize();
            Players.Add(character);
        }

        EnemyTemplate test1 = _test1.Instantiate<EnemyTemplate>();
        test1.PositionIndex = 1;
        EnemyTemplate test2 = _test1.Instantiate<EnemyTemplate>();
        test2.PositionIndex = 5;
        EnemyTemplate test4 = _test1.Instantiate<EnemyTemplate>();
        test4.PositionIndex = 9;
        Enemies = new List<EnemyTemplate>() { test1, test2, test4 };

        Players = Players.OrderBy(x => x.PositionIndex).ToList();
        Enemies = Enemies.OrderBy(x => x.PositionIndex).ToList();
        SetCharaterPostion(); //加入节点树

        for (int i = 0; i < Enemies.Count; i++)
        {
            Enemies[i].BattleNode = this;
            Enemies[i].Initialize();
        }

        await ToSignal(GetTree().CreateTimer(0.2f), "timeout");

        CharaterControl.DisableAll();
        for (int i = 0; i < Players.Count; i++)
        {
            GD.Print(Players[i].State);
        }
        await Task.Delay(1000);
        
        BattleBegin1();
    }

    public void SetCharaterPostion()
    {
        Vector2 gapy = new Vector2(0, 200);
        Vector2 gapx = new Vector2(280, 0);
        Vector2 xoffset = new Vector2(50, 0);
        for (int j = 0; j < Players.Count; j++)
        {
            int i = Players[j].PositionIndex;
            Left.AddChild(Players[j]);
            if (i <= 3)
            {
                Players[j].Position = (gapy + xoffset) * (i - 1);
            }
            else if (i <= 6)
            {
                Players[j].Position = -gapx + (gapy + xoffset) * (i - 4);
            }
            else
            {
                Players[j].Position = -2 * gapx + (gapy + xoffset) * (i - 7);
            }
            Players[j].OriginalPosition = Players[j].Position;
        }

        for (int j = 0; j < Enemies.Count; j++)
        {
            int i = Enemies[j].PositionIndex;
            Right.AddChild(Enemies[j]);
            if (i <= 3)
            {
                Enemies[j].Position = (gapy + xoffset) * (i - 1);
            }
            else if (i <= 6)
            {
                Enemies[j].Position = gapx + (gapy + xoffset) * (i - 4);
            }
            else
            {
                Enemies[j].Position = 2 * gapx + (gapy + xoffset) * (i - 7);
            }
            Enemies[j].OriginalPosition = Enemies[j].Position;
        }
    }

    public void EmitS()
    {
        EmitSignal(SignalName.Next);
    }

    public async Task BattleBegin1()
    {
        if (Players.Sum(x => x.Speed) < Enemies.Sum(x => x.Speed))
        {
            Enemies[0].StartAction();
            Enemies.Reverse(1, Enemies.Count - 1);
            Enemies.Reverse();
            await ToSignal(this, SignalName.Next);
            await Task.Delay(500);
        }

        for (int i = 0; i < 100; i++)
        {
            GD.Print("turn ", i);
            DyingDetector(Players);

            // Check if there are any alive players
            if (Players.Count > 0 && Players[0].State == Character.CharaterState.Normal)
            {
                Players[0].StartAction();
                Players.Reverse(1, Players.Count - 1);
                Players.Reverse();
            }

            await ToSignal(this, SignalName.Next);
            await Task.Delay(800);
            
            if (
                Players.All(x => x.State == Character.CharaterState.Dying)
                || Enemies.All(x => x.State == Character.CharaterState.Dying)
            )
            {
                GD.Print("over");
                Retreat();
                return;
            }

            

            DyingDetector(Enemies);

            // Check if there are any alive enemies
            if (Enemies.Count > 0)
            {
                Enemies[0].StartAction();
                GD.Print(Enemies[0]);
                Enemies.Reverse(1, Enemies.Count - 1);
                Enemies.Reverse();
            }

            await ToSignal(this, SignalName.Next);
            await Task.Delay(500);

            if (
                Players.All(x => x.State == Character.CharaterState.Dying)
                || Enemies.All(x => x.State == Character.CharaterState.Dying)
            )
            {
                
                Retreat();
                return;
            }
        }

        // Battle completed after 100 turns - retreat
        GD.Print("Battle completed after 100 turns");
        Retreat();
    }

    public void DyingDetector<T>(List<T> c)
        where T : Character
    {
        if (c.All(x => x.State == Character.CharaterState.Dying))
            return;
        while (c.Count > 0)
        {
            if (c[0].State == Character.CharaterState.Dying)
            {
                c.Reverse(1, c.Count - 1);
                c.Reverse();
                GD.Print("complate");
            }
            else
            {
                break;
            }
        }
    }

    public async void Retreat()
    {
        BattlePlayer.Play("end");
        
        // Disable retreat button to prevent multiple retreats
        if (RetreatButton != null)
        {
            RetreatButton.Disabled = true;
        }
        
        // Mark all players as dying
        for (int i = 0; i < Players.Count; i++)
        {
            if (Players[i] != null && IsInstanceValid(Players[i]))
            {
                Players[i].State = Character.CharaterState.Dying;
            }
        }

        EmitS();

        await Task.Delay(1000);
        
        // Remove all players from scene tree
        for (int i = 0; i < Players.Count; i++)
        {
            if (Players[i] != null && IsInstanceValid(Players[i]))
            {
                Players[i].State = Character.CharaterState.Normal;
                if (Left != null && Left.IsAncestorOf(Players[i]))
                {
                    Left.RemoveChild(Players[i]);
                }
            }
        }
        
        // Remove all enemies from scene tree
        for (int i = 0; i < Enemies.Count; i++)
        {
            if (Enemies[i] != null && IsInstanceValid(Enemies[i]))
            {
                if (Right != null && Right.IsAncestorOf(Enemies[i]))
                {
                    Right.RemoveChild(Enemies[i]);
                }
            }
        }
        
        // Clear lists
        Players.Clear();
        Enemies.Clear();
        
        // Change scene - check if Battle instance is still valid
        if (IsInstanceValid(this))
        {
            var tree = GetTree();
            if (tree != null)
            {
                tree.ChangeSceneToFile("res://Map/Map.tscn");
            }
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

        // 可扩展其他方法（如 Insert、Clear 等）
    }
}
