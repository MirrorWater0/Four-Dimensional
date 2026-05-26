using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;

public partial class PlayerCharacter : Character
{
    public const int MaxBattleHandSize = 7;
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
        CompactBattleHand();
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

        while (
            HasBattleHandSpace()
            && BattleNode.HasDrawablePlayerBattleSkill(this)
            && SpecialBuff.TryConsumeCardRefresh(this)
        )
        {
            DrawBattleCards(1);
        }

        InvalidateSkillTooltipCache();
    }

    private bool HasBattleHandSpace()
    {
        EnsureBattleHandSize();
        return Skills.Any(skill => skill == null);
    }

    public void RemoveBattleHandCardAt(int skillIndex)
    {
        EnsureBattleHandSize();
        if (skillIndex < 0 || skillIndex >= Skills.Length)
            return;

        Skills[skillIndex] = null;
        InvalidateSkillTooltipCache();
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

        bool discardedAny = false;
        for (int i = 0; i < Skills.Length; i++)
        {
            Skill skill = Skills[i];
            if (skill == null)
                continue;

            await skill.OnTurnEndInHand(this);

            if (Skills[i] != skill)
                continue;

            BattleNode.DiscardBattleSkill(this, skill, atTurnEnd: true);
            Skills[i] = null;
            discardedAny = true;
        }

        if (discardedAny)
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
