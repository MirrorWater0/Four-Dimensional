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
    [Export]
    public bool WarmupMode { get; set; }

    public static bool Istest = false;
    public Random BattleIntentionRandom;
    private readonly CancellationTokenSource _lifetimeCts = new();
    private ulong _battleInstanceId;
    private bool _retreating;

    [Signal]
    public delegate void NextEventHandler(Character character);

    PackedScene _test1 = (PackedScene)
        ResourceLoader.Load("res://character/EnemyCharacter/Evil.tscn");
    public Map MapNode => field ??= GetNodeOrNull<Map>("/root/Map");
    public List<PlayerCharacter> PlayersList = new();
    public List<EnemyCharacter> EnemiesList = new();
    public Node2D Right => field ??= GetNode<Node2D>("Right");
    public Node2D Left => field ??= GetNode<Node2D>("Left");

    public AnimationPlayer BattleAnimationPlayer =>
        field ??= GetNode<AnimationPlayer>("BattlePlayer");
    private int _turn;
    public CharacterControl CharacterControl =>
        field ??= GetNode<CharacterControl>("CharacterControl");

    public ObservableList<Skill> UsedSkills = new ObservableList<Skill>();
    public Button RetreatButton => field ??= GetNode<Button>("Retreat");
    public bool SuppressSpeedGainThisTurn { get; set; }

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

    public GlowLabel PlayerSpeedLabel =>
        field ??= GetNodeOrNull<GlowLabel>("SpeedBox/PlayerSpeed/Label");
    public GlowLabel EnemySpeedLabel =>
        field ??= GetNodeOrNull<GlowLabel>("SpeedBox/EnemySpeed/Label");
    public ProgressBar PlayerSpeedBar =>
        field ??= GetNodeOrNull<ProgressBar>("SpeedBox/PlayerSpeed");
    public ProgressBar EnemySpeedBar => field ??= GetNodeOrNull<ProgressBar>("SpeedBox/EnemySpeed");
    public LevelNode CurrentLevelNode;
    public Character dummy => field ??= GetNode<Character>("Dummy");
    public RichTextLabel BattleRecord => field ??= GetNodeOrNull<RichTextLabel>("UI/BattleRecord");
    public Button RecordButton => field ??= GetNodeOrNull<Button>("UI/RecordButton");

    private const float RecordSlideDuration = 0.2f;
    private const float RecordHideMargin = 18f;
    private bool _recordInitialized;
    private bool _recordVisible;
    private float _recordVisibleLeft;
    private float _recordVisibleRight;
    private float _recordHiddenLeft;
    private float _recordHiddenRight;
    private Tween _recordTween;
    private int _recordIndex;

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
        if (WarmupMode)
        {
            SetProcess(false);
            SetProcessInput(false);
            SetPhysicsProcess(false);
            return;
        }

        var token = _lifetimeCts.Token;
        InitDummy();
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
        if (BattleRecord != null)
        {
            BattleRecord.Text = string.Empty;
        }
        _recordIndex = 0;
        InitRecordButton();
        UsedSkills.ItemAdded -= OnSkillUsed;
        UsedSkills.ItemAdded += OnSkillUsed;

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

        var relics = MapNode.PlayerResourceState.RelicList;
        for (int i = 0; i < relics.Count; i++)
        {
            await relics[i].BattleEffect(this);
        }
        await BattleBegin1(token);
    }

    private void OnSkillUsed(Skill skill)
    {
        if (skill == null)
        {
            return;
        }

        string characterName = skill.OwnerCharater?.CharacterName;
        if (string.IsNullOrWhiteSpace(characterName))
        {
            characterName = skill.OwnerCharater?.Name ?? "Unknown";
        }

        string skillName = string.IsNullOrWhiteSpace(skill.SkillName)
            ? skill.GetType().Name
            : skill.SkillName;

        var record = BattleRecord;
        if (record == null)
        {
            return;
        }
        _recordIndex++;
        const string nameColor = "#ffd36b";
        const string skillColor = "#b56bff";
        record.AppendText(
            $"[color=#b0b6c2]{_recordIndex:00}[/color]  [color={nameColor}]{characterName}[/color]  释放  [color={skillColor}]{skillName}[/color]\n"
        );
    }

    private void InitRecordButton()
    {
        if (_recordInitialized)
        {
            return;
        }

        var record = BattleRecord;
        var button = RecordButton;
        if (record == null || button == null)
        {
            return;
        }

        _recordVisibleLeft = record.OffsetLeft;
        _recordVisibleRight = record.OffsetRight;

        float width = _recordVisibleRight - _recordVisibleLeft;
        float shift = width + RecordHideMargin;
        _recordHiddenLeft = _recordVisibleLeft + shift;
        _recordHiddenRight = _recordVisibleRight + shift;

        button.Pressed += ToggleRecord;
        _recordVisible = false;
        SetRecordOffsets(_recordHiddenLeft, _recordHiddenRight);
        _recordInitialized = true;
    }

    private void ToggleRecord()
    {
        if (!_recordInitialized)
        {
            InitRecordButton();
        }

        ShowBattleRecord(!_recordVisible);
    }

    private void ShowBattleRecord(bool show)
    {
        var record = BattleRecord;
        if (record == null)
        {
            return;
        }

        _recordVisible = show;
        _recordTween?.Kill();
        _recordTween = CreateTween();
        _recordTween.SetParallel();
        _recordTween
            .TweenProperty(
                record,
                "offset_left",
                show ? _recordVisibleLeft : _recordHiddenLeft,
                RecordSlideDuration
            )
            .SetTrans(Tween.TransitionType.Sine)
            .SetEase(Tween.EaseType.Out);
        _recordTween
            .TweenProperty(
                record,
                "offset_right",
                show ? _recordVisibleRight : _recordHiddenRight,
                RecordSlideDuration
            )
            .SetTrans(Tween.TransitionType.Sine)
            .SetEase(Tween.EaseType.Out);
    }

    private void SetRecordOffsets(float left, float right)
    {
        var record = BattleRecord;
        if (record == null)
        {
            return;
        }

        record.OffsetLeft = left;
        record.OffsetRight = right;
    }

    public void SetCharaterPostion()
    {
        // 你的核心基准参数
        float bGapY = 140f; // 纵向行距
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

    public List<Func<Task>> StartEffectList = new();

    public async Task BattleBegin1(CancellationToken token)
    {
        for (int i = 0; i < StartEffectList.Count; i++)
        {
            await StartEffectList[i]();
        }
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
                SuppressSpeedGainThisTurn = true;
                try
                {
                    await CharacterAction(PlayersList, token);
                }
                finally
                {
                    SuppressSpeedGainThisTurn = false;
                }
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
                SuppressSpeedGainThisTurn = true;
                try
                {
                    await CharacterAction(EnemiesList, token);
                }
                finally
                {
                    SuppressSpeedGainThisTurn = false;
                }
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
            ConfigureRewards(reward);
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

    private void ConfigureRewards(Reward reward)
    {
        if (reward == null)
            return;

        reward.ClearRewardItems();
        reward.AddSkillRewardEntry();

        var rng = new Random(CurrentLevelNode.RandomNum);
        var levelType = CurrentLevelNode?.Type ?? LevelNode.LevelType.Normal;

        int equipCount;

        bool addRelic;
        switch (levelType)
        {
            case LevelNode.LevelType.Boss:
                addRelic = true;
                equipCount = 2;
                break;
            case LevelNode.LevelType.Elite:
                addRelic = true;
                equipCount = 1;
                break;
            case LevelNode.LevelType.Event:
                addRelic = rng.Next(0, 100) < 50;
                equipCount = rng.Next(0, 100) < 50 ? 1 : 0;
                break;
            default:
                addRelic = rng.Next(0, 100) < 20;
                equipCount = rng.Next(0, 100) < 10 ? 1 : 0;
                break;
        }

        if (addRelic)
            reward.AddRelicRewardEntry(RelicID.Blessing);

        if (equipCount > 0)
        {
            for (int i = 0; i < equipCount; i++)
            {
                var pick = Equipment.Catalog[rng.Next(0, Equipment.Catalog.Length)];
                reward.AddEquipmentRewardEntry(Equipment.Clone(pick));
            }
        }

        if (rng.Next(0, 100) < 30)
        {
            ItemID[] itemPool = { ItemID.Health, ItemID.Explosion };
            var itemPick = itemPool[rng.Next(0, itemPool.Length)];
            reward.AddItemRewardEntry(itemPick);
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

    private void InitDummy()
    {
        dummy.BattleNode = this;
        dummy.Visible = false;
        dummy.Position = new Vector2(10000, -10000);
        dummy.ConfigureCombatStats(
            dummy.BattlePower,
            dummy.BattleSurvivability,
            dummy.Speed,
            1_000_000_000
        );
        dummy.Skills = [new Skill(Skill.SkillTypes.Attack)];
        dummy.Initialize();
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
