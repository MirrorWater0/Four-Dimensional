using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;

public partial class PlayerCharacter : Character
{
    public const int MaxBattleHandSize = 8;
    private const int TurnStartDrawCount = 4;

    public Frame SelfFrame;
    public Control SkillButtonControl;
    public List<Skill> UntakeSkills;
    public bool Istest;
    public int CharacterIndex;
    public string CharacterKey { get; private set; }

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
            TalentTree.GetEffectiveSpeed(info),
            info.LifeMax
        );
        Life = BattleMaxLife;
        base.Initialize();
        IsPlayer = true;
        BlockLabel.Position += new Vector2(230, 0);
        BlockLabel.HorizontalAlignment = HorizontalAlignment.Left;
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

    public bool TryDrawBattleCards(int count)
    {
        if (
            count <= 0
            || Skills == null
            || BattleNode == null
            || !GodotObject.IsInstanceValid(BattleNode)
            || State == Character.CharacterState.Dying
        )
        {
            return false;
        }

        EnsureBattleHandSize();
        int beforeCount = Skills.Count(skill => skill != null);
        DrawBattleCards(count);
        int afterCount = Skills.Count(skill => skill != null);
        if (afterCount == beforeCount)
            return false;

        InvalidateSkillTooltipCache();
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

        Skill redrawnSkill = DrawBattleSkill(avoidSkillId);
        if (redrawnSkill == null)
            return;

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

        DrawBattleCards(TurnStartDrawCount + Relic.GetTurnStartDrawBonus(BattleNode));

        int extraDrawCount = Math.Min(GetBattleHandEmptySlotCount(), SpecialBuff.GetCardRefreshStack(this));
        if (extraDrawCount > 0)
        {
            int beforeCount = GetBattleHandCardCount();
            DrawBattleCards(extraDrawCount);
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

            BattleNode.DiscardBattleSkill(this, skill, atTurnEnd: true);
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
                CharacterControl characterControl = BattleNode.CharacterControl;
                if (characterControl != null && GodotObject.IsInstanceValid(characterControl))
                {
                    await characterControl.PlayTurnEndStatusTriggerAnimationAsync(
                        this,
                        i,
                        skill,
                        () => skill.OnTurnEndInHand(this)
                    );
                }
                else
                    await skill.OnTurnEndInHand(this);

                if (Skills[i] == skill)
                {
                    BattleNode.DiscardBattleSkill(
                        this,
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
                await skill.OnTurnEndInHand(this);

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

            BattleNode.DiscardBattleSkill(this, skill, atTurnEnd: true);
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

        ResolveTurnStartCardDraw();
        BattleNode.CharacterControl?.ShowPlayerTurn(this);
        BattleNode.RetreatButton.Disabled = !BattleNode.CanManualRetreat();
        BattleNode?.MapNode?.PlayerResourceState?.SetItemsEnabled(true);
    }

    public override void OnActionEnd()
    {
        if (BattleNode == null || !GodotObject.IsInstanceValid(BattleNode))
        {
            base.OnActionEnd();
            return;
        }

        BattleNode.RetreatButton.Disabled = true;
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

        await DiscardBattleHandAtTurnEndAsync();
        OnActionEnd();

        await base.ResolveTurnEndPhaseAsync();

        await battleNode.EndEmitS(this);
        CreateTween().TweenProperty(trail, "modulate", new Color(1, 0, 0, 0), 0.2f);
        await ToSignal(GetTree().CreateTimer(0.2f), "timeout");
        TrailAnimation.Stop();
    }

    protected override async Task ResolveTurnEndPhaseAsync()
    {
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
}
