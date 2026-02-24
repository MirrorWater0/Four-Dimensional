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
    private readonly CancellationTokenSource _lifetimeCts = new();
    private ulong _battleInstanceId;
    private bool _retreating;

    [Signal]
    public delegate void NextEventHandler(Character character);

    PackedScene _test1 = (PackedScene)
        ResourceLoader.Load("res://character/EnemyCharacter/Evil.tscn");
    Map MapNode => field ??= GetNodeOrNull<Map>("/root/Map");
    public List<PlayerCharacter> PlayersList = new();
    public List<EnemyCharacter> EnemiesList = new();
    public Node2D Right => field ??= GetNode("Right") as Node2D;
    public Node2D Left => field ??= GetNode("Left") as Node2D;

    public AnimationPlayer BattleAnimationPlayer =>
        field ??= GetNode("BattlePlayer") as AnimationPlayer;
    private int _turn;
    public CharacterControl CharacterControl =>
        field ??= GetNode("CharacterControl") as CharacterControl;

    public ObservableList<Skill> UsedSkills = new ObservableList<Skill>();
    public Button RetreatButton => field ??= GetNode("Retreat") as Button;

    private int _playerSpeed = 0;
    private int _enemySpeed = 0;

    public int PlayerSpeed
    {
        get => _playerSpeed;
        set
        {
            _playerSpeed = Math.Clamp(value, 0, 100);
            if (!IsBattleAlive())
            {
                return;
            }

            var label = PlayerSpeedLabel;
            if (GodotObject.IsInstanceValid(label))
            {
                int speedSum =
                    PlayersList
                        ?.Where(x => x.State != Character.CharacterState.Dying)
                        .Sum(x => x.Speed) ?? 0;
                label.Text = _playerSpeed + "(" + speedSum + ")";
            }

            var bar = PlayerSpeedBar;
            if (GodotObject.IsInstanceValid(bar))
            {
                CreateTween().TweenProperty(bar, "value", _playerSpeed, 0.3f);
            }
        }
    }

    public int EnemySpeed
    {
        get => _enemySpeed;
        set
        {
            _enemySpeed = Math.Clamp(value, 0, 100);
            if (!IsBattleAlive())
            {
                return;
            }

            var label = EnemySpeedLabel;
            if (GodotObject.IsInstanceValid(label))
            {
                int speedSum =
                    EnemiesList
                        ?.Where(x => x.State != Character.CharacterState.Dying)
                        .Sum(x => x.Speed) ?? 0;
                label.Text = _enemySpeed + "(" + speedSum + ")";
            }

            var bar = EnemySpeedBar;
            if (GodotObject.IsInstanceValid(bar))
            {
                CreateTween().TweenProperty(bar, "value", _enemySpeed, 0.3f);
            }
        }
    }

    private GlowLabel _playerSpeedLabel;
    private GlowLabel _enemySpeedLabel;
    private ProgressBar _playerSpeedBar;
    private ProgressBar _enemySpeedBar;

    public GlowLabel PlayerSpeedLabel
    {
        get
        {
            if (_playerSpeedLabel == null || !GodotObject.IsInstanceValid(_playerSpeedLabel))
            {
                _playerSpeedLabel = GetNodeOrNull<GlowLabel>("SpeedBox/PlayerSpeed/Label");
            }
            return _playerSpeedLabel;
        }
    }

    public GlowLabel EnemySpeedLabel
    {
        get
        {
            if (_enemySpeedLabel == null || !GodotObject.IsInstanceValid(_enemySpeedLabel))
            {
                _enemySpeedLabel = GetNodeOrNull<GlowLabel>("SpeedBox/EnemySpeed/Label");
            }
            return _enemySpeedLabel;
        }
    }

    public ProgressBar PlayerSpeedBar
    {
        get
        {
            if (_playerSpeedBar == null || !GodotObject.IsInstanceValid(_playerSpeedBar))
            {
                _playerSpeedBar = GetNodeOrNull<ProgressBar>("SpeedBox/PlayerSpeed");
            }
            return _playerSpeedBar;
        }
    }

    public ProgressBar EnemySpeedBar
    {
        get
        {
            if (_enemySpeedBar == null || !GodotObject.IsInstanceValid(_enemySpeedBar))
            {
                _enemySpeedBar = GetNodeOrNull<ProgressBar>("SpeedBox/EnemySpeed");
            }
            return _enemySpeedBar;
        }
    }
    public LevelNode CurrentLevelNode;

    public override void _EnterTree()
    {
        _battleInstanceId = GetInstanceId();
    }

    public override void _ExitTree()
    {
        _retreating = true;
        TryCancelLifetime();
    }

    public override async void _Ready()
    {
        var token = _lifetimeCts.Token;

        for (int i = 0; i < CurrentLevelNode.EnemiesRegeditList.Count; i++)
        {
            var regedit = CurrentLevelNode.EnemiesRegeditList[i];
            EnemyCharacter enemy = regedit.CharacterScene.Instantiate<EnemyCharacter>();
            enemy.Registry = regedit;
            enemy.PositionIndex = regedit.PositionIndex;
            enemy.BattleNode = this;
            enemy.Initialize();
            EnemiesList.Add(enemy);
        }

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

        PlayersList = PlayersList.OrderBy(x => x.PositionIndex).ToList();
        EnemiesList = EnemiesList.OrderBy(x => x.PositionIndex).ToList();
        SetCharaterPostion(); //加入节点树
        CharacterControl.Connect();
        await ToSignal(GetTree().CreateTimer(0.2f), "timeout");
        if (token.IsCancellationRequested || !IsBattleAlive())
        {
            return;
        }

        CharacterControl.DisableAll();
        for (int i = 0; i < EnemiesList.Count; i++)
        {
            if (token.IsCancellationRequested || !IsBattleAlive())
            {
                return;
            }

            var enemy = EnemiesList[i];
            enemy.IntentionIndex = BattleIntentionRandom.Next(0, enemy.Skills.Length);
            await enemy.DisappearIntention();
            if (token.IsCancellationRequested || !IsBattleAlive())
            {
                return;
            }
            enemy.IntentionContorl.Visible = true;
            enemy.DisplayIntention();
        }

        if (!await DelayOrCancel(800, token))
        {
            return;
        }
        PlayerSpeed = 0;
        EnemySpeed = 0;

        await BattleBegin1(token);
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

    public List<Func<Character, Task>> EmitList = new();

    public async Task EmitS(Character character)
    {
        for (int i = 0; i < EmitList.Count; i++)
        {
            await EmitList[i](character);
        }
        EmitSignal(SignalName.Next, character);
    }

    public async Task BattleBegin1(CancellationToken token)
    {
        // Null checks for lists
        if (token.IsCancellationRequested || !IsBattleAlive())
        {
            return;
        }

        if (PlayersList.Sum(x => x.Speed) < EnemiesList.Sum(x => x.Speed))
        {
            await CharacterAction(EnemiesList, token);
        }

        for (int i = 0; i < 100; i++)
        {
            if (token.IsCancellationRequested || !IsBattleAlive())
            {
                return;
            }

            await CharacterAction(PlayersList, token);
            if (token.IsCancellationRequested || !IsBattleAlive())
            {
                return;
            }
            await CharacterAction(EnemiesList, token);
        }

        // Battle completed after 100 turns - retreat
        GD.Print("Battle completed after 100 turns");
        Retreat();
    }

    public async Task CharacterAction<T>(List<T> characterlist, CancellationToken token)
        where T : Character
    {
        // Null check for characterlist
        if (characterlist == null || characterlist.Count == 0)
        {
            return;
        }

        if (token.IsCancellationRequested || !IsBattleAlive())
        {
            return;
        }

        DyingDetector(characterlist);
        characterlist[0].StartAction();

        characterlist.Reverse(1, characterlist.Count - 1);
        characterlist.Reverse();
        await ToSignal(this, SignalName.Next);
        if (token.IsCancellationRequested || !IsBattleAlive())
        {
            return;
        }
        if (!await DelayOrCancel(800, token))
        {
            return;
        }

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
                if (!await DelayOrCancel(5000, token))
                {
                    return;
                }
            }
        }

        if (PlayerSpeed == 100)
        {
            PlayerSpeed = 0;
            BuffHintLabel label = Buff.HintScene.Instantiate() as BuffHintLabel;
            label.TargetPosition = Vector2.Zero;
            label.Text = "[color=yellow]超速触发[/color]";
            await ToSignal(GetTree().CreateTimer(0.2f), "timeout");
            if (PlayersList != null && PlayersList.Count > 0)
            {
                PlayersList[0].AddChild(label);
                await CharacterAction(PlayersList, token);
            }
        }

        if (EnemySpeed == 100)
        {
            EnemySpeed = 0;

            BuffHintLabel label = Buff.HintScene.Instantiate() as BuffHintLabel;
            label.TargetPosition = Vector2.Zero;
            label.Text = "[color=yellow]超速触发[/color]";
            await ToSignal(GetTree().CreateTimer(0.4f), "timeout");
            if (EnemiesList != null && EnemiesList.Count > 0)
            {
                EnemiesList[0].AddChild(label);
                await CharacterAction(EnemiesList, token);
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
        if (_retreating)
        {
            return;
        }

        _retreating = true;
        TryCancelLifetime();
        TryEmitNextToUnblock();

        // Check if Battle instance is still valid before proceeding
        if (!IsBattleInstanceValid())
        {
            return;
        }

        // Check if lists are initialized
        if (PlayersList == null || EnemiesList == null)
        {
            return;
        }

        // Disable retreat button to prevent multiple retreats
        if (RetreatButton != null)
        {
            RetreatButton.Disabled = true;
        }
        MapNode?.BlackMaskAnimation(0.8f);
        await Task.Delay(800);

        if (!IsBattleInstanceValid())
        {
            return;
        }

        bool enemyAllDead =
            EnemiesList != null
            && EnemiesList.Count > 0
            && EnemiesList.All(x => x != null && x.State == Character.CharacterState.Dying);

        bool playerHasSurvivor =
            PlayersList != null
            && PlayersList.Count > 0
            && PlayersList.Any(x => x != null && x.State == Character.CharacterState.Normal);

        bool isWin = enemyAllDead && playerHasSurvivor;
        if (isWin || Istest)
        {
            var reward = Reward.Show(this);
            if ((isWin || Istest) && CurrentLevelNode != null)
            {
                reward.SetCompleteNodeOnClose(CurrentLevelNode);
            }
        }

        // Clear lists
        PlayersList.Clear();
        EnemiesList.Clear();

        // Change scene - check if Battle instance is still valid
        if (IsBattleInstanceValid())
        {
            GetParent()?.QueueFree();
        }
    }

    public void TestBattle()
    {
        EnemiesList =
        [
            _test1.Instantiate<EnemyCharacter>(),
            _test1.Instantiate<EnemyCharacter>(),
            _test1.Instantiate<EnemyCharacter>(),
        ];

        for (int i = 0; i < EnemiesList.Count; i++)
        {
            EnemiesList[i].PositionIndex = i + 1;
            EnemiesList[i].BattleNode = this;
            EnemiesList[i].Initialize();
        }
    }

    private bool IsBattleInstanceValid()
    {
        return _battleInstanceId != 0 && GodotObject.IsInstanceIdValid(_battleInstanceId);
    }

    private bool IsBattleAlive()
    {
        return !_retreating && !_lifetimeCts.IsCancellationRequested && IsBattleInstanceValid();
    }

    private void TryCancelLifetime()
    {
        try
        {
            _lifetimeCts.Cancel();
        }
        catch (ObjectDisposedException) { }
    }

    private void TryEmitNextToUnblock()
    {
        if (!IsBattleInstanceValid())
        {
            return;
        }

        try
        {
            EmitSignal(SignalName.Next);
        }
        catch (ObjectDisposedException) { }
    }

    private async Task<bool> DelayOrCancel(int milliseconds, CancellationToken token)
    {
        try
        {
            await Task.Delay(milliseconds, token);
            return true;
        }
        catch (TaskCanceledException)
        {
            return false;
        }
    }
}
