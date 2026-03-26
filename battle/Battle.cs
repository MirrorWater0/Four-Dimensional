using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Godot;

public partial class Battle : Node2D
{
    [Export]
    public bool WarmupMode { get; set; }

    public static bool Istest = true;
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
        set =>
            SetSpeedValue(ref _playerSpeed, value, PlayerSpeedLabel, PlayerSpeedBar, PlayersList);
    }

    public int EnemySpeed
    {
        get => _enemySpeed;
        set => SetSpeedValue(ref _enemySpeed, value, EnemySpeedLabel, EnemySpeedBar, EnemiesList);
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
    private const float FormationGapY = 140f;
    private const float FormationGapX = 280f;
    private const float FormationSkew = 10f;
    private const float FormationRowOffset = 100f;
    private const int MaxBattleTurns = 100;
    private const int PostActionDelayMs = 800;
    private const int BattleOverDelayMs = 5000;
    private const int PlayerSpeedHintDelayMs = 200;
    private const int EnemySpeedHintDelayMs = 400;
    private const int SpeedTriggerThreshold = 100;
    private const string SpeedTriggerText = "[color=yellow]超速触发[/color]";

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
            DisableBattleProcessing();
            return;
        }

        var token = _lifetimeCts.Token;
        InitDummy();
        InitializeBattleCharacters();
        InitializeBattleUi();
        SetCharaterPostion();
        CharacterControl.Connect();
        if (!await DelayOrCancel(PlayerSpeedHintDelayMs, token))
        {
            return;
        }

        CharacterControl.DisableAll();
        if (!await InitializeEnemyIntentions(token))
        {
            return;
        }

        PlayerSpeed = 0;
        EnemySpeed = 0;
        await ApplyRelicBattleEffects(token);
        await BattleBegin1(token);
    }

    private void DisableBattleProcessing()
    {
        SetProcess(false);
        SetProcessInput(false);
        SetPhysicsProcess(false);
    }

    private void InitializeBattleCharacters()
    {
        EnemiesList.Clear();
        foreach (var regedit in CurrentLevelNode.EnemiesRegeditList)
        {
            var enemy = regedit.CharacterScene.Instantiate<EnemyCharacter>();
            enemy.Registry = regedit;
            enemy.PositionIndex = regedit.PositionIndex;
            InitializeCharacter(enemy);
            EnemiesList.Add(enemy);
        }

        PlayersList.Clear();
        for (int i = 0; i < GameInfo.PlayerCharacters.Length; i++)
        {
            var character = GD.Load<PackedScene>(GameInfo.PlayerCharacters[i].CharacterScenePath)
                .Instantiate<PlayerCharacter>();
            character.CharacterIndex = i;
            InitializeCharacter(character);
            PlayersList.Add(character);
        }

        PlayersList = PlayersList.OrderBy(x => x.PositionIndex).ToList();
        EnemiesList = EnemiesList.OrderBy(x => x.PositionIndex).ToList();
    }

    private void InitializeCharacter(Character character)
    {
        character.BattleNode = this;
        character.Initialize();
    }

    private void InitializeBattleUi()
    {
        RetreatButton.ButtonDown += ManualRetreat;
        UsedSkills.Clear();
        UsedSkills.ItemAdded -= OnSkillUsed;
        UsedSkills.ItemAdded += OnSkillUsed;
        _recordIndex = 0;
        if (BattleRecord != null)
        {
            BattleRecord.Text = string.Empty;
        }
        InitRecordButton();
    }

    private async Task<bool> InitializeEnemyIntentions(CancellationToken token)
    {
        foreach (var enemy in EnemiesList)
        {
            if (!CanContinue(token))
            {
                return false;
            }

            enemy.IntentionIndex = enemy.RollIntentionIndex();
            await enemy.DisappearIntention();
            if (!CanContinue(token))
            {
                return false;
            }

            enemy.IntentionContorl.Visible = true;
            enemy.DisplayIntention();
        }

        return await DelayOrCancel(PostActionDelayMs, token);
    }

    private async Task ApplyRelicBattleEffects(CancellationToken token)
    {
        var relics = MapNode?.PlayerResourceState?.RelicList;
        if (relics == null)
        {
            return;
        }

        foreach (var relic in relics)
        {
            if (!CanContinue(token))
            {
                return;
            }

            await relic.BattleEffect(this);
        }
    }

    private void SetSpeedValue(
        ref int currentValue,
        int nextValue,
        GlowLabel label,
        ProgressBar bar,
        IEnumerable<Character> characters
    )
    {
        currentValue = Math.Max(nextValue, 0);
        if (!IsBattleAlive())
        {
            return;
        }

        if (GodotObject.IsInstanceValid(label))
        {
            label.Text = $"{currentValue}({SumAliveSpeed(characters)})";
        }

        if (GodotObject.IsInstanceValid(bar))
        {
            CreateTween().TweenProperty(bar, "value", currentValue, 0.3f);
        }
    }

    private static int SumAliveSpeed(IEnumerable<Character> characters) =>
        characters?.Where(IsCharacterAlive).Sum(x => x.Speed) ?? 0;

    private void OnSkillUsed(Skill skill)
    {
        var record = BattleRecord;
        if (skill == null || record == null)
        {
            return;
        }

        string characterName = string.IsNullOrWhiteSpace(skill.OwnerCharater?.CharacterName)
            ? skill.OwnerCharater?.Name ?? "Unknown"
            : skill.OwnerCharater.CharacterName;
        string skillName = string.IsNullOrWhiteSpace(skill.SkillName)
            ? skill.GetType().Name
            : skill.SkillName;
        const string nameColor = "#ffd36b";
        const string skillColor = "#b56bff";
        record.AppendText(
            $"[color=#b0b6c2]{++_recordIndex:00}[/color]  [color={nameColor}]{characterName}[/color]  释放  [color={skillColor}]{skillName}[/color]\n"
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
            InitRecordButton();
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
            return;
        record.OffsetLeft = left;
        record.OffsetRight = right;
    }

    public void SetCharaterPostion()
    {
        SetCharacterPositionGroup(PlayersList, Left, -1);
        SetCharacterPositionGroup(EnemiesList, Right, 1);
    }

    private void SetCharacterPositionGroup<T>(List<T> characters, Node container, int side)
        where T : Character
    {
        foreach (var character in characters)
        {
            int row = (character.PositionIndex - 1) % 3;
            ReparentCharacter(character, container);
            character.Position = GetFormationPosition(character.PositionIndex, side);
            character.OriginalPosition = character.Position;
            character.ZIndex = row;
        }
    }

    private static void ReparentCharacter(Character character, Node container)
    {
        if (character.GetParent() == container)
        {
            return;
        }

        character.GetParent()?.RemoveChild(character);
        container.AddChild(character);
    }

    private static Vector2 GetFormationPosition(int positionIndex, int side)
    {
        int row = (positionIndex - 1) % 3;
        int col = (positionIndex - 1) / 3;
        float xPos =
            col * FormationGapX * side - (row * FormationSkew - FormationRowOffset * (row - 1));
        return new Vector2(xPos, row * FormationGapY);
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

        if (!CanContinue(token))
        {
            return;
        }

        if (PlayersList.Sum(x => x.Speed) < EnemiesList.Sum(x => x.Speed))
        {
            await CharacterAction(EnemiesList, token);
        }

        for (int i = 0; i < MaxBattleTurns && CanContinue(token); i++)
        {
            await CharacterAction(PlayersList, token);
            if (!CanContinue(token))
            {
                return;
            }

            await CharacterAction(EnemiesList, token);
        }

        if (CanContinue(token))
        {
            GD.Print("Battle completed after 100 turns");
            Retreat();
        }
    }

    public async Task CharacterAction<T>(List<T> characterlist, CancellationToken token)
        where T : Character
    {
        if (!CanAct(characterlist, token))
        {
            return;
        }

        DyingDetector(characterlist);
        characterlist[0].StartAction();
        RotateFrontToBack(characterlist);
        await ToSignal(this, SignalName.Next);
        if (!await DelayOrCancel(PostActionDelayMs, token) || await HandleBattleOver(token))
        {
            return;
        }

        await TryTriggerSpeedBurst(
            PlayersList,
            () => PlayerSpeed,
            value => PlayerSpeed = value,
            PlayerSpeedHintDelayMs,
            token
        );
        if (CanContinue(token))
        {
            await TryTriggerSpeedBurst(
                EnemiesList,
                () => EnemySpeed,
                value => EnemySpeed = value,
                EnemySpeedHintDelayMs,
                token
            );
        }
    }

    public void DyingDetector<T>(List<T> c)
        where T : Character
    {
        if (c == null || c.Count == 0 || IsTeamDefeated(c))
        {
            return;
        }

        while (c[0].State == Character.CharacterState.Dying)
        {
            RotateFrontToBack(c);
        }
    }

    private void ManualRetreat()
    {
        if (CanManualRetreat())
        {
            Retreat(consumeTransitionEnergy: true);
        }
    }

    public bool CanManualRetreat() =>
        !_retreating
        && (MapNode?.PlayerResourceState?.TransitionEnergy ?? GameInfo.TransitionEnergy) > 0;

    public async void Retreat(bool consumeTransitionEnergy = false)
    {
        if (_retreating)
        {
            return;
        }

        _retreating = true;
        TryCancelLifetime();
        TryEmitNextToUnblock();
        if (!IsBattleInstanceValid())
        {
            return;
        }

        if (RetreatButton != null)
        {
            RetreatButton.Disabled = true;
        }

        if (consumeTransitionEnergy)
        {
            ConsumeRetreatTransitionEnergy();
        }

        MapNode?.BlackMaskAnimation(0.8f);
        await Task.Delay(PostActionDelayMs);

        if (!IsBattleInstanceValid())
        {
            return;
        }

        bool isWin = IsTeamDefeated(EnemiesList) && HasLivingMember(PlayersList);
        if (isWin || Istest)
        {
            var reward = Reward.Show(this);
            ConfigureRewards(reward);
            if (CurrentLevelNode != null)
            {
                reward.SetCompleteNodeOnClose(CurrentLevelNode);
            }
        }

        PlayersList?.Clear();
        EnemiesList?.Clear();

        if (IsBattleInstanceValid())
        {
            GetParent()?.QueueFree();
        }
    }

    private bool CanContinue(CancellationToken token) =>
        !token.IsCancellationRequested && IsBattleAlive();

    private bool CanAct<T>(List<T> characters, CancellationToken token)
        where T : Character => characters is { Count: > 0 } && CanContinue(token);

    private async Task<bool> HandleBattleOver(CancellationToken token)
    {
        if (!IsTeamDefeated(PlayersList) && !IsTeamDefeated(EnemiesList))
        {
            return false;
        }

        GD.Print("over");
        Retreat();
        await DelayOrCancel(BattleOverDelayMs, token);
        return true;
    }

    private async Task TryTriggerSpeedBurst<T>(
        List<T> team,
        Func<int> getSpeed,
        Action<int> setSpeed,
        int delayMs,
        CancellationToken token
    )
        where T : Character
    {
        if (team == null || team.Count == 0 || getSpeed() < SpeedTriggerThreshold)
        {
            return;
        }

        setSpeed(getSpeed() - SpeedTriggerThreshold);
        if (!await DelayOrCancel(delayMs, token) || !CanContinue(token))
        {
            return;
        }

        team[0].AddChild(CreateSpeedTriggerHint());
        SuppressSpeedGainThisTurn = true;
        try
        {
            await CharacterAction(team, token);
        }
        finally
        {
            SuppressSpeedGainThisTurn = false;
        }
    }

    private static BuffHintLabel CreateSpeedTriggerHint()
    {
        var label = Buff.HintScene.Instantiate<BuffHintLabel>();
        label.TargetPosition = Vector2.Zero;
        label.Text = SpeedTriggerText;
        return label;
    }

    private static void RotateFrontToBack<T>(List<T> characters)
    {
        if (characters.Count <= 1)
            return;
        characters.Reverse(1, characters.Count - 1);
        characters.Reverse();
    }

    private static bool IsTeamDefeated<T>(IEnumerable<T> characters)
        where T : Character =>
        characters?.Any() == true && characters.All(character => !IsCharacterAlive(character));

    private static bool HasLivingMember<T>(IEnumerable<T> characters)
        where T : Character => characters?.Any(IsCharacterAlive) == true;

    private static bool IsCharacterAlive(Character character) =>
        character != null && character.State != Character.CharacterState.Dying;

    private void ConsumeRetreatTransitionEnergy()
    {
        var resourceState = MapNode?.PlayerResourceState;
        if (resourceState != null)
        {
            resourceState.TransitionEnergy = Math.Max(0, resourceState.TransitionEnergy - 1);
            return;
        }

        GameInfo.TransitionEnergy = Math.Clamp(
            GameInfo.TransitionEnergy - 1,
            0,
            GameInfo.TransitionEnergyMax
        );
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
            InitializeCharacter(EnemiesList[i]);
        }
    }

    private void ConfigureRewards(Reward reward)
    {
        if (reward == null)
            return;

        reward.ClearRewardItems();
        reward.AddSkillRewardEntry();

        var rng = new Random(CurrentLevelNode?.RandomNum ?? System.Environment.TickCount);
        var (addRelic, equipCount) = GetRewardConfig(
            CurrentLevelNode?.Type ?? LevelNode.LevelType.Normal,
            rng
        );

        TryAddRelicReward(reward, rng, addRelic);
        AddEquipmentRewards(reward, rng, equipCount);
        TryAddItemReward(reward, rng);
    }

    private static (bool AddRelic, int EquipCount) GetRewardConfig(
        LevelNode.LevelType levelType,
        Random rng
    )
    {
        return levelType switch
        {
            LevelNode.LevelType.Boss => (true, 2),
            LevelNode.LevelType.Elite => (true, 1),
            LevelNode.LevelType.Event => (rng.Next(100) < 50, rng.Next(100) < 50 ? 1 : 0),
            _ => (false, rng.Next(100) < 10 ? 1 : 0),
        };
    }

    private static void TryAddRelicReward(Reward reward, Random rng, bool addRelic)
    {
        if (!addRelic)
        {
            return;
        }

        var relicDropPool = Relic.GetUnownedOfferPool();
        if (relicDropPool.Length > 0)
        {
            reward.AddRelicRewardEntry(PickRandom(relicDropPool, rng));
        }
    }

    private static void AddEquipmentRewards(Reward reward, Random rng, int equipCount)
    {
        for (int i = 0; i < equipCount; i++)
        {
            reward.AddEquipmentRewardEntry(Equipment.Clone(PickRandom(Equipment.Catalog, rng)));
        }
    }

    private static void TryAddItemReward(Reward reward, Random rng)
    {
        if (rng.Next(100) >= 30)
        {
            return;
        }

        ItemID[] itemPool = [ItemID.Health, ItemID.Explosion];
        reward.AddItemRewardEntry(PickRandom(itemPool, rng));
    }

    private static T PickRandom<T>(IReadOnlyList<T> pool, Random rng) => pool[rng.Next(pool.Count)];

    private bool IsBattleInstanceValid() =>
        _battleInstanceId != 0 && GodotObject.IsInstanceIdValid(_battleInstanceId);

    private bool IsBattleAlive() =>
        !_retreating && !_lifetimeCts.IsCancellationRequested && IsBattleInstanceValid();

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
            return;
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
        catch (OperationCanceledException)
        {
            return false;
        }
    }
}
