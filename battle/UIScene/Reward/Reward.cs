using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;

public partial class Reward : CanvasLayer
{
    private static readonly PackedScene RewardScene = GD.Load<PackedScene>(
        "res://battle/UIScene/Reward/Reward.tscn"
    );
    private static readonly PackedScene SkillRewardCardScene = GD.Load<PackedScene>(
        "res://battle/UIScene/Reward/SkillCard.tscn"
    );
    private static readonly PackedScene RewardItemScene = GD.Load<PackedScene>(
        "res://Equipment/CardSlot.tscn"
    );
    private static readonly PackedScene TipScene = GD.Load<PackedScene>(
        "res://battle/UIScene/Tip.tscn"
    );
    private static readonly Shader TalentUnlockShockWaveShader = GD.Load<Shader>(
        "res://shader/shockwave.gdshader"
    );
    private static readonly Shader TalentUnlockSparkLightShader = GD.Load<Shader>(
        "res://shader/Effect/SparkLight.gdshader"
    );

    private const int ExpectedSkillSlots = 4;
    private const string RuntimeRewardMeta = "reward_runtime";
    private const float TalentNodeWidth = 76f;
    private const float TalentNodeHeight = 76f;
    private const float TalentNodeLabelHeight = 22f;
    private const float TalentLineThickness = 5f;

    private InventoryGrid InventoryGridNode => field ??= GetNode<InventoryGrid>("Panel/Inventory");
    private Button SkillRewardButton => field ??= GetNodeOrNull<Button>("Panel/Inventory/Button");
    private HBoxContainer SkillRewardsContainer =>
        field ??= GetNodeOrNull<HBoxContainer>("HBoxContainer");
    private ColorRect BG => field ??= GetNode<ColorRect>("BG");
    private ColorRect SkillMask => field ??= GetNodeOrNull<ColorRect>("SkillMask");
    private Control PanelNode => field ??= GetNode<Control>("Panel");
    private Control DecorNode => field ??= GetNodeOrNull<Control>("Panel/Decor");
    private Button SkipButton => field ??= GetNodeOrNull<Button>("Panel/Decor/SkipButton");
    private Button SkillRewardSkipButton =>
        field ??= GetNodeOrNull<Button>("SkillRewardSkipButton");
    private Control TalentOverlay =>
        field ??= GetNodeOrNull<Control>("TalentTreeOverlay");
    private ColorRect TalentOverlayBackdrop =>
        field ??= GetNodeOrNull<ColorRect>("TalentTreeOverlay/Backdrop");
    private Panel TalentPanel =>
        field ??= GetNodeOrNull<Panel>("TalentTreeOverlay/TalentPanel");
    private Label TalentCharacterLabel =>
        field ??= GetNodeOrNull<Label>("TalentTreeOverlay/TalentPanel/Header/Title");
    private Label TalentPointLabel =>
        field ??= GetNodeOrNull<Label>("TalentTreeOverlay/TalentPanel/Header/PointLabel");
    private Control TalentTreeRoot =>
        field ??= GetNodeOrNull<Control>("TalentTreeOverlay/TalentPanel/TalentTreeRoot");
    private Button TalentCloseButton =>
        field ??= GetNodeOrNull<Button>("TalentTreeOverlay/TalentPanel/Footer/CloseButton");
    public Map MapNode => field ??= GetNode<Map>("/root/Map");

    private readonly List<SkillCard> _skillRewardSlots = new();
    private readonly HashSet<SkillCard> _wiredSkillCards = new();
    private readonly Dictionary<Control, RewardEntry> _rewardEntries = new();
    private readonly HashSet<Control> _staticRewardControls = new();
    private SkillID?[] _offeredSkillIds = Array.Empty<SkillID?>();
    private int[] _offeredPlayerIndexes = Array.Empty<int>();
    private bool _isSkillRewardOpen;
    private bool _pickedSkill;
    private Control _activeSkillRewardControl;
    private LevelNode _completeNodeOnClose;
    private Tween _introTween;
    private Tween _outroTween;
    private Tween _maskTween;
    private Vector2 _panelBasePosition;
    private bool _panelBaseCached;
    private bool _battleTalentPointRewardGranted;
    private int _nextSkillRewardGroupIndex;
    private bool _skillRewardOffersGenerated;
    private bool _isTalentTreeOpen;
    private int _activeTalentCharacterIndex = -1;
    private const float RewardReflowDuration = 0.18f;
    public bool AllowRareSkillRewards { get; set; } = true;

    private enum RewardKind
    {
        Skill,
        Relic,
        Item,
        TalentPoint,
    }

    private sealed class RewardEntry
    {
        public RewardKind Kind;
        public RelicID RelicId;
        public ItemID ItemId;
        public int SkillGroupIndex = -1;
        public bool ForceRareSkillReward;
        public bool AllowRareSkillReward = true;
        public SkillID?[] OfferedSkillIds;
        public int[] OfferedPlayerIndexes;
        public string TalentPointCharacterName;
        public int TalentPointAmount;
    }

    // Call from anywhere (e.g. Battle) to show the reward UI.
    public static Reward Show(Node caller)
    {
        var tree = caller.GetTree();
        var root = tree.Root;
        var siteUi = root.GetNodeOrNull<CanvasLayer>("Map/SiteUI")
            ?? root.GetNodeOrNull<CanvasLayer>("/root/Map/SiteUI");

        var existing = siteUi?.GetNodeOrNull<Reward>("Reward") ?? root.GetNodeOrNull<Reward>("Reward");
        if (existing != null && existing.IsQueuedForDeletion())
            existing = null;

        if (existing != null)
        {
            if (siteUi != null && existing.GetParent() != siteUi)
            {
                existing.GetParent()?.RemoveChild(existing);
                siteUi.AddChild(existing);
                existing.Layer = 1;
            }

            existing.CallDeferred(nameof(Open));
            return existing;
        }

        if (siteUi == null)
        {
            GD.PushError("Reward: SiteUI layer not found, cannot attach reward UI.");
            return null;
        }

        var reward = RewardScene.Instantiate<Reward>();
        reward.Name = "Reward";
        reward.Layer = 1;
        siteUi.AddChild(reward);
        reward.CallDeferred(nameof(Open));
        return reward;
    }

    public override void _Ready()
    {
        Visible = false;
        if (SkillRewardsContainer != null)
            SkillRewardsContainer.Visible = false;
        if (SkillMask != null)
            SkillMask.Visible = false;
        if (SkillRewardSkipButton != null)
            SkillRewardSkipButton.Visible = false;

        CachePanelTransform();
        if (PanelNode != null)
        {
            PanelNode.ItemRectChanged += UpdatePanelPivot;
            UpdatePanelPivot();
        }

        WireStaticRewardControls();
        if (SkipButton != null)
            SkipButton.Pressed += SkipRewards;
        if (SkillRewardSkipButton != null)
            SkillRewardSkipButton.Pressed += SkipSkillRewardSelection;
        if (TalentOverlay != null)
            TalentOverlay.Visible = false;
        if (TalentCloseButton != null)
            TalentCloseButton.Pressed += CloseTalentRewardTree;
    }

    public void SetCompleteNodeOnClose(LevelNode node)
    {
        _completeNodeOnClose = node;
        _battleTalentPointRewardGranted = false;
    }

    /// <summary>Show reward UI. Default to a skill reward entry if none exists.</summary>
    public void Open()
    {
        ResetSkillRewardPanel();
        bool hasRuntimeEntries = _rewardEntries.Count > 0;
        if (!hasRuntimeEntries)
        {
            ClearRewardItems(clearStatic: false);
            WireStaticRewardControls();
            EnsureDefaultSkillRewardEntry();
        }
        else if (SkillRewardButton != null)
        {
            SkillRewardButton.Visible = false;
            SkillRewardButton.Disabled = true;
        }
        InventoryGridNode?.LayoutCards(force: true);
        Visible = true;
        PlayIntroAnimation();
    }

    /// <summary>Clear reward entries. Static UI nodes can be preserved.</summary>
    public void ClearRewardItems(bool clearStatic = true)
    {
        foreach (var control in _rewardEntries.Keys.ToArray())
        {
            if (!clearStatic && IsStaticRewardControl(control))
            {
                control.Visible = true;
                continue;
            }
            RemoveRewardControl(control);
        }

        if (
            !_rewardEntries.Values.Any(entry =>
                entry.Kind == RewardKind.Skill && entry.SkillGroupIndex >= 0
            )
        )
        {
            _nextSkillRewardGroupIndex = 0;
            _skillRewardOffersGenerated = false;
        }

        InventoryGridNode?.LayoutCards(force: true);
    }

    /// <summary>Add a skill reward entry to the inventory list.</summary>
    public CardSlot AddSkillRewardEntry(string title = null, bool forceRare = false)
    {
        title ??= I18n.Tr("ui.reward.skill_reward", "技能奖励");
        var entry = new RewardEntry
        {
            Kind = RewardKind.Skill,
            SkillGroupIndex = _nextSkillRewardGroupIndex++,
            ForceRareSkillReward = forceRare,
            AllowRareSkillReward = AllowRareSkillRewards,
        };
        var card = CreateRewardCard(title, I18n.Tr("ui.reward.click_expand_skill", "点击展开技能卡"));
        if (card == null)
            return null;
        card.ConfigureSkillRewardStyle();
        _skillRewardOffersGenerated = false;
        RegisterRewardControl(card, entry, isRuntime: true);
        return card;
    }

    /// <summary>Add a relic reward entry to the inventory list.</summary>
    public CardSlot AddRelicRewardEntry(RelicID relicId, string displayName = null)
    {
        var relic = Relic.Create(relicId);
        string title = displayName
            ?? I18n.Format("ui.reward.relic_title", "遗物 · {name}", ("name", relic.RelicName));
        var entry = new RewardEntry { Kind = RewardKind.Relic, RelicId = relicId };
        var card = CreateRewardCard(title, I18n.Tr("ui.reward.click_claim_relic", "点击领取遗物"));
        if (card == null)
            return null;
        card.ConfigureRelicRewardStyle(relicId);
        RegisterRewardControl(card, entry, isRuntime: true);
        return card;
    }

    /// <summary>Add a consumable item reward entry to the inventory list.</summary>
    public CardSlot AddItemRewardEntry(ItemID itemId, string displayName = null)
    {
        string title = displayName
            ?? I18n.Format("ui.reward.item_title", "道具 · {name}", ("name", GetItemName(itemId)));
        string detail = GetItemDescription(itemId);
        var entry = new RewardEntry { Kind = RewardKind.Item, ItemId = itemId };
        var card = CreateRewardCard(title, detail);
        if (card == null)
            return null;
        card.ConfigureItemRewardStyle(itemId);
        RegisterRewardControl(card, entry, isRuntime: true);
        return card;
    }

    /// <summary>Add a talent point reward entry to the inventory list.</summary>
    public CardSlot AddTalentPointRewardEntry(TalentPointRewardResult talentReward)
    {
        if (!talentReward.Granted)
            return null;

        string title = I18n.Tr("ui.reward.talent_points", "天赋点");
        string detail = talentReward.CharacterName;
        var entry = new RewardEntry
        {
            Kind = RewardKind.TalentPoint,
            TalentPointCharacterName = talentReward.CharacterName,
            TalentPointAmount = talentReward.Amount,
        };
        var card = CreateRewardCard(title, detail);
        if (card == null)
            return null;
        card.ConfigureTalentPointRewardStyle();
        RegisterRewardControl(card, entry, isRuntime: true);
        return card;
    }

    private void WireStaticRewardControls()
    {
        var button = SkillRewardButton;
        if (button == null)
            return;
        if (_rewardEntries.ContainsKey(button))
            return;

        button.Text = I18n.Tr("ui.reward.skill_reward", "技能奖励");
        button.Visible = true;
        button.Text = string.Empty;
        ConfigureStaticSkillRewardButton(button);
        button.Disabled = false;
        button.Modulate = new Color(1, 1, 1, 1);
        RegisterRewardControl(
            button,
            new RewardEntry { Kind = RewardKind.Skill },
            isRuntime: false
        );
    }

    private void EnsureDefaultSkillRewardEntry()
    {
        if (_rewardEntries.Values.Any(entry => entry.Kind == RewardKind.Skill))
            return;
        AddSkillRewardEntry();
    }

    private static void ConfigureStaticSkillRewardButton(Button button)
    {
        if (button == null)
            return;

        const string iconName = "SkillRewardIcon";
        const string labelName = "RewardLabel";
        var icon = button.GetNodeOrNull<ColorRect>(iconName);
        if (icon == null)
        {
            icon = new ColorRect
            {
                Name = iconName,
                Color = Colors.White,
                MouseFilter = Control.MouseFilterEnum.Ignore,
            };
            button.AddChild(icon);
        }

        if (icon.Material == null)
        {
            var shader = GD.Load<Shader>("res://shader/Effect/SkillRewardIcon.gdshader");
            if (shader != null)
            {
                icon.Material = new ShaderMaterial
                {
                    Shader = shader,
                    ResourceLocalToScene = true,
                };
            }
        }

        icon.SetAnchorsPreset(Control.LayoutPreset.TopLeft);
        icon.AnchorTop = 0.5f;
        icon.AnchorBottom = 0.5f;
        icon.OffsetLeft = 18f;
        icon.OffsetTop = -34f;
        icon.OffsetRight = 86f;
        icon.OffsetBottom = 34f;

        var label = button.GetNodeOrNull<Label>(labelName);
        if (label == null)
        {
            label = new Label
            {
                Name = labelName,
                MouseFilter = Control.MouseFilterEnum.Ignore,
            };
            button.AddChild(label);
        }

        label.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        label.OffsetLeft = 104f;
        label.OffsetTop = 6f;
        label.OffsetRight = -18f;
        label.OffsetBottom = -6f;
        label.Text = I18n.Tr("ui.reward.skill_reward_with_hint", "技能奖励\n点击展开技能卡");
        label.HorizontalAlignment = HorizontalAlignment.Left;
        label.VerticalAlignment = VerticalAlignment.Center;
        label.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        label.ClipText = true;
        label.AddThemeFontSizeOverride("font_size", 18);
        label.AddThemeColorOverride("font_color", new Color(0.9f, 0.96f, 1f, 1f));
        label.AddThemeColorOverride("font_shadow_color", new Color(0f, 0.08f, 0.16f, 0.78f));
        label.AddThemeConstantOverride("shadow_offset_x", 1);
        label.AddThemeConstantOverride("shadow_offset_y", 1);
    }

    private CardSlot CreateRewardCard(string title, string detail)
    {
        if (RewardItemScene == null)
        {
            GD.PushError("Reward item scene is missing: res://Equipment/CardSlot.tscn");
            return null;
        }

        var card = RewardItemScene.Instantiate<CardSlot>();
        if (card == null)
            return null;

        if (card.label != null)
        {
            if (string.IsNullOrWhiteSpace(detail))
                card.label.Text = title;
            else
                card.label.Text = $"{title}\n{detail}";
        }

        InventoryGridNode?.AddChild(card);
        return card;
    }

    private void RegisterRewardControl(Control control, RewardEntry entry, bool isRuntime)
    {
        if (control == null)
            return;

        if (_rewardEntries.ContainsKey(control))
        {
            _rewardEntries[control] = entry;
            return;
        }

        _rewardEntries[control] = entry;
        if (!isRuntime)
            _staticRewardControls.Add(control);
        else
            control.SetMeta(RuntimeRewardMeta, true);

        switch (control)
        {
            case CardSlot card:
                card.Clicked += () => OnRewardControlClicked(control);
                break;
            case Button button:
                button.Pressed += () => OnRewardControlClicked(control);
                break;
        }

        InventoryGridNode?.LayoutCards(force: true);
    }

    private bool IsStaticRewardControl(Control control)
    {
        return control != null && _staticRewardControls.Contains(control);
    }

    private bool IsRuntimeRewardControl(Control control)
    {
        return control != null
            && control.HasMeta(RuntimeRewardMeta)
            && (bool)control.GetMeta(RuntimeRewardMeta);
    }

    private void RemoveRewardControl(Control control)
    {
        if (control == null)
            return;

        _rewardEntries.Remove(control);
        _staticRewardControls.Remove(control);

        if (!GodotObject.IsInstanceValid(control))
            return;

        if (IsRuntimeRewardControl(control))
            control.QueueFree();
        else
            control.Visible = false;
    }

    private void OnRewardControlClicked(Control control)
    {
        if (control == null)
            return;
        if (!_rewardEntries.TryGetValue(control, out var entry))
            return;

        if (control is CardSlot card)
            card.Unselect();

        switch (entry.Kind)
        {
            case RewardKind.Skill:
                OpenSkillRewards(control);
                break;
            case RewardKind.Relic:
                GrantRelicReward(entry.RelicId);
                RemoveRewardControlWithReflow(control);
                TryCloseIfDone();
                break;
            case RewardKind.Item:
                if (GrantItemReward(entry.ItemId))
                {
                    RemoveRewardControlWithReflow(control);
                    TryCloseIfDone();
                }
                else
                {
                    PlayRewardRejected(control);
                }
                break;
            case RewardKind.TalentPoint:
                if (GrantTalentPointReward())
                {
                    RemoveRewardControlWithReflow(control);
                    TryCloseIfDone();
                }
                else
                {
                    PlayRewardRejected(control);
                }
                break;
        }
    }

    private void OpenSkillRewards(Control sourceControl)
    {
        if (_isSkillRewardOpen)
            return;

        if (SkillRewardsContainer == null)
        {
            GD.PushError("Missing skill reward container: HBoxContainer");
            return;
        }

        _activeSkillRewardControl = sourceControl;
        _pickedSkill = false;
        _isSkillRewardOpen = true;
        SkillRewardsContainer.Visible = true;
        if (SkillRewardSkipButton != null)
        {
            SkillRewardSkipButton.Visible = true;
            SkillRewardSkipButton.Disabled = false;
        }
        InventoryGridNode?.SetInputBlocked(true);
        ShowSkillMask(true);

        EnsureSkillRewardSlots();
        BuildSkillRewards();

        for (int i = 0; i < _skillRewardSlots.Count; i++)
        {
            _skillRewardSlots[i].ResetState();
            if (_skillRewardSlots[i].Visible)
                _skillRewardSlots[i].StartAnimation(0.05f * i + 0.3f);
        }
    }

    private void ResetSkillRewardPanel()
    {
        _pickedSkill = false;
        _isSkillRewardOpen = false;
        _activeSkillRewardControl = null;
        InventoryGridNode?.SetInputBlocked(false);
        if (SkillRewardsContainer != null)
            SkillRewardsContainer.Visible = false;
        if (SkillRewardSkipButton != null)
            SkillRewardSkipButton.Visible = false;
        ShowSkillMask(false);
    }

    private void EnsureSkillRewardSlots()
    {
        _skillRewardSlots.Clear();
        if (SkillRewardsContainer == null)
            return;

        _skillRewardSlots.AddRange(SkillRewardsContainer.GetChildren().OfType<SkillCard>());

        if (_skillRewardSlots.Count < ExpectedSkillSlots)
        {
            for (int i = _skillRewardSlots.Count; i < ExpectedSkillSlots; i++)
            {
                var slot = SkillRewardCardScene.Instantiate<SkillCard>();
                slot.Name = $"RewardCard{i + 1}";
                SkillRewardsContainer.AddChild(slot);
                _skillRewardSlots.Add(slot);
            }
        }

        WireSkillCardButtons();
    }

    private void WireSkillCardButtons()
    {
        for (int i = 0; i < _skillRewardSlots.Count; i++)
        {
            var slot = _skillRewardSlots[i];
            if (slot == null || _wiredSkillCards.Contains(slot))
                continue;

            int slotIndex = i;
            slot.Button.Pressed += () => PickSkillReward(slotIndex);
            _wiredSkillCards.Add(slot);
        }
    }

    /// <summary>Populate each visible slot with its offered skill (one offer per player).</summary>
    private void BuildSkillRewards()
    {
        RewardEntry activeEntry = null;
        if (_activeSkillRewardControl != null)
            _rewardEntries.TryGetValue(_activeSkillRewardControl, out activeEntry);

        GameInfo.NormalizePlayerCharacters();
        var players =
            GameInfo.PlayerCharacters
            ?? throw new InvalidOperationException("GameInfo.PlayerCharacters is null.");

        int count = Math.Min(players.Length, _skillRewardSlots.Count);
        EnsureSkillRewardOffersGenerated(players, count);
        if (
            activeEntry != null
            && (activeEntry.OfferedSkillIds == null || activeEntry.OfferedPlayerIndexes == null)
        )
        {
            GenerateSkillRewardOffers(
                activeEntry,
                players,
                count,
                previouslyGeneratedEntries: Array.Empty<RewardEntry>()
            );
        }

        _offeredSkillIds = activeEntry?.OfferedSkillIds?.ToArray() ?? new SkillID?[count];
        _offeredPlayerIndexes =
            activeEntry?.OfferedPlayerIndexes?.ToArray() ?? Enumerable.Range(0, count).ToArray();

        for (int i = 0; i < _skillRewardSlots.Count; i++)
        {
            _skillRewardSlots[i].Visible = i < count;
            _skillRewardSlots[i].Button.Disabled = i >= count;
        }

        for (int i = 0; i < count; i++)
        {
            int playerIndex = _offeredPlayerIndexes[i];
            var slot = _skillRewardSlots[i];
            SkillID? pickedId = i < _offeredSkillIds.Length ? _offeredSkillIds[i] : null;
            if (pickedId == null)
            {
                slot.PreviewCharacterName = string.Empty;
                slot.SetSkill(null);
                slot.Button.Disabled = true;
                continue;
            }

            var skill = Skill.GetSkill(pickedId.Value);
            if (skill == null)
            {
                slot.PreviewCharacterName = string.Empty;
                slot.SetSkill(null);
                slot.Button.Disabled = true;
                continue;
            }

            var info = players[playerIndex];
            slot.PreviewCharacterName = GameInfo.PlayerCharacters[playerIndex].CharacterName;
            slot.PreviewCharacterKey = ExtractCharacterKeyFromScenePath(GameInfo.PlayerCharacters[playerIndex].CharacterScenePath);
            skill.SetPreviewStats(
                TalentTree.GetEffectivePower(info),
                TalentTree.GetEffectiveSurvivability(info),
                1
            );
            slot.SetSkill(skill);
        }
    }

    private void EnsureSkillRewardOffersGenerated(PlayerInfoStructure[] players, int count)
    {
        if (_skillRewardOffersGenerated)
            return;

        RewardEntry[] skillEntries = _rewardEntries
            .Values.Where(entry => entry.Kind == RewardKind.Skill && entry.SkillGroupIndex >= 0)
            .OrderBy(entry => entry.SkillGroupIndex)
            .ToArray();

        List<RewardEntry> generatedEntries = new();
        for (int i = 0; i < skillEntries.Length; i++)
        {
            RewardEntry entry = skillEntries[i];
            if (entry.OfferedSkillIds == null || entry.OfferedPlayerIndexes == null)
                GenerateSkillRewardOffers(entry, players, count, generatedEntries);

            generatedEntries.Add(entry);
        }

        _skillRewardOffersGenerated = true;
    }

    private void GenerateSkillRewardOffers(
        RewardEntry entry,
        PlayerInfoStructure[] players,
        int count,
        IEnumerable<RewardEntry> previouslyGeneratedEntries
    )
    {
        if (entry == null)
            return;

        entry.OfferedSkillIds = new SkillID?[count];
        entry.OfferedPlayerIndexes = new int[count];

        int skillSeed = _completeNodeOnClose?.RandomNum ?? GameInfo.Seed;
        int groupSeed =
            entry.SkillGroupIndex >= 0
                ? unchecked(skillSeed * 397 ^ (entry.SkillGroupIndex + 1) * 7919)
                : skillSeed;
        var rng = new Random(groupSeed);
        RewardEntry[] priorEntries = previouslyGeneratedEntries?.ToArray() ?? Array.Empty<RewardEntry>();

        for (int i = 0; i < count; i++)
        {
            entry.OfferedPlayerIndexes[i] = i;

            HashSet<SkillID> avoidSkillIds = new();
            for (int j = 0; j < priorEntries.Length; j++)
            {
                RewardEntry priorEntry = priorEntries[j];
                if (priorEntry?.OfferedSkillIds == null || i >= priorEntry.OfferedSkillIds.Length)
                    continue;

                SkillID? priorSkillId = priorEntry.OfferedSkillIds[i];
                if (priorSkillId.HasValue)
                    avoidSkillIds.Add(priorSkillId.Value);
            }

            entry.OfferedSkillIds[i] = PickSkillId(
                players[i],
                rng,
                avoidSkillIds,
                entry.ForceRareSkillReward ? Skill.SkillRarity.Rare : null,
                entry.AllowRareSkillReward
            );
        }
    }

    private static SkillID? PickSkillId(
        PlayerInfoStructure info,
        Random rng,
        ISet<SkillID> avoidSkillIds = null,
        Skill.SkillRarity? forcedRarity = null,
        bool allowRare = true
    )
    {
        var pool = info.AllSkills;
        if (pool == null || pool.Length == 0)
            return null;

        Skill.SkillRarity rolledRarity = forcedRarity ?? Skill.RollRewardRarity(rng, allowRare);
        if (!allowRare && rolledRarity == Skill.SkillRarity.Rare)
            rolledRarity = Skill.SkillRarity.Uncommon;

        SkillID[] filteredPool = GetRewardSkillPoolForRarity(pool, rolledRarity, allowRare);
        if (filteredPool.Length == 0)
            filteredPool = allowRare
                ? pool
                : pool.Where(skillId => Skill.GetRarity(skillId) != Skill.SkillRarity.Rare).ToArray();
        if (filteredPool.Length == 0)
            filteredPool = pool;

        SkillID[] pickPool = filteredPool;
        if (avoidSkillIds != null && avoidSkillIds.Count > 0)
        {
            SkillID[] filteredNonDuplicate = filteredPool
                .Where(skillId => !avoidSkillIds.Contains(skillId))
                .ToArray();
            if (filteredNonDuplicate.Length > 0)
            {
                pickPool = filteredNonDuplicate;
            }
            else
            {
                SkillID[] fullPoolNonDuplicate = pool
                    .Where(skillId =>
                        !avoidSkillIds.Contains(skillId)
                        && (allowRare || Skill.GetRarity(skillId) != Skill.SkillRarity.Rare)
                    )
                    .ToArray();
                if (fullPoolNonDuplicate.Length > 0)
                    pickPool = fullPoolNonDuplicate;
            }
        }

        return pickPool[rng.Next(0, pickPool.Length)];
    }

    private static SkillID[] GetRewardSkillPoolForRarity(
        IEnumerable<SkillID> pool,
        Skill.SkillRarity rolledRarity,
        bool allowRare = true
    )
    {
        SkillID[] sourcePool = (pool ?? Array.Empty<SkillID>()).ToArray();
        if (sourcePool.Length == 0)
            return Array.Empty<SkillID>();

        foreach (Skill.SkillRarity rarity in Skill.GetRewardRarityFallbackOrder(rolledRarity, allowRare))
        {
            SkillID[] filtered = Skill.FilterSkillPoolByRarity(sourcePool, rarity);
            if (filtered.Length > 0)
                return filtered;
        }

        return sourcePool;
    }

    /// <summary>Pick the skill reward from a slot and hide the skill cards.</summary>
    private void PickSkillReward(int slotIndex)
    {
        if (_pickedSkill)
            return;
        _pickedSkill = true;
        if (SkillRewardSkipButton != null)
        {
            SkillRewardSkipButton.Disabled = true;
            SkillRewardSkipButton.Visible = false;
        }

        var skillId = _offeredSkillIds[slotIndex]!.Value;
        int playerIndex = _offeredPlayerIndexes[slotIndex];

        var players =
            GameInfo.PlayerCharacters
            ?? throw new InvalidOperationException("GameInfo.PlayerCharacters is null.");

        var info = players[playerIndex];
        info.GainedSkills ??= new List<SkillID>();
        info.GainedSkills.Add(skillId);
        players[playerIndex] = info;
        GameInfo.PlayerCharacters = players;

        for (int i = 0; i < _skillRewardSlots.Count; i++)
            _skillRewardSlots[i].Button.Disabled = true;

        for (int i = 0; i < _skillRewardSlots.Count; i++)
        {
            var slot = _skillRewardSlots[i];
            if (i == slotIndex || !slot.Visible)
                continue;
            slot.Vanish();
        }

        var tween = CreateTween();
        tween.TweenInterval(0.35f);
        tween.TweenCallback(Callable.From(CloseSkillRewardPanel));
    }

    private void CloseSkillRewardPanel()
    {
        _isSkillRewardOpen = false;
        _pickedSkill = false;
        if (SkillRewardsContainer != null)
            SkillRewardsContainer.Visible = false;
        if (SkillRewardSkipButton != null)
            SkillRewardSkipButton.Visible = false;
        InventoryGridNode?.SetInputBlocked(false);
        ShowSkillMask(false);

        if (_activeSkillRewardControl != null)
        {
            RemoveRewardControlWithReflow(_activeSkillRewardControl);
            _activeSkillRewardControl = null;
        }

        TryCloseIfDone();
    }

    private void GrantRelicReward(RelicID relicId)
    {
        var resourceState = MapNode?.PlayerResourceState;
        if (resourceState == null)
            return;

        var existing = resourceState.RelicList?.FirstOrDefault(relic => relic.ID == relicId);
        if (existing == null)
        {
            Relic.RelicAdd(resourceState, relicId);
            return;
        }

        int addNum = Relic.GetAcquireAmount(relicId);
        existing.Num += addNum;
        GameInfo.Relics[relicId] = existing.Num;
        existing.UpdateIconLabel();
    }

    private bool GrantItemReward(ItemID itemId)
    {
        var resourceState = MapNode?.PlayerResourceState;
        if (resourceState == null)
            return false;

        return ConsumeItem.TryAddItem(resourceState, itemId, syncGameInfo: true);
    }

    private bool GrantTalentPointReward()
    {
        if (_battleTalentPointRewardGranted)
            return true;

        var talentReward = GameInfo.TryGrantBattleTalentPointReward(_completeNodeOnClose);
        if (!talentReward.Granted)
            return false;

        _battleTalentPointRewardGranted = true;
        PrintTalentPointReward(talentReward);
        OpenTalentRewardTree(talentReward.CharacterName);
        return true;
    }

    private void OpenTalentRewardTree(string characterName)
    {
        int characterIndex = FindPlayerIndexByCharacterName(characterName);
        if (characterIndex < 0 || TalentOverlay == null)
            return;

        _activeTalentCharacterIndex = characterIndex;
        _isTalentTreeOpen = true;
        InventoryGridNode?.SetInputBlocked(true);
        ShowSkillMask(true);

        if (TalentCharacterLabel != null)
            TalentCharacterLabel.Text = I18n.Format(
                "ui.common.talent_tree_title",
                "{name} 天赋树",
                ("name", characterName)
            );

        TalentOverlay.Visible = true;
        TalentOverlay.Modulate = new Color(1f, 1f, 1f, 0f);
        var tween = CreateTween();
        tween.SetParallel(true);
        tween.TweenProperty(TalentOverlay, "modulate:a", 1.0f, 0.18f);
        if (TalentPanel != null)
        {
            TalentPanel.Scale = new Vector2(0.96f, 0.96f);
            tween
                .TweenProperty(TalentPanel, "scale", Vector2.One, 0.2f)
                .SetEase(Tween.EaseType.Out)
                .SetTrans(Tween.TransitionType.Cubic);
        }

        RefreshTalentRewardTree(characterIndex);
    }

    private void CloseTalentRewardTree()
    {
        _isTalentTreeOpen = false;
        _activeTalentCharacterIndex = -1;
        HideTalentTooltip();
        InventoryGridNode?.SetInputBlocked(false);
        ShowSkillMask(false);

        if (TalentOverlay != null)
            TalentOverlay.Visible = false;

        ClearTalentRewardTree();
        TryCloseIfDone();
    }

    private int FindPlayerIndexByCharacterName(string characterName)
    {
        var players = GameInfo.PlayerCharacters;
        if (players == null || string.IsNullOrWhiteSpace(characterName))
            return -1;

        for (int i = 0; i < players.Length; i++)
        {
            if (string.Equals(players[i].CharacterName, characterName, StringComparison.OrdinalIgnoreCase))
                return i;
        }

        return -1;
    }

    private void RefreshTalentRewardTree(int characterIndex)
    {
        ClearTalentRewardTree();

        var players = GameInfo.PlayerCharacters;
        if (players == null || characterIndex < 0 || characterIndex >= players.Length)
        {
            if (TalentPointLabel != null)
                TalentPointLabel.Text = I18n.Tr("ui.common.talent_points_zero", "天赋点 0");
            return;
        }

        var info = players[characterIndex];
        info.UnlockedTalents ??= new List<string>();
        players[characterIndex] = info;
        GameInfo.PlayerCharacters = players;

        if (TalentPointLabel != null)
            TalentPointLabel.Text = I18n.Format(
                "ui.common.remaining_talent_points",
                "剩余天赋点 {value}",
                ("value", info.TalentPoints)
            );

        var nodes = TalentTree.GetNodes(info);
        var nodesById = nodes.ToDictionary(node => node.Id);

        foreach (var node in nodes)
        {
            foreach (string prerequisiteId in node.Prerequisites)
            {
                if (!nodesById.TryGetValue(prerequisiteId, out var prerequisite))
                    continue;

                bool active =
                    TalentTree.HasUnlocked(info, prerequisite.Id)
                    && TalentTree.HasUnlocked(info, node.Id);
                AddTalentConnection(prerequisite.Position, node.Position, active);
            }
        }

        foreach (var node in nodes)
        {
            bool unlocked = TalentTree.HasUnlocked(info, node.Id);
            bool canUnlock = TalentTree.CanUnlock(info, node, out string reason);
            TalentTreeRoot?.AddChild(
                CreateTalentNodeControl(characterIndex, node, unlocked, canUnlock, reason)
            );
        }
    }

    private void ClearTalentRewardTree()
    {
        if (TalentTreeRoot == null)
            return;

        for (int i = TalentTreeRoot.GetChildCount() - 1; i >= 0; i--)
        {
            var child = TalentTreeRoot.GetChild(i);
            TalentTreeRoot.RemoveChild(child);
            child.QueueFree();
        }
    }

    private Control CreateTalentNodeControl(
        int characterIndex,
        TalentNodeDefinition node,
        bool unlocked,
        bool canUnlock,
        string reason
    )
    {
        var wrapper = new Control
        {
            Position = node.Position,
            Size = new Vector2(TalentNodeWidth, TalentNodeHeight + TalentNodeLabelHeight),
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };

        var button = new Button
        {
            Position = Vector2.Zero,
            Size = new Vector2(TalentNodeWidth, TalentNodeHeight),
            CustomMinimumSize = new Vector2(TalentNodeWidth, TalentNodeHeight),
            Text = GetTalentIconText(node),
            TooltipText = string.Empty,
            FocusMode = Control.FocusModeEnum.None,
            Flat = false,
            Disabled = false,
        };

        button.AddThemeFontSizeOverride("font_size", 30);
        button.AddThemeColorOverride("font_color", new Color(0.92f, 0.96f, 1f, 1f));
        button.AddThemeColorOverride("font_hover_color", new Color(1f, 0.92f, 0.72f, 1f));
        button.AddThemeColorOverride("font_pressed_color", new Color(1f, 0.86f, 0.56f, 1f));
        button.AddThemeStyleboxOverride("normal", CreateTalentNodeStyle(unlocked, canUnlock, 0f));
        button.AddThemeStyleboxOverride("hover", CreateTalentNodeStyle(unlocked, canUnlock, 0.12f));
        button.AddThemeStyleboxOverride(
            "pressed",
            CreateTalentNodeStyle(unlocked, canUnlock, 0.22f)
        );
        button.AddThemeStyleboxOverride("disabled", CreateTalentNodeStyle(unlocked, canUnlock, 0f));
        button.MouseEntered += () => ShowTalentTooltip(node, unlocked, canUnlock, reason);
        button.MouseExited += HideTalentTooltip;
        button.Pressed += () => OnTalentRewardNodePressed(characterIndex, node.Id);

        var progressLabel = new Label
        {
            Position = new Vector2(0f, TalentNodeHeight - 2f),
            Size = new Vector2(TalentNodeWidth, TalentNodeLabelHeight),
            Text = unlocked ? "1/1" : "0/1",
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        progressLabel.AddThemeFontSizeOverride("font_size", 15);
        progressLabel.AddThemeColorOverride(
            "font_color",
            unlocked ? new Color(1f, 0.88f, 0.54f, 1f)
                : canUnlock ? new Color(0.82f, 0.94f, 1f, 0.95f)
                : new Color(0.58f, 0.64f, 0.72f, 0.72f)
        );

        wrapper.AddChild(button);
        wrapper.AddChild(progressLabel);
        wrapper.SetMeta("talent_id", node.Id);
        return wrapper;
    }

    private void ShowTalentTooltip(
        TalentNodeDefinition node,
        bool unlocked,
        bool canUnlock,
        string reason
    )
    {
        var tip = EnsureGlobalTooltip();
        if (tip == null)
            return;

        tip.FollowMouse = true;
        tip.AnchorOffset = new Vector2(24f, 20f);
        tip.MinContentWidth = 360f;
        tip.SetText(BuildTalentTooltipText(node, unlocked, canUnlock, reason));
    }

    private void HideTalentTooltip()
    {
        EnsureGlobalTooltip()?.HideTooltip();
    }

    private static string BuildTalentTooltipText(
        TalentNodeDefinition node,
        bool unlocked,
        bool canUnlock,
        string reason
    )
    {
        string stateText = unlocked
            ? I18n.Tr("ui.common.unlocked", "已点亮")
            : canUnlock ? I18n.Tr("ui.common.available_to_unlock", "可点亮")
            : reason;
        string stateColor = unlocked ? "#ffd987" : canUnlock ? "#9ff5ff" : "#9aa3b5";
        string effect = string.IsNullOrWhiteSpace(node.EffectDescription)
            ? I18n.Tr("ui.common.effect_unconfigured", "暂未配置效果。")
            : node.EffectDescription;

        return $"[b]{node.DisplayName}[/b]\n"
            + I18n.Format(
                "ui.common.talent_stage_cost_bbcode",
                "[color=#cfd6e6]阶段 {stage} / 消耗 {cost} 点天赋点[/color]\n",
                ("stage", node.Stage + 1),
                ("cost", node.Cost)
            )
            + $"[color={stateColor}]{stateText}[/color]\n\n"
            + I18n.Format(
                "ui.common.effect_bbcode",
                "[color=#ffd987]效果[/color]\n{value}",
                ("value", effect)
            );
    }

    private static StyleBoxFlat CreateTalentNodeStyle(
        bool unlocked,
        bool canUnlock,
        float hoverBoost
    )
    {
        Color borderColor =
            unlocked ? new Color(1f, 0.78f, 0.38f, 0.88f)
            : canUnlock ? new Color(0.56f, 0.82f, 1f, 0.72f)
            : new Color(0.35f, 0.44f, 0.55f, 0.46f);
        Color bgColor =
            unlocked ? new Color(0.34f, 0.22f, 0.08f, 0.56f + hoverBoost)
            : canUnlock ? new Color(0.08f, 0.17f, 0.25f, 0.54f + hoverBoost)
            : new Color(0.05f, 0.08f, 0.12f, 0.38f);

        return new StyleBoxFlat
        {
            BgColor = bgColor,
            BorderColor = borderColor,
            BorderWidthLeft = 4,
            BorderWidthTop = 4,
            BorderWidthRight = 4,
            BorderWidthBottom = 4,
            CornerRadiusTopLeft = 38,
            CornerRadiusTopRight = 38,
            CornerRadiusBottomRight = 38,
            CornerRadiusBottomLeft = 38,
            ContentMarginLeft = 6,
            ContentMarginRight = 6,
            ContentMarginTop = 6,
            ContentMarginBottom = 6,
        };
    }

    private static string GetTalentIconText(TalentNodeDefinition node)
    {
        if (node.Id.EndsWith(".Core", StringComparison.Ordinal))
            return "✦";
        if (node.Id.EndsWith(".Attack1", StringComparison.Ordinal))
            return "◇";
        if (node.Id.EndsWith(".Attack2", StringComparison.Ordinal))
            return "✧";
        if (node.Id.EndsWith(".Survive1", StringComparison.Ordinal))
            return "◆";
        return "✹";
    }

    private void AddTalentConnection(Vector2 fromPosition, Vector2 toPosition, bool active)
    {
        Color color = active
            ? new Color(1f, 0.76f, 0.36f, 0.72f)
            : new Color(0.48f, 0.62f, 0.78f, 0.28f);
        Vector2 start = fromPosition + new Vector2(TalentNodeWidth * 0.5f, TalentNodeHeight * 0.5f);
        Vector2 end = toPosition + new Vector2(TalentNodeWidth * 0.5f, TalentNodeHeight * 0.5f);
        AddTalentLine(start, end, color);
    }

    private void AddTalentLine(Vector2 start, Vector2 end, Color color)
    {
        if (TalentTreeRoot == null)
            return;

        var line = new Line2D
        {
            Points = [start, end],
            Width = TalentLineThickness,
            DefaultColor = color,
            Antialiased = true,
        };
        TalentTreeRoot.AddChild(line);
    }

    private async void OnTalentRewardNodePressed(int characterIndex, string talentId)
    {
        var players = GameInfo.PlayerCharacters;
        if (players == null || characterIndex < 0 || characterIndex >= players.Length)
            return;

        var info = players[characterIndex];
        bool unlocked = TalentTree.TryUnlock(ref info, talentId, out string message);
        if (unlocked)
        {
            players[characterIndex] = info;
            GameInfo.PlayerCharacters = players;
            SaveSystem.SaveAll();
        }

        GD.Print(message);
        RefreshTalentRewardTree(characterIndex);
        if (unlocked)
            await PlayTalentUnlockEffectAsync(FindTalentRewardNodeControl(talentId));

        if (players[characterIndex].TalentPoints <= 0)
            CloseTalentRewardTree();
    }

    private Control FindTalentRewardNodeControl(string talentId)
    {
        if (TalentTreeRoot == null || string.IsNullOrWhiteSpace(talentId))
            return null;

        foreach (var child in TalentTreeRoot.GetChildren())
        {
            if (child is Control control && (string)control.GetMeta("talent_id", string.Empty) == talentId)
                return control;
        }

        return null;
    }

    private async Task PlayTalentUnlockEffectAsync(Control nodeControl)
    {
        if (nodeControl == null || !GodotObject.IsInstanceValid(nodeControl) || !IsInsideTree())
            return;

        var shockWave = CreateTalentUnlockEffectRect(
            TalentUnlockShockWaveShader,
            new Vector2(-104f, -104f),
            new Vector2(284f, 284f)
        );
        var sparkLight = CreateTalentUnlockEffectRect(
            TalentUnlockSparkLightShader,
            new Vector2(-62f, -62f),
            new Vector2(200f, 200f)
        );

        if (shockWave == null || sparkLight == null)
            return;

        nodeControl.AddChild(shockWave);
        nodeControl.AddChild(sparkLight);

        Vector2 baseScale = nodeControl.Scale;
        nodeControl.PivotOffset = new Vector2(TalentNodeWidth * 0.5f, TalentNodeHeight * 0.5f);

        var tween = CreateTween();
        tween.SetParallel(true);
        tween.SetEase(Tween.EaseType.Out);
        tween.TweenProperty(nodeControl, "scale", baseScale * 1.12f, 0.12f);
        tween.TweenProperty(nodeControl, "scale", baseScale, 0.22f).SetDelay(0.12f);
        tween.TweenProperty(nodeControl, "modulate", new Color(1.35f, 1.18f, 0.72f, 1f), 0.08f);
        tween.TweenProperty(nodeControl, "modulate", Colors.White, 0.26f).SetDelay(0.08f);
        tween.TweenMethod(
            Callable.From<float>(value =>
                ((ShaderMaterial)shockWave.Material).SetShaderParameter("progress", value)
            ),
            0.24f,
            1f,
            0.42f
        );
        tween.TweenMethod(
            Callable.From<float>(value =>
                ((ShaderMaterial)sparkLight.Material).SetShaderParameter("progress", value)
            ),
            0f,
            1f,
            0.38f
        );

        await ToSignal(tween, Tween.SignalName.Finished);

        if (GodotObject.IsInstanceValid(shockWave))
            shockWave.QueueFree();
        if (GodotObject.IsInstanceValid(sparkLight))
            sparkLight.QueueFree();
        if (GodotObject.IsInstanceValid(nodeControl))
        {
            nodeControl.Scale = baseScale;
            nodeControl.Modulate = Colors.White;
        }
    }

    private static ColorRect CreateTalentUnlockEffectRect(
        Shader shader,
        Vector2 position,
        Vector2 size
    )
    {
        if (shader == null)
            return null;

        var material = new ShaderMaterial { Shader = shader, ResourceLocalToScene = true };
        if (shader == TalentUnlockShockWaveShader)
        {
            material.SetShaderParameter("line_color", new Color(1f, 0.95f, 0.68f, 1f));
            material.SetShaderParameter("glow_color", new Color(1f, 0.7f, 0.22f, 1f));
            material.SetShaderParameter("progress", 1f);
            material.SetShaderParameter("base_thickness", 0.005f);
            material.SetShaderParameter("max_thickness", 0.0f);
            material.SetShaderParameter("glow_intensity", 19.0f);
        }
        else
        {
            material.SetShaderParameter("main_color", new Color(1f, 0.72f, 0.24f, 1f));
            material.SetShaderParameter("progress", 1f);
            material.SetShaderParameter("glow_intensity", 25.0f);
            material.SetShaderParameter("ray_count", 15.0f);
        }

        return new ColorRect
        {
            Position = position,
            Size = size,
            Material = material,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
    }

    private CanvasLayer EnsureTipLayer()
    {
        var root = GetTree()?.Root;
        if (root == null)
            return null;

        var layer = root.GetNodeOrNull<CanvasLayer>("TipLayer");
        if (layer != null)
            return layer;

        layer = new CanvasLayer { Layer = 6, Name = "TipLayer" };
        root.AddChild(layer);
        return layer;
    }

    private Tip EnsureGlobalTooltip()
    {
        var layer = EnsureTipLayer();
        if (layer == null)
            return null;

        var tip = layer.GetNodeOrNull<Tip>("Tip");
        if (tip != null)
            return tip;

        if (TipScene == null)
            return null;

        tip = TipScene.Instantiate<Tip>();
        tip.Name = "Tip";
        tip.FollowMouse = true;
        tip.AnchorOffset = new Vector2(24f, 20f);
        layer.AddChild(tip);
        return tip;
    }

    private void PlayRewardRejected(Control control)
    {
        if (control == null || !GodotObject.IsInstanceValid(control))
            return;

        if (control is CardSlot card)
        {
            card.PlayRejectAnimation();
            return;
        }

        Vector2 basePosition = control.Position;
        Color baseModulate = control.Modulate;
        var tween = CreateTween();
        tween.SetTrans(Tween.TransitionType.Sine);
        tween.SetEase(Tween.EaseType.Out);
        tween.TweenProperty(control, "position", basePosition + new Vector2(-10f, 0f), 0.045f);
        tween.TweenProperty(control, "position", basePosition + new Vector2(10f, 0f), 0.045f);
        tween.TweenProperty(control, "position", basePosition + new Vector2(-5.5f, 0f), 0.045f);
        tween.TweenProperty(control, "position", basePosition, 0.045f);
        control.Modulate = new Color(1f, 0.45f, 0.45f, baseModulate.A);
        tween.SetParallel(true);
        tween.TweenProperty(control, "modulate", baseModulate, 0.16f);
        tween.Finished += () =>
        {
            if (control != null && GodotObject.IsInstanceValid(control))
            {
                control.Position = basePosition;
                control.Modulate = baseModulate;
            }
        };
    }

    private void RemoveRewardControlWithReflow(Control control)
    {
        if (InventoryGridNode == null)
        {
            RemoveRewardControl(control);
            return;
        }

        var previousPositions = CaptureRewardPositions();
        if (control != null && GodotObject.IsInstanceValid(control))
            control.Visible = false;
        RemoveRewardControl(control);
        InventoryGridNode.LayoutCards(force: true);
        _ = AnimateRewardReflowAsync(previousPositions);
    }

    private Dictionary<Control, Vector2> CaptureRewardPositions()
    {
        var positions = new Dictionary<Control, Vector2>();
        if (InventoryGridNode == null)
            return positions;

        foreach (var child in InventoryGridNode.GetChildren())
        {
            if (child is not Control control)
                continue;
            if (!control.Visible)
                continue;
            positions[control] = control.Position;
        }

        return positions;
    }

    private async System.Threading.Tasks.Task AnimateRewardReflowAsync(
        Dictionary<Control, Vector2> previousPositions
    )
    {
        if (InventoryGridNode == null || previousPositions == null || previousPositions.Count == 0)
            return;

        var toAnimate = new List<(Control control, Vector2 from, Vector2 to)>();

        foreach (var pair in previousPositions)
        {
            var control = pair.Key;
            if (!GodotObject.IsInstanceValid(control) || !control.Visible)
                continue;

            Vector2 target = control.Position;
            Vector2 from = pair.Value;
            if (from.DistanceSquaredTo(target) < 0.25f)
                continue;

            toAnimate.Add((control, from, target));
        }

        if (toAnimate.Count == 0)
            return;

        InventoryGridNode.SetLayoutSuppressed(true);
        foreach (var entry in toAnimate)
            entry.control.Position = entry.from;

        var tween = CreateTween();
        tween.SetParallel(true);
        tween.SetEase(Tween.EaseType.Out);
        tween.SetTrans(Tween.TransitionType.Cubic);

        foreach (var entry in toAnimate)
            tween.TweenProperty(entry.control, "position", entry.to, RewardReflowDuration);

        await ToSignal(tween, Tween.SignalName.Finished);

        InventoryGridNode.SetLayoutSuppressed(false);
        InventoryGridNode.LayoutCards(force: true);
    }

    private void TryCloseIfDone()
    {
        if (_isSkillRewardOpen)
            return;
        if (_isTalentTreeOpen)
            return;
        if (_rewardEntries.Count > 0)
            return;
        CloseReward();
    }

    private void CloseReward()
    {
        HideTalentTooltip();
        PlayOutroAnimation(() =>
        {
            if (BG != null)
                BG.Modulate = new Color(1, 1, 1, 1);
            Visible = false;
            TryCompleteNodeOnClose();
            QueueFree();
        });
    }

    private void SkipRewards()
    {
        if (_isTalentTreeOpen)
            CloseTalentRewardTree();
        ResetSkillRewardPanel();
        ClearRewardItems(clearStatic: true);
        CloseReward();
    }

    private void SkipSkillRewardSelection()
    {
        if (!_isSkillRewardOpen || _pickedSkill)
            return;

        ResetSkillRewardPanel();
    }

    private void PlayIntroAnimation()
    {
        _outroTween?.Kill();
        _introTween?.Kill();
        _maskTween?.Kill();

        CachePanelTransform();

        if (BG != null)
            BG.Modulate = new Color(1, 1, 1, 0);
        if (SkillMask != null)
        {
            SkillMask.Modulate = new Color(1, 1, 1, 0);
            SkillMask.Visible = false;
        }

        if (PanelNode != null)
        {
            PanelNode.Modulate = new Color(1, 1, 1, 0);
            PanelNode.Position = _panelBasePosition + new Vector2(0, 36);
            PanelNode.Scale = new Vector2(0.96f, 0.96f);
        }

        if (DecorNode != null)
        {
            DecorNode.Modulate = new Color(1, 1, 1, 0);
            DecorNode.Scale = new Vector2(0.98f, 0.98f);
        }

        _introTween = CreateTween();
        _introTween.SetParallel(true);

        if (BG != null)
            _introTween.TweenProperty(BG, "modulate:a", 1.0f, 0.25f);
        if (PanelNode != null)
        {
            _introTween
                .TweenProperty(PanelNode, "position", _panelBasePosition, 0.35f)
                .SetTrans(Tween.TransitionType.Cubic)
                .SetEase(Tween.EaseType.Out);
            _introTween
                .TweenProperty(PanelNode, "scale", Vector2.One, 0.35f)
                .SetTrans(Tween.TransitionType.Cubic)
                .SetEase(Tween.EaseType.Out);
            _introTween
                .TweenProperty(PanelNode, "modulate:a", 1.0f, 0.25f)
                .SetEase(Tween.EaseType.Out);
        }

        _introTween.SetParallel(false);
        _introTween.TweenInterval(0.08f);
        _introTween.SetParallel(true);
        if (DecorNode != null)
        {
            _introTween.TweenProperty(DecorNode, "modulate:a", 1.0f, 0.2f);
            _introTween
                .TweenProperty(DecorNode, "scale", Vector2.One, 0.2f)
                .SetTrans(Tween.TransitionType.Sine)
                .SetEase(Tween.EaseType.Out);
        }
    }

    private void PlayOutroAnimation(Action onComplete)
    {
        _introTween?.Kill();
        _outroTween?.Kill();
        _maskTween?.Kill();

        CachePanelTransform();

        _outroTween = CreateTween();
        _outroTween.SetParallel(true);

        if (DecorNode != null)
            _outroTween.TweenProperty(DecorNode, "modulate:a", 0.0f, 0.15f);
        if (PanelNode != null)
        {
            _outroTween
                .TweenProperty(PanelNode, "scale", new Vector2(0.96f, 0.96f), 0.2f)
                .SetTrans(Tween.TransitionType.Sine)
                .SetEase(Tween.EaseType.In);
            _outroTween
                .TweenProperty(
                    PanelNode,
                    "position",
                    _panelBasePosition + new Vector2(0, 20),
                    0.22f
                )
                .SetTrans(Tween.TransitionType.Cubic)
                .SetEase(Tween.EaseType.In);
            _outroTween.TweenProperty(PanelNode, "modulate:a", 0.0f, 0.2f);
        }

        if (BG != null)
            _outroTween.TweenProperty(BG, "modulate:a", 0.0f, 0.22f);
        if (SkillMask != null)
        {
            SkillMask.Visible = false;
            SkillMask.Modulate = new Color(1, 1, 1, 0);
        }

        _outroTween.SetParallel(false);
        _outroTween.TweenCallback(Callable.From(() => onComplete?.Invoke()));
    }

    private void ShowSkillMask(bool show)
    {
        if (SkillMask == null)
            return;

        _maskTween?.Kill();
        if (show)
        {
            SkillMask.Visible = true;
            SkillMask.Modulate = new Color(1, 1, 1, 0);
            _maskTween = CreateTween();
            _maskTween
                .TweenProperty(SkillMask, "modulate:a", 1.0f, 0.18f)
                .SetEase(Tween.EaseType.Out)
                .SetTrans(Tween.TransitionType.Sine);
        }
        else
        {
            _maskTween = CreateTween();
            _maskTween
                .TweenProperty(SkillMask, "modulate:a", 0.0f, 0.16f)
                .SetEase(Tween.EaseType.In)
                .SetTrans(Tween.TransitionType.Sine);
            _maskTween.TweenCallback(Callable.From(() => SkillMask.Visible = false));
        }
    }

    private void CachePanelTransform()
    {
        if (_panelBaseCached || PanelNode == null)
            return;

        _panelBasePosition = PanelNode.Position;
        _panelBaseCached = true;
    }

    private void UpdatePanelPivot()
    {
        if (PanelNode == null)
            return;
        PanelNode.PivotOffset = PanelNode.Size * 0.5f;
    }

    private void TryCompleteNodeOnClose()
    {
        var node = _completeNodeOnClose;
        _completeNodeOnClose = null;

        if (node == null || !GodotObject.IsInstanceValid(node))
            return;
        if (!node.IsInsideTree())
            return;
        if (node.State == LevelNode.LevelState.Completed)
            return;

        int rewardCoin = GetElectricityCoinReward(node);
        var talentReward = _battleTalentPointRewardGranted
            ? default
            : GameInfo.TryGrantBattleTalentPointReward(node);
        if (talentReward.Granted)
        {
            _battleTalentPointRewardGranted = true;
            GD.Print(
                I18n.Format(
                    "ui.reward.elite_talent_reward_log",
                    "精英奖励：{name} 获得 {amount} 点天赋点。",
                    ("name", talentReward.CharacterName),
                    ("amount", talentReward.Amount)
                )
            );
        }

        if (MapNode?.PlayerResourceState != null)
        {
            MapNode.PlayerResourceState.ElectricityCoin += rewardCoin;
        }

        node.Completed();
    }

    private static void PrintTalentPointReward(TalentPointRewardResult talentReward)
    {
        GD.Print(
            I18n.Format(
                "ui.reward.battle_talent_reward_log",
                "战斗奖励：{name} 获得 {amount} 点天赋点。",
                ("name", talentReward.CharacterName),
                ("amount", talentReward.Amount)
            )
        );
    }

    private static int GetElectricityCoinReward(LevelNode node)
    {
        if (node.Type == LevelNode.LevelType.Elite)
            return 0;

        int baseReward = node.Type switch
        {
            LevelNode.LevelType.Boss => 150,
            _ => 30,
        };
        int offset = new Random(node.RandomNum).Next(-10, 11);
        return Relic.ApplyElectricityCoinBonus(Math.Max(0, baseReward + offset));
    }

    private static string BuildEquipmentBonusInline(Equipment equip)
    {
        if (equip == null)
            return string.Empty;

        var segments = new List<string>();

        AddStat(segments, equip.Power, I18n.Tr("property.power", "力量"));
        AddStat(segments, equip.Survivability, I18n.Tr("property.survivability", "生存"));
        AddStat(segments, equip.Speed, I18n.Tr("property.speed", "速度"));
        AddStat(segments, equip.MaxLife, I18n.Tr("ui.common.life", "生命"));

        return string.Join("  ", segments);
    }

    private static void AddStat(List<string> segments, int value, string label)
    {
        if (value == 0)
            return;
        segments.Add($"{FormatSignedStat(value)} {label}");
    }

    private static string FormatSignedStat(int value)
    {
        return value.ToString("+0;-0;0");
    }

    private static string GetItemName(ItemID itemId)
    {
        return ConsumeItem.GetItemName(itemId);
    }

    private static string GetItemDescription(ItemID itemId)
    {
        return ConsumeItem.GetItemDescription(itemId, I18n.Tr("ui.reward.item_use_select_character", "点击后选择角色"));
    }

    private static string ExtractCharacterKeyFromScenePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return null;

        var parts = path.Split('/');
        return parts.Length >= 2 ? parts[^2] : null;
    }
}
