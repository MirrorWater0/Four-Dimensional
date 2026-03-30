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
        Formation,
    }

    public static PackedScene PortaitScene = GD.Load<PackedScene>(
        "res://battle/UIScene/BattleReady/PortaitFrame.tscn"
    );
    public Control Grid => field ??= ResolveNode<Control>("FormationModeRoot/GridContainer");
    private Control FormationModeRoot => field ??= ResolveNode<Control>("FormationModeRoot");
    private Control TacticsModeRoot => field ??= ResolveNode<Control>("TacticsModeRoot");
    private Control ModeSelectorRoot => field ??= ResolveNode<Control>("ModeSelectorRoot");
    private Control ModeSelectorThumb =>
        field ??= ResolveNode<Control>("ModeSelectorRoot/ModeThumb");
    private Control CharacterSelectorThumb =>
        field ??= ResolveNode<Control>("TacticsModeRoot/CharacterSelectRoot/CharacterSelectThumb");
    private Button TacticsModeButton =>
        field ??= ResolveNode<Button>(
            "ModeSelectorRoot/ModeButtonsMargin/ModeButtons/TacticsModeButton"
        );
    private Button FormationModeButton =>
        field ??= ResolveNode<Button>(
            "ModeSelectorRoot/ModeButtonsMargin/ModeButtons/FormationModeButton"
        );
    private Container SkillContainer =>
        field ??= ResolveNode<Container>("TacticsModeRoot/SkillContainer");
    private Control CharacterSelectRoot =>
        field ??= ResolveNode<Control>("TacticsModeRoot/CharacterSelectRoot");
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
    private Control TopAccent => field ??= GetNode<Control>("ColorRect");
    public ColorRect BG => field ??= GetNode<ColorRect>("BG");
    private Control CharacterSelectPanel =>
        field ??= ResolveNode<Control>("TacticsModeRoot/CharacterSelectRoot/CharacterSelectPanel");
    private Button[] CharacterButtons =>
        field ??= [
            ResolveNode<Button>(
                "TacticsModeRoot/CharacterSelectRoot/CharacterSelectPanel/CharacterSelectList/EchoButton"
            ),
            ResolveNode<Button>(
                "TacticsModeRoot/CharacterSelectRoot/CharacterSelectPanel/CharacterSelectList/KasiyaButton"
            ),
            ResolveNode<Button>(
                "TacticsModeRoot/CharacterSelectRoot/CharacterSelectPanel/CharacterSelectList/MariyaButton"
            ),
            ResolveNode<Button>(
                "TacticsModeRoot/CharacterSelectRoot/CharacterSelectPanel/CharacterSelectList/NightingaleButton"
            ),
        ];

    private static PackedScene SelectButtonScene = GD.Load<PackedScene>(
        "res://battle/UIScene/BattleReady/SelectButton.tscn"
    );
    private static readonly Theme SkillPagerButtonTheme = GD.Load<Theme>(
        "res://battle/ButtonTheme1.tres"
    );
    private const int SkillButtonsPerPage = 4;
    private const float SkillPagerButtonWidth = 96f;
    private const float SkillButtonHeight = 52f;
    private const float SkillButtonEnterStagger = 0.022f;
    private const float SkillButtonExitStagger = 0.018f;
    private const float SkillButtonExitSettleTime = 0.12f;

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

    private Container GetSkillFence(int index) => SkillContainer.GetChild<Container>(index);
    private readonly List<SkillID>[] _skillBuckets =
        new List<SkillID>[] { new(), new(), new() };
    private readonly int[] _skillPageIndices = new int[3];
    private readonly bool[] _skillPageTransitioning = new bool[3];

    private readonly struct SkillPage(int startIndex, int skillCount, bool hasPrev, bool hasNext)
    {
        public int StartIndex { get; } = startIndex;
        public int SkillCount { get; } = skillCount;
        public bool HasPrev { get; } = hasPrev;
        public bool HasNext { get; } = hasNext;
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

    private static List<SkillPage> BuildSkillPages(int totalCount)
    {
        var pages = new List<SkillPage>();
        if (totalCount <= 0)
            return pages;

        for (int startIndex = 0; startIndex < totalCount; startIndex += SkillButtonsPerPage)
        {
            int skillCount = Math.Min(SkillButtonsPerPage, totalCount - startIndex);
            bool hasPrev = startIndex > 0;
            bool hasNext = startIndex + skillCount < totalCount;
            pages.Add(new SkillPage(startIndex, skillCount, hasPrev, hasNext));
        }

        return pages;
    }

    private void CacheCharacterSkillBuckets(int characterIndex)
    {
        foreach (var bucket in _skillBuckets)
            bucket.Clear();

        var character = GameInfo.PlayerCharacters[characterIndex];
        foreach (var skillID in character.GainedSkills)
        {
            var skill = Skill.GetSkill(skillID);
            int skillIndex = GetSkillCategoryIndex(skill);
            if (skillIndex >= 0)
                _skillBuckets[skillIndex].Add(skillID);
        }
    }

    private int GetInitialSkillPage(int skillIndex)
    {
        var skills = _skillBuckets[skillIndex];
        var pages = BuildSkillPages(skills.Count);
        int pageCount = pages.Count;
        if (pageCount <= 1)
            return 0;

        var selectedSkill = GameInfo.PlayerCharacters[_selectedCharacterIndex].TakenSkills[skillIndex];
        for (int i = 0; i < skills.Count; i++)
        {
            if (EqualityComparer<SkillID>.Default.Equals(skills[i], selectedSkill))
            {
                for (int pageIndex = 0; pageIndex < pages.Count; pageIndex++)
                {
                    var page = pages[pageIndex];
                    if (i >= page.StartIndex && i < page.StartIndex + page.SkillCount)
                        return pageIndex;
                }
            }
        }

        return 0;
    }

    private IEnumerable<SelectButton> EnumerateSkillButtons(Container fence)
    {
        for (int i = 0; i < fence.GetChildCount(); i++)
        {
            if (fence.GetChild(i) is SelectButton button)
                yield return button;
        }
    }

    private void ClearFenceChildren(Container fence)
    {
        for (int i = fence.GetChildCount() - 1; i >= 0; i--)
        {
            var child = fence.GetChild(i);
            fence.RemoveChild(child);
            child.QueueFree();
        }
    }

    private Button CreateSkillPageButton(
        int characterIndex,
        int skillIndex,
        int pageDelta,
        string symbol,
        string tooltip
    )
    {
        var button = new Button
        {
            CustomMinimumSize = new Vector2(SkillPagerButtonWidth, SkillButtonHeight),
            Theme = SkillPagerButtonTheme,
            Text = symbol,
            TooltipText = tooltip,
            Flat = false,
        };
        button.AddThemeFontSizeOverride("font_size", 38);
        button.Pressed += () => AdvanceSkillPage(characterIndex, skillIndex, pageDelta);
        return button;
    }

    private static void AnimatePagerButtonIn(Control control, float delay)
    {
        control.PivotOffset = new Vector2(SkillPagerButtonWidth * 0.5f, SkillButtonHeight * 0.5f);
        control.Modulate = control.Modulate with { A = 0.0f };
        control.Scale = new Vector2(0.84f, 0.84f);
        var tween = control.CreateTween();
        tween.SetParallel(true);
        tween.TweenProperty(control, "modulate:a", 1.0f, 0.12f).SetDelay(delay);
        tween.TweenProperty(control, "scale", Vector2.One, 0.14f).SetDelay(delay);
    }

    private static void AnimatePagerButtonOut(Control control, float delay)
    {
        control.PivotOffset = new Vector2(SkillPagerButtonWidth * 0.5f, SkillButtonHeight * 0.5f);
        var tween = control.CreateTween();
        tween.SetParallel(true);
        tween.TweenProperty(control, "modulate:a", 0.0f, 0.10f).SetDelay(delay);
        tween.TweenProperty(control, "scale", new Vector2(0.84f, 0.84f), 0.12f).SetDelay(delay);
    }

    private async Task AnimateFenceExitAsync(Container fence)
    {
        int animatedCount = 0;
        for (int i = 0; i < fence.GetChildCount(); i++)
        {
            if (fence.GetChild(i) is SelectButton skillButton)
            {
                skillButton.FadeAnimation(SkillButtonExitStagger * animatedCount);
                animatedCount++;
            }
            else if (fence.GetChild(i) is Control pagerButton)
            {
                AnimatePagerButtonOut(pagerButton, SkillButtonExitStagger * animatedCount);
                animatedCount++;
            }
        }

        if (animatedCount > 0)
            await ToSignal(
                GetTree().CreateTimer(
                    SkillButtonExitSettleTime + SkillButtonExitStagger * (animatedCount - 1)
                ),
                "timeout"
            );

        ClearFenceChildren(fence);
    }

    private async void AdvanceSkillPage(int characterIndex, int skillIndex, int pageDelta)
    {
        if (_isTransitioning || _isModeTransitioning || _skillPageTransitioning[skillIndex])
            return;

        int pageCount = BuildSkillPages(_skillBuckets[skillIndex].Count).Count;
        if (pageCount <= 1)
            return;

        int targetPage = Math.Clamp(
            _skillPageIndices[skillIndex] + pageDelta,
            0,
            pageCount - 1
        );
        if (targetPage == _skillPageIndices[skillIndex])
            return;

        _skillPageTransitioning[skillIndex] = true;
        try
        {
            await AnimateFenceExitAsync(GetSkillFence(skillIndex));
            _skillPageIndices[skillIndex] = targetPage;
            RenderSkillFencePage(characterIndex, skillIndex, true);
        }
        finally
        {
            _skillPageTransitioning[skillIndex] = false;
        }
    }

    private SelectButton CreateSkillButton(int characterIndex, int skillIndex, SkillID skillID)
    {
        var selectbutton = SelectButtonScene.Instantiate<SelectButton>();
        selectbutton.MySkill = Skill.GetSkill(skillID);
        if (selectbutton.MySkill == null)
        {
            selectbutton.QueueFree();
            return null;
        }

        selectbutton.ThisLabel.Text = selectbutton.MySkill.SkillName;

        if (GameInfo.PlayerCharacters[characterIndex].TakenSkills.Contains(skillID))
        {
            selectbutton.Button.ButtonPressed = true;
            selectbutton.animation.Play("explode");
        }

        selectbutton.Button.Pressed += () =>
        {
            GameInfo.PlayerCharacters[characterIndex].TakenSkills[skillIndex] = skillID;
            selectbutton.Button.ButtonPressed = true;
            foreach (var button in EnumerateSkillButtons(GetSkillFence(skillIndex)))
            {
                if (button != selectbutton)
                    button.Button.ButtonPressed = false;
            }
        };

        return selectbutton;
    }

    private void RenderSkillFencePage(int characterIndex, int skillIndex, bool animate)
    {
        var fence = GetSkillFence(skillIndex);
        ClearFenceChildren(fence);

        var skills = _skillBuckets[skillIndex];
        int totalCount = skills.Count;
        if (totalCount <= 0)
        {
            fence.QueueSort();
            return;
        }

        var pages = BuildSkillPages(totalCount);
        int pageCount = pages.Count;
        int currentPage = Math.Clamp(_skillPageIndices[skillIndex], 0, Math.Max(pageCount - 1, 0));
        _skillPageIndices[skillIndex] = currentPage;
        var page = pages[currentPage];

        if (page.HasPrev)
        {
            var pager = CreateSkillPageButton(characterIndex, skillIndex, -1, "◀", "上一页");
            fence.AddChild(pager);
            if (animate)
                AnimatePagerButtonIn(pager, 0.00f);
        }

        for (int i = 0; i < page.SkillCount; i++)
        {
            var button = CreateSkillButton(
                characterIndex,
                skillIndex,
                skills[page.StartIndex + i]
            );
            if (button == null)
                continue;

            fence.AddChild(button);
            if (animate)
                button.StartAnimation(SkillButtonEnterStagger * (i + (page.HasPrev ? 1 : 0)));
        }

        if (page.HasNext)
        {
            var pager = CreateSkillPageButton(characterIndex, skillIndex, 1, "▶", "下一页");
            fence.AddChild(pager);
            if (animate)
                AnimatePagerButtonIn(
                    pager,
                    SkillButtonEnterStagger * (page.SkillCount + (page.HasPrev ? 1 : 0))
                );
        }

        fence.QueueSort();
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
                new AssemblyItem(SkillContainer, new Vector2(88f, 14f), 0.26f),
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
        SetModeSelectorEnabled(false);
        HideTransientTooltips();
        _dragTarget = null;
        _dragMouseOffset = Vector2.Zero;

        try
        {
            UpdateModeButtonState(targetMode);
            UpdateModeSelectorPosition(targetMode, true);

            await AnimateModeExitAsync(_currentMode);
            SetModeVisible(_currentMode, false);

            _currentMode = targetMode;
            SetModeVisible(_currentMode, true);
            await AnimateModeEnterAsync(_currentMode);
        }
        finally
        {
            _isModeTransitioning = false;
            SetModeSelectorEnabled(true);
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
        SetModeVisible(BattleReadyMode.Formation, mode == BattleReadyMode.Formation);
        ResetModeItemsToBase(
            BattleReadyMode.Tactics,
            mode == BattleReadyMode.Tactics ? 1.0f : 0.0f
        );
        ResetModeItemsToBase(
            BattleReadyMode.Formation,
            mode == BattleReadyMode.Formation ? 1.0f : 0.0f
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
            _ =>
            [
                new AssemblyItem(CharacterSelectRoot, new Vector2(-88f, 24f), 0.00f),
                new AssemblyItem(SkillAreaFrame, new Vector2(96f, 18f), 0.00f),
                new AssemblyItem(SkillAreaHeaderFrame, new Vector2(78f, 0f), 0.06f),
                new AssemblyItem(SkillAreaHeader, new Vector2(78f, 0f), 0.10f),
                new AssemblyItem(SkillTypeFrame, new Vector2(66f, 0f), 0.12f),
                new AssemblyItem(SkillTypeIcons, new Vector2(54f, -18f), 0.16f),
                new AssemblyItem(SkillDividers, new Vector2(60f, 24f), 0.20f),
                new AssemblyItem(SkillContainer, new Vector2(88f, 14f), 0.24f),
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
        var root = mode == BattleReadyMode.Formation ? FormationModeRoot : TacticsModeRoot;
        root.Visible = visible;
        root.MouseFilter = visible ? MouseFilterEnum.Stop : MouseFilterEnum.Ignore;
    }

    private void SetModeSelectorEnabled(bool enabled)
    {
        TacticsModeButton.Disabled = !enabled;
        FormationModeButton.Disabled = !enabled;
    }

    private void UpdateModeButtonState(BattleReadyMode mode)
    {
        SetModeButtonState(TacticsModeButton, mode == BattleReadyMode.Tactics);
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
        var button = mode == BattleReadyMode.Formation ? FormationModeButton : TacticsModeButton;
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
            tip.Visible = false;

        var buffTip = GetTree().Root.GetNodeOrNull<Tip>("TipLayer/BuffTip");
        if (buffTip != null)
            buffTip.Visible = false;
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
                portrait.PortaitRect.Texture = GD.Load<Texture2D>(players[i].PortaitPath);

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
                _dragTarget = portrait;
                _dragMouseOffset = GetViewport().GetMousePosition() - portrait.GlobalPosition;
                portrait.ZIndex = 1;
            };
            portrait.PortaitButton.ButtonUp += () =>
            {
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
        if (_currentMode != BattleReadyMode.Tactics || _isModeTransitioning)
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
            return;
        }

        _selectedCharacterIndex = characterIndex;
        UpdateCharacterButtonState(true);
        await ClearSkillContainer();
        PopulateSkillButtons(characterIndex);
    }

    private void PopulateSkillButtons(int characterIndex)
    {
        CacheCharacterSkillBuckets(characterIndex);
        for (int skillIndex = 0; skillIndex < _skillBuckets.Length; skillIndex++)
        {
            _skillPageIndices[skillIndex] = GetInitialSkillPage(skillIndex);
            RenderSkillFencePage(characterIndex, skillIndex, true);
        }
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
        for (int i = 0; i < SkillContainer.GetChildCount(); i++)
        {
            if (EnumerateSkillButtons(GetSkillFence(i)).Any())
                return true;
        }

        return false;
    }

    public async Task ClearSkillContainer()
    {
        HideTransientTooltips();

        bool hasChildren = false;
        int buttonsCount = 0;
        int cumulativeIndex = 0;
        for (int i = 0; i < SkillContainer.GetChildCount(); i++)
        {
            var fence = GetSkillFence(i);
            hasChildren |= fence.GetChildCount() > 0;
            for (int j = 0; j < fence.GetChildCount(); j++)
            {
                if (fence.GetChild(j) is SelectButton button)
                    button.FadeAnimation(SkillButtonExitStagger * cumulativeIndex);
                else if (fence.GetChild(j) is Control pagerButton)
                    AnimatePagerButtonOut(pagerButton, SkillButtonExitStagger * cumulativeIndex);

                buttonsCount++;
                cumulativeIndex++;
            }
        }

        if (!hasChildren)
            return;

        if (buttonsCount > 0)
            await ToSignal(
                GetTree().CreateTimer(
                    SkillButtonExitSettleTime + SkillButtonExitStagger * (buttonsCount - 1)
                ),
                "timeout"
            );

        for (int i = 0; i < SkillContainer.GetChildCount(); i++)
            ClearFenceChildren(GetSkillFence(i));
    }

    public void ComfirmTactics()
    {
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
