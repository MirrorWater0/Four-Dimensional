using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;

public partial class PlayerCharacter : Character
{
    public const int MaxBattleHandSize = 10;
    public const int TeamTurnStartDrawContribution = 2;

    public Frame SelfFrame;
    public Control SkillButtonControl;
    public List<Skill> UntakeSkills;
    public bool Istest;
    public int CharacterIndex;
    public string CharacterKey { get; private set; }
    protected override bool ResolvesTurnStartOnActionStart =>
        BattleNode?.IsResolvingPlayerTeamActionPhase != true;

    public override void Initialize()
    {
        var info = GameInfo.PlayerCharacters[CharacterIndex];

        PositionIndex = info.PositionIndex;
        CharacterName = info.CharacterName;
        CharacterKey = ExtractCharacterKeyFromScenePath(info.CharacterScenePath);
        Portrait = LoadPortrait(info.PortaitPath);
        PassiveName = info.PassiveName;
        PassiveDescription = info.PassiveDescription;
        Skills =
            BattleNode != null && GodotObject.IsInstanceValid(BattleNode)
                ? new Skill[MaxBattleHandSize]
                : CreateFallbackSkillLoadout();
        SetCombatStats(
            TalentTree.GetEffectivePower(info),
            TalentTree.GetEffectiveSurvivability(info),
            0,
            info.LifeMax
        );
        base.Initialize();
        IsPlayer = true;
        Life = Math.Clamp(info.LifeInitialized ? info.Life : BattleMaxLife, 0, BattleMaxLife);
        if (Life <= 0)
        {
            State = CharacterState.Dying;
            Modulate = new Color(1, 1, 1, 0);
        }
        SyncLifeBarsToCurrent(syncBufferValue: true);
        SyncPersistentLife();
    }

    public void SyncPersistentLife()
    {
        if (
            CharacterIndex < 0
            || GameInfo.PlayerCharacters == null
            || CharacterIndex >= GameInfo.PlayerCharacters.Length
        )
        {
            return;
        }

        var info = GameInfo.PlayerCharacters[CharacterIndex];
        info.LifeMax = Math.Max(1, BattleMaxLife);
        info.Life = Math.Clamp(Life, 0, info.LifeMax);
        info.LifeInitialized = true;
        GameInfo.PlayerCharacters[CharacterIndex] = info;
    }

    private static string ExtractCharacterKeyFromScenePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return null;

        var parts = path.Split('/');
        return parts.Length >= 2 ? parts[^2] : null;
    }

    private static Texture2D LoadPortrait(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return null;

        if (PreloadeScene.PreloadedTextures.TryGetValue(path, out var texture))
            return texture;

        return ResourceLoader.Exists(path) ? GD.Load<Texture2D>(path) : null;
    }

    private void DrawBattleCards(int count)
    {
        if (count <= 0)
            return;

        if (
            BattleNode != null
            && GodotObject.IsInstanceValid(BattleNode)
            && BattleNode.IsResolvingPlayerTeamActionPhase
        )
        {
            BattleNode.DrawPlayerBattleCardsToTeamHand(this, count, refreshUi: false);
            return;
        }

        EnsureBattleHandSize();
        for (int i = 0; i < Skills.Length && count > 0; i++)
        {
            if (Skills[i] != null)
                continue;

            Skill drawnSkill = DrawBattleSkill();
            if (drawnSkill == null)
                return;

            drawnSkill.OwnerCharater = this;
            drawnSkill.UpdateDescription();
            Skills[i] = drawnSkill;
            drawnSkill.OnDrawnToHand(this);
            count--;
        }
    }

    public bool TryDrawBattleCards(int count, bool refreshUi = true)
    {
        if (
            count <= 0
            || BattleNode == null
            || !GodotObject.IsInstanceValid(BattleNode)
            || State == Character.CharacterState.Dying
        )
        {
            return false;
        }

        if (BattleNode.IsResolvingPlayerTeamActionPhase)
            return BattleNode.DrawPlayerBattleCardsToTeamHand(this, count, refreshUi);

        EnsureBattleHandSize();
        int beforeCount = Skills.Count(skill => skill != null);
        DrawBattleCards(count);
        int afterCount = Skills.Count(skill => skill != null);
        if (afterCount == beforeCount)
            return false;

        InvalidateSkillTooltipCache();
        if (refreshUi)
            BattleNode?.CharacterControl?.RefreshCurrentTurnUi();
        return true;
    }

    private static Skill[] CreateFallbackSkillLoadout()
    {
        return
        [
            Skill.GetSkill(SkillID.BasicAttack),
            Skill.GetSkill(SkillID.BasicDefense),
            Skill.GetSkill(SkillID.BasicSpecial),
        ];
    }

    private Skill DrawBattleSkill(SkillID? avoidSkillId = null)
    {
        Skill skill = BattleNode?.DrawPlayerBattleSkill(this, avoidSkillId: avoidSkillId);
        if (skill != null)
            return skill;

        if (BattleNode != null && GodotObject.IsInstanceValid(BattleNode))
            return null;

        return Skill.GetSkill(SkillID.BasicAttack);
    }

    public void RedrawBattleSkill(int skillIndex, SkillID? avoidSkillId = null)
    {
        EnsureBattleHandSize();
        if (skillIndex < 0 || skillIndex >= Skills.Length)
            return;

        Skill oldSkill = Skills[skillIndex];
        SkillID? resolvedAvoidSkillId = avoidSkillId ?? oldSkill?.SkillId;
        Skill redrawnSkill = DrawBattleSkill(resolvedAvoidSkillId);
        if (redrawnSkill == null)
            return;

        if (oldSkill != null)
            BattleNode?.DiscardBattleSkill(GetBattlePileOwner(oldSkill), oldSkill, forceDiscard: true);

        redrawnSkill.OwnerCharater = this;
        redrawnSkill.UpdateDescription();
        Skills[skillIndex] = redrawnSkill;
        InvalidateSkillTooltipCache();
    }

    public void ClearBattleSkill(int skillIndex)
    {
        EnsureBattleHandSize();
        if (skillIndex < 0 || skillIndex >= Skills.Length)
            return;

        Skills[skillIndex] = null;
        CompactBattleHand();
        InvalidateSkillTooltipCache();
    }

    private void ResolveTurnStartCardDraw()
    {
        if (
            Skills == null
            || BattleNode == null
            || !GodotObject.IsInstanceValid(BattleNode)
            || BattleNode.CurrentActionCharacter != this
            || State == Character.CharacterState.Dying
        )
        {
            return;
        }

        DrawBattleCards(TeamTurnStartDrawContribution + Relic.GetTurnStartDrawBonus(BattleNode));

        if (GetBattleHandEmptySlotCount() > 0 && SpecialBuff.GetCardRefreshStack(this) > 0)
        {
            int beforeCount = GetBattleHandCardCount();
            DrawBattleCards(1);
            int drawnCount = Math.Max(0, GetBattleHandCardCount() - beforeCount);
            SpecialBuff.ConsumeCardRefresh(this, drawnCount);
        }

        InvalidateSkillTooltipCache();
    }

    private bool HasBattleHandSpace()
    {
        EnsureBattleHandSize();
        return Skills.Any(skill => skill == null);
    }

    private int GetBattleHandCardCount()
    {
        EnsureBattleHandSize();
        return Skills.Count(skill => skill != null);
    }

    private int GetBattleHandEmptySlotCount()
    {
        EnsureBattleHandSize();
        return Skills.Count(skill => skill == null);
    }

    public void RemoveBattleHandCardAt(int skillIndex)
    {
        EnsureBattleHandSize();
        if (skillIndex < 0 || skillIndex >= Skills.Length)
            return;

        Skills[skillIndex] = null;
        CompactBattleHand();
        InvalidateSkillTooltipCache();
    }

    public bool TryRestoreBattleHandCardAt(int skillIndex, Skill skill)
    {
        EnsureBattleHandSize();
        if (skill == null || skillIndex < 0 || skillIndex >= Skills.Length)
            return false;

        if (Skills[skillIndex] != null)
        {
            int emptyIndex = Array.FindIndex(Skills, skillIndex, x => x == null);
            if (emptyIndex < 0)
                return false;

            for (int i = emptyIndex; i > skillIndex; i--)
                Skills[i] = Skills[i - 1];
        }

        skill.OwnerCharater = this;
        skill.UpdateDescription();
        Skills[skillIndex] = skill;
        InvalidateSkillTooltipCache();
        return true;
    }

    public void DiscardBattleHand()
    {
        if (Skills == null || BattleNode == null || !GodotObject.IsInstanceValid(BattleNode))
            return;

        bool discardedAny = false;
        for (int i = 0; i < Skills.Length; i++)
        {
            Skill skill = Skills[i];
            if (skill == null)
                continue;

            BattleNode.DiscardBattleSkill(GetBattlePileOwner(skill), skill, atTurnEnd: true);
            Skills[i] = null;
            discardedAny = true;
        }

        if (discardedAny)
            InvalidateSkillTooltipCache();
    }

    public async Task DiscardBattleHandAtTurnEndAsync()
    {
        if (Skills == null || BattleNode == null || !GodotObject.IsInstanceValid(BattleNode))
            return;

        var discardIndexes = new HashSet<int>();
        for (int i = 0; i < Skills.Length; i++)
        {
            Skill skill = Skills[i];
            if (skill == null)
                continue;

            if (skill.TriggersAtTurnEndInHand)
            {
                PlayerCharacter skillOwner = GetBattlePileOwner(skill);
                CharacterControl characterControl = BattleNode.CharacterControl;
                if (characterControl != null && GodotObject.IsInstanceValid(characterControl))
                {
                    await characterControl.PlayTurnEndStatusTriggerAnimationAsync(
                        this,
                        i,
                        skill,
                        () => skill.OnTurnEndInHand(skillOwner)
                    );
                }
                else
                    await skill.OnTurnEndInHand(skillOwner);

                if (Skills[i] == skill)
                {
                    BattleNode.DiscardBattleSkill(
                        GetBattlePileOwner(skill),
                        skill,
                        atTurnEnd: true,
                        forceDiscard: true
                    );
                    Skills[i] = null;
                    InvalidateSkillTooltipCache();
                }
                continue;
            }
            else
                await skill.OnTurnEndInHand(GetBattlePileOwner(skill));

            if (Skills[i] != skill)
                continue;

            discardIndexes.Add(i);
        }

        if (discardIndexes.Count == 0)
            return;

        CharacterControl endTurnCharacterControl = BattleNode.CharacterControl;
        if (
            endTurnCharacterControl != null
            && GodotObject.IsInstanceValid(endTurnCharacterControl)
        )
        {
            await endTurnCharacterControl.PlayEndTurnHandDiscardAnimationsAsync(
                this,
                discardIndexes
            );
        }

        foreach (int index in discardIndexes)
        {
            if (index < 0 || index >= Skills.Length)
                continue;

            Skill skill = Skills[index];
            if (skill == null)
                continue;

            BattleNode.DiscardBattleSkill(GetBattlePileOwner(skill), skill, atTurnEnd: true);
            Skills[index] = null;
        }

        if (discardIndexes.Count > 0)
            InvalidateSkillTooltipCache();
    }

    public override void OnActionStart()
    {
        base.OnActionStart();
        if (
            BattleNode == null
            || !GodotObject.IsInstanceValid(BattleNode)
            || BattleNode.MapNode == null
            || !GodotObject.IsInstanceValid(BattleNode.MapNode)
        )
            return;

        if (BattleNode.IsResolvingPlayerTeamActionPhase)
            BattleNode.BeginPlayerTeamAction();
        else
        {
            ResolveTurnStartCardDraw();
            BattleNode.CharacterControl?.ShowPlayerTurn(this);
        }
        BattleNode?.MapNode?.PlayerResourceState?.SetItemsEnabled(true);
    }

    public override void OnActionEnd()
    {
        if (BattleNode == null || !GodotObject.IsInstanceValid(BattleNode))
        {
            base.OnActionEnd();
            return;
        }

        BattleNode.CharacterControl?.DisablePlayerActions(this);
        var mapNode = BattleNode.MapNode;
        if (mapNode != null && GodotObject.IsInstanceValid(mapNode))
            mapNode.PlayerResourceState?.SetItemsEnabled(false);
        base.OnActionEnd();
    }

    public override async void EndAction()
    {
        var battleNode = BattleNode;
        if (battleNode == null || !GodotObject.IsInstanceValid(battleNode))
            return;

        bool isTeamActionPhase = battleNode.IsResolvingPlayerTeamActionPhase;
        if (isTeamActionPhase)
            await battleNode.DiscardPlayerTeamBattleHandAtTurnEndAsync(this);
        else
            await DiscardBattleHandAtTurnEndAsync();
        OnActionEnd();
        if (!isTeamActionPhase)
            await base.ResolveTurnEndPhaseAsync();

        await battleNode.EndEmitS(this);
        CreateTween().TweenProperty(trail, "modulate", new Color(1, 0, 0, 0), 0.2f);
        await ToSignal(GetTree().CreateTimer(0.2f), "timeout");
        TrailAnimation.Stop();
    }

    protected override async Task ResolveTurnEndPhaseAsync()
    {
        if (BattleNode?.IsResolvingPlayerTeamActionPhase == true)
        {
            await base.ResolveTurnEndPhaseAsync();
            return;
        }

        await DiscardBattleHandAtTurnEndAsync();
        await base.ResolveTurnEndPhaseAsync();
    }

    public override void DisableSkill()
    {
        BattleNode?.CharacterControl?.SetPlayerInputsEnabled(this, enabled: false);
    }

    protected bool HasPassiveTalentUpgrade()
    {
        if (CharacterIndex < 0 || GameInfo.PlayerCharacters == null || CharacterIndex >= GameInfo.PlayerCharacters.Length)
            return false;

        return TalentTree.HasPassiveUpgrade(GameInfo.PlayerCharacters[CharacterIndex]);
    }

    private void EnsureBattleHandSize()
    {
        if (Skills != null && Skills.Length == MaxBattleHandSize)
            return;

        Skill[] resized = new Skill[MaxBattleHandSize];
        if (Skills != null)
            Array.Copy(Skills, resized, Math.Min(Skills.Length, resized.Length));
        Skills = resized;
    }

    private void CompactBattleHand()
    {
        EnsureBattleHandSize();
        Skill[] compacted = new Skill[MaxBattleHandSize];
        int next = 0;
        for (int i = 0; i < Skills.Length; i++)
        {
            if (Skills[i] == null)
                continue;

            compacted[next++] = Skills[i];
        }

        Skills = compacted;
    }

    private PlayerCharacter GetBattlePileOwner(Skill skill)
    {
        return skill?.OwnerCharater as PlayerCharacter ?? this;
    }
}
