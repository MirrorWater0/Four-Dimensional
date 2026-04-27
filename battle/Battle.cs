using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Godot;

public partial class Battle : Node2D
{
    public static bool Istest = false;

    [Export]
    public bool WarmupMode { get; set; }

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
    public List<SummonCharacter> PlayerSummons = new();
    public List<SummonCharacter> EnemySummons = new();
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
    private int _playerActionCount = 0;
    private int _enemyActionCount = 0;
    private readonly Dictionary<ulong, int> _characterActionCounts = new();
    private readonly Dictionary<ulong, int> _pendingExtraActions = new();
    private readonly HashSet<ulong> _activeExtraActionCharacters = new();

    public int PlayerSpeed
    {
        get => _playerSpeed;
        set =>
            SetSpeedValue(
                ref _playerSpeed,
                value,
                PlayerSpeedLabel,
                PlayerSpeedBar,
                GetTeamCharacters(true)
            );
    }

    public int EnemySpeed
    {
        get => _enemySpeed;
        set =>
            SetSpeedValue(
                ref _enemySpeed,
                value,
                EnemySpeedLabel,
                EnemySpeedBar,
                GetTeamCharacters(false)
            );
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
    private const float FormationGapY = 140f;
    private const float FormationGapX = 280f;
    private const float FormationSkew = 10f;
    private const float FormationRowOffset = 100f;
    private const int MaxFormationSlots = 9;
    private const int MaxBattleTurns = 100;
    private const int PostActionDelayMs = 800;
    private const int BattleOverDelayMs = 5000;
    private const int PlayerSpeedHintDelayMs = 200;
    private const int EnemySpeedHintDelayMs = 400;
    private const int SpeedTriggerThreshold = 100;
    private const int EarlyBattleBonusSkillRewardBattles = 3;
    private const int EarlyBattleExtraSkillRewardGroups = 1;
    private const string SpeedTriggerText = "[color=yellow]超速触发[/color]";

    public Character CurrentActionCharacter { get; private set; }

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
        InitializeBattleUi();
        InitializeBattleCharacters();
        StartEffectList.Insert(0, ApplyEquipmentBattleStartEffects);
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

        int playerOpeningSpeed = GetAliveTeamSpeed(isPlayer: true);
        int enemyOpeningSpeed = GetAliveTeamSpeed(isPlayer: false);

        PlayerSpeed = 0;
        EnemySpeed = 0;
        await ApplyRelicBattleEffects(token);
        await BattleBegin1(token, playerOpeningSpeed, enemyOpeningSpeed);
    }

    private void DisableBattleProcessing()
    {
        SetProcess(false);
        SetProcessInput(false);
        SetPhysicsProcess(false);
    }

    private void InitializeBattleCharacters()
    {
        ClearSummons(queueFree: true);
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
        _playerActionCount = 0;
        _enemyActionCount = 0;
        _characterActionCounts.Clear();
        _pendingExtraActions.Clear();
        _activeExtraActionCharacters.Clear();
        InitializeNonGameplayUi();
    }

    public void SetCurrentActionCharacter(Character character)
    {
        CurrentActionCharacter = character;
    }

    public void ClearCurrentActionCharacter(Character character = null)
    {
        if (character == null || CurrentActionCharacter == character)
            CurrentActionCharacter = null;
    }

    public IEnumerable<Character> GetTeamCharacters(bool isPlayer, bool includeSummons = true)
    {
        IEnumerable<Character> core = isPlayer
            ? PlayersList.Cast<Character>()
            : EnemiesList.Cast<Character>();
        if (!includeSummons)
            return core;

        IEnumerable<Character> summons = isPlayer
            ? PlayerSummons.Cast<Character>()
            : EnemySummons.Cast<Character>();
        return core.Concat(summons);
    }

    public Character[] GetOrderedTeamCharacters(
        bool isPlayer,
        bool includeSummons = true,
        bool dyingFilter = false
    )
    {
        IEnumerable<Character> query = GetTeamCharacters(isPlayer, includeSummons)
            .Where(x => x != null);
        if (dyingFilter)
            query = query.Where(x => x.State != Character.CharacterState.Dying);
        return query.OrderBy(x => x.PositionIndex).ToArray();
    }

    public int GetAliveTeamSpeed(bool isPlayer) =>
        GetTeamCharacters(isPlayer)
            .Where(IsCharacterAlive)
            .Where(x => x.CountsTowardTeamSpeed)
            .Sum(x => x.Speed);

    private void RegisterAction(Character character)
    {
        if (character == null)
            return;

        if (character.IsPlayer)
            _playerActionCount++;
        else
            _enemyActionCount++;

        ulong id = character.GetInstanceId();
        _characterActionCounts[id] = _characterActionCounts.TryGetValue(id, out int currentCount)
            ? currentCount + 1
            : 1;
    }

    public void RequestExtraAction(Character character, int count = 1)
    {
        if (
            count <= 0
            || character == null
            || !GodotObject.IsInstanceValid(character)
            || !character.ParticipatesInTurnRotation
            || !IsCharacterAlive(character)
        )
        {
            return;
        }

        ulong id = character.GetInstanceId();
        _pendingExtraActions[id] = _pendingExtraActions.TryGetValue(id, out int currentCount)
            ? currentCount + count
            : count;
    }

    public bool IsResolvingExtraAction(Character character)
    {
        if (character == null || !GodotObject.IsInstanceValid(character))
            return false;

        return _activeExtraActionCharacters.Contains(character.GetInstanceId());
    }

    private void SetExtraActionState(Character character, bool active)
    {
        if (character == null || !GodotObject.IsInstanceValid(character))
            return;

        ulong id = character.GetInstanceId();
        if (active)
            _activeExtraActionCharacters.Add(id);
        else
            _activeExtraActionCharacters.Remove(id);
    }

    private bool TryConsumeExtraAction(Character character)
    {
        if (character == null || !GodotObject.IsInstanceValid(character))
            return false;

        ulong id = character.GetInstanceId();
        if (!_pendingExtraActions.TryGetValue(id, out int pendingCount) || pendingCount <= 0)
            return false;

        if (!IsCharacterAlive(character))
        {
            _pendingExtraActions.Remove(id);
            return false;
        }

        pendingCount--;
        if (pendingCount <= 0)
            _pendingExtraActions.Remove(id);
        else
            _pendingExtraActions[id] = pendingCount;

        return true;
    }

    public int GetAlliedActionCountExcludingSelf(Character character)
    {
        if (character == null)
            return 0;

        int teamTotal = character.IsPlayer ? _playerActionCount : _enemyActionCount;
        int selfActions = _characterActionCounts.TryGetValue(
            character.GetInstanceId(),
            out int actionCount
        )
            ? actionCount
            : 0;
        return Math.Max(0, teamTotal - selfActions);
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

    private Task ApplyEquipmentBattleStartEffects()
    {
        if (PlayersList == null || PlayersList.Count == 0 || EnemiesList == null)
            return Task.CompletedTask;

        foreach (var player in PlayersList)
        {
            if (player == null || player.State == Character.CharacterState.Dying)
                continue;

            if (
                player.CharacterIndex < 0
                || GameInfo.PlayerCharacters == null
                || player.CharacterIndex >= GameInfo.PlayerCharacters.Length
            )
            {
                continue;
            }

            var info = GameInfo.PlayerCharacters[player.CharacterIndex];
            ApplyOverloadMarkEffect(player, info.Equipments);

            if (!HasShockPendant(info.Equipments))
                continue;

            foreach (var enemy in EnemiesList)
            {
                if (
                    enemy == null
                    || enemy.State == Character.CharacterState.Dying
                    || enemy.PositionIndex != player.PositionIndex
                )
                {
                    continue;
                }

                SkillBuff.BuffAdd(Buff.BuffName.Stun, enemy, 1, player);
            }
        }

        return Task.CompletedTask;
    }

    private static void ApplyOverloadMarkEffect(PlayerCharacter player, Equipment[] equipments)
    {
        if (player == null || !HasOverloadMark(equipments))
            return;

        SpecialBuff.BuffAdd(Buff.BuffName.ExtraPower, player, 2, player);
    }

    private static bool HasShockPendant(Equipment[] equipments)
    {
        if (equipments == null || equipments.Length == 0)
            return false;

        foreach (var equipment in equipments)
        {
            if (equipment?.Name == Equipment.EquipmentName.ShockPendant)
                return true;
        }

        return false;
    }

    private static bool HasOverloadMark(Equipment[] equipments)
    {
        if (equipments == null || equipments.Length == 0)
            return false;

        foreach (var equipment in equipments)
        {
            if (equipment?.Name == Equipment.EquipmentName.OverloadMark)
                return true;
        }

        return false;
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
        characters?.Where(IsCharacterAlive).Where(x => x.CountsTowardTeamSpeed).Sum(x => x.Speed)
        ?? 0;


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
            ReparentCharacter(character, container);
            ApplyFormationPosition(character, side);
        }
    }

    private static void ReparentCharacter(Character character, Node container)
    {
        if (character.GetParent() == container)
        {
            character.PrepareHoverTooltipInstances();
            return;
        }

        character.GetParent()?.RemoveChild(character);
        container.AddChild(character);
        character.PrepareHoverTooltipInstances();
    }

    private static Vector2 GetFormationPosition(int positionIndex, int side)
    {
        int row = (positionIndex - 1) % 3;
        int col = (positionIndex - 1) / 3;
        float xPos =
            col * FormationGapX * side - (row * FormationSkew - FormationRowOffset * (row - 1));
        return new Vector2(xPos, row * FormationGapY);
    }

    private static void ApplyFormationPosition(Character character, int side)
    {
        if (character == null)
            return;

        int row = (character.PositionIndex - 1) % 3;
        character.Position = GetFormationPosition(character.PositionIndex, side);
        character.OriginalPosition = character.Position;
        character.ZIndex = row;
    }

    public T AddSummon<T>(T summon, Character summoner, int slotSelector = 0)
        where T : SummonCharacter
    {
        if (summon == null || summoner == null || !GodotObject.IsInstanceValid(summoner))
            return null;

        int slot = GetAvailableFormationSlot(
            summoner.IsPlayer,
            slotSelector,
            summoner.PositionIndex
        );
        if (slot < 0)
        {
            summon.Free();
            return null;
        }

        summon.BindToSummoner(summoner);
        summon.PositionIndex = slot;
        InitializeCharacter(summon);

        var teamSummons = summon.IsPlayer ? PlayerSummons : EnemySummons;
        if (!teamSummons.Contains(summon))
            teamSummons.Add(summon);
        if (!summoner.Summons.Contains(summon))
            summoner.Summons.Add(summon);

        var container = summon.IsPlayer ? Left : Right;
        ReparentCharacter(summon, container);
        RefreshSummonPositions(summoner);
        RecordSummon(summon, summoner);
        return summon;
    }

    public void RemoveSummon(SummonCharacter summon, bool queueFree = true)
    {
        if (summon == null)
            return;

        var summoner = summon.Summoner;
        PlayerSummons.Remove(summon);
        EnemySummons.Remove(summon);
        summoner?.Summons.Remove(summon);
        summon.DetachFromSummoner();

        if (summoner != null)
            RefreshSummonPositions(summoner);

        if (queueFree && GodotObject.IsInstanceValid(summon))
            summon.QueueFree();
    }

    public void RefreshSummonPositions(Character summoner)
    {
        if (summoner == null)
            return;

        var summons = summoner
            .Summons.Where(x => x != null && GodotObject.IsInstanceValid(x))
            .ToArray();
        for (int i = 0; i < summons.Length; i++)
        {
            var summon = summons[i];
            ApplyFormationPosition(summon, summon.IsPlayer ? -1 : 1);
        }
    }

    public int GetAvailableFormationSlot(
        bool isPlayer,
        int slotSelector = 0,
        int anchorPositionIndex = 0
    )
    {
        var occupied = GetTeamCharacters(isPlayer, includeSummons: true)
            .Where(x => x != null && GodotObject.IsInstanceValid(x))
            .Select(x => x.PositionIndex)
            .Where(index => index > 0)
            .ToHashSet();

        int[] emptySlots = Enumerable
            .Range(1, MaxFormationSlots)
            .Where(slot => !occupied.Contains(slot))
            .ToArray();
        if (emptySlots.Length == 0)
            return -1;

        if (slotSelector == 0)
            return emptySlots[0];

        if (slotSelector == MaxFormationSlots)
            return emptySlots[^1];

        if (anchorPositionIndex <= 0)
            return -1;

        return SelectRelativeEmptySlot(slotSelector, anchorPositionIndex, occupied);
    }

    private static int SelectRelativeEmptySlot(
        int slotSelector,
        int anchorPositionIndex,
        HashSet<int> occupied
    )
    {
        if (anchorPositionIndex <= 0 || slotSelector == 0)
            return -1;

        int step = Math.Sign(slotSelector);
        int startSlot = anchorPositionIndex + slotSelector;
        for (int slot = startSlot; slot >= 1 && slot <= MaxFormationSlots; slot += step)
        {
            if (!occupied.Contains(slot))
                return slot;
        }

        return -1;
    }

    private void ClearSummons(bool queueFree)
    {
        var summons = PlayerSummons.Concat(EnemySummons).ToArray();
        for (int i = 0; i < summons.Length; i++)
        {
            RemoveSummon(summons[i], queueFree);
        }
        PlayerSummons.Clear();
        EnemySummons.Clear();
    }

    public List<Func<Character, Task>> EmitList = new();
    public List<Func<Character, Character, Task>> DyingEmitList = new();

    public async Task EmitCharacterDying(Character target, Character source)
    {
        if (target == null || DyingEmitList == null || DyingEmitList.Count == 0)
            return;

        for (int i = 0; i < DyingEmitList.Count; i++)
        {
            await DyingEmitList[i](target, source);
        }
    }

    public async Task EndEmitS(Character character)
    {
        // Summons advance their local action chain directly via Next and should not
        // participate in the regular end-of-action emit pipeline.
        if (character?.IsSummon == true)
        {
            ClearCurrentActionCharacter(character);
            EmitSignal(SignalName.Next, character);
            return;
        }

        for (int i = 0; i < EmitList.Count; i++)
        {
            await EmitList[i](character);
        }

        await TriggerGlobalTurnEndBuffs(character);
        RegisterAction(character);

        if (SuppressSpeedGainThisTurn != true && character?.ParticipatesInTurnRotation == true)
        {
            if (character.IsPlayer)
                PlayerSpeed += GetAliveTeamSpeed(isPlayer: true);
            else
                EnemySpeed += GetAliveTeamSpeed(isPlayer: false);
        }

        await TriggerSummonsAfterOwner(character);
        EmitSignal(SignalName.Next, character);
        ClearCurrentActionCharacter(character);
    }

    public async Task TriggerGlobalTurnEndBuffs(Character actingCharacter)
    {
        if (actingCharacter == null || HasBattleEnded() || !IsBattleAlive())
            return;

        Character[] targets = GetTeamCharacters(true)
            .Concat(GetTeamCharacters(false))
            .Where(x =>
                x != null
                && GodotObject.IsInstanceValid(x)
                && x.State != Character.CharacterState.Dying
            )
            .ToArray();

        for (int i = 0; i < targets.Length; i++)
        {
            Character target = targets[i];
            EndActionBuff disaster = target.EndActionBuffs?.FirstOrDefault(x =>
                x != null && x.ThisBuffName == Buff.BuffName.Disaster && x.Stack > 0
            );
            if (disaster == null)
                continue;

            using var _ = target.BeginEffectSource(Buff.BuffName.Disaster.GetDescription());
            await target.GetHurt(disaster.Stack, target);

            if (HasBattleEnded() || !IsBattleAlive())
                return;
        }
    }

    public Task EndEmitExtraActionS(Character character)
    {
        if (character?.IsSummon == true)
        {
            ClearCurrentActionCharacter(character);
            EmitSignal(SignalName.Next, character);
            return Task.CompletedTask;
        }

        EmitSignal(SignalName.Next, character);
        ClearCurrentActionCharacter(character);
        return Task.CompletedTask;
    }

    public List<Func<Task>> StartEffectList = new();

    public async Task BattleBegin1(
        CancellationToken token,
        int playerOpeningSpeed,
        int enemyOpeningSpeed
    )
    {
        for (int i = 0; i < StartEffectList.Count; i++)
        {
            await StartEffectList[i]();
        }

        if (!CanContinue(token))
        {
            return;
        }

        if (playerOpeningSpeed < enemyOpeningSpeed)
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
        Character actingCharacter = characterlist[0];
        RotateFrontToBack(characterlist);
        bool isExtraAction = false;
        while (true)
        {
            if (
                !CanContinue(token)
                || actingCharacter == null
                || !GodotObject.IsInstanceValid(actingCharacter)
                || actingCharacter.State == Character.CharacterState.Dying
            )
            {
                break;
            }

            SetExtraActionState(actingCharacter, isExtraAction);
            try
            {
                SetCurrentActionCharacter(actingCharacter);
                actingCharacter.StartAction();
                await WaitForNextFrom(actingCharacter);
            }
            finally
            {
                SetExtraActionState(actingCharacter, false);
            }

            if (!CanContinue(token) || !TryConsumeExtraAction(actingCharacter))
                break;

            isExtraAction = true;
        }

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
        ClearSummons(queueFree: true);

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

        BuffHintLabel.Spawn(team[0], SpeedTriggerText, Vector2.Zero);
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

    private async Task TriggerSummonsAfterOwner(Character character)
    {
        if (
            character == null
            || character.Summons == null
            || character.Summons.Count == 0
            || HasBattleEnded()
            || !IsBattleAlive()
        )
        {
            return;
        }

        var summons = character
            .Summons.Where(x =>
                x != null
                && GodotObject.IsInstanceValid(x)
                && x.State != Character.CharacterState.Dying
            )
            .ToArray();

        for (int i = 0; i < summons.Length; i++)
        {
            if (HasBattleEnded() || !IsBattleAlive())
                return;

            SetCurrentActionCharacter(summons[i]);
            summons[i].StartAction();
            await WaitForNextFrom(summons[i]);
        }
    }

    private async Task WaitForNextFrom(Character expectedCharacter)
    {
        while (true)
        {
            Variant[] signalArgs = await ToSignal(this, SignalName.Next);
            if (signalArgs == null || signalArgs.Length == 0)
                return;

            Character emittedCharacter = signalArgs[0].As<Character>();
            if (emittedCharacter == null || emittedCharacter == expectedCharacter)
                return;
        }
    }

    private bool HasBattleEnded() => IsTeamDefeated(PlayersList) || IsTeamDefeated(EnemiesList);

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
        int skillRewardGroups = GetSkillRewardGroupCount();
        for (int i = 0; i < skillRewardGroups; i++)
            reward.AddSkillRewardEntry();

        var rng = new Random(CurrentLevelNode?.RandomNum ?? System.Environment.TickCount);
        var levelType = CurrentLevelNode?.Type ?? LevelNode.LevelType.Normal;
        bool addRelic = ShouldAddRelicReward(levelType);
        int equipCount = GameInfo.RollBattleEquipmentDropCount(levelType, rng);
        bool addItem = GameInfo.RollBattleItemDrop(rng);

        TryAddRelicReward(reward, rng, addRelic);
        AddEquipmentRewards(reward, rng, equipCount);
        TryAddItemReward(reward, rng, addItem);
    }

    private static int GetSkillRewardGroupCount()
    {
        int completedBattleCount =
            GameInfo.CompletedLevelNodeRecords?.Values.Count(record =>
                record != null
                && record.NodeType
                    is LevelNode.LevelType.Normal
                        or LevelNode.LevelType.Elite
                        or LevelNode.LevelType.Boss
            ) ?? 0;

        int bonusGroups =
            completedBattleCount < EarlyBattleBonusSkillRewardBattles
                ? EarlyBattleExtraSkillRewardGroups
                : 0;
        return 1 + bonusGroups;
    }

    private static bool ShouldAddRelicReward(LevelNode.LevelType levelType)
    {
        return levelType is LevelNode.LevelType.Boss or LevelNode.LevelType.Elite;
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

    private static void TryAddItemReward(Reward reward, Random rng, bool addItem)
    {
        if (!addItem)
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
