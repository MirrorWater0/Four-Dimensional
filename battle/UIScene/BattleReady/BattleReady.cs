using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;

public partial class BattleReady : Control
{
    private static readonly bool AllowManualPlayerFormationAdjustment = false;

    private enum BattleReadyMode
    {
        Tactics,
        Talent,
        Formation,
    }

    public static PackedScene PortaitScene = GD.Load<PackedScene>(
        "res://battle/UIScene/BattleReady/PortaitFrame.tscn"
    );
    public Control Grid => field ??= ResolveNode<Control>("FormationModeRoot/GridContainer");
    private Control FormationModeRoot => field ??= ResolveNode<Control>("FormationModeRoot");
    private Control TacticsModeRoot => field ??= ResolveNode<Control>("TacticsModeRoot");
    private Control TalentModeRoot => field ??= ResolveNode<Control>("TalentModeRoot");
    private Control ModeSelectorRoot => field ??= ResolveNode<Control>("ModeSelectorRoot");
    private Control ModeSelectorThumb =>
        field ??= ResolveNode<Control>("ModeSelectorRoot/ModeThumb");
    private Control CharacterSelectorThumb =>
        field ??= ResolveNode<Control>("CharacterSelectRoot/CharacterSelectThumb");
    private Button TacticsModeButton =>
        field ??= ResolveNode<Button>(
            "ModeSelectorRoot/ModeButtonsMargin/ModeButtons/TacticsModeButton"
        );
    private Button TalentModeButton =>
        field ??= ResolveNode<Button>(
            "ModeSelectorRoot/ModeButtonsMargin/ModeButtons/TalentModeButton"
        );
    private Button FormationModeButton =>
        field ??= ResolveNode<Button>(
            "ModeSelectorRoot/ModeButtonsMargin/ModeButtons/FormationModeButton"
        );
    private ScrollContainer SkillContainer =>
        field ??= ResolveNode<ScrollContainer>("TacticsModeRoot/SkillContainer");
    private GridContainer SkillGrid =>
        field ??= ResolveNode<GridContainer>(
            "TacticsModeRoot/SkillContainer/SkillScrollMargin/SkillGrid"
        );
    private Control CharacterSelectRoot =>
        field ??= ResolveNode<Control>("CharacterSelectRoot");
    private Control FormationFrame =>
        field ??= ResolveNode<Control>("FormationModeRoot/FormationFrame");
    private Control FormationHeaderFrame =>
        field ??= ResolveNode<Control>("FormationModeRoot/FormationHeaderFrame");
    private Control SkillAreaFrame =>
        field ??= ResolveNode<Control>("TacticsModeRoot/SkillAreaFrame");
    private Control SkillAreaHeaderFrame =>
        field ??= ResolveNode<Control>("TacticsModeRoot/SkillAreaHeaderFrame");
    private Control SkillAreaHeader =>
        field ??= ResolveNode<Control>("TacticsModeRoot/SkillAreaHeaderFrame/SkillAreaHeader");
    private Control SkillTypeFrame =>
        field ??= ResolveNode<Control>("TacticsModeRoot/SkillTypeFrame");
    private Control SkillTypeIcons =>
        field ??= ResolveNode<Control>("TacticsModeRoot/SkillTypeFrame/SkillTypeIcons");
    private Control SkillDividers =>
        field ??= ResolveNode<Control>("TacticsModeRoot/SkillDividers");
    private Control TalentTreeFrame =>
        field ??= ResolveNode<Control>("TalentModeRoot/TalentTreeFrame");
    private Control TalentTreeHeaderFrame =>
        field ??= ResolveNode<Control>("TalentModeRoot/TalentTreeHeaderFrame");
    private Control TalentPointFrame =>
        field ??= ResolveNode<Control>("TalentModeRoot/TalentPointFrame");
    private Label TalentPointLabel =>
        field ??= ResolveNode<Label>("TalentModeRoot/TalentPointFrame/TalentPointLabel");
    private Control TalentTreeRoot =>
        field ??= ResolveNode<Control>("TalentModeRoot/TalentTreeRoot");
    private Control TopAccent => field ??= GetNode<Control>("ColorRect");
    public ColorRect BG => field ??= GetNode<ColorRect>("BG");
    private Control CharacterSelectPanel =>
        field ??= ResolveNode<Control>("CharacterSelectRoot/CharacterSelectPanel");
    private Button[] CharacterButtons =>
        field ??= [
            ResolveNode<Button>(
                "CharacterSelectRoot/CharacterSelectPanel/CharacterSelectList/EchoButton"
            ),
            ResolveNode<Button>(
                "CharacterSelectRoot/CharacterSelectPanel/CharacterSelectList/KasiyaButton"
            ),
            ResolveNode<Button>(
                "CharacterSelectRoot/CharacterSelectPanel/CharacterSelectList/MariyaButton"
            ),
            ResolveNode<Button>(
                "CharacterSelectRoot/CharacterSelectPanel/CharacterSelectList/NightingaleButton"
            ),
        ];

    private static readonly PackedScene SkillCardScene = GD.Load<PackedScene>(
        "res://battle/UIScene/Reward/SkillCard.tscn"
    );
    private static readonly PackedScene TipScene = GD.Load<PackedScene>(
        "res://battle/UIScene/Tip.tscn"
    );
    private const float SkillButtonExitStagger = 0.018f;
    private const float SkillCardEnterStagger = 0.035f;
    private const float SkillCardExitSettleTime = 0.14f;
    private static readonly Vector2 SkillCardBaseDisplaySize = new(240f, 370f);
    private static readonly Vector2 BattleReadySkillCardScale = new(0.78f, 0.78f);
    private static readonly Vector2 SkillCardHoverPadding = Vector2.Zero;
    private const float TalentNodeWidth = 76f;
    private const float TalentNodeHeight = 76f;
    private const float TalentNodeLabelHeight = 22f;
    private const float TalentLineThickness = 5f;

    private T ResolveNode<T>(string path, string fallbackName = null)
        where T : Node
    {
        var directNode = GetNodeOrNull<T>(path);
        if (directNode != null)
            return directNode;

        string nodeName = fallbackName ?? path.Split('/').Last();
        var fallbackNode = FindChild(nodeName, true, false);
        if (fallbackNode is T typedNode)
            return typedNode;

        throw new InvalidOperationException(
            $"BattleReady node not found: '{path}' (fallback name '{nodeName}')"
        );
    }

    private readonly List<SkillDisplayEntry>[] _skillBuckets =
        new List<SkillDisplayEntry>[] { new(), new(), new() };
    private readonly Random _skillAnimationRandom = new();

    private readonly struct SkillDisplayEntry(SkillID skillId, int count)
    {
        public SkillID SkillId { get; } = skillId;
        public int Count { get; } = count;
    }

    private static int GetSkillCategoryIndex(Skill skill) =>
        skill == null ? -1 : GetSkillCategoryIndex(skill.SkillType);

    private static int GetSkillCategoryIndex(Skill.SkillTypes skillType)
    {
        return skillType switch
        {
            Skill.SkillTypes.Attack => 0,
            Skill.SkillTypes.Survive => 1,
            Skill.SkillTypes.Special => 2,
            _ => -1,
        };
    }

    private void CacheCharacterSkillBuckets(int characterIndex)
    {
        foreach (var bucket in _skillBuckets)
            bucket.Clear();

        var character = GameInfo.PlayerCharacters[characterIndex];
        var groupedSkills = (character.GainedSkills ?? new List<SkillID>())
            .GroupBy(skillId => skillId)
            .Select(group => new SkillDisplayEntry(group.Key, group.Count()));

        foreach (var entry in groupedSkills)
        {
            var skill = Skill.GetSkill(entry.SkillId);
            int skillIndex = GetSkillCategoryIndex(skill);
            if (skillIndex >= 0)
                _skillBuckets[skillIndex].Add(entry);
        }
    }

    private static string GetSkillDisplayName(Skill skill, int count)
    {
        string name = skill?.SkillName ?? string.Empty;
        return count > 1 ? $"{name} x{count}" : name;
    }

    private static Vector2 GetBattleReadySkillCardSize()
    {
        return SkillCardBaseDisplaySize * BattleReadySkillCardScale + SkillCardHoverPadding * 2f;
    }

    private static Control CreateSkillCardHolder(SkillCard card)
    {
        var holder = new Control
        {
            CustomMinimumSize = GetBattleReadySkillCardSize(),
            SizeFlagsHorizontal = SizeFlags.ShrinkCenter,
            SizeFlagsVertical = SizeFlags.ShrinkCenter,
            MouseFilter = MouseFilterEnum.Ignore,
        };

        card.Position =
            SkillCardHoverPadding
            - 0.5f * (Vector2.One - BattleReadySkillCardScale) * SkillCardBaseDisplaySize;
        holder.AddChild(card);
        return holder;
    }

    private static void ClearSkillGridChildren(GridContainer skillGrid)
    {
        for (int i = skillGrid.GetChildCount() - 1; i >= 0; i--)
        {
            var child = skillGrid.GetChild(i);
            skillGrid.RemoveChild(child);
            child.QueueFree();
        }
    }

    private void ShuffleSkillAnimationOrder<T>(IList<T> items)
    {
        for (int i = items.Count - 1; i > 0; i--)
        {
            int swapIndex = _skillAnimationRandom.Next(i + 1);
            (items[i], items[swapIndex]) = (items[swapIndex], items[i]);
        }
    }

    private SkillCard CreateSkillCard(SkillDisplayEntry entry, PlayerInfoStructure character)
    {
        var skill = Skill.GetSkill(entry.SkillId);
        if (skill == null)
            return null;

        skill.SetPreviewStats(character.Power, character.Survivability, 1);

        var card = SkillCardScene.Instantiate<SkillCard>();
        card.Name = $"SkillCard_{entry.SkillId}";
        card.ConfigureDisplayScale(BattleReadySkillCardScale);
        card.AutoPressEffect = false;
        card.Button.ToggleMode = false;
        card.Button.ButtonPressed = false;
        card.Button.FocusMode = Control.FocusModeEnum.None;
        card.CharacterName.Text = character.CharacterName ?? string.Empty;
        card.SetSkill(skill);
        card.CharacterName.Text = character.CharacterName ?? string.Empty;
        card.NameLabel.Text = GetSkillDisplayName(skill, entry.Count);
        return card;
    }

    private void RefreshTalentTree(int characterIndex)
    {
        ClearTalentTree();

        var players = GameInfo.PlayerCharacters;
        if (players == null || characterIndex < 0 || characterIndex >= players.Length)
        {
            if (TalentPointLabel != null)
                TalentPointLabel.Text = "天赋点 0";
            return;
        }

        var info = players[characterIndex];
        info.UnlockedTalents ??= new List<string>();
        players[characterIndex] = info;

        if (TalentPointLabel != null)
            TalentPointLabel.Text = $"天赋点 {info.TalentPoints}";

        var nodes = TalentTree.GetNodes(info.CharacterName);
        var nodesById = nodes.ToDictionary(node => node.Id);

        foreach (var node in nodes)
        {
            foreach (string prerequisiteId in node.Prerequisites)
            {
                if (!nodesById.TryGetValue(prerequisiteId, out var prerequisite))
                    continue;

                bool active = TalentTree.HasUnlocked(info, prerequisite.Id)
                    && TalentTree.HasUnlocked(info, node.Id);
                AddTalentConnection(prerequisite.Position, node.Position, active);
            }
        }

        foreach (var node in nodes)
        {
            bool unlocked = TalentTree.HasUnlocked(info, node.Id);
            bool canUnlock = TalentTree.CanUnlock(info, node, out string reason);
            var button = CreateTalentNodeControl(characterIndex, node, unlocked, canUnlock, reason);
            TalentTreeRoot.AddChild(button);
        }
    }

    private void ClearTalentTree()
    {
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
            MouseFilter = MouseFilterEnum.Ignore,
        };

        var button = new Button
        {
            Position = Vector2.Zero,
            Size = new Vector2(TalentNodeWidth, TalentNodeHeight),
            CustomMinimumSize = new Vector2(TalentNodeWidth, TalentNodeHeight),
            Text = GetTalentIconText(node),
            TooltipText = string.Empty,
            FocusMode = FocusModeEnum.None,
            Flat = false,
            Disabled = false,
        };

        button.AddThemeFontSizeOverride("font_size", 30);
        button.AddThemeColorOverride("font_color", new Color(0.92f, 0.96f, 1f, 1f));
        button.AddThemeColorOverride("font_hover_color", new Color(1f, 0.92f, 0.72f, 1f));
        button.AddThemeColorOverride("font_pressed_color", new Color(1f, 0.86f, 0.56f, 1f));
        button.AddThemeStyleboxOverride("normal", CreateTalentNodeStyle(unlocked, canUnlock, 0f));
        button.AddThemeStyleboxOverride("hover", CreateTalentNodeStyle(unlocked, canUnlock, 0.12f));
        button.AddThemeStyleboxOverride("pressed", CreateTalentNodeStyle(unlocked, canUnlock, 0.22f));
        button.AddThemeStyleboxOverride("disabled", CreateTalentNodeStyle(unlocked, canUnlock, 0f));
        button.MouseEntered += () => ShowTalentTooltip(node, unlocked, canUnlock, reason);
        button.MouseExited += HideTalentTooltip;
        button.Pressed += () => OnTalentNodePressed(characterIndex, node.Id);

        var progressLabel = new Label
        {
            Position = new Vector2(0f, TalentNodeHeight - 2f),
            Size = new Vector2(TalentNodeWidth, TalentNodeLabelHeight),
            Text = unlocked ? "1/1" : "0/1",
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        progressLabel.AddThemeFontSizeOverride("font_size", 15);
        progressLabel.AddThemeColorOverride(
            "font_color",
            unlocked
                ? new Color(1f, 0.88f, 0.54f, 1f)
                : canUnlock
                    ? new Color(0.82f, 0.94f, 1f, 0.95f)
                    : new Color(0.58f, 0.64f, 0.72f, 0.72f)
        );

        wrapper.AddChild(button);
        wrapper.AddChild(progressLabel);
        return wrapper;
    }

    private Button CreateTalentNodeButton(
        int characterIndex,
        TalentNodeDefinition node,
        bool unlocked,
        bool canUnlock,
        string reason
    )
    {
        var button = new Button
        {
            Position = node.Position,
            Size = new Vector2(TalentNodeWidth, TalentNodeHeight),
            CustomMinimumSize = new Vector2(TalentNodeWidth, TalentNodeHeight),
            Text =
                $"{(unlocked ? "◆" : canUnlock ? "◇" : "·")} {node.DisplayName}\n阶段 {node.Stage + 1} / 消耗 {node.Cost}",
            TooltipText = string.Empty,
            FocusMode = FocusModeEnum.None,
            Flat = false,
            Disabled = unlocked || !canUnlock,
        };

        button.AddThemeFontSizeOverride("font_size", 13);
        button.AddThemeColorOverride("font_color", new Color(0.92f, 0.96f, 1f, 1f));
        button.AddThemeColorOverride("font_hover_color", new Color(1f, 0.92f, 0.72f, 1f));
        button.AddThemeColorOverride("font_pressed_color", new Color(1f, 0.86f, 0.56f, 1f));
        button.AddThemeColorOverride(
            "font_disabled_color",
            unlocked
                ? new Color(1f, 0.86f, 0.52f, 0.92f)
                : new Color(0.54f, 0.61f, 0.68f, 0.62f)
        );
        button.AddThemeStyleboxOverride("normal", CreateTalentNodeStyle(unlocked, canUnlock, 0f));
        button.AddThemeStyleboxOverride("hover", CreateTalentNodeStyle(unlocked, canUnlock, 0.12f));
        button.AddThemeStyleboxOverride("pressed", CreateTalentNodeStyle(unlocked, canUnlock, 0.22f));
        button.AddThemeStyleboxOverride("disabled", CreateTalentNodeStyle(unlocked, canUnlock, 0f));
        button.MouseEntered += () => ShowTalentTooltip(node, unlocked, canUnlock, reason);
        button.MouseExited += HideTalentTooltip;
        button.Pressed += () => OnTalentNodePressed(characterIndex, node.Id);
        return button;
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
        string stateText = unlocked ? "已点亮" : canUnlock ? "可点亮" : reason;
        string stateColor = unlocked ? "#ffd987" : canUnlock ? "#9ff5ff" : "#9aa3b5";
        string description = string.IsNullOrWhiteSpace(node.Description) ? "-" : node.Description;
        string effect = string.IsNullOrWhiteSpace(node.EffectDescription)
            ? "暂未配置效果。"
            : node.EffectDescription;

        return
            $"[b]{node.DisplayName}[/b]\n"
            + $"[color=#cfd6e6]阶段 {node.Stage + 1} / 消耗 {node.Cost} 点天赋点[/color]\n"
            + $"[color={stateColor}]{stateText}[/color]\n\n"
            + $"[color=#9fb5d6]说明[/color]\n{description}\n\n"
            + $"[color=#ffd987]效果[/color]\n{effect}";
    }

    private static StyleBoxFlat CreateTalentNodeStyle(bool unlocked, bool canUnlock, float hoverBoost)
    {
        Color borderColor = unlocked
            ? new Color(1f, 0.78f, 0.38f, 0.88f)
            : canUnlock
                ? new Color(0.56f, 0.82f, 1f, 0.72f)
                : new Color(0.35f, 0.44f, 0.55f, 0.46f);
        Color bgColor = unlocked
            ? new Color(0.34f, 0.22f, 0.08f, 0.56f + hoverBoost)
            : canUnlock
                ? new Color(0.08f, 0.17f, 0.25f, 0.54f + hoverBoost)
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
        var line = new Line2D
        {
            Points = [start, end],
            Width = TalentLineThickness,
            DefaultColor = color,
            Antialiased = true,
        };
        TalentTreeRoot.AddChild(line);
    }

    private void OnTalentNodePressed(int characterIndex, string talentId)
    {
        var players = GameInfo.PlayerCharacters;
        if (players == null || characterIndex < 0 || characterIndex >= players.Length)
            return;

        var info = players[characterIndex];
        if (TalentTree.TryUnlock(ref info, talentId, out string message))
        {
            players[characterIndex] = info;
            SaveSystem.SaveAll();
        }

        GD.Print(message);
        RefreshTalentTree(characterIndex);
    }

    private PortaitFrame _dragTarget;
    private Vector2 _dragMouseOffset;
    private int _selectedCharacterIndex;
    private bool _isTransitioning;
    private bool _isModeTransitioning;
    private bool _modeSelectorPositioned;
    private bool _characterSelectorPositioned;
    private BattleReadyMode _currentMode = BattleReadyMode.Tactics;
    private Tween _modeSelectorTween;
    private Tween _characterSelectorTween;
    private readonly Dictionary<Control, Vector2> _basePositions = [];

    private readonly struct AssemblyItem(Control control, Vector2 offset, float delay)
    {
        public Control Control { get; } = control;
        public Vector2 Offset { get; } = offset;
        public float Delay { get; } = delay;
    }

    public override void _Ready()
    {
        Modulate = new Color(1, 1, 1, 0);
        SetControlAlpha(BG, 0.0f);
        Initialize();
        CacheAssemblyBasePositions();
        WireModeSelector();
        ApplyModeStateImmediate(_currentMode);
        ModeSelectorRoot.Resized += RefreshModeSelectorLayout;
        TacticsModeButton.Resized += RefreshModeSelectorLayout;
        TalentModeButton.Resized += RefreshModeSelectorLayout;
        FormationModeButton.Resized += RefreshModeSelectorLayout;
        CharacterSelectRoot.Resized += RefreshCharacterSelectorLayout;
        foreach (var button in CharacterButtons)
            button.Resized += RefreshCharacterSelectorLayout;
        CallDeferred(nameof(RefreshModeSelectorLayout));
        CallDeferred(nameof(RefreshCharacterSelectorLayout));
    }

    public async void StartAnimation()
    {
        await PlayAssembleAnimationAsync();
    }

    public async Task PlayCloseAnimationAsync()
    {
        while (_isTransitioning || _isModeTransitioning)
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

        _isTransitioning = true;
        try
        {
            var tween = CreateTween();
            tween.SetParallel(true);
            tween.SetEase(Tween.EaseType.In);
            tween.SetTrans(Tween.TransitionType.Cubic);
            tween.TweenProperty(this, "modulate:a", 0.0f, 0.28f);
            tween.TweenProperty(BG, "modulate:a", 0.0f, 0.24f);

            foreach (var item in GetAssemblyItemsForMode(_currentMode))
            {
                if (!_basePositions.TryGetValue(item.Control, out var basePos))
                    continue;

                tween.TweenProperty(item.Control, "position", basePos + item.Offset * 0.75f, 0.22f);
                tween.TweenProperty(item.Control, "modulate:a", 0.0f, 0.2f);
            }

            await ToSignal(tween, Tween.SignalName.Finished);
        }
        finally
        {
            _isTransitioning = false;
        }
    }

    private async Task PlayAssembleAnimationAsync()
    {
        if (_isTransitioning)
            return;

        _isTransitioning = true;
        try
        {
            Modulate = Modulate with { A = 0.0f };
            SetControlAlpha(BG, 0.0f);

            var items = GetAssemblyItemsForMode(_currentMode);
            foreach (var item in items)
            {
                if (!_basePositions.TryGetValue(item.Control, out var basePos))
                    continue;

                item.Control.Position = basePos + item.Offset;
                SetControlAlpha(item.Control, 0.0f);
            }

            var tween = CreateTween();
            tween.SetParallel(true);
            tween.SetEase(Tween.EaseType.Out);
            tween.SetTrans(Tween.TransitionType.Cubic);
            tween.TweenProperty(this, "modulate:a", 1.0f, 0.3f);
            tween.TweenProperty(BG, "modulate:a", 1.0f, 0.36f);

            foreach (var item in items)
            {
                if (!_basePositions.TryGetValue(item.Control, out var basePos))
                    continue;

                tween.TweenProperty(item.Control, "position", basePos, 0.32f).SetDelay(item.Delay);
                tween.TweenProperty(item.Control, "modulate:a", 1.0f, 0.28f).SetDelay(item.Delay);
            }

            await ToSignal(tween, Tween.SignalName.Finished);
        }
        finally
        {
            _isTransitioning = false;
        }
    }

    private void CacheAssemblyBasePositions()
    {
        _basePositions.Clear();
        foreach (var item in GetAllAssemblyItems())
            _basePositions[item.Control] = item.Control.Position;
    }

    private AssemblyItem[] GetAllAssemblyItems()
    {
        return
        [
            new AssemblyItem(ModeSelectorRoot, new Vector2(0f, -26f), 0.00f),
            new AssemblyItem(CharacterSelectRoot, new Vector2(-88f, 24f), 0.00f),
            new AssemblyItem(FormationFrame, new Vector2(-74f, 22f), 0.04f),
            new AssemblyItem(FormationHeaderFrame, new Vector2(-92f, 0f), 0.08f),
            new AssemblyItem(Grid, new Vector2(-60f, 28f), 0.12f),
            new AssemblyItem(SkillAreaFrame, new Vector2(96f, 18f), 0.00f),
            new AssemblyItem(SkillAreaHeaderFrame, new Vector2(78f, 0f), 0.06f),
            new AssemblyItem(SkillAreaHeader, new Vector2(78f, 0f), 0.1f),
            new AssemblyItem(SkillTypeFrame, new Vector2(66f, 0f), 0.12f),
            new AssemblyItem(SkillTypeIcons, new Vector2(54f, -18f), 0.16f),
            new AssemblyItem(TalentTreeFrame, new Vector2(72f, 8f), 0.16f),
            new AssemblyItem(TalentTreeHeaderFrame, new Vector2(58f, 0f), 0.18f),
            new AssemblyItem(TalentPointFrame, new Vector2(50f, -4f), 0.20f),
            new AssemblyItem(TalentTreeRoot, new Vector2(66f, 12f), 0.22f),
            new AssemblyItem(SkillDividers, new Vector2(60f, 24f), 0.2f),
            new AssemblyItem(SkillContainer, new Vector2(88f, 14f), 0.24f),
            new AssemblyItem(TopAccent, new Vector2(0f, -40f), 0.2f),
        ];
    }

    private AssemblyItem[] GetAssemblyItemsForMode(BattleReadyMode mode)
    {
        return mode switch
        {
            BattleReadyMode.Formation =>
            [
                new AssemblyItem(ModeSelectorRoot, new Vector2(0f, -26f), 0.00f),
                new AssemblyItem(FormationFrame, new Vector2(-74f, 22f), 0.1f),
                new AssemblyItem(FormationHeaderFrame, new Vector2(-92f, 0f), 0.14f),
                new AssemblyItem(Grid, new Vector2(-60f, 28f), 0.18f),
                new AssemblyItem(TopAccent, new Vector2(0f, -40f), 0.16f),
            ],
            BattleReadyMode.Talent =>
            [
                new AssemblyItem(ModeSelectorRoot, new Vector2(0f, -26f), 0.00f),
                new AssemblyItem(CharacterSelectRoot, new Vector2(-88f, 24f), 0.08f),
                new AssemblyItem(TalentTreeFrame, new Vector2(72f, 8f), 0.10f),
                new AssemblyItem(TalentTreeHeaderFrame, new Vector2(58f, 0f), 0.14f),
                new AssemblyItem(TalentPointFrame, new Vector2(50f, -4f), 0.16f),
                new AssemblyItem(TalentTreeRoot, new Vector2(66f, 12f), 0.18f),
                new AssemblyItem(TopAccent, new Vector2(0f, -40f), 0.18f),
            ],
            _ =>
            [
                new AssemblyItem(ModeSelectorRoot, new Vector2(0f, -26f), 0.00f),
                new AssemblyItem(CharacterSelectRoot, new Vector2(-88f, 24f), 0.08f),
                new AssemblyItem(SkillAreaFrame, new Vector2(96f, 18f), 0.08f),
                new AssemblyItem(SkillAreaHeaderFrame, new Vector2(78f, 0f), 0.12f),
                new AssemblyItem(SkillAreaHeader, new Vector2(78f, 0f), 0.14f),
                new AssemblyItem(SkillTypeFrame, new Vector2(66f, 0f), 0.16f),
                new AssemblyItem(SkillTypeIcons, new Vector2(54f, -18f), 0.2f),
                new AssemblyItem(SkillDividers, new Vector2(60f, 24f), 0.22f),
                new AssemblyItem(SkillContainer, new Vector2(88f, 14f), 0.28f),
                new AssemblyItem(TopAccent, new Vector2(0f, -40f), 0.18f),
            ],
        };
    }

    private static void SetControlAlpha(Control control, float alpha)
    {
        control.Modulate = control.Modulate with { A = alpha };
    }

    private void WireModeSelector()
    {
        TacticsModeButton.Pressed += () => OnModeButtonPressed(BattleReadyMode.Tactics);
        TalentModeButton.Pressed += () => OnModeButtonPressed(BattleReadyMode.Talent);
        FormationModeButton.Pressed += () => OnModeButtonPressed(BattleReadyMode.Formation);
        SnapModeSelectorToCurrentButton();
    }

    private async void OnModeButtonPressed(BattleReadyMode mode)
    {
        await SwitchModeAsync(mode);
    }

    private async Task SwitchModeAsync(BattleReadyMode targetMode)
    {
        if (targetMode == _currentMode || _isTransitioning || _isModeTransitioning)
            return;

        _isModeTransitioning = true;
        HideTransientTooltips();
        _dragTarget = null;
        _dragMouseOffset = Vector2.Zero;

        try
        {
            _modeSelectorTween?.Kill();
            _characterSelectorTween?.Kill();

            UpdateModeButtonState(targetMode);
            UpdateModeSelectorPosition(targetMode, true);

            await AnimateModeExitAsync(_currentMode);
            SetModeVisible(_currentMode, false);

            _currentMode = targetMode;
            SetModeVisible(BattleReadyMode.Tactics, targetMode == BattleReadyMode.Tactics);
            SetModeVisible(BattleReadyMode.Talent, targetMode == BattleReadyMode.Talent);
            SetModeVisible(BattleReadyMode.Formation, targetMode == BattleReadyMode.Formation);

            if (targetMode == BattleReadyMode.Talent)
                RefreshTalentTree(_selectedCharacterIndex);

            await AnimateModeEnterAsync(_currentMode);
        }
        finally
        {
            _isModeTransitioning = false;
            UpdateModeButtonState(_currentMode);
        }
    }

    private async Task AnimateModeExitAsync(BattleReadyMode mode)
    {
        var tween = CreateTween();
        tween.SetParallel(true);
        tween.SetEase(Tween.EaseType.In);
        tween.SetTrans(Tween.TransitionType.Cubic);

        foreach (var item in GetModeItems(mode))
        {
            if (!_basePositions.TryGetValue(item.Control, out var basePos))
                continue;

            tween.TweenProperty(
                item.Control,
                "position",
                basePos + GetModeExitOffset(mode, item),
                0.18f
            );
            tween.TweenProperty(item.Control, "modulate:a", 0.0f, 0.16f);
        }

        await ToSignal(tween, Tween.SignalName.Finished);
        ResetModeItemsToBase(mode, 0.0f);
    }

    private async Task AnimateModeEnterAsync(BattleReadyMode mode)
    {
        foreach (var item in GetModeItems(mode))
        {
            if (!_basePositions.TryGetValue(item.Control, out var basePos))
                continue;

            item.Control.Position = basePos + GetModeEnterOffset(mode, item);
            SetControlAlpha(item.Control, 0.0f);
        }

        var tween = CreateTween();
        tween.SetParallel(true);
        tween.SetEase(Tween.EaseType.Out);
        tween.SetTrans(Tween.TransitionType.Cubic);

        foreach (var item in GetModeItems(mode))
        {
            if (!_basePositions.TryGetValue(item.Control, out var basePos))
                continue;

            tween
                .TweenProperty(item.Control, "position", basePos, 0.24f)
                .SetDelay(item.Delay * 0.35f);
            tween
                .TweenProperty(item.Control, "modulate:a", 1.0f, 0.2f)
                .SetDelay(item.Delay * 0.35f);
        }

        await ToSignal(tween, Tween.SignalName.Finished);
        ResetModeItemsToBase(mode, 1.0f);
    }

    private void ApplyModeStateImmediate(BattleReadyMode mode)
    {
        _currentMode = mode;
        SetModeVisible(BattleReadyMode.Tactics, mode == BattleReadyMode.Tactics);
        SetModeVisible(BattleReadyMode.Talent, mode == BattleReadyMode.Talent);
        SetModeVisible(BattleReadyMode.Formation, mode == BattleReadyMode.Formation);
        ResetModeItemsToBase(
            BattleReadyMode.Tactics,
            mode == BattleReadyMode.Tactics ? 1.0f : 0.0f
        );
        ResetModeItemsToBase(
            BattleReadyMode.Talent,
            mode == BattleReadyMode.Talent ? 1.0f : 0.0f
        );
        ResetModeItemsToBase(
            BattleReadyMode.Formation,
            mode == BattleReadyMode.Formation ? 1.0f : 0.0f
        );
        SetControlAlpha(
            CharacterSelectRoot,
            mode is BattleReadyMode.Tactics or BattleReadyMode.Talent ? 1.0f : 0.0f
        );
        UpdateModeButtonState(mode);
        UpdateModeSelectorPosition(mode, false);
    }

    private void ResetModeItemsToBase(BattleReadyMode mode, float alpha)
    {
        foreach (var item in GetModeItems(mode))
        {
            if (!_basePositions.TryGetValue(item.Control, out var basePos))
                continue;

            item.Control.Position = basePos;
            SetControlAlpha(item.Control, alpha);
        }
    }

    private AssemblyItem[] GetModeItems(BattleReadyMode mode)
    {
        return mode switch
        {
            BattleReadyMode.Formation =>
            [
                new AssemblyItem(FormationFrame, new Vector2(-74f, 22f), 0.04f),
                new AssemblyItem(FormationHeaderFrame, new Vector2(-92f, 0f), 0.08f),
                new AssemblyItem(Grid, new Vector2(-60f, 28f), 0.12f),
            ],
            BattleReadyMode.Talent =>
            [
                new AssemblyItem(CharacterSelectRoot, new Vector2(-88f, 24f), 0.00f),
                new AssemblyItem(TalentTreeFrame, new Vector2(72f, 8f), 0.06f),
                new AssemblyItem(TalentTreeHeaderFrame, new Vector2(58f, 0f), 0.10f),
                new AssemblyItem(TalentPointFrame, new Vector2(50f, -4f), 0.12f),
                new AssemblyItem(TalentTreeRoot, new Vector2(66f, 12f), 0.14f),
            ],
            _ =>
            [
                new AssemblyItem(CharacterSelectRoot, new Vector2(-88f, 24f), 0.00f),
                new AssemblyItem(SkillAreaFrame, new Vector2(96f, 18f), 0.00f),
                new AssemblyItem(SkillAreaHeaderFrame, new Vector2(78f, 0f), 0.06f),
                new AssemblyItem(SkillAreaHeader, new Vector2(78f, 0f), 0.10f),
                new AssemblyItem(SkillTypeFrame, new Vector2(66f, 0f), 0.12f),
                new AssemblyItem(SkillTypeIcons, new Vector2(54f, -18f), 0.16f),
                new AssemblyItem(SkillDividers, new Vector2(60f, 24f), 0.20f),
                new AssemblyItem(SkillContainer, new Vector2(88f, 14f), 0.26f),
            ],
        };
    }

    private static Vector2 GetModeEnterOffset(BattleReadyMode mode, AssemblyItem item)
    {
        float horizontal = mode == BattleReadyMode.Formation ? 70f : -70f;
        return item.Offset * 0.45f + new Vector2(horizontal, 10f);
    }

    private static Vector2 GetModeExitOffset(BattleReadyMode mode, AssemblyItem item)
    {
        float horizontal = mode == BattleReadyMode.Formation ? 34f : -34f;
        return item.Offset * 0.28f + new Vector2(horizontal, 8f);
    }

    private void SetModeVisible(BattleReadyMode mode, bool visible)
    {
        var root = mode switch
        {
            BattleReadyMode.Formation => FormationModeRoot,
            BattleReadyMode.Talent => TalentModeRoot,
            _ => TacticsModeRoot,
        };
        root.Visible = visible;
        root.MouseFilter = visible ? MouseFilterEnum.Stop : MouseFilterEnum.Ignore;

        bool showCharacterSelector = _currentMode is BattleReadyMode.Tactics or BattleReadyMode.Talent;
        CharacterSelectRoot.Visible = showCharacterSelector;
        CharacterSelectRoot.MouseFilter = showCharacterSelector
            ? MouseFilterEnum.Stop
            : MouseFilterEnum.Ignore;
    }

    private void SetModeSelectorEnabled(bool enabled)
    {
        TacticsModeButton.Disabled = !enabled;
        TalentModeButton.Disabled = !enabled;
        FormationModeButton.Disabled = !enabled;
    }

    private void UpdateModeButtonState(BattleReadyMode mode)
    {
        SetModeButtonState(TacticsModeButton, mode == BattleReadyMode.Tactics);
        SetModeButtonState(TalentModeButton, mode == BattleReadyMode.Talent);
        SetModeButtonState(FormationModeButton, mode == BattleReadyMode.Formation);
    }

    private static void SetModeButtonState(Button button, bool active)
    {
        if (button == null)
            return;

        button.SetPressedNoSignal(active);
        button.Modulate = active ? Colors.White : new Color(0.84f, 0.88f, 0.94f, 0.78f);
    }

    private void SnapModeSelectorToCurrentButton()
    {
        UpdateModeSelectorPosition(_currentMode, false);
    }

    private void RefreshModeSelectorLayout()
    {
        UpdateModeSelectorPosition(_currentMode, false);
    }

    private void RefreshCharacterSelectorLayout()
    {
        UpdateCharacterSelectorPosition(false);
    }

    private void UpdateModeSelectorPosition(BattleReadyMode mode, bool animate)
    {
        var button = mode switch
        {
            BattleReadyMode.Formation => FormationModeButton,
            BattleReadyMode.Talent => TalentModeButton,
            _ => TacticsModeButton,
        };
        if (button == null || !GodotObject.IsInstanceValid(button))
            return;

        Rect2 selectorRect = ModeSelectorRoot.GetGlobalRect();
        Rect2 buttonRect = button.GetGlobalRect();
        if (selectorRect.Size.X <= 0f || buttonRect.Size.X <= 0f)
            return;

        Vector2 targetPosition = buttonRect.Position - selectorRect.Position;
        Vector2 targetSize = buttonRect.Size;

        _modeSelectorTween?.Kill();
        if (animate && _modeSelectorPositioned)
        {
            _modeSelectorTween = CreateTween();
            _modeSelectorTween.SetParallel(true);
            _modeSelectorTween.SetEase(Tween.EaseType.Out);
            _modeSelectorTween.SetTrans(Tween.TransitionType.Cubic);
            _modeSelectorTween.TweenProperty(ModeSelectorThumb, "position", targetPosition, 0.22f);
            _modeSelectorTween.TweenProperty(ModeSelectorThumb, "size", targetSize, 0.22f);
        }
        else
        {
            ModeSelectorThumb.Position = targetPosition;
            ModeSelectorThumb.Size = targetSize;
        }

        _modeSelectorPositioned = true;
    }

    private void UpdateCharacterSelectorPosition(bool animate)
    {
        var button = GetSelectedCharacterButton();
        if (button == null || !button.Visible)
        {
            CharacterSelectorThumb.Visible = false;
            return;
        }

        Rect2 selectorRect = CharacterSelectRoot.GetGlobalRect();
        Rect2 buttonRect = button.GetGlobalRect();
        if (selectorRect.Size.X <= 0f || buttonRect.Size.X <= 0f)
            return;

        CharacterSelectorThumb.Visible = true;
        Vector2 targetPosition = buttonRect.Position - selectorRect.Position;
        Vector2 targetSize = buttonRect.Size;

        _characterSelectorTween?.Kill();
        if (animate && _characterSelectorPositioned && CharacterSelectRoot.Visible)
        {
            _characterSelectorTween = CreateTween();
            _characterSelectorTween.SetParallel(true);
            _characterSelectorTween.SetEase(Tween.EaseType.Out);
            _characterSelectorTween.SetTrans(Tween.TransitionType.Cubic);
            _characterSelectorTween.TweenProperty(
                CharacterSelectorThumb,
                "position",
                targetPosition,
                0.2f
            );
            _characterSelectorTween.TweenProperty(CharacterSelectorThumb, "size", targetSize, 0.2f);
        }
        else
        {
            CharacterSelectorThumb.Position = targetPosition;
            CharacterSelectorThumb.Size = targetSize;
        }

        _characterSelectorPositioned = true;
    }

    private Button GetSelectedCharacterButton()
    {
        if (
            _selectedCharacterIndex >= 0
            && _selectedCharacterIndex < CharacterButtons.Length
            && CharacterButtons[_selectedCharacterIndex].Visible
        )
        {
            return CharacterButtons[_selectedCharacterIndex];
        }

        for (int i = 0; i < CharacterButtons.Length; i++)
        {
            if (CharacterButtons[i].Visible)
                return CharacterButtons[i];
        }

        return null;
    }

    private void HideTransientTooltips()
    {
        var tip = GetTree().Root.GetNodeOrNull<Tip>("TipLayer/Tip");
        if (tip != null)
            tip.HideTooltip();

        var buffTip = GetTree().Root.GetNodeOrNull<Tip>("TipLayer/BuffTip");
        if (buffTip != null)
            buffTip.HideTooltip();
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

    public override void _Process(double delta)
    {
        if (_currentMode != BattleReadyMode.Formation || !FormationModeRoot.Visible)
            return;

        if (_dragTarget != null)
            _dragTarget.GlobalPosition = GetViewport().GetMousePosition() - _dragMouseOffset;

        var mousePos = GetViewport().GetMousePosition();
        for (int i = 0; i < Grid.GetChildCount(); i++)
        {
            var tex = Grid.GetChild<ColorRect>(i);
            bool isOver = tex.GetGlobalRect().HasPoint(mousePos);
            Color accentColor = new Color(0.69f, 0.75f, 0.80f);
            Color targetColor = isOver
                ? accentColor + 5 * new Color(0.2f, 0.2f, 0.2f)
                : accentColor;
            ((ShaderMaterial)tex.Material).SetShaderParameter("line_color", targetColor);
        }
    }

    public void Initialize()
    {
        InitializePostion();
        InitializeCharacterButtons();
        _ = SelectCharacter(_selectedCharacterIndex);
    }

    public static System.Collections.Generic.Dictionary<int, int> remap { get; } =
        new System.Collections.Generic.Dictionary<int, int>()
        {
            [7] = 1,
            [4] = 2,
            [1] = 3,
            [8] = 4,
            [5] = 5,
            [2] = 6,
            [9] = 7,
            [6] = 8,
            [3] = 9,
        };

    public void InitializePostion()
    {
        if (Grid == null)
        {
            GD.PushError("BattleReady: Formation grid is missing.");
            return;
        }

        Color accentColor = new Color(0.69f, 0.75f, 0.80f);
        for (int i = 0; i < Grid.GetChildCount(); i++)
        {
            if (Grid.GetChild(i) is not ColorRect tex)
                continue;

            if (tex.Material is not ShaderMaterial gridMaterial)
                continue;

            gridMaterial.ResourceLocalToScene = true;
            var uniqueMaterial = gridMaterial.Duplicate() as ShaderMaterial;
            if (uniqueMaterial == null)
                continue;

            tex.Material = uniqueMaterial;
            uniqueMaterial.SetShaderParameter("line_color", accentColor);
        }

        var players = GameInfo.PlayerCharacters;
        if (players == null || players.Length == 0)
            return;

        if (PortaitScene == null)
        {
            GD.PushError("BattleReady: Portrait frame scene is missing.");
            return;
        }

        for (int i = 0; i < players.Length; i++)
        {
            var portrait = PortaitScene.Instantiate<PortaitFrame>();
            if (portrait == null)
            {
                GD.PushError("BattleReady: Failed to instantiate portrait frame.");
                continue;
            }

            if (portrait.PortaitRect == null || portrait.PortaitButton == null)
            {
                GD.PushError("BattleReady: Portrait frame is missing required child nodes.");
                portrait.QueueFree();
                continue;
            }

            if (!string.IsNullOrWhiteSpace(players[i].PortaitPath))
                portrait.PortaitRect.Texture = PreloadeScene.GetTexture(players[i].PortaitPath);

            portrait.PortaitIndex = i;
            int positionindex = players[i].PositionIndex;

            if (!remap.TryGetValue(positionindex, out int mappedSlot))
            {
                GD.PrintErr($"Invalid PositionIndex: {positionindex}. Valid values are 1-9.");
                portrait.QueueFree();
                continue;
            }

            int slotIndex = mappedSlot - 1;
            if (slotIndex < 0 || slotIndex >= Grid.GetChildCount())
            {
                GD.PrintErr($"BattleReady: Grid slot index {slotIndex} is out of range.");
                portrait.QueueFree();
                continue;
            }

            var slot = Grid.GetChild(slotIndex);
            if (slot == null)
            {
                GD.PrintErr($"BattleReady: Grid slot {slotIndex} is missing.");
                portrait.QueueFree();
                continue;
            }

            slot.AddChild(portrait);

            portrait.PortaitButton.ButtonDown += () =>
            {
                if (!AllowManualPlayerFormationAdjustment)
                    return;
                _dragTarget = portrait;
                _dragMouseOffset = GetViewport().GetMousePosition() - portrait.GlobalPosition;
                portrait.ZIndex = 1;
            };
            portrait.PortaitButton.ButtonUp += () =>
            {
                if (!AllowManualPlayerFormationAdjustment)
                    return;
                _dragTarget = null;
                _dragMouseOffset = Vector2.Zero;
                portrait.ZIndex = 0;
                var olderParent = portrait.GetParent();
                var newParent = Grid.GetChildren()
                    .OfType<ColorRect>()
                    .FirstOrDefault(x =>
                        x.GetGlobalRect().HasPoint(GetViewport().GetMousePosition())
                    );

                if (newParent != null)
                {
                    if (newParent.GetChildCount() > 0 && olderParent != newParent)
                    {
                        var overPortait = newParent.GetChild<PortaitFrame>(0);
                        overPortait.Reparent(olderParent);
                        TweenSetAnimation(overPortait, 0.2f);
                    }
                    portrait.Reparent(newParent);
                    TweenSetAnimation(portrait, 0.2f);
                    _dragTarget = null;
                    _dragMouseOffset = Vector2.Zero;
                }
                else
                {
                    TweenSetAnimation(portrait, 0.1f);
                }

                void TweenSetAnimation(PortaitFrame p, float time)
                {
                    CreateTween().TweenProperty(p, "position", Vector2.Zero, time);
                    CreateTween()
                        .Chain()
                        .TweenCallback(
                            Callable.From(() =>
                            {
                                p.Animation.Play("explode");
                            })
                        );
                }
            };
        }
    }

    private void InitializeCharacterButtons()
    {
        var players = GameInfo.PlayerCharacters ?? Array.Empty<PlayerInfoStructure>();
        _selectedCharacterIndex = Math.Clamp(
            _selectedCharacterIndex,
            0,
            Math.Max(players.Length - 1, 0)
        );

        for (int i = 0; i < CharacterButtons.Length; i++)
        {
            var button = CharacterButtons[i];
            bool exists = i < players.Length;
            button.Visible = exists;
            button.Disabled = !exists;
            if (!exists)
                continue;

            var info = players[i];
            button.ToggleMode = true;
            button.Text = string.IsNullOrWhiteSpace(info.CharacterName)
                ? $"角色{i + 1}"
                : info.CharacterName;

            int capturedIndex = i;
            button.Pressed += () => OnCharacterButtonPressed(capturedIndex);
        }

        UpdateCharacterButtonState(false);
    }

    private async void OnCharacterButtonPressed(int characterIndex)
    {
        if (
            _currentMode is not (BattleReadyMode.Tactics or BattleReadyMode.Talent)
            || _isModeTransitioning
        )
            return;

        await SelectCharacter(characterIndex);
    }

    private async Task SelectCharacter(int characterIndex)
    {
        if (_isModeTransitioning)
            return;

        var players = GameInfo.PlayerCharacters;
        if (players == null || characterIndex < 0 || characterIndex >= players.Length)
            return;

        if (characterIndex == _selectedCharacterIndex && HasSkillButtons())
        {
            UpdateCharacterButtonState(true);
            RefreshTalentTree(characterIndex);
            return;
        }

        _selectedCharacterIndex = characterIndex;
        UpdateCharacterButtonState(true);
        await ClearSkillContainer();
        PopulateSkillButtons(characterIndex);
        RefreshTalentTree(characterIndex);
    }

    private void PopulateSkillButtons(int characterIndex)
    {
        CacheCharacterSkillBuckets(characterIndex);
        SkillContainer.ScrollVertical = 0;

        var character = GameInfo.PlayerCharacters[characterIndex];
        var cardsToAnimate = new List<SkillCard>();
        foreach (var entry in _skillBuckets.SelectMany(bucket => bucket))
        {
            var card = CreateSkillCard(entry, character);
            if (card == null)
                continue;

            var holder = CreateSkillCardHolder(card);
            SkillGrid.AddChild(holder);
            cardsToAnimate.Add(card);
        }

        SkillGrid.QueueSort();

        ShuffleSkillAnimationOrder(cardsToAnimate);
        for (int i = 0; i < cardsToAnimate.Count; i++)
            cardsToAnimate[i].CallDeferred(
                nameof(SkillCard.StartAnimation),
                SkillCardEnterStagger * i
            );
    }

    private void UpdateCharacterButtonState(bool animateSelector)
    {
        for (int i = 0; i < CharacterButtons.Length; i++)
        {
            var button = CharacterButtons[i];
            bool active = button.Visible && i == _selectedCharacterIndex;
            button.Disabled = !button.Visible;
            SetModeButtonState(button, active);
        }

        UpdateCharacterSelectorPosition(animateSelector);
    }

    private bool HasSkillButtons()
    {
        return SkillGrid.GetChildCount() > 0;
    }

    public async Task ClearSkillContainer()
    {
        HideTransientTooltips();

        int cardCount = SkillGrid.GetChildCount();
        if (cardCount <= 0)
            return;

        var holdersToAnimate = new List<Control>();
        for (int i = 0; i < cardCount; i++)
        {
            if (SkillGrid.GetChild(i) is Control holder)
                holdersToAnimate.Add(holder);
        }

        ShuffleSkillAnimationOrder(holdersToAnimate);
        for (int i = 0; i < holdersToAnimate.Count; i++)
        {
            var holder = holdersToAnimate[i];
            float delay = SkillButtonExitStagger * i;
            var tween = holder.CreateTween();
            tween.SetParallel(true);
            tween.TweenProperty(holder, "modulate:a", 0.0f, 0.10f).SetDelay(delay);
            tween.TweenProperty(holder, "scale", new Vector2(0.96f, 0.96f), 0.12f)
                .SetDelay(delay);

            if (holder.GetChildCount() > 0 && holder.GetChild(0) is SkillCard card)
            {
                var cardTween = card.CreateTween();
                cardTween.TweenCallback(Callable.From(() => card.Vanish())).SetDelay(delay);
            }
        }

        if (holdersToAnimate.Count <= 0)
            return;

        await ToSignal(
            GetTree()
                .CreateTimer(
                    SkillCardExitSettleTime + SkillButtonExitStagger * (holdersToAnimate.Count - 1)
                ),
            "timeout"
        );

        ClearSkillGridChildren(SkillGrid);
    }

    public async void RefreshFromExternalChange()
    {
        var players = GameInfo.PlayerCharacters;
        if (players == null || players.Length == 0)
            return;

        _selectedCharacterIndex = Math.Clamp(_selectedCharacterIndex, 0, players.Length - 1);
        UpdateCharacterButtonState(false);
        await ClearSkillContainer();
        PopulateSkillButtons(_selectedCharacterIndex);
        RefreshTalentTree(_selectedCharacterIndex);
    }

    public void ComfirmTactics()
    {
        if (!AllowManualPlayerFormationAdjustment)
        {
            var previewDisabled = GetTree().Root.GetNodeOrNull<BattlePreview>("Map/SiteUI/BattlePreview");
            if (previewDisabled != null)
                previewDisabled.SetPortraitPostion();
            return;
        }

        var map = new System.Collections.Generic.Dictionary<int, int>()
        {
            [1] = 7,
            [2] = 4,
            [3] = 1,
            [4] = 8,
            [5] = 5,
            [6] = 2,
            [7] = 9,
            [8] = 6,
            [9] = 3,
        };

        for (int i = 0; i < Grid.GetChildCount(); i++)
        {
            var texture = Grid.GetChild<ColorRect>(i);
            if (texture.GetChildCount() <= 0)
                continue;

            int gridIndex = i + 1;
            if (!map.ContainsKey(gridIndex))
                continue;

            var portrait = texture.GetChild<PortaitFrame>(0);
            if (portrait != null)
                GameInfo.PlayerCharacters[portrait.PortaitIndex].PositionIndex = map[gridIndex];
            else
                GD.PrintErr($"Portrait or Charater is null at grid index {gridIndex}");
        }

        SaveSystem.SaveAll();
        var preview = GetTree().Root.GetNodeOrNull<BattlePreview>("Map/SiteUI/BattlePreview");
        if (preview != null)
            preview.SetPortraitPostion();
    }
}
