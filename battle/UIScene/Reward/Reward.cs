using System;
using System.Collections.Generic;
using System.Linq;
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

    private const int ExpectedSkillSlots = 4;
    private const string RuntimeRewardMeta = "reward_runtime";

    private InventoryGrid InventoryGridNode => field ??= GetNode<InventoryGrid>("Panel/Inventory");
    private Button SkillRewardButton => field ??= GetNodeOrNull<Button>("Panel/Inventory/Button");
    private HBoxContainer SkillRewardsContainer => field ??= GetNodeOrNull<HBoxContainer>("HBoxContainer");
    private ColorRect BG => field ??= GetNode<ColorRect>("BG");
    private ColorRect SkillMask => field ??= GetNodeOrNull<ColorRect>("SkillMask");
    private Control PanelNode => field ??= GetNode<Control>("Panel");
    private Control DecorNode => field ??= GetNodeOrNull<Control>("Panel/Decor");
    private Button SkipButton => field ??= GetNodeOrNull<Button>("Panel/Decor/SkipButton");
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
    private const float RewardReflowDuration = 0.18f;

    private enum RewardKind
    {
        Skill,
        Relic,
        Equipment,
        Item,
    }

    private sealed class RewardEntry
    {
        public RewardKind Kind;
        public RelicID RelicId;
        public Equipment Equipment;
        public ItemID ItemId;
    }

    // Call from anywhere (e.g. Battle) to show the reward UI.
    public static Reward Show(Node caller)
    {
        var tree = caller.GetTree();
        var root = tree.Root;

        var existing = root.GetNodeOrNull<Reward>("Reward");
        if (existing != null)
        {
            existing.CallDeferred(nameof(Open));
            return existing;
        }

        var reward = RewardScene.Instantiate<Reward>();
        reward.Name = "Reward";
        reward.Layer = 1;
        root.AddChild(reward);
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

        CachePanelTransform();
        if (PanelNode != null)
        {
            PanelNode.ItemRectChanged += UpdatePanelPivot;
            UpdatePanelPivot();
        }

        WireStaticRewardControls();
        if (SkipButton != null)
            SkipButton.Pressed += SkipRewards;
    }

    public void SetCompleteNodeOnClose(LevelNode node)
    {
        _completeNodeOnClose = node;
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

        InventoryGridNode?.LayoutCards(force: true);
    }

    /// <summary>Add a skill reward entry to the inventory list.</summary>
    public CardSlot AddSkillRewardEntry(string title = "技能奖励")
    {
        var entry = new RewardEntry { Kind = RewardKind.Skill };
        var card = CreateRewardCard(title, "点击展开技能卡");
        if (card == null)
            return null;
        RegisterRewardControl(card, entry, isRuntime: true);
        return card;
    }

    /// <summary>Add a relic reward entry to the inventory list.</summary>
    public CardSlot AddRelicRewardEntry(RelicID relicId, string displayName = null)
    {
        string title = displayName ?? $"遗物 · {relicId}";
        var entry = new RewardEntry { Kind = RewardKind.Relic, RelicId = relicId };
        var card = CreateRewardCard(title, "点击领取遗物");
        if (card == null)
            return null;
        RegisterRewardControl(card, entry, isRuntime: true);
        return card;
    }

    /// <summary>Add an equipment reward entry to the inventory list.</summary>
    public CardSlot AddEquipmentRewardEntry(Equipment equipment)
    {
        if (equipment == null)
            return null;

        string bonus = BuildEquipmentBonusInline(equipment);
        string title = equipment.DisplayName;
        var entry = new RewardEntry { Kind = RewardKind.Equipment, Equipment = equipment };
        var card = CreateRewardCard(title, bonus);
        if (card == null)
            return null;
        RegisterRewardControl(card, entry, isRuntime: true);
        return card;
    }

    public CardSlot AddEquipmentRewardEntry(Equipment.EquipmentName equipmentName)
    {
        return AddEquipmentRewardEntry(Equipment.Create(equipmentName));
    }

    /// <summary>Add a consumable item reward entry to the inventory list.</summary>
    public CardSlot AddItemRewardEntry(ItemID itemId, string displayName = null)
    {
        string title = displayName ?? $"道具 · {GetItemName(itemId)}";
        string detail = GetItemDescription(itemId);
        var entry = new RewardEntry { Kind = RewardKind.Item, ItemId = itemId };
        var card = CreateRewardCard(title, detail);
        if (card == null)
            return null;
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

        button.Text = "技能奖励";
        button.Visible = true;
        button.Disabled = false;
        button.Modulate = new Color(1, 1, 1, 1);
        RegisterRewardControl(button, new RewardEntry { Kind = RewardKind.Skill }, isRuntime: false);
    }

    private void EnsureDefaultSkillRewardEntry()
    {
        if (_rewardEntries.Values.Any(entry => entry.Kind == RewardKind.Skill))
            return;
        AddSkillRewardEntry();
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
            case RewardKind.Equipment:
                GrantEquipmentReward(entry.Equipment);
                RemoveRewardControlWithReflow(control);
                TryCloseIfDone();
                break;
            case RewardKind.Item:
                GrantItemReward(entry.ItemId);
                RemoveRewardControlWithReflow(control);
                TryCloseIfDone();
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
        var players =
            GameInfo.PlayerCharacters
            ?? throw new InvalidOperationException("GameInfo.PlayerCharacters is null.");

        int count = Math.Min(players.Length, _skillRewardSlots.Count);
        _offeredSkillIds = new SkillID?[count];
        _offeredPlayerIndexes = new int[count];

        var rng = new Random(GameInfo.Seed);

        for (int i = 0; i < _skillRewardSlots.Count; i++)
        {
            _skillRewardSlots[i].Visible = i < count;
            _skillRewardSlots[i].Button.Disabled = i >= count;
        }

        for (int i = 0; i < count; i++)
        {
            int playerIndex = i;
            var info = players[playerIndex];

            var pickedId = PickSkillId(info, rng);
            _offeredSkillIds[i] = pickedId;
            _offeredPlayerIndexes[i] = playerIndex;

            var slot = _skillRewardSlots[i];
            if (pickedId == null)
            {
                slot.SetSkill(null);
                slot.Button.Disabled = true;
                continue;
            }

            var skill = Skill.GetSkill(pickedId.Value);
            if (skill == null)
            {
                slot.SetSkill(null);
                slot.Button.Disabled = true;
                continue;
            }

            slot.CharacterName.Text = GameInfo.PlayerCharacters[playerIndex].CharacterName;
            skill.SetPreviewStats(info.Power, info.Survivability, 1);
            slot.SetSkill(skill);
        }
    }

    private static SkillID? PickSkillId(PlayerInfoStructure info, Random rng)
    {
        var pool = info.AllSkills;
        var gained = info.GainedSkills ?? new List<SkillID>();
        var candidates = pool.Where(id => !gained.Contains(id)).ToArray();
        if (candidates.Length == 0)
            candidates = pool;

        return candidates[rng.Next(0, candidates.Length)];
    }

    /// <summary>Pick the skill reward from a slot and hide the skill cards.</summary>
    private void PickSkillReward(int slotIndex)
    {
        if (_pickedSkill)
            return;
        _pickedSkill = true;

        var skillId = _offeredSkillIds[slotIndex]!.Value;
        int playerIndex = _offeredPlayerIndexes[slotIndex];

        var players =
            GameInfo.PlayerCharacters
            ?? throw new InvalidOperationException("GameInfo.PlayerCharacters is null.");

        var info = players[playerIndex];
        info.GainedSkills ??= new List<SkillID>();
        if (!info.GainedSkills.Contains(skillId))
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

        int addNum = relicId switch
        {
            RelicID.Blessing => 3,
            _ => -1,
        };
        existing.Num += addNum;
        GameInfo.Relic[relicId] = existing.Num;
        if (existing.IconNode != null)
            existing.IconNode.GetNode<Label>("Label").Text = existing.Num.ToString();
    }

    private void GrantEquipmentReward(Equipment equipment)
    {
        if (equipment == null)
            return;
        GameInfo.OwnedEquipments ??= new List<Equipment>();
        GameInfo.OwnedEquipments.Add(Equipment.Clone(equipment));
    }

    private void GrantItemReward(ItemID itemId)
    {
        var resourceState = MapNode?.PlayerResourceState;
        if (resourceState == null)
            return;

        ConsumeItem.AddItem(resourceState, itemId, syncGameInfo: true);
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
        if (_rewardEntries.Count > 0)
            return;
        CloseReward();
    }

    private void CloseReward()
    {
        PlayOutroAnimation(() =>
        {
            if (BG != null)
                BG.Modulate = new Color(1, 1, 1, 1);
            Visible = false;
            TryCompleteNodeOnClose();
        });
    }

    private void SkipRewards()
    {
        ResetSkillRewardPanel();
        ClearRewardItems(clearStatic: true);
        CloseReward();
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
            _introTween.TweenProperty(PanelNode, "position", _panelBasePosition, 0.35f)
                .SetTrans(Tween.TransitionType.Cubic)
                .SetEase(Tween.EaseType.Out);
            _introTween.TweenProperty(PanelNode, "scale", Vector2.One, 0.35f)
                .SetTrans(Tween.TransitionType.Cubic)
                .SetEase(Tween.EaseType.Out);
            _introTween.TweenProperty(PanelNode, "modulate:a", 1.0f, 0.25f)
                .SetEase(Tween.EaseType.Out);
        }

        _introTween.SetParallel(false);
        _introTween.TweenInterval(0.08f);
        _introTween.SetParallel(true);
        if (DecorNode != null)
        {
            _introTween.TweenProperty(DecorNode, "modulate:a", 1.0f, 0.2f);
            _introTween.TweenProperty(DecorNode, "scale", Vector2.One, 0.2f)
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
            _outroTween.TweenProperty(PanelNode, "scale", new Vector2(0.96f, 0.96f), 0.2f)
                .SetTrans(Tween.TransitionType.Sine)
                .SetEase(Tween.EaseType.In);
            _outroTween.TweenProperty(
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
            _maskTween.TweenProperty(SkillMask, "modulate:a", 1.0f, 0.18f)
                .SetEase(Tween.EaseType.Out)
                .SetTrans(Tween.TransitionType.Sine);
        }
        else
        {
            _maskTween = CreateTween();
            _maskTween.TweenProperty(SkillMask, "modulate:a", 0.0f, 0.16f)
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

        var rngN = new Random(node.RandomNum).Next(-10, 10);
        MapNode.PlayerResourceState.ElectricityCoin += 30 + rngN;
        node.Completed();
    }

    private static string BuildEquipmentBonusInline(Equipment equip)
    {
        if (equip == null)
            return string.Empty;

        var segments = new List<string>();

        AddStat(segments, equip.Power, "力量");
        AddStat(segments, equip.Survivability, "生存");
        AddStat(segments, equip.Speed, "速度");
        AddStat(segments, equip.MaxLife, "生命");

        return string.Join("  ", segments);
    }

    private static void AddStat(List<string> segments, int value, string label)
    {
        if (value == 0)
            return;
        string prefix = value > 0 ? "+" : string.Empty;
        segments.Add($"{prefix}{value} {label}");
    }

    private static string GetItemName(ItemID itemId)
    {
        return itemId switch
        {
            ItemID.Health => "医疗包",
            ItemID.Explosion => "爆裂弹",
            _ => "未知道具",
        };
    }

    private static string GetItemDescription(ItemID itemId)
    {
        return itemId switch
        {
            ItemID.Health => "点击后选择角色，回复15生命。",
            ItemID.Explosion => "点击后选择角色，造成30伤害。",
            _ => string.Empty,
        };
    }
}
