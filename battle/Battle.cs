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
    public List<PlayerCharacter> PlayersList = new();
    public List<EnemyCharacter> EnemiesList;
    public Node2D Right => field ??= GetNode("Right") as Node2D;
    public Node2D Left => field ??= GetNode("Left") as Node2D;

    public AnimationPlayer BattleAnimationPlayer =>
        field ??= GetNode("BattlePlayer") as AnimationPlayer;
    private int _turn;
    public CharaterControl CharaterControl =>
        field ??= GetNode("CharaterControl") as CharaterControl;

    public int PlayerDyingNum;
    public int EnemiesDyingNum;

    public ObservableList<Skill> UsedSkills = new ObservableList<Skill>();
    public Button RetreatButton;

    private int _playerSpeed = 0;
    private int _enemySpeed = 0;

    public int PlayerSpeed
    {
        get => _playerSpeed;
        set
        {
            if (_playerSpeed != value)
            {
                _playerSpeed = Math.Clamp(value, 0, 100);
                PlayerSpeedLabel.Text =
                    _playerSpeed.ToString()
                    + "("
                    + PlayersList
                        .Where(x => x.State != Character.CharaterState.Dying)
                        .Sum(x => x.Speed)
                    + ")";
                CreateTween().TweenProperty(PlayerSpeedBar, "value", _playerSpeed, 0.3f);
            }
        }
    }

    public int EnemySpeed
    {
        get => _enemySpeed;
        set
        {
            if (_enemySpeed != value)
            {
                _enemySpeed = Math.Clamp(value, 0, 100);
                EnemySpeedLabel.Text =
                    _enemySpeed.ToString()
                    + "("
                    + EnemiesList
                        .Where(x => x.State != Character.CharaterState.Dying)
                        .Sum(x => x.Speed)
                    + ")";
                CreateTween().TweenProperty(EnemySpeedBar, "value", _enemySpeed, 0.3f);
            }
        }
    }

    public GlowLabel PlayerSpeedLabel =>
        field ??= GetNode("SpeedBox/PlayerSpeed/Label") as GlowLabel;
    public GlowLabel EnemySpeedLabel => field ??= GetNode("SpeedBox/EnemySpeed/Label") as GlowLabel;
    public ProgressBar PlayerSpeedBar => field ??= GetNode("SpeedBox/PlayerSpeed") as ProgressBar;
    public ProgressBar EnemySpeedBar => field ??= GetNode("SpeedBox/EnemySpeed") as ProgressBar;

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
            PlayersList.Add(character);
        }

        EnemyCharacter test1 = _test1.Instantiate<EnemyCharacter>();
        test1.PositionIndex = 1;
        EnemyCharacter test2 = _test1.Instantiate<EnemyCharacter>();
        test2.PositionIndex = 2;
        EnemyCharacter test4 = _test1.Instantiate<EnemyCharacter>();
        test4.PositionIndex = 3;
        EnemiesList = new() { test1, test2, test4 };

        PlayersList = PlayersList.OrderBy(x => x.PositionIndex).ToList();
        EnemiesList = EnemiesList.OrderBy(x => x.PositionIndex).ToList();
        SetCharaterPostion(); //加入节点树

        for (int i = 0; i < EnemiesList.Count; i++)
        {
            EnemiesList[i].BattleNode = this;
            EnemiesList[i].Initialize();
        }

        await ToSignal(GetTree().CreateTimer(0.2f), "timeout");

        CharaterControl.DisableAll();
        for (int i = 0; i < PlayersList.Count; i++)
        {
            GD.Print(PlayersList[i].State);
        }

        await Task.Delay(1000);
        PlayerSpeed = 0;
        EnemySpeed = 0;
        PlayerSpeedLabel.Text = "(" + PlayersList.Sum(x => x.Speed) + ")";
        EnemySpeedLabel.Text = "(" + EnemiesList.Sum(x => x.Speed) + ")";

        BattleBegin1();
    }

    public void SetCharaterPostion()
    {
        // 你的核心基准参数
        float bGapY = 200f; // 纵向行距
        float bGapX = 280f; // 横向列距
        float bSkew = 50f; // 每一行的水平偏移 (xoffset)

        void ProcessList<T>(List<T> list, Node container, int side)
            where T : Character
        {
            for (int j = 0; j < list.Count; j++)
            {
                T c = list[j];
                // 映射行列：row (0,1,2), col (0,1,2)
                int row = (c.PositionIndex - 1) % 3;
                int col = (c.PositionIndex - 1) / 3;

                // 1. 节点层级管理
                if (c.GetParent() != container)
                {
                    c.GetParent()?.RemoveChild(c);
                    container.AddChild(c);
                }

                // 3. 基础位置计算 (平行四边形逻辑)
                // x = (列间距 + 行偏移) * 阵营系数
                float xPos = (col * bGapX + row * bSkew - 100 * (row - 1)) * side;
                float yPos = row * bGapY;

                c.Position = new Vector2(xPos, yPos);
                c.OriginalPosition = c.Position;
                c.ZIndex = row; // 确保前排遮挡后排

                // 4. 同步方框 Shader 参数 (让斜度完全匹配)
                // var colorRect = c.GetNodeOrNull<ColorRect>("ColorRect");
                // if (colorRect?.Material is ShaderMaterial mat)
                // {
                //     // 斜率固定为 50/200 = 0.25
                //     float skewVal = (bSkew / bGapY) * side;
                //     mat.SetShaderParameter("skew_strength", skewVal);

                //     // 平行四边形要求：顶边宽度和底边宽度完全一致
                //     float boxWidth = 0.5f;
                //     mat.SetShaderParameter("base_top_width", boxWidth);
                //     mat.SetShaderParameter("base_bottom_width", boxWidth);

                //     // 垂直位置对齐 (根据你的 ColorRect 大小调整)
                //     mat.SetShaderParameter("base_top_y", 0.5f);
                //     mat.SetShaderParameter("base_bottom_y", 0.8f);
                // }
            }
        }

        ProcessList(PlayersList, Left, -1);
        ProcessList(EnemiesList, Right, -1);
    }

    public void EmitS()
    {
        EmitSignal(SignalName.Next);
    }

    public async Task BattleBegin1()
    {
        if (PlayersList.Sum(x => x.Speed) < EnemiesList.Sum(x => x.Speed))
        {
            await CharacterAction(EnemiesList);
        }

        for (int i = 0; i < 100; i++)
        {
            await CharacterAction(PlayersList);
            await CharacterAction(EnemiesList);
        }

        // Battle completed after 100 turns - retreat
        GD.Print("Battle completed after 100 turns");
        Retreat();
    }

    public async Task CharacterAction<T>(List<T> characterlist)
        where T : Character
    {
        DyingDetector(characterlist);
        characterlist[0].StartAction();

        characterlist.Reverse(1, characterlist.Count - 1);
        characterlist.Reverse();
        await ToSignal(this, SignalName.Next);
        await Task.Delay(800);

        if (
            PlayersList.All(x => x.State == Character.CharaterState.Dying)
            || EnemiesList.All(x => x.State == Character.CharaterState.Dying)
        )
        {
            GD.Print("over");
            Retreat();
            await Task.Delay(5000);
        }

        if (PlayerSpeed == 100)
        {
            PlayerSpeed = 0;
            BuffHintLabel label = Buff.HintScene.Instantiate() as BuffHintLabel;
            label.Text = "[color=yellow]超速触发[/color]";
            PlayersList[PlayersList.Count - 1].AddChild(label);
            await CharacterAction(PlayersList);
        }

        if (EnemySpeed == 100)
        {
            EnemySpeed = 0;

            BuffHintLabel label = Buff.HintScene.Instantiate() as BuffHintLabel;
            label.Position = Vector2.Zero;
            label.Text = "[color=yellow]超速触发[/color]";
            EnemiesList[EnemiesList.Count - 1].AddChild(label);
            await CharacterAction(EnemiesList);
        }
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
        BattleAnimationPlayer.Play("end");

        // Disable retreat button to prevent multiple retreats
        if (RetreatButton != null)
        {
            RetreatButton.Disabled = true;
        }

        // Mark all players as dying
        for (int i = 0; i < PlayersList.Count; i++)
        {
            if (PlayersList[i] != null && IsInstanceValid(PlayersList[i]))
            {
                PlayersList[i].State = Character.CharaterState.Dying;
            }
        }

        EmitS();

        await Task.Delay(800);

        // Remove all players from scene tree
        for (int i = 0; i < PlayersList.Count; i++)
        {
            if (PlayersList[i] != null && IsInstanceValid(PlayersList[i]))
            {
                PlayersList[i].State = Character.CharaterState.Normal;
                if (Left != null && Left.IsAncestorOf(PlayersList[i]))
                {
                    Left.RemoveChild(PlayersList[i]);
                }
            }
        }

        // Remove all enemies from scene tree
        for (int i = 0; i < EnemiesList.Count; i++)
        {
            if (EnemiesList[i] != null && IsInstanceValid(EnemiesList[i]))
            {
                if (Right != null && Right.IsAncestorOf(EnemiesList[i]))
                {
                    Right.RemoveChild(EnemiesList[i]);
                }
            }
        }

        // Clear lists
        PlayersList.Clear();
        EnemiesList.Clear();

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
