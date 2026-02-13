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
    public Random BattleIntentionRandom;

    [Signal]
    public delegate void NextEventHandler();

    PackedScene _test1 = (PackedScene)
        ResourceLoader.Load("res://character/EnemyCharacter/Evil.tscn");
    public List<PlayerCharacter> PlayersList = new();
    public List<EnemyCharacter> EnemiesList = new();
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
            _playerSpeed = Math.Clamp(value, 0, 100);
            PlayerSpeedLabel.Text =
                _playerSpeed.ToString()
                + "("
                + PlayersList
                    .Where(x => x.State != Character.CharacterState.Dying)
                    .Sum(x => x.Speed)
                + ")";
            CreateTween().TweenProperty(PlayerSpeedBar, "value", _playerSpeed, 0.3f);
        }
    }

    public int EnemySpeed
    {
        get => _enemySpeed;
        set
        {
            _enemySpeed = Math.Clamp(value, 0, 100);
            EnemySpeedLabel.Text =
                _enemySpeed.ToString()
                + "("
                + EnemiesList
                    .Where(x => x.State != Character.CharacterState.Dying)
                    .Sum(x => x.Speed)
                + ")";
            CreateTween().TweenProperty(EnemySpeedBar, "value", _enemySpeed, 0.3f);
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
            PlayerCharacter character = GD.Load<PackedScene>(
                    GameInfo.PlayerCharacters[i].CharacterScenePath
                )
                .Instantiate<PlayerCharacter>();
            character.CharacterIndex = i;
            character.BattleNode = this;
            character.Initialize();
            PlayersList.Add(character);
        }

        for (int i = 0; i < LevelNode.EnemiesRegeditList.Count; i++)
        {
            EnemyCharacter enemy = LevelNode
                .EnemiesRegeditList[i]
                .CharacterScene.Instantiate<EnemyCharacter>();
            enemy.PositionIndex = LevelNode.EnemiesRegeditList[i].PositionIndex;
            enemy.BattleNode = this;
            enemy.Initialize();
            EnemiesList.Add(enemy);
        }

        if (EnemiesList == null)
        {
            EnemyCharacter test1 = _test1.Instantiate<EnemyCharacter>();
            EnemyCharacter test2 = _test1.Instantiate<EnemyCharacter>();
            EnemyCharacter test4 = _test1.Instantiate<EnemyCharacter>();
            EnemiesList = new() { test1, test2, test4 };
        }

        PlayersList = PlayersList.OrderBy(x => x.PositionIndex).ToList();
        EnemiesList = EnemiesList.OrderBy(x => x.PositionIndex).ToList();
        SetCharaterPostion(); //加入节点树

        await ToSignal(GetTree().CreateTimer(0.2f), "timeout");

        CharaterControl.DisableAll();
        for (int i = 0; i < EnemiesList.Count; i++)
        {
            var enemy = EnemiesList[i];
            enemy.IntentionIndex = BattleIntentionRandom.Next(0, enemy.Skills.Length);
            await enemy.DisappearIntention();
            enemy.IntentionContorl.Visible = true;
            enemy.DisplayIntention();
        }

        await Task.Delay(800);
        PlayerSpeed = 0;
        EnemySpeed = 0;

        await BattleBegin1();
    }

    public void SetCharaterPostion()
    {
        // 你的核心基准参数
        float bGapY = 160f; // 纵向行距
        float bGapX = 280f; // 横向列距
        float bSkew = 10f; // 每一行的水平偏移 (xoffset)

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
                float xPos = col * bGapX * side - (row * bSkew - 100 * (row - 1));
                float yPos = row * bGapY;

                c.Position = new Vector2(xPos, yPos);
                c.OriginalPosition = c.Position;
                c.ZIndex = row; // 确保前排遮挡后排
            }
        }

        ProcessList(PlayersList, Left, -1);
        ProcessList(EnemiesList, Right, 1);
    }

    public void EmitS()
    {
        EmitSignal(SignalName.Next);
    }

    public async Task BattleBegin1()
    {
        // Null checks for lists
        if (PlayersList == null || EnemiesList == null)
        {
            return;
        }

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
        // Null check for characterlist
        if (characterlist == null || characterlist.Count == 0)
        {
            return;
        }

        DyingDetector(characterlist);
        characterlist[0].StartAction();

        characterlist.Reverse(1, characterlist.Count - 1);
        characterlist.Reverse();
        await ToSignal(this, SignalName.Next);
        await Task.Delay(800);

        // Null checks for lists before accessing
        if (PlayersList != null && EnemiesList != null)
        {
            if (
                PlayersList.All(x => x.State == Character.CharacterState.Dying)
                || EnemiesList.All(x => x.State == Character.CharacterState.Dying)
            )
            {
                GD.Print("over");
                Retreat();
                await Task.Delay(5000);
            }
        }

        if (PlayerSpeed == 100)
        {
            PlayerSpeed = 0;
            BuffHintLabel label = Buff.HintScene.Instantiate() as BuffHintLabel;
            label.TargetPosition = Vector2.Zero;
            label.Text = "[color=yellow]超速触发[/color]";
            if (PlayersList != null && PlayersList.Count > 0)
            {
                PlayersList[PlayersList.Count - 1].AddChild(label);
                await CharacterAction(PlayersList);
            }
        }

        if (EnemySpeed == 100)
        {
            EnemySpeed = 0;

            BuffHintLabel label = Buff.HintScene.Instantiate() as BuffHintLabel;
            label.TargetPosition = Vector2.Zero;
            label.Text = "[color=yellow]超速触发[/color]";
            if (EnemiesList != null && EnemiesList.Count > 0)
            {
                EnemiesList[EnemiesList.Count - 1].AddChild(label);
                await CharacterAction(EnemiesList);
            }
        }
    }

    public void DyingDetector<T>(List<T> c)
        where T : Character
    {
        // Null check for list
        if (c == null || c.Count == 0)
        {
            return;
        }

        if (c.All(x => x.State == Character.CharacterState.Dying))
            return;
        while (c.Count > 0)
        {
            if (c[0].State == Character.CharacterState.Dying)
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
        // Check if Battle instance is still valid before proceeding
        if (!IsInstanceValid(this))
        {
            return;
        }

        // Check if lists are initialized
        if (PlayersList == null || EnemiesList == null)
        {
            return;
        }

        BattleAnimationPlayer?.Play("end");

        // Disable retreat button to prevent multiple retreats
        if (RetreatButton != null)
        {
            RetreatButton.Disabled = true;
        }

        await Task.Delay(800);

        // Clear lists
        PlayersList.Clear();
        EnemiesList.Clear();

        // Change scene - check if Battle instance is still valid
        if (IsInstanceValid(this))
        {
            GetParent().QueueFree();
        }
    }
}
