using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;

public readonly struct BattleReadyDeckOperationResult(
    bool changed,
    int playerIndex,
    SkillID sourceSkillId,
    SkillID? resultSkillId,
    string message
)
{
    public bool Changed { get; } = changed;
    public int PlayerIndex { get; } = playerIndex;
    public SkillID SourceSkillId { get; } = sourceSkillId;
    public SkillID? ResultSkillId { get; } = resultSkillId;
    public string Message { get; } = message;
}

public readonly struct TransformDeckCardRequest(
    int playerIndex,
    SkillID sourceSkillId,
    SkillCard sourceCard = null,
    CardDeckOperationSnapshot? snapshot = null
)
{
    public int PlayerIndex { get; } = playerIndex;
    public SkillID SourceSkillId { get; } = sourceSkillId;
    public SkillCard SourceCard { get; } = sourceCard;
    public CardDeckOperationSnapshot? Snapshot { get; } =
        snapshot ?? CardDeckOperationSnapshot.FromCard(sourceCard);
}

public readonly struct CardDeckOperationSnapshot(
    Vector2 globalPosition,
    Vector2 scale,
    float rotation,
    Vector2 pivotOffset
)
{
    public Vector2 GlobalPosition { get; } = globalPosition;
    public Vector2 Scale { get; } = scale;
    public float Rotation { get; } = rotation;
    public Vector2 PivotOffset { get; } = pivotOffset;

    public static CardDeckOperationSnapshot? FromCard(SkillCard card)
    {
        if (card == null || !GodotObject.IsInstanceValid(card) || !card.IsInsideTree())
            return null;

        return new CardDeckOperationSnapshot(
            card.GlobalPosition,
            card.Scale,
            card.Rotation,
            card.PivotOffset
        );
    }
}

public partial class BattleReady
{
    private const float DeckOperationExhaustDuration = 0.5f;
    private static readonly Vector2 DeckOperationCardDisplayScale = new(0.66f, 0.66f);

    public static async Task<BattleReadyDeckOperationResult> AcquireDeckCardAsync(
        Node caller,
        int playerIndex,
        SkillID skillId,
        SkillCard sourceCard = null,
        bool allowDuplicate = true
    )
    {
        if (!TryAddDeckOperationCard(playerIndex, skillId, allowDuplicate))
            return default;

        SkillCard flyCard = IsUsableAnimationCard(sourceCard)
            ? await PrepareDeckOperationSourceCardForFlyAsync(caller, sourceCard)
            : await CreateFloatingDeckOperationCardAsync(caller, playerIndex, skillId, sourceCard);
        await FlyDeckOperationCardToTacticsAsync(caller, flyCard);

        return new BattleReadyDeckOperationResult(
            true,
            playerIndex,
            skillId,
            skillId,
            $"获得卡牌：[b]{GetDeckOperationSkillName(skillId)}[/b]"
        );
    }

    public static async Task<BattleReadyDeckOperationResult> CopyDeckCardAsync(
        Node caller,
        int playerIndex,
        SkillID skillId,
        SkillCard sourceCard = null
    )
    {
        if (!TryAddDeckOperationCard(playerIndex, skillId, allowDuplicate: true))
            return default;

        SkillCard flyCard = await CreateFloatingDeckOperationCardAsync(
            caller,
            playerIndex,
            skillId,
            sourceCard
        );
        await FlyDeckOperationCardToTacticsAsync(caller, flyCard);

        return new BattleReadyDeckOperationResult(
            true,
            playerIndex,
            skillId,
            skillId,
            $"复制卡牌：[b]{GetDeckOperationSkillName(skillId)}[/b]"
        );
    }

    public static async Task<BattleReadyDeckOperationResult> RemoveDeckCardAsync(
        Node caller,
        int playerIndex,
        SkillID skillId,
        SkillCard sourceCard = null
    )
    {
        if (!TryGetDeckOperationPlayer(playerIndex, out var players, out var info))
            return default;

        info.GainedSkills ??= new List<SkillID>();
        if (!info.GainedSkills.Remove(skillId))
            return default;

        EnsureTakenSkillsStillOwned(ref info);
        players[playerIndex] = info;
        GameInfo.PlayerCharacters = players;

        SkillCard exhaustCard =
            IsUsableAnimationCard(sourceCard)
                ? sourceCard
                : await CreateFloatingDeckOperationCardAsync(caller, playerIndex, skillId, sourceCard);
        await PlayDeckOperationExhaustAsync(exhaustCard);

        return new BattleReadyDeckOperationResult(
            true,
            playerIndex,
            skillId,
            null,
            $"删除卡牌：[b]{GetDeckOperationSkillName(skillId)}[/b]"
        );
    }

    public static async Task<BattleReadyDeckOperationResult> TransformDeckCardAsync(
        Node caller,
        int playerIndex,
        SkillID sourceSkillId,
        SkillCard sourceCard = null,
        Random rng = null,
        CardDeckOperationSnapshot? snapshot = null
    )
    {
        IReadOnlyList<BattleReadyDeckOperationResult> results = await TransformDeckCardsAsync(
            caller,
            new[]
            {
                new TransformDeckCardRequest(
                    playerIndex,
                    sourceSkillId,
                    sourceCard,
                    snapshot
                ),
            },
            rng
        );
        return results.Count > 0 ? results[0] : default;
    }

    public static async Task<IReadOnlyList<BattleReadyDeckOperationResult>> TransformDeckCardsAsync(
        Node caller,
        IReadOnlyList<TransformDeckCardRequest> requests,
        Random rng = null
    )
    {
        if (requests == null || requests.Count == 0)
            return Array.Empty<BattleReadyDeckOperationResult>();

        rng ??= new Random();
        var pending = new List<TransformDeckCardJob>();
        var results = new List<BattleReadyDeckOperationResult>();

        foreach (TransformDeckCardRequest request in requests)
        {
            if (!TryGetDeckOperationPlayer(request.PlayerIndex, out var players, out var info))
                continue;

            SkillID? replacement = PickTransformDeckCard(info, request.SourceSkillId, rng);
            if (!replacement.HasValue)
            {
                results.Add(
                    new BattleReadyDeckOperationResult(
                        false,
                        request.PlayerIndex,
                        request.SourceSkillId,
                        null,
                        "没有可变化的目标卡牌"
                    )
                );
                continue;
            }

            pending.Add(
                new TransformDeckCardJob(
                    request,
                    players,
                    info,
                    replacement.Value,
                    BuildDeckOperationPreviewSkill(replacement.Value, info, request.PlayerIndex)
                )
            );
        }

        if (pending.Count == 0)
            return results;

        foreach (TransformDeckCardJob job in pending)
        {
            if (!ApplyTransformDeckCardData(job))
                continue;

            results.Add(
                new BattleReadyDeckOperationResult(
                    true,
                    job.Request.PlayerIndex,
                    job.Request.SourceSkillId,
                    job.ReplacementSkillId,
                    $"变化卡牌：[b]{GetDeckOperationSkillName(job.Request.SourceSkillId)}[/b] → [b]{GetDeckOperationSkillName(job.ReplacementSkillId)}[/b]"
                )
            );
        }

        if (results.Count == 0)
            return results;

        var usedSourceCards = new HashSet<SkillCard>();
        var animationCards = new List<SkillCard>(pending.Count);
        var revealSkills = new List<Skill>(pending.Count);

        foreach (TransformDeckCardJob job in pending)
        {
            if (!job.DataSwapped)
                continue;

            SkillCard card = await ResolveTransformDeckOperationCardAsync(
                caller,
                job,
                usedSourceCards
            );
            animationCards.Add(card);
            revealSkills.Add(job.ReplacementSkill);
        }

        var transformTasks = new List<Task>(animationCards.Count);
        for (int i = 0; i < animationCards.Count; i++)
        {
            SkillCard card = animationCards[i];
            Skill replacementSkill = revealSkills[i];
            if (IsUsableAnimationCard(card))
            {
                transformTasks.Add(
                    CardTransformShineVfx.PlayOnCardAsync(
                        card,
                        () =>
                        {
                            if (replacementSkill != null)
                                card.SetSkill(replacementSkill);
                        }
                    )
                );
            }
            else if (replacementSkill != null)
            {
                card?.SetSkill(replacementSkill);
            }
        }

        if (transformTasks.Count > 0)
            await Task.WhenAll(transformTasks);

        var flyTasks = new List<Task>(animationCards.Count);
        foreach (SkillCard card in animationCards)
        {
            if (IsUsableAnimationCard(card))
                flyTasks.Add(FlyDeckOperationCardToTacticsAsync(caller, card));
        }

        if (flyTasks.Count > 0)
            await Task.WhenAll(flyTasks);

        return results;
    }

    private static bool ApplyTransformDeckCardData(TransformDeckCardJob job)
    {
        PlayerInfoStructure[] players = GameInfo.PlayerCharacters;
        if (
            players == null
            || (uint)job.Request.PlayerIndex >= (uint)players.Length
        )
        {
            return false;
        }

        PlayerInfoStructure info = players[job.Request.PlayerIndex];
        info.GainedSkills ??= new List<SkillID>();
        if (!info.GainedSkills.Remove(job.Request.SourceSkillId))
            return false;

        info.GainedSkills.Add(job.ReplacementSkillId);
        EnsureTakenSkillsStillOwned(ref info, job.ReplacementSkillId);
        players[job.Request.PlayerIndex] = info;
        GameInfo.PlayerCharacters = players;
        job.Players = players;
        job.Info = info;
        job.DataSwapped = true;
        job.ReplacementSkill = BuildDeckOperationPreviewSkill(
            job.ReplacementSkillId,
            info,
            job.Request.PlayerIndex
        );
        return true;
    }

    private sealed class TransformDeckCardJob
    {
        public TransformDeckCardJob(
            TransformDeckCardRequest request,
            PlayerInfoStructure[] players,
            PlayerInfoStructure info,
            SkillID replacementSkillId,
            Skill replacementSkill
        )
        {
            Request = request;
            Players = players;
            Info = info;
            ReplacementSkillId = replacementSkillId;
            ReplacementSkill = replacementSkill;
        }

        public TransformDeckCardRequest Request { get; }
        public PlayerInfoStructure[] Players { get; set; }
        public PlayerInfoStructure Info { get; set; }
        public SkillID ReplacementSkillId { get; }
        public Skill ReplacementSkill { get; set; }
        public bool DataSwapped { get; set; }
    }

    private static async Task<SkillCard> ResolveTransformDeckOperationCardAsync(
        Node caller,
        TransformDeckCardJob job,
        HashSet<SkillCard> usedSourceCards
    )
    {
        SkillCard sourceCard = job.Request.SourceCard;
        if (
            IsUsableAnimationCard(sourceCard)
            && usedSourceCards.Add(sourceCard)
            && TryDetachDeckOperationCard(caller, sourceCard)
        )
        {
            return sourceCard;
        }

        return await CreateFloatingDeckOperationCardAsync(
            caller,
            job.Request.PlayerIndex,
            job.Request.SourceSkillId,
            sourceCard,
            job.Request.Snapshot
        );
    }

    public static bool CanTransformDeckCard(PlayerInfoStructure info, SkillID sourceSkillId) =>
        GetTransformDeckCardPool(info, sourceSkillId).Length > 0;

    public static SkillID? PickTransformDeckCard(
        PlayerInfoStructure info,
        SkillID sourceSkillId,
        Random rng = null
    )
    {
        SkillID[] pool = GetTransformDeckCardPool(info, sourceSkillId);
        if (pool.Length == 0)
            return null;

        rng ??= new Random();
        return pool[rng.Next(pool.Length)];
    }

    public static SkillID[] GetTransformDeckCardPool(
        PlayerInfoStructure info,
        SkillID sourceSkillId
    )
    {
        var pool = (info.AllSkills ?? Array.Empty<SkillID>())
            .Where(skillId =>
                skillId != sourceSkillId
                && !GameInfo.IsBasicSkill(skillId)
                && IsDeckOperationCard(skillId)
            )
            .ToArray();

        return pool.Length > 0
            ? pool
            : (info.AllSkills ?? Array.Empty<SkillID>())
                .Where(skillId => skillId != sourceSkillId && IsDeckOperationCard(skillId))
                .ToArray();
    }

    private static bool TryAddDeckOperationCard(
        int playerIndex,
        SkillID skillId,
        bool allowDuplicate
    )
    {
        if (!TryGetDeckOperationPlayer(playerIndex, out var players, out var info))
            return false;

        info.GainedSkills ??= new List<SkillID>();
        if (!allowDuplicate && info.GainedSkills.Contains(skillId))
            return false;

        info.GainedSkills.Add(skillId);
        players[playerIndex] = info;
        GameInfo.PlayerCharacters = players;
        return true;
    }

    private static Task<SkillCard> PrepareDeckOperationSourceCardForFlyAsync(
        Node caller,
        SkillCard sourceCard
    )
    {
        if (!IsUsableAnimationCard(sourceCard))
            return Task.FromResult<SkillCard>(null);

        TryDetachDeckOperationCard(caller, sourceCard);
        return Task.FromResult(sourceCard);
    }

    public static bool TryDetachDeckOperationCard(Node caller, SkillCard sourceCard)
    {
        if (!IsUsableAnimationCard(sourceCard))
            return false;

        Node parent = ResolveDeckOperationAnimationParent(caller, sourceCard);
        if (parent == null)
            return false;

        Vector2 globalPosition = sourceCard.GlobalPosition;
        Vector2 scale = sourceCard.Scale;
        float rotation = sourceCard.Rotation;
        Vector2 pivotOffset = sourceCard.PivotOffset;

        Node currentParent = sourceCard.GetParent();
        if (currentParent != parent)
        {
            currentParent?.RemoveChild(sourceCard);
            parent.AddChild(sourceCard);
        }

        sourceCard.GlobalPosition = globalPosition;
        sourceCard.Scale = scale;
        sourceCard.Rotation = rotation;
        sourceCard.PivotOffset = pivotOffset;
        sourceCard.TopLevel = true;
        sourceCard.GlobalPosition = globalPosition;
        sourceCard.AutoPressEffect = false;
        sourceCard.UseDefaultHoverEffect = false;
        sourceCard.HoverUiEnabled = false;
        sourceCard.MouseFilter = Control.MouseFilterEnum.Ignore;
        sourceCard.Button.Disabled = true;
        sourceCard.Button.MouseFilter = Control.MouseFilterEnum.Ignore;
        sourceCard.HoverHint.Visible = false;
        sourceCard.ZIndex = 2000;
        sourceCard.MoveToFront();
        sourceCard.HideMoveTrail();
        return true;
    }

    private static async Task<SkillCard> CreateFloatingDeckOperationCardAsync(
        Node caller,
        int playerIndex,
        SkillID skillId,
        SkillCard sourceCard = null,
        CardDeckOperationSnapshot? snapshot = null
    )
    {
        Node parent = ResolveDeckOperationAnimationParent(caller, sourceCard);
        if (parent == null || SkillCardScene == null)
            return null;

        var card = SkillCardScene.Instantiate<SkillCard>();
        parent.AddChild(card);
        await WaitProcessFrameAsync(caller ?? card);

        if (!TryGetDeckOperationPlayer(playerIndex, out _, out var info))
            info = default;

        var skill = Skill.GetSkill(skillId);
        if (skill != null)
        {
            skill.SetPreviewStats(
                TalentTree.GetEffectivePower(info),
                TalentTree.GetEffectiveSurvivability(info),
                1,
                playerIndex: playerIndex
            );
        }

        card.PreviewCharacterName = info.CharacterName;
        card.PreviewCharacterKey = ExtractCharacterKeyFromScenePath(info.CharacterScenePath);
        card.ConfigureDisplayScale(DeckOperationCardDisplayScale);
        card.SetSkill(skill);
        card.AutoPressEffect = false;
        card.UseDefaultHoverEffect = false;
        card.HoverUiEnabled = false;
        card.MouseFilter = Control.MouseFilterEnum.Ignore;
        card.Button.Disabled = true;
        card.Button.MouseFilter = Control.MouseFilterEnum.Ignore;
        card.ZIndex = 2000;
        card.MoveToFront();
        card.HideMoveTrail();

        Vector2 position =
            snapshot?.GlobalPosition
            ?? GetCardSpawnGlobalPosition(sourceCard, caller);
        Vector2 scale =
            snapshot?.Scale
            ?? (IsUsableAnimationCard(sourceCard) ? sourceCard.Scale : Vector2.One);
        float rotation = snapshot?.Rotation ?? 0f;
        Vector2 pivotOffset = snapshot?.PivotOffset ?? card.PivotOffset;

        card.PivotOffset = pivotOffset;
        card.TopLevel = true;
        card.GlobalPosition = position;
        card.Scale = scale;
        card.Rotation = rotation;
        return card;
    }

    private static async Task FlyDeckOperationCardToTacticsAsync(Node caller, SkillCard card)
    {
        if (!IsUsableAnimationCard(card))
            return;

        Control target = ResolveDeckOperationFlyTarget(caller);
        if (target != null)
        {
            bool playedFly = await card.FlyWithTrailToControlAsync(
                target,
                new CardTrailMoveOptions
                {
                    CompressDuration = 0.25f,
                    FlyDuration = 0.32f,
                    TrailFadeDuration = 0.14f,
                    CompressedScaleFactor = 0.22f,
                    TargetScaleFactor = 0.28f,
                    CenterVanish = 0.92f,
                    GlowMultiplier = 1.28f,
                }
            );
            if (playedFly)
                PulseDeckOperationFlyTarget(target);
        }
        else
        {
            await WaitSecondsAsync(caller ?? card, 0.35f);
        }

        if (GodotObject.IsInstanceValid(card))
            card.QueueFree();
    }

    private static async Task PlayDeckOperationExhaustAsync(SkillCard card)
    {
        if (!IsUsableAnimationCard(card))
            return;

        card.PlayExhaustEffect(DeckOperationExhaustDuration);
        await WaitSecondsAsync(card, DeckOperationExhaustDuration);
        if (GodotObject.IsInstanceValid(card))
            card.QueueFree();
    }

    private static Control ResolveDeckOperationFlyTarget(Node caller)
    {
        var root = caller?.GetTree()?.Root;
        if (root == null)
            return null;

        return root.GetNodeOrNull<Control>(
                "Map/BattleReadyLayer/BattleReady/ModeSelectorRoot/ModeButtonsMargin/ModeButtons/TacticsModeButton"
            )
            ?? root.GetNodeOrNull<Control>(
                "/root/Map/BattleReadyLayer/BattleReady/ModeSelectorRoot/ModeButtonsMargin/ModeButtons/TacticsModeButton"
            )
            ?? root.GetNodeOrNull<Control>("Map/UI/ReadyButton")
            ?? root.GetNodeOrNull<Control>("/root/Map/UI/ReadyButton");
    }

    private static Node ResolveDeckOperationAnimationParent(Node caller, SkillCard sourceCard)
    {
        var root = caller?.GetTree()?.Root ?? sourceCard?.GetTree()?.Root;
        if (root == null)
            return null;

        return root.GetNodeOrNull<CanvasLayer>("Map/SiteUI")
            ?? root.GetNodeOrNull<CanvasLayer>("/root/Map/SiteUI")
            ?? sourceCard?.GetParent()
            ?? caller
            ?? root;
    }

    private static Vector2 GetCardSpawnGlobalPosition(SkillCard sourceCard, Node caller)
    {
        if (IsUsableAnimationCard(sourceCard))
            return sourceCard.GlobalPosition;

        Control target = ResolveDeckOperationFlyTarget(caller);
        if (target != null && GodotObject.IsInstanceValid(target))
            return target.GetGlobalRect().GetCenter() - new Vector2(80f, 120f);

        return new Vector2(960f, 540f);
    }

    private static void PulseDeckOperationFlyTarget(Control target)
    {
        if (target == null || !GodotObject.IsInstanceValid(target))
            return;

        target.PivotOffset = target.Size * 0.5f;
        Tween tween = target.CreateTween();
        tween.SetParallel(false);
        tween
            .TweenProperty(target, "scale", new Vector2(1.08f, 1.08f), 0.1f)
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.Out);
        tween
            .TweenProperty(target, "scale", Vector2.One, 0.16f)
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.InOut);
    }

    private static bool TryGetDeckOperationPlayer(
        int playerIndex,
        out PlayerInfoStructure[] players,
        out PlayerInfoStructure info
    )
    {
        GameInfo.NormalizePlayerCharacters();
        players = GameInfo.PlayerCharacters;
        if (players == null || (uint)playerIndex >= (uint)players.Length)
        {
            info = default;
            return false;
        }

        info = players[playerIndex];
        return true;
    }

    private static bool IsDeckOperationCard(SkillID skillId)
    {
        var skill = Skill.GetSkill(skillId);
        return skill != null && skill.SkillType != Skill.SkillTypes.none && !skill.IsStatusCard;
    }

    private static void EnsureTakenSkillsStillOwned(
        ref PlayerInfoStructure info,
        SkillID? preferredReplacement = null
    )
    {
        info.GainedSkills ??= new List<SkillID>();
        if (info.GainedSkills.Count == 0)
            info.GainedSkills.Add(SkillID.BasicAttack);

        info.TakenSkills ??= new SkillID[3];
        for (int i = 0; i < info.TakenSkills.Length; i++)
        {
            if (info.GainedSkills.Contains(info.TakenSkills[i]))
                continue;

            info.TakenSkills[i] = PickOwnedDeckOperationReplacement(info, preferredReplacement);
        }
    }

    private static SkillID PickOwnedDeckOperationReplacement(
        PlayerInfoStructure info,
        SkillID? preferredReplacement
    )
    {
        if (preferredReplacement.HasValue && info.GainedSkills.Contains(preferredReplacement.Value))
            return preferredReplacement.Value;

        foreach (SkillID skillId in info.GainedSkills)
        {
            if (IsDeckOperationCard(skillId))
                return skillId;
        }

        return info.GainedSkills.FirstOrDefault();
    }

    private static bool IsUsableAnimationCard(SkillCard card) =>
        card != null && GodotObject.IsInstanceValid(card) && card.IsInsideTree();

    private static Skill BuildDeckOperationPreviewSkill(
        SkillID skillId,
        PlayerInfoStructure info,
        int playerIndex
    )
    {
        Skill skill = Skill.GetSkill(skillId);
        skill?.SetPreviewStats(
            TalentTree.GetEffectivePower(info),
            TalentTree.GetEffectiveSurvivability(info),
            1,
            playerIndex: playerIndex
        );
        return skill;
    }

    private static string GetDeckOperationSkillName(SkillID skillId) =>
        Skill.GetSkill(skillId)?.SkillName ?? skillId.ToString();

    private static async Task WaitSecondsAsync(Node node, double seconds)
    {
        var tree = node?.GetTree();
        if (tree == null)
            return;

        await node.ToSignal(tree.CreateTimer(seconds), SceneTreeTimer.SignalName.Timeout);
    }

    private static async Task WaitProcessFrameAsync(Node node)
    {
        var tree = node?.GetTree();
        if (tree == null)
            return;

        await node.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
    }
}
