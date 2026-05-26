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

    [Export]
    public bool TestBattleTutorial { get; set; }

    public Random BattleIntentionRandom;
    private Random _carrySkillRandom;
    private readonly CancellationTokenSource _lifetimeCts = new();
    private readonly Dictionary<string, PlayerBattleCardPiles> _playerBattleCardPiles = new();
    private readonly Dictionary<string, Random> _playerBattleSkillRandoms = new();
    private ulong _battleInstanceId;
    private bool _retreating;
    private Tween _screenShakeTween;
    private Vector2 _battleBasePosition;
    private bool _battleBasePositionInitialized;

    private sealed class PlayerBattleCardPiles
    {
        public readonly List<SkillID> DrawPile = new();
        public readonly List<SkillID> DiscardPile = new();
        public readonly List<SkillID> Exhausted = new();
    }

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
    private NinePatchRect HitScreenFlash =>
        field ??= GetNode<NinePatchRect>("CanvasLayer/ColorRect");
    private ColorRect BackgroundRect => field ??= GetNode<ColorRect>("bg");

    public void PlayHitEffect()
    {
        if (HitScreenFlash != null && GodotObject.IsInstanceValid(HitScreenFlash))
        {
            HitScreenFlash.SelfModulate = new Color(1, 1, 1, 1);
            Tween flashTween = CreateTween();
            flashTween.TweenInterval(0.333333f);
            flashTween.TweenProperty(
                HitScreenFlash,
                "self_modulate",
                new Color(1, 1, 1, 0),
                0.166667f
            );
        }

        if (BackgroundRect != null && GodotObject.IsInstanceValid(BackgroundRect))
        {
            Tween bgTween = CreateTween();
            bgTween.TweenProperty(
                BackgroundRect,
                "self_modulate",
                new Color(0.4f, 0.4f, 0.4f, 1),
                0.1f
            );
            bgTween.TweenInterval(0.233333f);
            bgTween.TweenProperty(
                BackgroundRect,
                "self_modulate",
                new Color(1, 1, 1, 1),
                0.133334f
            );
        }

        PlayCameraShake();
    }

    private void CacheBattleBasePosition()
    {
        if (_battleBasePositionInitialized)
            return;

        _battleBasePosition = Position;
        _battleBasePositionInitialized = true;
    }

    private void PlayCameraShake()
    {
        CacheBattleBasePosition();
        if (!_battleBasePositionInitialized)
            return;

        float shakeScale = UserSettings.GetBattleShakeMultiplier();
        if (shakeScale <= 0.0f)
            return;

        if (_screenShakeTween != null && GodotObject.IsInstanceValid(_screenShakeTween))
            _screenShakeTween.Kill();

        Position = _battleBasePosition;
        _screenShakeTween = CreateTween();
        _screenShakeTween
            .TweenProperty(
                this,
                "position",
                _battleBasePosition + new Vector2(15, 10) * shakeScale,
                0.045f
            )
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.Out);
        _screenShakeTween
            .TweenProperty(
                this,
                "position",
                _battleBasePosition + new Vector2(20, -5) * shakeScale,
                0.04f
            )
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.Out);
        _screenShakeTween
            .TweenProperty(
                this,
                "position",
                _battleBasePosition + new Vector2(10, 10) * shakeScale,
                0.035f
            )
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.Out);
        _screenShakeTween
            .TweenProperty(this, "position", _battleBasePosition, 0.06f)
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.In);
        _screenShakeTween.Finished += OnCameraShakeFinished;
    }

    private void OnCameraShakeFinished()
    {
        Position = _battleBasePosition;

        _screenShakeTween = null;
    }

    public ObservableList<Skill> UsedSkills = new ObservableList<Skill>();
    public Button RetreatButton => field ??= GetNode<Button>("Retreat");
    public bool SuppressActionPoinGainThisTurn { get; set; }

    private int _playerActionPoin = 0;
    private int _enemyActionPoin = 0;
    private int _playerActionCount = 0;
    private int _enemyActionCount = 0;
    private readonly Dictionary<ulong, int> _characterActionCounts = new();
    private readonly Dictionary<ulong, int> _pendingExtraActions = new();
    private readonly HashSet<ulong> _activeExtraActionCharacters = new();
    private readonly List<Label> _enemyAttackPreviewLabels = new();
    private Character[] _enemyAttackPreviewTargets = Array.Empty<Character>();
    private Character _nextActionPreviewCharacter;
    private bool? _battleStartPlayerActsFirst;
    private static readonly bool ShowNextActionGroundPreview = false;
    private bool _actionPoinUiRefreshScheduled;
    private bool _pendingPlayerActionPoinUiRefresh;
    private bool _pendingEnemyActionPoinUiRefresh;

    public int PlayerActionPoin
    {
        get => _playerActionPoin;
        set =>
            SetActionPoinValue(
                ref _playerActionPoin,
                value,
                PlayerActionPoinLabel,
                PlayerTotalSpeedLabel,
                PlayerActionPoinBar,
                GetTeamCharacters(true)
            );
    }

    public int EnemyActionPoin
    {
        get => _enemyActionPoin;
        set =>
            SetActionPoinValue(
                ref _enemyActionPoin,
                value,
                EnemyActionPoinLabel,
                EnemyTotalSpeedLabel,
                EnemyActionPoinBar,
                GetTeamCharacters(false)
            );
    }

    public GlowLabel PlayerActionPoinLabel =>
        field ??= GetNodeOrNull<GlowLabel>("ActionPoinBox/PlayerActionPoin/Label");
    public GlowLabel EnemyActionPoinLabel =>
        field ??= GetNodeOrNull<GlowLabel>("ActionPoinBox/EnemyActionPoin/Label");
    public Label PlayerTotalSpeedLabel =>
        field ??= GetNodeOrNull<Label>("ActionPoinBox/PlayerActionPoin/TotalLabel");
    public Label EnemyTotalSpeedLabel =>
        field ??= GetNodeOrNull<Label>("ActionPoinBox/EnemyActionPoin/TotalLabel");
    public ProgressBar PlayerActionPoinBar =>
        field ??= GetNodeOrNull<ProgressBar>("ActionPoinBox/PlayerActionPoin");
    public ProgressBar EnemyActionPoinBar =>
        field ??= GetNodeOrNull<ProgressBar>("ActionPoinBox/EnemyActionPoin");
    public LevelNode CurrentLevelNode;
    public Character dummy => field ??= GetNode<Character>("Dummy");
    private const float FormationGapY = 140f;
    private const float FormationGapX = 280f;
    private const float FormationSkew = 10f;
    private const float FormationRowOffset = 100f;
    public const int MaxFormationSlots = 9;
    private const int MaxBattleTurns = 100;
    private const int PostActionDelayMs = 800;
    private const int BattleOverDelayMs = 5000;
    private const int PlayerActionPoinHintDelayMs = 200;
    private const int EnemyActionPoinHintDelayMs = 400;
    private const int BattleStartEffectIntervalMs = 100;
    private const int ActionPoinTriggerThreshold = 100;
    private const int PlayerDyingCoreEnergyCost = 10;
    private const int ManualRetreatCoreEnergyCost = 5;
    private const int EarlyBattleBonusSkillRewardBattles = 3;
    private const int EarlyBattleExtraSkillRewardGroups = 1;
    private const int RegionalBonusRelicBattleNumber = 5;
    private static readonly Vector2 EnemyAttackPreviewLabelOffset = new(-50f, -138f);
    private static readonly Color EnemyAttackPreviewColor = new(1f, 0.84f, 0.63f, 1f);
    private static readonly Color EnemyAttackPreviewOutlineColor = new(0.02f, 0.03f, 0.06f, 0.95f);
    private const string ActionPoinTriggerText = "[color=yellow]行动点数触发[/color]";

    public Character CurrentActionCharacter { get; private set; }

    public override void _EnterTree()
    {
        _battleInstanceId = GetInstanceId();
    }

    public override void _ExitTree()
    {
        _retreating = true;
        HideEnemyAttackPreview();
        FreeEnemyAttackPreviewLabels();
        TryCancelLifetime();
        MapNode?.Camera?.MakeCurrent();
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
        CacheBattleBasePosition();
        InitializeBattleCharacters();
        CaptureBattleStartInitiative();
        SetCharaterPostion();
        RefreshTurnOrderPreview();
        CharacterControl.Connect();
        if (!await DelayOrCancel(PlayerActionPoinHintDelayMs, token))
        {
            return;
        }

        CharacterControl.DisableAll();
        if (!await InitializeEnemyIntentions(token))
        {
            return;
        }

        if (!await ShowFirstBattleTutorialIfNeeded(token))
        {
            return;
        }

        PlayerActionPoin = 0;
        EnemyActionPoin = 0;
        RequestActionPoinUiRefresh(isPlayer: true);
        RequestActionPoinUiRefresh(isPlayer: false);
        await ApplyRelicBattleEffects(token);
        await BattleBegin1(token);
    }

    private void DisableBattleProcessing()
    {
        SetProcess(false);
        SetProcessInput(false);
        SetPhysicsProcess(false);
    }

    private async Task<bool> ShowFirstBattleTutorialIfNeeded(CancellationToken token)
    {
        if (!TestBattleTutorial && BattleTutorialOverlay.HasSeenTutorial())
        {
            GameInfo.HasSeenBattleTutorial = true;
            return true;
        }

        await BattleTutorialOverlay.ShowAsync(this);
        if (!CanContinue(token))
            return false;

        if (TestBattleTutorial)
            return true;

        GameInfo.HasSeenBattleTutorial = true;
        BattleTutorialOverlay.MarkTutorialSeen();
        return true;
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
            var characterScene = PreloadeScene.GetPackedScene(
                GameInfo.PlayerCharacters[i].CharacterScenePath
            );
            var character = characterScene?.Instantiate<PlayerCharacter>();
            if (character == null)
                continue;

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
        BattleIntentionRandom ??= CreateBattleRandom(unchecked((int)0x13572468));
        _carrySkillRandom ??= CreateBattleRandom(unchecked((int)0x5A17C0DE));
        RetreatButton.ButtonDown += ManualRetreat;
        _playerActionCount = 0;
        _enemyActionCount = 0;
        _playerBattleCardPiles.Clear();
        _playerBattleSkillRandoms.Clear();
        _characterActionCounts.Clear();
        _pendingExtraActions.Clear();
        _activeExtraActionCharacters.Clear();
        InitializeNonGameplayUi();
    }

    private Random CreateBattleRandom(int salt)
    {
        int baseSeed = CurrentLevelNode?.RandomNum ?? GameInfo.Seed;
        int instanceSeed = unchecked((int)_battleInstanceId);
        return new Random(baseSeed ^ salt ^ instanceSeed);
    }

    public Skill DrawPlayerBattleSkill(
        PlayerCharacter player,
        Skill.SkillTypes skillType = Skill.SkillTypes.none,
        SkillID? avoidSkillId = null
    )
    {
        if (player == null || GameInfo.PlayerCharacters == null)
            return null;

        if (player.CharacterIndex < 0 || player.CharacterIndex >= GameInfo.PlayerCharacters.Length)
            return null;

        PlayerBattleCardPiles piles = GetOrCreatePlayerBattleCardPiles(player);
        if (piles == null)
            return null;

        if (!TryEnsureDrawableCards(player, piles, skillType))
            return null;

        int[] candidateIndexes = GetDrawableCardIndexes(piles.DrawPile, skillType);
        if (candidateIndexes.Length == 0)
            return null;

        int[] pickPool =
            avoidSkillId.HasValue && candidateIndexes.Length > 1
                ? candidateIndexes
                    .Where(index => piles.DrawPile[index] != avoidSkillId.Value)
                    .ToArray()
                : candidateIndexes;
        if (pickPool.Length == 0)
            pickPool = candidateIndexes;

        int pickedIndex = pickPool[0];
        SkillID pickedId = piles.DrawPile[pickedIndex];
        piles.DrawPile.RemoveAt(pickedIndex);
        Skill pickedSkill = Skill.GetSkill(pickedId);
        if (pickedSkill != null)
            pickedSkill.OwnerCharater = player;
        player.InvalidateSkillTooltipCache();
        return pickedSkill;
    }

    public void AddPlayerBattleStatusCardsToDrawPile(
        PlayerCharacter player,
        SkillID skillId,
        int count,
        Character source = null
    )
    {
        if (count <= 0 || player == null || !GodotObject.IsInstanceValid(player))
            return;

        PlayerBattleCardPiles piles = GetOrCreatePlayerBattleCardPiles(player);
        if (piles == null)
            return;

        Random rng = GetOrCreatePlayerBattleSkillRandom(player);
        RefillDrawPileFromDiscard(piles, rng);
        for (int i = 0; i < count; i++)
        {
            int insertIndex = rng.Next(piles.DrawPile.Count + 1);
            piles.DrawPile.Insert(insertIndex, skillId);
        }

        player.InvalidateSkillTooltipCache();
        RecordStatusCardInsert(player, skillId, count, source: source);
    }

    public int AddPlayerBattleStatusCardsToHand(
        PlayerCharacter player,
        SkillID skillId,
        int count,
        Character source = null
    )
    {
        if (count <= 0 || player == null || !GodotObject.IsInstanceValid(player))
            return 0;

        if (player.Skills == null)
            return 0;

        int added = 0;
        for (int i = 0; i < player.Skills.Length && added < count; i++)
        {
            if (player.Skills[i] != null)
                continue;

            Skill skill = Skill.GetSkill(skillId);
            if (skill == null)
                continue;

            skill.OwnerCharater = player;
            skill.UpdateDescription();
            player.Skills[i] = skill;
            added++;
        }

        if (added > 0)
        {
            player.InvalidateSkillTooltipCache();
            RecordStatusCardInsert(player, skillId, added, toHand: true, source: source);
            CharacterControl?.RefreshCurrentTurnUi();
        }

        return added;
    }

    public void DiscardBattleSkill(Character actor, Skill skill, bool atTurnEnd = false)
    {
        if (actor is not PlayerCharacter player || skill == null || !skill.SkillId.HasValue)
        {
            return;
        }

        PlayerBattleCardPiles piles = GetOrCreatePlayerBattleCardPiles(player);
        if (piles == null)
            return;

        SkillID skillId = skill.SkillId.Value;
        if (skill.ExhaustsAfterUse || (atTurnEnd && skill.ExhaustsAtTurnEndInHand))
            piles.Exhausted.Add(skillId);
        else
            piles.DiscardPile.Add(skillId);

        player.InvalidateSkillTooltipCache();
    }

    public async Task ExhaustAllPlayerBattleStatusCardsAsync(Character source = null)
    {
        var handStatusIndexes = new Dictionary<PlayerCharacter, List<int>>();

        foreach (PlayerCharacter player in PlayersList.ToArray())
        {
            if (player == null || !GodotObject.IsInstanceValid(player))
                continue;

            PlayerBattleCardPiles piles = GetOrCreatePlayerBattleCardPiles(player);
            if (piles == null)
                continue;

            bool changed = false;
            changed |= MoveStatusCardsToExhausted(piles.DrawPile, piles.Exhausted);
            changed |= MoveStatusCardsToExhausted(piles.DiscardPile, piles.Exhausted);

            List<int> indexes = CollectHandStatusCardIndexes(player, piles);
            if (indexes.Count > 0)
            {
                changed = true;
                handStatusIndexes[player] = indexes;
            }

            if (changed)
                player.InvalidateSkillTooltipCache();
        }

        if (
            handStatusIndexes.Count > 0
            && CharacterControl != null
            && GodotObject.IsInstanceValid(CharacterControl)
        )
        {
            await Task.WhenAll(
                handStatusIndexes.Select(entry =>
                    CharacterControl.PlayHandCardExhaustAnimationAsync(entry.Key, entry.Value)
                )
            );
        }

        foreach (var entry in handStatusIndexes)
        {
            PlayerCharacter player = entry.Key;
            if (player == null || !GodotObject.IsInstanceValid(player))
                continue;

            foreach (int index in entry.Value)
                player.RemoveBattleHandCardAt(index);
        }

        CharacterControl?.RefreshCurrentTurnUi();
    }

    private static List<int> CollectHandStatusCardIndexes(
        PlayerCharacter player,
        PlayerBattleCardPiles piles
    )
    {
        var indexes = new List<int>();
        if (player?.Skills == null || piles == null)
            return indexes;

        for (int i = 0; i < player.Skills.Length; i++)
        {
            Skill skill = player.Skills[i];
            if (skill == null || !skill.SkillId.HasValue)
                continue;

            SkillID skillId = skill.SkillId.Value;
            if (!IsBattleStatusCard(skillId))
                continue;

            piles.Exhausted.Add(skillId);
            indexes.Add(i);
        }

        return indexes;
    }

    private static bool MoveStatusCardsToExhausted(List<SkillID> source, List<SkillID> exhausted)
    {
        if (source == null || exhausted == null || source.Count == 0)
            return false;

        bool movedAny = false;
        for (int i = source.Count - 1; i >= 0; i--)
        {
            SkillID skillId = source[i];
            if (!IsBattleStatusCard(skillId))
                continue;

            source.RemoveAt(i);
            exhausted.Add(skillId);
            movedAny = true;
        }

        return movedAny;
    }

    private static bool IsBattleStatusCard(SkillID skillId)
    {
        Skill skill = Skill.GetSkill(skillId);
        return skill?.IsStatusCard == true;
    }

    public SkillID[] GetDrawBattleCardPile(PlayerCharacter player) =>
        GetOrCreatePlayerBattleCardPiles(player)?.DrawPile.ToArray() ?? Array.Empty<SkillID>();

    public SkillID[] GetDiscardBattleCardPile(PlayerCharacter player) =>
        GetOrCreatePlayerBattleCardPiles(player)?.DiscardPile.ToArray() ?? Array.Empty<SkillID>();

    public SkillID[] GetExhaustedBattleCardPile(PlayerCharacter player) =>
        GetOrCreatePlayerBattleCardPiles(player)?.Exhausted.ToArray() ?? Array.Empty<SkillID>();

    public bool HasDrawablePlayerBattleSkill(
        PlayerCharacter player,
        Skill.SkillTypes skillType = Skill.SkillTypes.none
    )
    {
        PlayerBattleCardPiles piles = GetOrCreatePlayerBattleCardPiles(player);
        if (piles == null)
            return false;

        if (piles.DrawPile.Count > 0)
            return HasDrawableCards(piles.DrawPile, skillType);

        return HasDrawableCards(piles.DiscardPile, skillType);
    }

    private PlayerBattleCardPiles GetOrCreatePlayerBattleCardPiles(PlayerCharacter player)
    {
        if (player == null || GameInfo.PlayerCharacters == null)
            return null;

        string characterKey = GetPlayerBattleSkillCharacterKey(player);
        if (string.IsNullOrEmpty(characterKey))
            return null;

        if (_playerBattleCardPiles.TryGetValue(characterKey, out var piles))
            return piles;

        piles = new PlayerBattleCardPiles();
        var info = GameInfo.PlayerCharacters[player.CharacterIndex];
        foreach (SkillID skillId in info.GainedSkills ?? new List<SkillID>())
        {
            Skill skill = Skill.GetSkill(skillId);
            if (skill == null || skill.SkillType == Skill.SkillTypes.none || skill.IsStatusCard)
                continue;

            piles.DrawPile.Add(skillId);
        }

        ShuffleSkillPile(piles.DrawPile, GetOrCreatePlayerBattleSkillRandom(player));
        _playerBattleCardPiles[characterKey] = piles;
        return piles;
    }

    private bool TryEnsureDrawableCards(
        PlayerCharacter player,
        PlayerBattleCardPiles piles,
        Skill.SkillTypes skillType
    )
    {
        if (player == null || piles == null)
            return false;

        int[] drawPileIndexes = GetDrawableCardIndexes(piles.DrawPile, skillType);
        if (drawPileIndexes.Length > 0)
            return true;

        if (piles.DrawPile.Count > 0)
            return false;

        if (piles.DiscardPile.Count == 0)
            return false;

        RefillDrawPileFromDiscard(piles, GetOrCreatePlayerBattleSkillRandom(player));
        return GetDrawableCardIndexes(piles.DrawPile, skillType).Length > 0;
    }

    private static void RefillDrawPileFromDiscard(PlayerBattleCardPiles piles, Random rng)
    {
        if (piles == null || piles.DrawPile.Count > 0 || piles.DiscardPile.Count == 0)
            return;

        piles.DrawPile.AddRange(piles.DiscardPile);
        piles.DiscardPile.Clear();
        ShuffleSkillPile(piles.DrawPile, rng);
    }

    private static void ShuffleSkillPile(List<SkillID> pile, Random rng)
    {
        if (pile == null || pile.Count <= 1)
            return;

        rng ??= new Random();
        for (int i = pile.Count - 1; i > 0; i--)
        {
            int swapIndex = rng.Next(i + 1);
            (pile[i], pile[swapIndex]) = (pile[swapIndex], pile[i]);
        }
    }

    private static int[] GetDrawableCardIndexes(List<SkillID> drawPile, Skill.SkillTypes skillType)
    {
        if (drawPile == null || drawPile.Count == 0)
            return Array.Empty<int>();

        return drawPile
            .Select((id, index) => new { id, index })
            .Where(entry =>
            {
                if (skillType == Skill.SkillTypes.none)
                    return Skill.GetSkill(entry.id) != null;

                Skill candidate = Skill.GetSkill(entry.id);
                return candidate != null && candidate.SkillType == skillType;
            })
            .Select(entry => entry.index)
            .ToArray();
    }

    private static bool HasDrawableCards(List<SkillID> pile, Skill.SkillTypes skillType)
    {
        if (pile == null || pile.Count == 0)
            return false;

        return pile.Any(id =>
        {
            Skill candidate = Skill.GetSkill(id);
            if (candidate == null)
                return false;

            return skillType == Skill.SkillTypes.none || candidate.SkillType == skillType;
        });
    }

    private Random GetOrCreatePlayerBattleSkillRandom(PlayerCharacter player)
    {
        string characterKey = GetPlayerBattleSkillCharacterKey(player);
        if (_playerBattleSkillRandoms.TryGetValue(characterKey, out Random rng))
            return rng;

        rng = new Random(CreatePlayerBattleSkillSeed(characterKey));
        _playerBattleSkillRandoms[characterKey] = rng;
        return rng;
    }

    private string GetPlayerBattleSkillCharacterKey(PlayerCharacter player)
    {
        if (
            player == null
            || GameInfo.PlayerCharacters == null
            || player.CharacterIndex < 0
            || player.CharacterIndex >= GameInfo.PlayerCharacters.Length
        )
        {
            return string.Empty;
        }

        PlayerInfoStructure info = GameInfo.PlayerCharacters[player.CharacterIndex];
        if (!string.IsNullOrWhiteSpace(info.CharacterScenePath))
            return $"{player.CharacterIndex}:{info.CharacterScenePath}";
        if (!string.IsNullOrWhiteSpace(info.CharacterName))
            return $"{player.CharacterIndex}:{info.CharacterName}";

        return $"character-index:{player.CharacterIndex}";
    }

    private int CreatePlayerBattleSkillSeed(string characterKey)
    {
        int baseSeed = CurrentLevelNode?.RandomNum ?? GameInfo.Seed;
        return HashSeed(baseSeed, unchecked((int)0x24681357), StableStringHash(characterKey));
    }

    private static int HashSeed(params int[] values)
    {
        unchecked
        {
            int hash = (int)2166136261;
            foreach (int value in values)
            {
                hash ^= value;
                hash *= 16777619;
            }

            return hash;
        }
    }

    private static int StableStringHash(string value)
    {
        unchecked
        {
            int hash = (int)2166136261;
            foreach (char c in value ?? string.Empty)
            {
                hash ^= c;
                hash *= 16777619;
            }

            return hash;
        }
    }

    public Skill DrawCarrySkill(Character character, Skill.SkillTypes skillType)
    {
        if (character == null)
            return null;

        if (character is PlayerCharacter player)
            return DrawRandomPlayerCarrySkill(player, skillType);

        Skill[] candidates = (character.Skills ?? Array.Empty<Skill>())
            .Where(skill =>
                skill != null
                && skill.SkillType != Skill.SkillTypes.none
                && !skill.IsStatusCard
                && (skillType == Skill.SkillTypes.none || skill.SkillType == skillType)
            )
            .ToArray();
        if (candidates.Length == 0)
            return null;

        _carrySkillRandom ??= CreateBattleRandom(unchecked((int)0x5A17C0DE));
        Skill pickedSkill =
            candidates.Length == 1
                ? candidates[0]
                : candidates[_carrySkillRandom.Next(candidates.Length)];
        if (pickedSkill != null)
            pickedSkill.OwnerCharater = character;
        return pickedSkill;
    }

    private Skill DrawRandomPlayerCarrySkill(PlayerCharacter player, Skill.SkillTypes skillType)
    {
        if (player == null || GameInfo.PlayerCharacters == null)
            return null;

        if (player.CharacterIndex < 0 || player.CharacterIndex >= GameInfo.PlayerCharacters.Length)
            return null;

        PlayerBattleCardPiles piles = GetOrCreatePlayerBattleCardPiles(player);
        if (piles == null)
            return null;

        var candidates = new List<(List<SkillID> Pile, int Index, SkillID SkillId)>();
        AddCarryCandidates(candidates, piles.DrawPile, skillType);
        AddCarryCandidates(candidates, piles.DiscardPile, skillType);
        if (candidates.Count == 0)
            return null;

        Random rng = GetOrCreatePlayerBattleSkillRandom(player);
        var picked = candidates[rng.Next(candidates.Count)];
        picked.Pile.RemoveAt(picked.Index);

        Skill pickedSkill = Skill.GetSkill(picked.SkillId);
        if (pickedSkill != null)
            pickedSkill.OwnerCharater = player;
        player.InvalidateSkillTooltipCache();
        return pickedSkill;
    }

    private static void AddCarryCandidates(
        List<(List<SkillID> Pile, int Index, SkillID SkillId)> candidates,
        List<SkillID> pile,
        Skill.SkillTypes skillType
    )
    {
        if (candidates == null || pile == null || pile.Count == 0)
            return;

        for (int index = 0; index < pile.Count; index++)
        {
            Skill candidate = Skill.GetSkill(pile[index]);
            if (
                candidate == null
                || candidate.SkillType == Skill.SkillTypes.none
                || candidate.IsStatusCard
                || (skillType != Skill.SkillTypes.none && candidate.SkillType != skillType)
            )
                continue;

            candidates.Add((pile, index, pile[index]));
        }
    }

    public void SetCurrentActionCharacter(Character character)
    {
        if (character?.IsSummon == true)
            return;

        ClearNextActionPreviewCharacter(character);
        CurrentActionCharacter = character;
        RefreshTurnOrderPreview();
    }

    public void ClearCurrentActionCharacter(Character character = null)
    {
        if (character == null || CurrentActionCharacter == character)
        {
            CurrentActionCharacter = null;
            RefreshTurnOrderPreview();
        }
    }

    public void SetNextActionPreviewCharacter(Character character)
    {
        if (_nextActionPreviewCharacter == character)
            return;

        ClearNextActionPreviewCharacter();
        if (
            character == null
            || !GodotObject.IsInstanceValid(character)
            || character == CurrentActionCharacter
            || character.State == Character.CharacterState.Dying
        )
        {
            return;
        }

        _nextActionPreviewCharacter = character;
        if (ShowNextActionGroundPreview)
            character.ShowNextActionPreview();
        RefreshTurnOrderPreview();
    }

    public void ClearNextActionPreviewCharacter(Character character = null)
    {
        if (_nextActionPreviewCharacter == null)
            return;
        if (character != null && _nextActionPreviewCharacter != character)
            return;

        Character previewCharacter = _nextActionPreviewCharacter;
        _nextActionPreviewCharacter = null;
        if (ShowNextActionGroundPreview && GodotObject.IsInstanceValid(previewCharacter))
            previewCharacter.HideNextActionPreview();
        RefreshTurnOrderPreview();
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

    public void RefreshTurnOrderPreview()
    {
        RefreshEnemyIntentionDamageSummaries();
        UserSettings.EnsureLoaded();
        if (!UserSettings.ShowBattleTurnOrderPreview || !IsBattleAlive())
        {
            HideAllTurnOrderPreviews();
            RefreshEnemyAttackPreview();
            return;
        }

        var orderByCharacter = new Dictionary<ulong, int>();
        int nextOrder = 0;
        var playerQueue = GetTurnOrderQueue(isPlayer: true);
        var enemyQueue = GetTurnOrderQueue(isPlayer: false);
        var previewExtraActions = GetPreviewExtraActionCounts(playerQueue, enemyQueue);
        int previewPlayerActionPoin = PlayerActionPoin;
        int previewEnemyActionPoin = EnemyActionPoin;
        Character pendingExtraActionCharacter = null;

        AddTurnOrderEvent(CurrentActionCharacter, orderByCharacter, ref nextOrder);

        ApplyPreviewActionPoinGain(
            CurrentActionCharacter,
            ref previewPlayerActionPoin,
            ref previewEnemyActionPoin,
            suppressActionPoinGain: SuppressActionPoinGainThisTurn
        );
        while (TryConsumePreviewExtraAction(CurrentActionCharacter, previewExtraActions))
        {
            AddTurnOrderEvent(CurrentActionCharacter, orderByCharacter, ref nextOrder);
            ApplyPreviewActionPoinGain(
                CurrentActionCharacter,
                ref previewPlayerActionPoin,
                ref previewEnemyActionPoin,
                suppressActionPoinGain: SuppressActionPoinGainThisTurn
            );
        }

        bool nextTeamIsPlayer = GetPreviewContinuationTeam(
            previewPlayerActionPoin,
            previewEnemyActionPoin
        );

        int previewCharacterCount = playerQueue.Count + enemyQueue.Count;
        int maxIterations = Math.Max(previewCharacterCount * 4, 8);

        for (int i = 0; i < maxIterations && orderByCharacter.Count < previewCharacterCount; i++)
        {
            Character simulatedCharacter = GetNextPreviewCharacter(
                playerQueue,
                enemyQueue,
                ref previewPlayerActionPoin,
                ref previewEnemyActionPoin,
                ref nextTeamIsPlayer,
                ref pendingExtraActionCharacter,
                out bool triggeredByActionPoinBurst
            );
            if (simulatedCharacter == null)
                break;

            AddTurnOrderEvent(simulatedCharacter, orderByCharacter, ref nextOrder);
            ApplyPreviewActionPoinGain(
                simulatedCharacter,
                ref previewPlayerActionPoin,
                ref previewEnemyActionPoin,
                triggeredByActionPoinBurst
            );
            if (TryConsumePreviewExtraAction(simulatedCharacter, previewExtraActions))
                pendingExtraActionCharacter = simulatedCharacter;
        }

        foreach (
            var character in GetTeamCharacters(isPlayer: true, includeSummons: false)
                .Concat(GetTeamCharacters(isPlayer: false, includeSummons: false))
        )
        {
            if (character == null || !GodotObject.IsInstanceValid(character))
                continue;

            if (orderByCharacter.TryGetValue(character.GetInstanceId(), out int order))
                character.ShowTurnOrderPreview(order);
            else
                character.HideTurnOrderPreview();
        }

        RefreshEnemyAttackPreview();
    }

    public void RefreshTurnOrderPreviewFromSettings() => RefreshTurnOrderPreview();

    public void RefreshEnemyAttackPreviewFromSettings() => RefreshEnemyAttackPreview();

    public void RefreshEnemyIntentionPreviews()
    {
        RefreshEnemyIntentionDamageSummaries();
        RefreshEnemyAttackPreview();
    }

    public void RefreshTextSizeFromSettings()
    {
        CharacterControl?.RefreshTextSizeFromSettings();

        foreach (
            var character in GetTeamCharacters(isPlayer: true, includeSummons: true)
                .Concat(GetTeamCharacters(isPlayer: false, includeSummons: true))
        )
        {
            if (character != null && GodotObject.IsInstanceValid(character))
                character.RefreshSkillTooltipTextFromSettings();
        }
    }

    public void RefreshBattleCardDescriptionModeFromSettings()
    {
        CharacterControl?.RefreshDisplayedSkillDescriptions();

        foreach (
            var character in GetTeamCharacters(isPlayer: true, includeSummons: true)
                .Concat(GetTeamCharacters(isPlayer: false, includeSummons: true))
        )
        {
            if (character != null && GodotObject.IsInstanceValid(character))
                character.RefreshSkillTooltipTextFromSettings();
        }
    }

    private int GetPreviewExtraActionCount(Character character)
    {
        if (
            character == null
            || !GodotObject.IsInstanceValid(character)
            || !character.ParticipatesInTurnRotation
            || !IsCharacterAlive(character)
        )
        {
            return 0;
        }

        ulong id = character.GetInstanceId();
        int count = _pendingExtraActions.TryGetValue(id, out int pendingCount)
            ? Math.Max(0, pendingCount)
            : 0;

        count +=
            character
                .EndActionBuffs?.Where(buff =>
                    buff != null && buff.ThisBuffName == Buff.BuffName.ExtraTurn && buff.Stack > 0
                )
                .Sum(buff => buff.Stack) ?? 0;

        return count;
    }

    private Dictionary<ulong, int> GetPreviewExtraActionCounts(
        List<Character> playerQueue,
        List<Character> enemyQueue
    )
    {
        var counts = new Dictionary<ulong, int>();
        foreach (
            Character character in playerQueue
                .Concat(enemyQueue)
                .Append(CurrentActionCharacter)
                .Where(character =>
                    character != null
                    && GodotObject.IsInstanceValid(character)
                    && character.ParticipatesInTurnRotation
                    && IsCharacterAlive(character)
                )
        )
        {
            int count = GetPreviewExtraActionCount(character);
            if (count > 0)
                counts[character.GetInstanceId()] = count;
        }

        return counts;
    }

    private static bool TryConsumePreviewExtraAction(
        Character character,
        Dictionary<ulong, int> previewExtraActions
    )
    {
        if (
            character == null
            || previewExtraActions == null
            || !previewExtraActions.TryGetValue(character.GetInstanceId(), out int count)
            || count <= 0
        )
        {
            return false;
        }

        count--;
        if (count <= 0)
            previewExtraActions.Remove(character.GetInstanceId());
        else
            previewExtraActions[character.GetInstanceId()] = count;

        return true;
    }

    private bool GetPreviewContinuationTeam(int previewPlayerActionPoin, int previewEnemyActionPoin)
    {
        if (previewPlayerActionPoin >= ActionPoinTriggerThreshold)
            return true;
        if (previewEnemyActionPoin >= ActionPoinTriggerThreshold)
            return false;

        if (
            _nextActionPreviewCharacter != null
            && GodotObject.IsInstanceValid(_nextActionPreviewCharacter)
        )
            return _nextActionPreviewCharacter.IsPlayer;

        if (CurrentActionCharacter != null && GodotObject.IsInstanceValid(CurrentActionCharacter))
            return !CurrentActionCharacter.IsPlayer;

        if (ShouldUseBattleStartInitiativePreview(previewPlayerActionPoin, previewEnemyActionPoin))
            return _battleStartPlayerActsFirst.Value;

        return GetAliveTeamSpeed(isPlayer: true) >= GetAliveTeamSpeed(isPlayer: false);
    }

    private bool ShouldUseBattleStartInitiativePreview(
        int previewPlayerActionPoin,
        int previewEnemyActionPoin
    )
    {
        return _battleStartPlayerActsFirst.HasValue
            && _playerActionCount == 0
            && _enemyActionCount == 0
            && CurrentActionCharacter == null
            && _nextActionPreviewCharacter == null
            && previewPlayerActionPoin <= 0
            && previewEnemyActionPoin <= 0;
    }

    private List<Character> GetTurnOrderQueue(bool isPlayer) =>
        (isPlayer ? PlayersList.Cast<Character>() : EnemiesList.Cast<Character>())
            .Where(character =>
                character != null
                && GodotObject.IsInstanceValid(character)
                && character.ParticipatesInTurnRotation
                && IsCharacterAlive(character)
            )
            .ToList();

    private void ApplyPreviewActionPoinGain(
        Character character,
        ref int previewPlayerActionPoin,
        ref int previewEnemyActionPoin,
        bool suppressActionPoinGain
    )
    {
        if (
            suppressActionPoinGain
            || character == null
            || !GodotObject.IsInstanceValid(character)
            || !character.ParticipatesInTurnRotation
            || !IsCharacterAlive(character)
        )
        {
            return;
        }

        if (character.IsPlayer)
            previewPlayerActionPoin += GetAliveTeamSpeed(isPlayer: true);
        else
            previewEnemyActionPoin += GetAliveTeamSpeed(isPlayer: false);
    }

    private bool TryConsumePreviewActionPoinBurst(
        Character expectedCharacter,
        List<Character> playerQueue,
        List<Character> enemyQueue,
        ref int previewPlayerActionPoin,
        ref int previewEnemyActionPoin
    )
    {
        if (
            expectedCharacter == null
            || !GodotObject.IsInstanceValid(expectedCharacter)
            || !expectedCharacter.ParticipatesInTurnRotation
            || !IsCharacterAlive(expectedCharacter)
        )
        {
            return false;
        }

        if (
            previewPlayerActionPoin >= ActionPoinTriggerThreshold
            && expectedCharacter.IsPlayer
            && playerQueue.Count > 0
            && playerQueue[0] == expectedCharacter
        )
        {
            previewPlayerActionPoin -= ActionPoinTriggerThreshold;
            return true;
        }

        if (
            previewEnemyActionPoin >= ActionPoinTriggerThreshold
            && !expectedCharacter.IsPlayer
            && enemyQueue.Count > 0
            && enemyQueue[0] == expectedCharacter
        )
        {
            previewEnemyActionPoin -= ActionPoinTriggerThreshold;
            return true;
        }

        return false;
    }

    private static void AdvancePreviewQueueForCharacter(
        Character character,
        List<Character> playerQueue,
        List<Character> enemyQueue
    )
    {
        if (character == null)
            return;

        List<Character> queue = character.IsPlayer ? playerQueue : enemyQueue;
        if (queue == null || queue.Count == 0)
            return;

        int index = queue.IndexOf(character);
        if (index < 0)
            return;

        queue.RemoveAt(index);
        queue.Add(character);
    }

    private Character GetNextPreviewCharacter(
        List<Character> playerQueue,
        List<Character> enemyQueue,
        ref int previewPlayerActionPoin,
        ref int previewEnemyActionPoin,
        ref bool nextTeamIsPlayer,
        ref Character pendingExtraActionCharacter,
        out bool triggeredByActionPoinBurst
    )
    {
        triggeredByActionPoinBurst = false;

        if (
            pendingExtraActionCharacter != null
            && GodotObject.IsInstanceValid(pendingExtraActionCharacter)
            && IsCharacterAlive(pendingExtraActionCharacter)
        )
        {
            Character extraActionCharacter = pendingExtraActionCharacter;
            pendingExtraActionCharacter = null;
            nextTeamIsPlayer = !extraActionCharacter.IsPlayer;
            return extraActionCharacter;
        }

        if (previewPlayerActionPoin >= ActionPoinTriggerThreshold)
        {
            Character playerBurstCharacter = ConsumePreviewBurstAndGetActor(
                playerQueue,
                ref previewPlayerActionPoin
            );
            if (playerBurstCharacter != null)
            {
                triggeredByActionPoinBurst = true;
                nextTeamIsPlayer = false;
                return playerBurstCharacter;
            }
        }

        if (previewEnemyActionPoin >= ActionPoinTriggerThreshold)
        {
            Character enemyBurstCharacter = ConsumePreviewBurstAndGetActor(
                enemyQueue,
                ref previewEnemyActionPoin
            );
            if (enemyBurstCharacter != null)
            {
                triggeredByActionPoinBurst = true;
                nextTeamIsPlayer = true;
                return enemyBurstCharacter;
            }
        }

        var preferredQueue = nextTeamIsPlayer ? playerQueue : enemyQueue;
        Character nextCharacter = PopNextPreviewQueueCharacter(preferredQueue);
        if (nextCharacter == null)
        {
            preferredQueue = nextTeamIsPlayer ? enemyQueue : playerQueue;
            nextCharacter = PopNextPreviewQueueCharacter(preferredQueue);
        }

        if (nextCharacter != null)
            nextTeamIsPlayer = !nextCharacter.IsPlayer;

        return nextCharacter;
    }

    private static Character ConsumePreviewBurstAndGetActor(
        List<Character> queue,
        ref int previewActionPoin
    )
    {
        Character burstActor = PopNextPreviewQueueCharacter(queue);
        if (burstActor == null)
            return null;

        previewActionPoin -= ActionPoinTriggerThreshold;
        return burstActor;
    }

    private static Character PopNextPreviewQueueCharacter(List<Character> queue)
    {
        if (queue == null || queue.Count == 0)
            return null;

        Character character = queue[0];
        queue.RemoveAt(0);
        queue.Add(character);
        return character;
    }

    private static bool TryAddNextTurnOrderEvent(
        List<Character> queue,
        Dictionary<ulong, int> orderByCharacter,
        ref int nextOrder
    )
    {
        if (queue == null || queue.Count == 0)
            return false;

        Character character = queue[0];
        queue.RemoveAt(0);
        queue.Add(character);
        return AddTurnOrderEvent(character, orderByCharacter, ref nextOrder);
    }

    private static bool AddTurnOrderEvent(
        Character character,
        Dictionary<ulong, int> orderByCharacter,
        ref int nextOrder
    )
    {
        if (
            character == null
            || !GodotObject.IsInstanceValid(character)
            || !character.ParticipatesInTurnRotation
            || !IsCharacterAlive(character)
        )
            return false;

        ulong id = character.GetInstanceId();
        if (orderByCharacter.ContainsKey(id))
        {
            nextOrder++;
            return true;
        }

        orderByCharacter[id] = nextOrder;
        nextOrder++;
        return true;
    }

    private void HideAllTurnOrderPreviews()
    {
        foreach (
            var character in GetTeamCharacters(isPlayer: true)
                .Concat(GetTeamCharacters(isPlayer: false))
        )
        {
            if (character != null && GodotObject.IsInstanceValid(character))
                character.HideTurnOrderPreview();
        }
    }

    private void RefreshEnemyIntentionDamageSummaries()
    {
        foreach (var enemy in EnemiesList)
        {
            if (enemy != null && GodotObject.IsInstanceValid(enemy))
                enemy.RefreshIntentionDamageSummary();
        }
    }

    private void RefreshEnemyAttackPreview()
    {
        HideEnemyAttackPreview();
        UserSettings.EnsureLoaded();
        if (!UserSettings.ShowEnemyAttackPreview || !IsBattleAlive())
            return;

        var previewTargets = new HashSet<Character>();
        var damageByTarget = new Dictionary<Character, (int Damage, int HitCount)>();
        EnemyCharacter enemy = GetNextEnemyAttackPreviewCharacter();
        if (enemy == null)
            return;

        {
            Character[] attackTargets = enemy.GetCurrentIntentionPreviewAttackTargets();
            Character[] debuffTargets = enemy.GetCurrentIntentionPreviewDebuffTargets();
            enemy.ShowIntentionPreviewCurves(attackTargets, debuffTargets);

            foreach (var target in enemy.GetCurrentIntentionPreviewTargets())
            {
                if (
                    target == null
                    || !GodotObject.IsInstanceValid(target)
                    || target.State == Character.CharacterState.Dying
                )
                {
                    continue;
                }

                previewTargets.Add(target);
            }

            foreach (var entry in enemy.GetCurrentIntentionPreviewDamageEntries())
            {
                if (
                    entry.Target == null
                    || !GodotObject.IsInstanceValid(entry.Target)
                    || entry.Target.State == Character.CharacterState.Dying
                )
                {
                    continue;
                }

                int damage = Math.Max(entry.Damage, 0);
                int hitCount = Math.Max(entry.HitCount, 1);
                if (damageByTarget.TryGetValue(entry.Target, out var aggregate))
                {
                    damageByTarget[entry.Target] = (
                        aggregate.Damage + damage,
                        aggregate.HitCount + hitCount
                    );
                }
                else
                {
                    damageByTarget[entry.Target] = (damage, hitCount);
                }
            }
        }

        _enemyAttackPreviewTargets = previewTargets.ToArray();
        for (int i = 0; i < _enemyAttackPreviewTargets.Length; i++)
            _enemyAttackPreviewTargets[i].ShowTargetPreview(new Color(1f, 0.32f, 0.32f, 1f));

        if (damageByTarget.Count == 0)
        {
            ClearEnemyAttackPreviewLabels();
            return;
        }

        var layer = EnsureTipLayer();
        if (layer == null)
            return;

        int labelIndex = 0;
        foreach (var kv in damageByTarget)
        {
            var label = GetOrCreateEnemyAttackPreviewLabel(layer, labelIndex++);
            ShowEnemyAttackPreviewLabel(
                label,
                kv.Value.Damage,
                kv.Value.HitCount,
                GetTargetScreenPosition(kv.Key)
            );
        }

        for (int i = labelIndex; i < _enemyAttackPreviewLabels.Count; i++)
        {
            if (GodotObject.IsInstanceValid(_enemyAttackPreviewLabels[i]))
                _enemyAttackPreviewLabels[i].Visible = false;
        }
    }

    private EnemyCharacter GetNextEnemyAttackPreviewCharacter()
    {
        return GetNextLivingCharacter(EnemiesList) as EnemyCharacter;
    }

    private void HideEnemyAttackPreview()
    {
        foreach (var enemy in GetTeamCharacters(isPlayer: false).OfType<EnemyCharacter>())
        {
            if (enemy != null && GodotObject.IsInstanceValid(enemy))
                enemy.HideAttackIntentCurve();
        }

        if (_enemyAttackPreviewTargets != null)
        {
            for (int i = 0; i < _enemyAttackPreviewTargets.Length; i++)
            {
                if (GodotObject.IsInstanceValid(_enemyAttackPreviewTargets[i]))
                    _enemyAttackPreviewTargets[i].HideTargetPreview();
            }
        }

        _enemyAttackPreviewTargets = Array.Empty<Character>();
        ClearEnemyAttackPreviewLabels();
    }

    private void ClearEnemyAttackPreviewLabels()
    {
        for (int i = 0; i < _enemyAttackPreviewLabels.Count; i++)
        {
            if (GodotObject.IsInstanceValid(_enemyAttackPreviewLabels[i]))
                _enemyAttackPreviewLabels[i].Visible = false;
        }
    }

    private void FreeEnemyAttackPreviewLabels()
    {
        for (int i = 0; i < _enemyAttackPreviewLabels.Count; i++)
        {
            if (GodotObject.IsInstanceValid(_enemyAttackPreviewLabels[i]))
                _enemyAttackPreviewLabels[i].QueueFree();
        }

        _enemyAttackPreviewLabels.Clear();
    }

    private Label GetOrCreateEnemyAttackPreviewLabel(CanvasLayer layer, int index)
    {
        while (_enemyAttackPreviewLabels.Count <= index)
        {
            var label = CreateEnemyAttackPreviewLabel();
            layer.AddChild(label);
            _enemyAttackPreviewLabels.Add(label);
        }

        var pooledLabel = _enemyAttackPreviewLabels[index];
        if (!GodotObject.IsInstanceValid(pooledLabel))
        {
            pooledLabel = CreateEnemyAttackPreviewLabel();
            layer.AddChild(pooledLabel);
            _enemyAttackPreviewLabels[index] = pooledLabel;
        }
        else if (pooledLabel.GetParent() == null)
        {
            layer.AddChild(pooledLabel);
        }

        return pooledLabel;
    }

    private static Label CreateEnemyAttackPreviewLabel()
    {
        var label = new Label
        {
            Visible = false,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            ClipText = false,
        };
        label.AddThemeFontSizeOverride("font_size", 28);
        label.AddThemeConstantOverride("outline_size", 5);
        label.AddThemeColorOverride("font_color", EnemyAttackPreviewColor);
        label.AddThemeColorOverride("font_outline_color", EnemyAttackPreviewOutlineColor);
        return label;
    }

    private static void ShowEnemyAttackPreviewLabel(
        Label label,
        int damage,
        int hitCount,
        Vector2 targetScreenPosition
    )
    {
        label.Text = hitCount > 1 ? $"{damage}({hitCount}次)" : damage.ToString();
        label.AddThemeColorOverride("font_color", EnemyAttackPreviewColor);
        label.AddThemeColorOverride("font_outline_color", EnemyAttackPreviewOutlineColor);
        label.Modulate = Colors.White;
        label.Scale = Vector2.One;
        label.Visible = true;

        Vector2 size = label.GetCombinedMinimumSize();
        if (size == Vector2.Zero)
            size = new Vector2(120f, 44f);
        label.Size = size;

        Vector2 anchor = targetScreenPosition + EnemyAttackPreviewLabelOffset;
        label.Position = anchor - size / 2f;
    }

    private CanvasLayer EnsureTipLayer()
    {
        var root = GetTree()?.Root;
        if (root == null)
            return null;

        var existingLayer = root.GetNodeOrNull<CanvasLayer>("TipLayer");
        if (existingLayer != null)
            return existingLayer;

        existingLayer = new CanvasLayer { Layer = 6, Name = "TipLayer" };
        root.AddChild(existingLayer);
        return existingLayer;
    }

    private static Vector2 GetTargetScreenPosition(Character target)
    {
        if (target == null || !GodotObject.IsInstanceValid(target))
            return Vector2.Zero;

        return target.GetGlobalTransformWithCanvas().Origin;
    }

    public int GetAliveTeamSpeed(bool isPlayer) =>
        GetTeamCharacters(isPlayer)
            .Where(IsCharacterAlive)
            .Where(x => x.CountsTowardTeamSpeed)
            .Sum(x => x.Speed);

    private void CaptureBattleStartInitiative()
    {
        _battleStartPlayerActsFirst =
            GetAliveTeamSpeed(isPlayer: true) >= GetAliveTeamSpeed(isPlayer: false);
    }

    private bool BattleStartPlayerActsFirst() =>
        _battleStartPlayerActsFirst
        ?? GetAliveTeamSpeed(isPlayer: true) >= GetAliveTeamSpeed(isPlayer: false);

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
        RefreshTurnOrderPreview();
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

        RefreshTurnOrderPreview();
        return true;
    }

    private bool HasPendingExtraAction(Character character)
    {
        if (
            character == null
            || !GodotObject.IsInstanceValid(character)
            || !character.ParticipatesInTurnRotation
            || !IsCharacterAlive(character)
        )
        {
            return false;
        }

        return _pendingExtraActions.TryGetValue(character.GetInstanceId(), out int pendingCount)
            && pendingCount > 0;
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

            enemy.IntentionIndex = enemy.RollIntentionIndex(
                EnemyCharacter.NextActionEnergyPreviewBonus
            );
            await enemy.DisappearIntention();
            if (!CanContinue(token))
            {
                return false;
            }

            enemy.IntentionContorl.Visible = true;
            enemy.DisplayIntention();
        }

        RefreshEnemyAttackPreview();
        return await DelayOrCancel(PostActionDelayMs, token);
    }

    private async Task ApplyRelicBattleEffects(CancellationToken token)
    {
        var relics = MapNode?.PlayerResourceState?.RelicList;
        if (relics == null)
        {
            return;
        }

        var relicSnapshot = relics.Where(relic => relic != null).ToArray();
        for (int i = 0; i < relicSnapshot.Length; i++)
        {
            if (!CanContinue(token))
            {
                return;
            }

            await relicSnapshot[i].BattleEffect(this);
            if (i < relicSnapshot.Length - 1)
            {
                await DelayOrCancel(BattleStartEffectIntervalMs, token);
            }
        }
    }

    private void SetActionPoinValue(
        ref int currentValue,
        int nextValue,
        GlowLabel label,
        Label totalLabel,
        ProgressBar bar,
        IEnumerable<Character> characters
    )
    {
        int clampedValue = Math.Max(nextValue, 0);
        bool speedValueChanged = currentValue != clampedValue;
        currentValue = clampedValue;
        if (!IsBattleAlive())
        {
            return;
        }

        if (GodotObject.IsInstanceValid(label))
        {
            label.Text = currentValue.ToString();
        }

        if (GodotObject.IsInstanceValid(totalLabel))
        {
            totalLabel.Text = $"({SumAliveSpeed(characters)})";
        }

        if (GodotObject.IsInstanceValid(bar))
        {
            if (speedValueChanged)
                CreateTween().TweenProperty(bar, "value", currentValue, 0.3f);
            else
                bar.Value = currentValue;
        }

        RefreshTurnOrderPreview();
    }

    private static int SumAliveSpeed(IEnumerable<Character> characters) =>
        characters?.Where(IsCharacterAlive).Where(x => x.CountsTowardTeamSpeed).Sum(x => x.Speed)
        ?? 0;

    public void RequestActionPoinUiRefresh(bool isPlayer)
    {
        if (isPlayer)
            _pendingPlayerActionPoinUiRefresh = true;
        else
            _pendingEnemyActionPoinUiRefresh = true;

        if (_actionPoinUiRefreshScheduled)
            return;

        _actionPoinUiRefreshScheduled = true;
        _ = FlushActionPoinUiRefreshNextFrameAsync();
    }

    private async Task FlushActionPoinUiRefreshNextFrameAsync()
    {
        if (!IsInsideTree())
        {
            _actionPoinUiRefreshScheduled = false;
            return;
        }

        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

        bool refreshPlayer = _pendingPlayerActionPoinUiRefresh;
        bool refreshEnemy = _pendingEnemyActionPoinUiRefresh;
        _pendingPlayerActionPoinUiRefresh = false;
        _pendingEnemyActionPoinUiRefresh = false;
        _actionPoinUiRefreshScheduled = false;

        if (!IsBattleAlive())
            return;

        if (refreshPlayer)
            PlayerActionPoin = PlayerActionPoin;
        if (refreshEnemy)
            EnemyActionPoin = EnemyActionPoin;
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
        ApplyBossSummonStatMultiplierIfNeeded(summon, summoner);

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

    private void ApplyBossSummonStatMultiplierIfNeeded(SummonCharacter summon, Character summoner)
    {
        if (
            summon == null
            || summoner == null
            || summoner.IsPlayer
            || CurrentLevelNode?.Type != LevelNode.LevelType.Boss
            || GameInfo.CurrentLevel <= 0
        )
        {
            return;
        }

        summon.ApplyCombatStatMultiplier(LevelNode.RegionTwoBossStatMultiplier, refillLife: true);
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

        if (SuppressActionPoinGainThisTurn != true && character?.ParticipatesInTurnRotation == true)
        {
            if (character.IsPlayer)
                PlayerActionPoin += GetAliveTeamSpeed(isPlayer: true);
            else
                EnemyActionPoin += GetAliveTeamSpeed(isPlayer: false);
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
            disaster.ConsumeOneStack();

            if (HasBattleEnded() || !IsBattleAlive())
                return;
        }

        TriggerSanctuaryTurnEndBuffs(actingCharacter, targets);
        await TriggerVoidTurnEndBuffs(actingCharacter, targets);
    }

    private void TriggerSanctuaryTurnEndBuffs(Character actingCharacter, Character[] targets)
    {
        if (actingCharacter == null || targets == null || targets.Length == 0)
            return;

        for (int i = 0; i < targets.Length; i++)
        {
            Character target = targets[i];
            if (target == null || target.IsPlayer != actingCharacter.IsPlayer)
                continue;

            EndActionBuff sanctuaryBuff = target.EndActionBuffs?.FirstOrDefault(x =>
                x != null && x.ThisBuffName == Buff.BuffName.Sanctuary && x.Stack > 0
            );
            if (sanctuaryBuff == null)
                continue;

            using var _ = target.BeginEffectSource(
                Buff.GetBuffDisplayName(Buff.BuffName.Sanctuary)
            );
            for (int triggerCount = 0; triggerCount < sanctuaryBuff.Stack; triggerCount++)
            {
                target.Recover(0, source: target);
            }
        }
    }

    private async Task TriggerVoidTurnEndBuffs(Character actingCharacter, Character[] targets)
    {
        if (actingCharacter == null || targets == null || targets.Length == 0)
            return;

        for (int i = 0; i < targets.Length; i++)
        {
            Character target = targets[i];
            if (
                target == null
                || target == actingCharacter
                || target.IsPlayer != actingCharacter.IsPlayer
                || target.State == Character.CharacterState.Dying
            )
                continue;

            EndActionBuff voidBuff = target.EndActionBuffs?.FirstOrDefault(x =>
                x != null && x.ThisBuffName == Buff.BuffName.Void && x.Stack > 0
            );
            if (voidBuff == null)
                continue;

            using var _ = target.BeginEffectSource(Buff.GetBuffDisplayName(Buff.BuffName.Void));
            await target.IncreaseProperties(PropertyType.Power, voidBuff.Stack, target);

            if (HasBattleEnded() || !IsBattleAlive())
                return;
        }
    }

    public Task EndEmitExtraActionS(Character character) => EndEmitS(character);

    public List<Func<Task>> StartEffectList = new();

    public async Task BattleBegin1(CancellationToken token)
    {
        for (int i = 0; i < StartEffectList.Count; i++)
        {
            if (!CanContinue(token))
            {
                return;
            }

            await StartEffectList[i]();
            if (i < StartEffectList.Count - 1)
            {
                await DelayOrCancel(BattleStartEffectIntervalMs, token);
            }
        }

        if (!CanContinue(token))
        {
            return;
        }

        if (!BattleStartPlayerActsFirst())
        {
            await CharacterAction(EnemiesList, token, () => GetNextLivingCharacter(PlayersList));
        }

        for (int i = 0; i < MaxBattleTurns && CanContinue(token); i++)
        {
            await CharacterAction(PlayersList, token, () => GetNextLivingCharacter(EnemiesList));
            if (!CanContinue(token))
            {
                return;
            }

            await CharacterAction(EnemiesList, token, () => GetNextLivingCharacter(PlayersList));
        }

        if (CanContinue(token))
        {
            GD.Print("Battle completed after 100 turns");
            Retreat();
        }
    }

    public async Task CharacterAction<T>(
        List<T> characterlist,
        CancellationToken token,
        Func<Character> getNextAfterAction = null
    )
        where T : Character
    {
        if (!CanAct(characterlist, token))
        {
            return;
        }

        DyingDetector(characterlist);
        Character actingCharacter = characterlist[0];
        RotateFrontToBack(characterlist);
        Character nextPreview = GetNextActionPreviewAfterCurrentAction(
            actingCharacter,
            characterlist,
            getNextAfterAction
        );
        if (nextPreview == actingCharacter)
            nextPreview = null;
        SetNextActionPreviewCharacter(nextPreview);
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
                if (
                    isExtraAction
                    && SuppressActionPoinGainThisTurn
                    && actingCharacter is PlayerCharacter extraActionPlayer
                    && GodotObject.IsInstanceValid(extraActionPlayer)
                )
                {
                    extraActionPlayer.TryDrawBattleCards(1);
                }

                await StartActionAndWaitForNext(actingCharacter);
            }
            finally
            {
                SetExtraActionState(actingCharacter, false);
            }

            bool hasPendingExtraAction = HasPendingExtraAction(actingCharacter);
            UpdateTurnOrderPreviewAfterResolvedAction(
                actingCharacter,
                characterlist,
                getNextAfterAction,
                hasPendingExtraAction
            );

            if (!CanContinue(token) || !TryConsumeExtraAction(actingCharacter))
                break;

            isExtraAction = true;
        }

        if (!await DelayOrCancel(PostActionDelayMs, token) || await HandleBattleOver(token))
        {
            return;
        }

        await TryTriggerActionPoinBurst(
            PlayersList,
            () => PlayerActionPoin,
            value => PlayerActionPoin = value,
            PlayerActionPoinHintDelayMs,
            token
        );
        if (CanContinue(token))
        {
            await TryTriggerActionPoinBurst(
                EnemiesList,
                () => EnemyActionPoin,
                value => EnemyActionPoin = value,
                EnemyActionPoinHintDelayMs,
                token
            );
        }
    }

    private Character GetNextActionPreviewAfterCurrentAction<T>(
        Character actingCharacter,
        List<T> rotatedCurrentTeam,
        Func<Character> getNextAfterAction
    )
        where T : Character
    {
        Character actionPoinBurstPreview = GetCurrentActionPoinBurstPreview();
        if (actionPoinBurstPreview != null)
            return actionPoinBurstPreview;

        return getNextAfterAction != null
            ? getNextAfterAction()
            : GetNextLivingCharacter(rotatedCurrentTeam);
    }

    private Character GetCurrentActionPoinBurstPreview()
    {
        if (PlayerActionPoin >= ActionPoinTriggerThreshold)
            return GetNextLivingCharacter(PlayersList);

        if (EnemyActionPoin >= ActionPoinTriggerThreshold)
            return GetNextLivingCharacter(EnemiesList);

        return null;
    }

    private void UpdateTurnOrderPreviewAfterResolvedAction<T>(
        Character actingCharacter,
        List<T> currentTeam,
        Func<Character> getNextAfterAction,
        bool sameCharacterActsAgain
    )
        where T : Character
    {
        if (!IsBattleAlive())
            return;

        Character nextPreview;
        if (sameCharacterActsAgain && IsCharacterAlive(actingCharacter))
        {
            nextPreview = actingCharacter;
        }
        else if (PlayerActionPoin >= ActionPoinTriggerThreshold)
        {
            nextPreview = GetNextLivingCharacter(PlayersList);
        }
        else if (EnemyActionPoin >= ActionPoinTriggerThreshold)
        {
            nextPreview = GetNextLivingCharacter(EnemiesList);
        }
        else
        {
            nextPreview =
                getNextAfterAction != null
                    ? getNextAfterAction()
                    : GetNextLivingCharacter(currentTeam);
        }

        SetNextActionPreviewCharacter(nextPreview);
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
        && (
            GameInfo.IsDifficultyBonusActive(GameDifficultyBonus.FreeRetreat)
            || (MapNode?.PlayerResourceState?.TransitionEnergy ?? GameInfo.TransitionEnergy) > 0
        );

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

        if (
            consumeTransitionEnergy
            && !GameInfo.IsDifficultyBonusActive(GameDifficultyBonus.FreeRetreat)
        )
        {
            ConsumeCoreEnergy(ManualRetreatCoreEnergyCost);
            if (IsCoreEnergyDepleted())
            {
                await ShowGameOverAsync();
                return;
            }
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
        UnlockMapNodes();

        if (IsBattleInstanceValid())
        {
            GetParent()?.QueueFree();
        }
    }

    public void AbortBattle(bool unlockMapNodes = true)
    {
        if (_retreating)
            return;

        _retreating = true;
        TryCancelLifetime();
        TryEmitNextToUnblock();

        if (!IsBattleInstanceValid())
            return;

        PlayersList?.Clear();
        EnemiesList?.Clear();
        ClearSummons(queueFree: true);
        if (unlockMapNodes)
            UnlockMapNodes();

        if (IsBattleInstanceValid())
            GetParent()?.QueueFree();
    }

    private async Task HandleDefeatAsync()
    {
        if (_retreating)
            return;

        _retreating = true;
        TryCancelLifetime();
        TryEmitNextToUnblock();

        if (RetreatButton != null)
            RetreatButton.Disabled = true;

        await ShowGameOverAsync();
    }

    private async Task ShowGameOverAsync()
    {
        GameInfo.RecordCurrentRunHistory(victory: false);
        SaveSystem.SaveAll();

        MapNode?.BlackMaskAnimation(0.55f, hideAfter: false);
        await Task.Delay(PostActionDelayMs);

        if (!IsBattleInstanceValid())
            return;

        GameOverSummary.Show(this);

        if (GetTree() != null)
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

        await (SceneTransitionLayer.Ensure(this)?.FadeFromBlackAsync(0.24f) ?? Task.CompletedTask);

        PlayersList?.Clear();
        EnemiesList?.Clear();
        ClearSummons(queueFree: true);

        if (IsBattleInstanceValid())
            GetParent()?.QueueFree();
    }

    private void UnlockMapNodes()
    {
        var levelProgress = MapNode?.GetNodeOrNull<LevelProgress>("LevelProgress");
        levelProgress ??= CurrentLevelNode?.GetParent()?.GetParent() as LevelProgress;
        levelProgress?.UnlockAllNodes();
    }

    private bool CanContinue(CancellationToken token) =>
        !token.IsCancellationRequested && IsBattleAlive();

    private bool CanAct<T>(List<T> characters, CancellationToken token)
        where T : Character => characters is { Count: > 0 } && CanContinue(token);

    public bool ShouldAbortSkillResolution() => _retreating || HasBattleEnded() || !IsBattleAlive();

    public async Task<bool> ResolveBattleOverAfterSkillAsync()
    {
        if (!IsBattleAlive())
            return true;

        return await HandleBattleOver(_lifetimeCts.Token, delayAfterHandling: false);
    }

    private async Task<bool> HandleBattleOver(
        CancellationToken token,
        bool delayAfterHandling = true
    )
    {
        bool playersDefeated = IsTeamDefeated(PlayersList);
        bool enemiesDefeated = IsTeamDefeated(EnemiesList);
        if (!playersDefeated && !enemiesDefeated)
        {
            return false;
        }

        GD.Print("over");
        if (enemiesDefeated && HasLivingMember(PlayersList))
        {
            Retreat();
        }
        else
        {
            Retreat(consumeTransitionEnergy: true);
        }

        if (delayAfterHandling)
            await DelayOrCancel(BattleOverDelayMs, token);
        return true;
    }

    private async Task TryTriggerActionPoinBurst<T>(
        List<T> team,
        Func<int> getActionPoin,
        Action<int> setActionPoin,
        int delayMs,
        CancellationToken token
    )
        where T : Character
    {
        if (team == null || team.Count == 0 || getActionPoin() < ActionPoinTriggerThreshold)
        {
            return;
        }

        setActionPoin(getActionPoin() - ActionPoinTriggerThreshold);
        if (!await DelayOrCancel(delayMs, token) || !CanContinue(token))
        {
            return;
        }

        BuffHintLabel.Spawn(team[0], ActionPoinTriggerText, Vector2.Zero);
        SuppressActionPoinGainThisTurn = true;
        try
        {
            DyingDetector(team);
            Character burstActor = team[0];
            if (burstActor is PlayerCharacter player && GodotObject.IsInstanceValid(player))
            {
                player.UpdataEnergy(1, player);
            }

            SetNextActionPreviewCharacter(GetNextLivingCharacter(team));
            Func<Character> getNextAfterBurstAction = burstActor.IsPlayer
                ? () => GetNextLivingCharacter(EnemiesList)
                : () => GetNextLivingCharacter(PlayersList);
            await CharacterAction(team, token, getNextAfterBurstAction);
        }
        finally
        {
            SuppressActionPoinGainThisTurn = false;
        }
    }

    private static void RotateFrontToBack<T>(List<T> characters)
    {
        if (characters.Count <= 1)
            return;
        characters.Reverse(1, characters.Count - 1);
        characters.Reverse();
    }

    private static Character GetNextLivingCharacter<T>(IEnumerable<T> characters)
        where T : Character => characters?.FirstOrDefault(IsCharacterAlive);

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

            await StartActionAndWaitForNext(summons[i]);
        }
    }

    private async Task StartActionAndWaitForNext(Character expectedCharacter)
    {
        if (!CanStartActionAndWait(expectedCharacter))
            return;

        bool receivedNext = false;
        void OnNext(Character emittedCharacter)
        {
            if (emittedCharacter == null || emittedCharacter == expectedCharacter)
                receivedNext = true;
        }

        Next += OnNext;
        try
        {
            SetCurrentActionCharacter(expectedCharacter);
            Relic.ApplyPlayerActionStartRelicEffects(
                this,
                expectedCharacter,
                GetUpcomingPlayerActionNumber(expectedCharacter)
            );
            expectedCharacter.StartAction();

            while (CanWaitForActionNext(expectedCharacter) && !receivedNext)
            {
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            }
        }
        finally
        {
            Next -= OnNext;
        }

        if (!receivedNext && expectedCharacter?.State == Character.CharacterState.Dying)
            CleanupDiedCurrentActionCharacter(expectedCharacter);
    }

    private int GetUpcomingPlayerActionNumber(Character character)
    {
        if (
            character == null
            || !character.IsPlayer
            || !character.ParticipatesInTurnRotation
            || character.IsSummon
        )
        {
            return 0;
        }

        return _playerActionCount + 1;
    }

    private bool CanWaitForActionNext(Character expectedCharacter)
    {
        return CanStartActionAndWait(expectedCharacter);
    }

    private bool CanStartActionAndWait(Character expectedCharacter)
    {
        return IsBattleAlive()
            && !HasBattleEnded()
            && expectedCharacter != null
            && GodotObject.IsInstanceValid(expectedCharacter)
            && expectedCharacter.State != Character.CharacterState.Dying;
    }

    private void CleanupDiedCurrentActionCharacter(Character character)
    {
        ClearCurrentActionCharacter(character);

        if (character is not PlayerCharacter player)
            return;

        player.DiscardBattleHand();
        CharacterControl?.DisablePlayerActions(player);
        RetreatButton.Disabled = true;
        MapNode?.PlayerResourceState?.SetItemsEnabled(false);
    }

    private bool HasBattleEnded() => IsTeamDefeated(PlayersList) || IsTeamDefeated(EnemiesList);

    public void HandleCharacterEnteredDying(Character target)
    {
        if (target is not PlayerCharacter || target.IsSummon)
            return;

        ConsumeCoreEnergy(PlayerDyingCoreEnergyCost);
        if (IsCoreEnergyDepleted())
            _ = HandleDefeatAsync();
    }

    private int ConsumeCoreEnergy(int amount)
    {
        if (amount <= 0)
            return GetCoreEnergy();

        var resourceState = MapNode?.PlayerResourceState;
        if (resourceState != null)
        {
            resourceState.TransitionEnergy = Math.Max(0, resourceState.TransitionEnergy - amount);
            return resourceState.TransitionEnergy;
        }

        GameInfo.TransitionEnergy = Math.Clamp(
            GameInfo.TransitionEnergy - amount,
            0,
            GameInfo.TransitionEnergyMax
        );
        return GameInfo.TransitionEnergy;
    }

    private int GetCoreEnergy() =>
        MapNode?.PlayerResourceState?.TransitionEnergy ?? GameInfo.TransitionEnergy;

    private bool IsCoreEnergyDepleted() => GetCoreEnergy() <= 0;

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
        var levelType = CurrentLevelNode?.Type ?? LevelNode.LevelType.Normal;
        bool allowRareSkillRewards = GetCompletedBattleCount() >= 3;
        reward.AllowRareSkillRewards = allowRareSkillRewards;
        int skillRewardGroups = GetSkillRewardGroupCount();
        for (int i = 0; i < skillRewardGroups; i++)
            reward.AddSkillRewardEntry(
                forceRare: allowRareSkillRewards && levelType == LevelNode.LevelType.Boss
            );

        var talentRewardPreview = GameInfo.PreviewBattleTalentPointReward(CurrentLevelNode);
        if (talentRewardPreview.Granted)
            reward.AddTalentPointRewardEntry(talentRewardPreview);

        var rng = new Random(CurrentLevelNode?.RandomNum ?? System.Environment.TickCount);
        bool addRelic = ShouldAddRelicReward(levelType);
        bool addRegionalBonusRelic = ShouldAddRegionalBonusRelicReward(levelType);
        bool addItem = GameInfo.RollBattleItemDrop(rng);
        var pendingRelics = new HashSet<RelicID>();

        TryAddRelicReward(reward, rng, addRelic, pendingRelics);
        TryAddRelicReward(reward, rng, addRegionalBonusRelic, pendingRelics);
        TryAddItemReward(reward, rng, addItem);
    }

    private static int GetSkillRewardGroupCount()
    {
        int completedBattleCount = GetCompletedBattleCount();

        int bonusGroups =
            completedBattleCount < EarlyBattleBonusSkillRewardBattles
                ? EarlyBattleExtraSkillRewardGroups
                : 0;
        return 1 + bonusGroups;
    }

    private static int GetCompletedBattleCount()
    {
        return GameInfo.CompletedLevelNodeRecords?.Values.Count(record =>
                record != null
                && record.NodeType
                    is LevelNode.LevelType.Normal
                        or LevelNode.LevelType.Elite
                        or LevelNode.LevelType.Boss
            ) ?? 0;
    }

    private static bool ShouldAddRelicReward(LevelNode.LevelType levelType)
    {
        return levelType == LevelNode.LevelType.Elite;
    }

    private bool ShouldAddRegionalBonusRelicReward(LevelNode.LevelType levelType)
    {
        if (!IsBattleNode(levelType) || levelType == LevelNode.LevelType.Boss)
            return false;

        return GetCurrentRegionalBattleNumber() == RegionalBonusRelicBattleNumber;
    }

    private int GetCurrentRegionalBattleNumber()
    {
        int completedBattleCount =
            GameInfo.CompletedLevelNodeRecords?.Values.Count(record =>
                record != null
                && record.MapLevel == GameInfo.CurrentLevel
                && IsBattleNode(record.NodeType)
            ) ?? 0;

        return completedBattleCount + 1;
    }

    private static bool IsBattleNode(LevelNode.LevelType levelType)
    {
        return levelType
            is LevelNode.LevelType.Normal
                or LevelNode.LevelType.Elite
                or LevelNode.LevelType.Boss;
    }

    private static void TryAddRelicReward(
        Reward reward,
        Random rng,
        bool addRelic,
        HashSet<RelicID> pendingRelics = null
    )
    {
        if (!addRelic)
        {
            return;
        }

        var relicDropPool = Relic
            .GetUnownedOfferPool()
            .Where(relicId => pendingRelics == null || !pendingRelics.Contains(relicId))
            .ToArray();
        if (relicDropPool.Length > 0)
        {
            RelicID relicId = PickRandom(relicDropPool, rng);
            reward.AddRelicRewardEntry(relicId);
            pendingRelics?.Add(relicId);
        }
    }

    private static void TryAddItemReward(Reward reward, Random rng, bool addItem)
    {
        if (!addItem)
        {
            return;
        }

        ItemID[] itemPool =
        [
            ItemID.Health,
            ItemID.Explosion,
            ItemID.ElectromagneticInterference,
            ItemID.SpaceOscillation,
            ItemID.StreamingTransmission,
        ];
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
