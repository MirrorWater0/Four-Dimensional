using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;

public partial class BattleReady : Control
{
    private enum BattleReadyMode
    {
        Tactics,
        Talent,
    }

    public static PackedScene PortaitScene = GD.Load<PackedScene>(
        "res://battle/UIScene/BattleReady/PortaitFrame.tscn"
    );
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
    private ScrollContainer SkillContainer =>
        field ??= ResolveNode<ScrollContainer>("TacticsModeRoot/SkillContainer");
    private GridContainer SkillGrid =>
        field ??= ResolveNode<GridContainer>(
            "TacticsModeRoot/SkillContainer/SkillScrollMargin/SkillGrid"
        );
    private Control CharacterSelectRoot => field ??= ResolveNode<Control>("CharacterSelectRoot");
    private Control SkillAreaHeaderFrame =>
        field ??= ResolveNode<Control>("TacticsModeRoot/SkillAreaHeaderFrame");
    private Control SkillAreaHeader =>
        field ??= ResolveNode<Control>("TacticsModeRoot/SkillAreaHeaderFrame/SkillAreaHeader");
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
    private static readonly Vector2 BattleReadySkillCardScale = new(0.8f, 0.8f);
    private static readonly Vector2 SkillCardHoverPadding = Vector2.Zero;
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

    private readonly List<SkillDisplayEntry> _skillDisplayEntries = new();
    private readonly Random _skillAnimationRandom = new();
    private int _skillPreviewCharacterIndex = -1;
    private bool _skillPreviewResourcePrewarmStarted;

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
            Skill.SkillTypes.Permanent => 1,
            Skill.SkillTypes.Special => 2,
            Skill.SkillTypes.Ability => 3,
            _ => -1,
        };
    }

    private void CacheCharacterSkillDisplayEntries(int characterIndex)
    {
        _skillDisplayEntries.Clear();

        var character = GameInfo.PlayerCharacters[characterIndex];
        var groupedSkills = (character.GainedSkills ?? new List<SkillID>())
            .GroupBy(skillId => skillId)
            .Select(group => new SkillDisplayEntry(group.Key, group.Count()));

        foreach (var entry in groupedSkills)
        {
            var skill = Skill.GetSkill(entry.SkillId);
            int skillIndex = GetSkillCategoryIndex(skill);
            if (skillIndex >= 0)
                _skillDisplayEntries.Add(entry);
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

    private SkillCard CreateSkillCard(
        SkillDisplayEntry entry,
        PlayerInfoStructure character,
        int characterIndex
    )
    {
        var skill = Skill.GetSkill(entry.SkillId);
        if (skill == null)
            return null;

        skill.SetPreviewStats(
            TalentTree.GetEffectivePower(character),
            TalentTree.GetEffectiveSurvivability(character),
            1,
            playerIndex: characterIndex
        );

        var card = SkillCardScene.Instantiate<SkillCard>();
        string displayName = GetSkillDisplayName(skill, entry.Count);
        card.Name = $"SkillCard_{entry.SkillId}";
        card.ConfigureDisplayScale(BattleReadySkillCardScale);
        card.AutoPressEffect = false;
        card.Button.ToggleMode = false;
        card.Button.ButtonPressed = false;
        card.Button.FocusMode = Control.FocusModeEnum.None;
        card.PreviewCharacterName = character.CharacterName;
        card.PreviewCharacterKey = ExtractCharacterKeyFromScenePath(character.CharacterScenePath);
        card.DisplayNameOverride = displayName;
        card.CharacterName.Text = character.CharacterName ?? string.Empty;
        card.SetSkill(skill);
        card.CharacterName.Text = character.CharacterName ?? string.Empty;
        card.NameLabel.Text = displayName;
        return card;
    }

    private void RefreshTalentTree(int characterIndex) {
        ClearTalentTree();
        var players = GameInfo.PlayerCharacters;
        if (players == null || characterIndex < 0 || characterIndex >= players.Length)
            return;
        var info = players[characterIndex];
        var choices = TalentTree.GetChoices(ref info);
        players[characterIndex] = info;
        if (TalentPointLabel != null)
            TalentPointLabel.Text = I18n.Format("ui.common.remaining_talent_points", "待选天赋 {value}", ("value", info.TalentPoints));
        TalentChoiceView.Populate(TalentTreeRoot, info, choices, id => OnTalentNodePressed(characterIndex, id));
    }

    private async Task RefreshTalentTreeAnimatedAsync(int characterIndex)
    {
        if (
            _currentMode != BattleReadyMode.Talent
            || TalentTreeRoot == null
            || !GodotObject.IsInstanceValid(TalentTreeRoot)
            || !IsInsideTree()
        )
        {
            RefreshTalentTree(characterIndex);
            return;
        }

        _talentTreeSwitchTween?.Kill();

        var root = TalentTreeRoot;
        Vector2 basePosition = root.Position;
        Color baseModulate = root.Modulate;

        _talentTreeSwitchTween = CreateTween();
        var exitTween = _talentTreeSwitchTween;
        exitTween.SetParallel(true);
        exitTween.SetEase(Tween.EaseType.In);
        exitTween.SetTrans(Tween.TransitionType.Cubic);
        exitTween.TweenProperty(root, "position", basePosition + new Vector2(-20f, 0f), 0.06f);
        exitTween.TweenProperty(root, "modulate:a", 0.0f, 0.055f);
        await ToSignal(exitTween, Tween.SignalName.Finished);

        if (!GodotObject.IsInstanceValid(root) || !IsInsideTree())
            return;

        RefreshTalentTree(characterIndex);
        root.Position = basePosition + new Vector2(24f, 0f);
        root.Modulate = baseModulate with { A = 0.0f };

        _talentTreeSwitchTween = CreateTween();
        var enterTween = _talentTreeSwitchTween;
        enterTween.SetParallel(true);
        enterTween.SetEase(Tween.EaseType.Out);
        enterTween.SetTrans(Tween.TransitionType.Cubic);
        enterTween.TweenProperty(root, "position", basePosition, 0.11f);
        enterTween.TweenProperty(root, "modulate:a", baseModulate.A, 0.1f);
        await ToSignal(enterTween, Tween.SignalName.Finished);

        if (_talentTreeSwitchTween == enterTween)
            _talentTreeSwitchTween = null;
    }

    private void ClearTalentTree()
    {
        if (TalentTreeRoot == null || !GodotObject.IsInstanceValid(TalentTreeRoot))
            return;
        for (int i = TalentTreeRoot.GetChildCount() - 1; i >= 0; i--)
        {
            var child = TalentTreeRoot.GetChild(i);
            TalentTreeRoot.RemoveChild(child);
            child.QueueFree();
        }
    }

    private void HideTalentTooltip() => EnsureGlobalTooltip()?.HideTooltip();

    private void OnTalentNodePressed(int characterIndex, string talentId) {
        var players = GameInfo.PlayerCharacters;
        if (players == null || characterIndex < 0 || characterIndex >= players.Length)
            return;
        var info = players[characterIndex];
        if (TalentTree.TryUnlock(ref info, talentId, out string message)) {
            players[characterIndex] = info;
            _skillPreviewCharacterIndex = -1;
            RefreshChrome();
        }
        GD.Print(message);
        RefreshTalentTree(characterIndex);
    }

    private int _selectedCharacterIndex;
    private bool _isTransitioning;
    private bool _isModeTransitioning;
    private bool _modeSelectorPositioned;
    private bool _characterSelectorPositioned;
    private BattleReadyMode _currentMode = BattleReadyMode.Tactics;
    private Tween _modeSelectorTween;
    private Tween _characterSelectorTween;
    private Tween _talentTreeSwitchTween;
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
        BuildChrome();
        Initialize();
        ConfigureSkillSmoothScroll();
        CacheAssemblyBasePositions();
        WireModeSelector();
        ApplyModeStateImmediate(_currentMode);
        ModeSelectorRoot.Resized += RefreshModeSelectorLayout;
        TacticsModeButton.Resized += RefreshModeSelectorLayout;
        TalentModeButton.Resized += RefreshModeSelectorLayout;
        CharacterSelectRoot.Resized += RefreshCharacterSelectorLayout;
        foreach (var button in CharacterButtons)
            button.Resized += RefreshCharacterSelectorLayout;
        CallDeferred(nameof(RefreshModeSelectorLayout));
        CallDeferred(nameof(RefreshCharacterSelectorLayout));
        CallDeferred(nameof(StartSkillPreviewResourcePrewarm));
    }

    public override void _Process(double delta)
    {
        UpdateSkillSmoothScroll((float)delta);
    }

    public override void _ExitTree()
    {
        if (
            _skillSmoothScrollInputTarget != null
            && GodotObject.IsInstanceValid(_skillSmoothScrollInputTarget)
        )
        {
            _skillSmoothScrollInputTarget.GuiInput -= OnSkillScrollGuiInput;
        }

        if (_skillSmoothScrollBar != null && GodotObject.IsInstanceValid(_skillSmoothScrollBar))
            _skillSmoothScrollBar.GuiInput -= OnSkillScrollBarGuiInput;
    }

    private async void StartSkillPreviewResourcePrewarm()
    {
        if (_skillPreviewResourcePrewarmStarted)
            return;

        _skillPreviewResourcePrewarmStarted = true;
        await PrewarmSkillPreviewResourcesAsync();
    }

    private async Task PrewarmSkillPreviewResourcesAsync()
    {
        var players = GameInfo.PlayerCharacters;
        if (players == null)
            return;

        int startIndex = players.Length == 0 ? 0 : Math.Max(_selectedCharacterIndex + 1, 0) % players.Length;
        for (int offset = 0; offset < players.Length; offset++)
        {
            int i = (startIndex + offset) % players.Length;
            PlayerInfoStructure character = players[i];
            if (character.GainedSkills == null)
                continue;

            string characterKey = ExtractCharacterKeyFromScenePath(character.CharacterScenePath);
            foreach (SkillID skillId in character.GainedSkills.Distinct())
            {
                Skill skill = Skill.GetSkill(skillId);
                if (skill == null || GetSkillCategoryIndex(skill) < 0)
                    continue;

                skill.SetPreviewStats(
                    TalentTree.GetEffectivePower(character),
                    TalentTree.GetEffectiveSurvivability(character),
                    1,
                    playerIndex: i
                );
                SkillCard.PrewarmSkillResources(skill, character.CharacterName, characterKey);
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                if (!IsInsideTree())
                    return;
            }
        }
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
            new AssemblyItem(TalentTreeRoot, new Vector2(66f, 12f), 0.22f),
            new AssemblyItem(_deploymentBrief, new Vector2(56f, 12f), 0.20f),
            new AssemblyItem(TopAccent, new Vector2(0f, -40f), 0.2f),
        ];
    }

    private AssemblyItem[] GetAssemblyItemsForMode(BattleReadyMode mode)
    {
        return mode switch
        {
            BattleReadyMode.Talent =>
            [
                new AssemblyItem(ModeSelectorRoot, new Vector2(0f, -26f), 0.00f),
                new AssemblyItem(CharacterSelectRoot, new Vector2(-88f, 24f), 0.08f),
                new AssemblyItem(TalentTreeHeaderFrame, new Vector2(58f, 0f), 0.14f),
                new AssemblyItem(TalentPointFrame, new Vector2(50f, -4f), 0.16f),
                new AssemblyItem(TalentTreeRoot, new Vector2(66f, 12f), 0.18f),
                new AssemblyItem(_deploymentBrief, new Vector2(56f, 12f), 0.20f),
                new AssemblyItem(TopAccent, new Vector2(0f, -40f), 0.18f),
            ],
            _ =>
            [
                new AssemblyItem(ModeSelectorRoot, new Vector2(0f, -26f), 0.00f),
                new AssemblyItem(CharacterSelectRoot, new Vector2(-88f, 24f), 0.08f),
                new AssemblyItem(SkillAreaHeaderFrame, new Vector2(78f, 0f), 0.12f),
                new AssemblyItem(SkillAreaHeader, new Vector2(78f, 0f), 0.14f),
                new AssemblyItem(_deploymentBrief, new Vector2(56f, 12f), 0.20f),
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

        try
        {
            _modeSelectorTween?.Kill();
            _characterSelectorTween?.Kill();
            _talentTreeSwitchTween?.Kill();
            _talentTreeSwitchTween = null;

            UpdateModeButtonState(targetMode);
            UpdateModeSelectorPosition(targetMode, true);

            await AnimateModeExitAsync(_currentMode);
            SetModeVisible(_currentMode, false);

            _currentMode = targetMode;
            SetModeVisible(BattleReadyMode.Tactics, targetMode == BattleReadyMode.Tactics);
            SetModeVisible(BattleReadyMode.Talent, targetMode == BattleReadyMode.Talent);

            if (targetMode == BattleReadyMode.Tactics)
                await RefreshSelectedSkillPreviewAsync();
            else if (targetMode == BattleReadyMode.Talent)
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
        ResetModeItemsToBase(
            BattleReadyMode.Tactics,
            mode == BattleReadyMode.Tactics ? 1.0f : 0.0f
        );
        ResetModeItemsToBase(BattleReadyMode.Talent, mode == BattleReadyMode.Talent ? 1.0f : 0.0f);
        SetControlAlpha(CharacterSelectRoot, 1.0f);
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
        // The per-mode section chips were folded into the console header band, so only the
        // selector and the mode's content block take part in the assemble/disassemble motion.
        return mode switch
        {
            BattleReadyMode.Talent =>
            [
                new AssemblyItem(CharacterSelectRoot, new Vector2(-88f, 24f), 0.00f),
                new AssemblyItem(TalentTreeRoot, new Vector2(66f, 12f), 0.14f),
            ],
            _ =>
            [
                new AssemblyItem(CharacterSelectRoot, new Vector2(-88f, 24f), 0.00f),
            ],
        };
    }

    private static Vector2 GetModeEnterOffset(BattleReadyMode mode, AssemblyItem item)
    {
        return item.Offset * 0.45f + new Vector2(-70f, 10f);
    }

    private static Vector2 GetModeExitOffset(BattleReadyMode mode, AssemblyItem item)
    {
        return item.Offset * 0.28f + new Vector2(-34f, 8f);
    }

    private void SetModeVisible(BattleReadyMode mode, bool visible)
    {
        var root = mode switch
        {
            BattleReadyMode.Talent => TalentModeRoot,
            _ => TacticsModeRoot,
        };
        root.Visible = visible;
        root.MouseFilter = visible ? MouseFilterEnum.Stop : MouseFilterEnum.Ignore;
        if (visible)
        {
            foreach (var item in GetModeItems(mode))
                item.Control.Visible = true;
        }

        CharacterSelectRoot.Visible = true;
        CharacterSelectRoot.MouseFilter = MouseFilterEnum.Stop;
    }

    private void SetModeSelectorEnabled(bool enabled)
    {
        TacticsModeButton.Disabled = !enabled;
        TalentModeButton.Disabled = !enabled;
    }

    private void UpdateModeButtonState(BattleReadyMode mode)
    {
        SetModeButtonState(TacticsModeButton, mode == BattleReadyMode.Tactics);
        SetModeButtonState(TalentModeButton, mode == BattleReadyMode.Talent);
        RefreshChrome(animateBrief: false);
    }

    private static void SetModeButtonState(Button button, bool active)
    {
        if (button == null)
            return;

        button.SetPressedNoSignal(active);
        button.Modulate = Colors.White;

        // The selector thumb slides a solid gold plate behind the active entry, so the active
        // label has to flip to dark ink while the rest stay dim steel.
        Color font = active ? BattleReadyStyle.InkOnGold : BattleReadyStyle.Steel with { A = 0.72f };
        Color hover = active ? BattleReadyStyle.InkOnGold : BattleReadyStyle.GoldBright;
        button.AddThemeColorOverride("font_color", font);
        button.AddThemeColorOverride("font_pressed_color", font);
        button.AddThemeColorOverride("font_focus_color", font);
        button.AddThemeColorOverride("font_hover_color", hover);
        button.AddThemeColorOverride("font_disabled_color", BattleReadyStyle.Steel with { A = 0.3f });
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

    public void Initialize()
    {
        GameInfo.NormalizePlayerCharacters();
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
                ? I18n.Format("ui.common.character_n", "角色 {index}", ("index", i + 1))
                : info.CharacterName;

            int capturedIndex = i;
            button.Pressed += () => OnCharacterButtonPressed(capturedIndex);
        }

        UpdateCharacterButtonState(false);
    }

    private async void OnCharacterButtonPressed(int characterIndex)
    {
        if (_isModeTransitioning)
            return;

        if (_currentMode is BattleReadyMode.Tactics or BattleReadyMode.Talent)
            await SelectCharacter(characterIndex);
    }

    private async Task SelectCharacter(int characterIndex)
    {
        if (_isModeTransitioning)
            return;

        var players = GameInfo.PlayerCharacters;
        if (players == null || characterIndex < 0 || characterIndex >= players.Length)
            return;

        bool sameCharacter = characterIndex == _selectedCharacterIndex;
        if (sameCharacter && _currentMode == BattleReadyMode.Talent)
        {
            UpdateCharacterButtonState(true);
            RefreshTalentTree(characterIndex);
            return;
        }

        _selectedCharacterIndex = characterIndex;
        UpdateCharacterButtonState(true);

        if (_currentMode == BattleReadyMode.Talent)
        {
            await RefreshTalentTreeAnimatedAsync(characterIndex);
            return;
        }

        if (_currentMode == BattleReadyMode.Tactics)
        {
            if (sameCharacter && _skillPreviewCharacterIndex == characterIndex)
                return;

            await ClearSkillContainer();
            PopulateSkillButtons(characterIndex);
            return;
        }

        RefreshTalentTree(characterIndex);
    }

    private void PopulateSkillButtons(int characterIndex)
    {
        _skillPreviewCharacterIndex = characterIndex;
        CacheCharacterSkillDisplayEntries(characterIndex);
        ResetSkillSmoothScroll();

        var character = GameInfo.PlayerCharacters[characterIndex];
        var cardsToAnimate = new List<SkillCard>();
        foreach (var entry in _skillDisplayEntries)
        {
            var card = CreateSkillCard(entry, character, characterIndex);
            if (card == null)
                continue;

            var holder = CreateSkillCardHolder(card);
            SkillGrid.AddChild(holder);
            cardsToAnimate.Add(card);
        }

        SkillGrid.QueueSort();

        ShuffleSkillAnimationOrder(cardsToAnimate);
        for (int i = 0; i < cardsToAnimate.Count; i++)
            cardsToAnimate[i]
                .CallDeferred(nameof(SkillCard.StartAnimation), SkillCardEnterStagger * i);

        CallDeferred(nameof(ResetSkillSmoothScroll));
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
        RefreshChrome(animateBrief: animateSelector);
    }

    private bool HasSkillButtons()
    {
        return SkillGrid.GetChildCount() > 0;
    }

    private async Task RefreshSelectedSkillPreviewAsync()
    {
        var players = GameInfo.PlayerCharacters;
        if (
            players == null
            || _selectedCharacterIndex < 0
            || _selectedCharacterIndex >= players.Length
        )
            return;

        if (_skillPreviewCharacterIndex == _selectedCharacterIndex)
            return;

        await ClearSkillContainer();
        PopulateSkillButtons(_selectedCharacterIndex);
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
            tween.TweenProperty(holder, "scale", new Vector2(0.96f, 0.96f), 0.12f).SetDelay(delay);

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
        var preview = GetTree().Root.GetNodeOrNull<BattlePreview>("Map/SiteUI/BattlePreview");
        if (preview != null)
            preview.SetPortraitPostion();
    }

    private static string ExtractCharacterKeyFromScenePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return null;

        var parts = path.Split('/');
        return parts.Length >= 2 ? parts[^2] : null;
    }
}
