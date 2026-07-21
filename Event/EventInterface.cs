using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Godot;

public partial class EventInterface : Control
{
    private const int EventResourceRandomSalt = unchecked((int)0x75a0f19b);
    private const float OptionPressPulseDuration = 0.11f;
    private const float OutcomeOverlayFadeDuration = 0.16f;
    private const float OutcomeBannerEnterDuration = 0.22f;
    private const float OutcomeOverlayDismissDuration = 0.12f;

    [Export(PropertyHint.Range, "0.2,4.0,0.05,or_greater")]
    public float IntroAnimationSpeed = 1.0f;

    private Button EnterButton => field ??= GetNode<Button>("Frame/EnterButton");
    private Button ExitButton => field ??= GetNode<Button>("Frame/ExitButton");
    private Control OptionsTitle => field ??= GetNode<Control>("Frame/OptionsTitle");
    private Control OptionsContainer => field ??= GetNode<Control>("Frame/Options");
    private Label TitleLabel => field ??= GetNode<Label>("Frame/Title");
    private RichTextLabel StoryText =>
        field ??= GetNode<RichTextLabel>("Frame/ContentGroup/StoryText");
    private Label SubtitleLabel => field ??= GetNode<Label>("Frame/Subtitle");
    private ColorRect Background => field ??= GetNode<ColorRect>("BG");
    private Control HeaderBar => field ??= GetNode<Control>("Frame/HeaderBar");
    private Control HeaderTitle => field ??= GetNode<Control>("Frame/HeaderBar/HeaderTitle");
    private Control HeaderCode => field ??= GetNode<Control>("Frame/HeaderBar/HeaderCode");
    private Control HeaderAccent => field ??= GetNode<Control>("Frame/HeaderBar/HeaderAccent");
    private Control FrameNode => field ??= GetNode<Control>("Frame");
    private Control LeftAccent => field ??= GetNode<Control>("Frame/LeftAccent");
    private Control RightAccent => field ??= GetNode<Control>("Frame/RightAccent");
    private Control Divider => field ??= GetNode<Control>("Frame/Divider");
    private Control ContentRow => field ??= GetNode<Control>("Frame/ContentGroup");
    private Tip OptionTooltip => field ??= GetTree().Root.GetNodeOrNull<Tip>("TipLayer/EventTip");
    private TargetSelectOverlay TargetSelectOverlay =>
        field ??= GetNode<TargetSelectOverlay>("Frame/TargetSelectOverlay");
    private Control OutcomeOverlay => field ??= GetNodeOrNull<Control>("Frame/OutcomeOverlay");
    private Control OutcomeBanner => field ??= GetNodeOrNull<Control>("Frame/OutcomeOverlay/Banner");
    private RichTextLabel OutcomeText =>
        field ??= GetNodeOrNull<RichTextLabel>("Frame/OutcomeOverlay/Banner/OutcomeText");

    private IReadOnlyList<Button> OptionButtons =>
        field ??= OptionsContainer.GetChildren().OfType<Button>().ToArray();

    public GameEvent ThisEvent;
    public LevelNode WhichNode;

    private sealed class PartState
    {
        public Control Node;
        public Vector2 BasePosition;
        public Vector2 BaseGlobalPosition;
        public float BaseAlpha;
        public bool BaseTopLevel;
    }

    private readonly struct AssemblyItem(
        Control control,
        Vector2 offset,
        float delay,
        float moveDuration,
        float fadeDuration
    )
    {
        public Control Control { get; } = control;
        public Vector2 Offset { get; } = offset;
        public float Delay { get; } = delay;
        public float MoveDuration { get; } = moveDuration;
        public float FadeDuration { get; } = fadeDuration;
    }

    private readonly Dictionary<Control, PartState> _partStates = [];
    private bool _isTransitioning;
    private bool _assembled;
    private bool _isShowingOutcomeOverlay;
    private string[] _optionTipTexts = Array.Empty<string>();
    private EventOption _pendingTargetOption;
    private EventOption _pendingRestSingleHealOption;
    private EventOption _pendingCardOption;
    private TaskCompletionSource<bool> _outcomeDismissSource;
    private Vector2 _outcomeBannerBasePosition;
    private bool _outcomeBannerBasePositionCached;
    private readonly Dictionary<EventOption, int> _electricityRolls = new();
    private readonly Dictionary<EventOption, int> _partyHealPercentRolls = new();
    private readonly Dictionary<EventOption, int> _propertyChangeCostRolls = new();
    private readonly Dictionary<EventOption, RelicID?> _relicRewardRolls = new();
    private EventCardSelectOverlay _cardSelectOverlay;
    private Random _resourceRandom;
    private string _pendingRestTalentCharacterName;
    private List<string> _pendingStarterBonusTalentCharacterNames;
    private const bool SkipEnterPrompt = true;

    private readonly struct AppliedResourceChanges(int partyHealPercent, int electricityChange, int propertyChangeElectricityCost = 0)
    {
        public int PartyHealPercent { get; } = partyHealPercent;
        public int ElectricityChange { get; } = electricityChange;
        public int PropertyChangeElectricityCost { get; } = propertyChangeElectricityCost;
        public bool HasAny => PartyHealPercent != 0 || ElectricityChange != 0 || PropertyChangeElectricityCost != 0;
    }

    private float IntroSpeed => MathF.Max(0.05f, IntroAnimationSpeed);
    private float IntroDuration(float baseSeconds) => baseSeconds / IntroSpeed;

    public override void _Ready()
    {
        EnsureTipLayer();
        OptionsTitle.Visible = false;
        OptionsContainer.Visible = false;
        SetControlAlpha(OptionsTitle, 0.0f);
        SetControlAlpha(OptionsContainer, 0.0f);
        TargetSelectOverlay.Visible = false;
        if (OutcomeOverlay != null)
        {
            OutcomeOverlay.Visible = false;
            SetControlAlpha(OutcomeOverlay, 0.0f);
            OutcomeOverlay.GuiInput += OnOutcomeOverlayGuiInput;
        }
        EnterButton.Pressed += OnEnterPressed;
        ExitButton.Pressed += OnExitPressed;
        if (SkipEnterPrompt)
        {
            EnterButton.Visible = false;
            ExitButton.Visible = false;
        }
        BindOptionButtons();
        TargetSelectOverlay.CharacterSelected += OnTargetCharacterSelected;
        TargetSelectOverlay.SelectionCanceled += OnTargetSelectionCanceled;
        EnsureCardSelectOverlay();
        _resourceRandom = CreateResourceRandom();
        ApplyEventData(ThisEvent ?? GameEvent.Catalog?.FirstOrDefault());
        CallDeferred(nameof(StartAnimation));
    }

    public override void _ExitTree()
    {
        TargetSelectOverlay.CharacterSelected -= OnTargetCharacterSelected;
        TargetSelectOverlay.SelectionCanceled -= OnTargetSelectionCanceled;
        if (_cardSelectOverlay != null)
        {
            _cardSelectOverlay.CardSelected -= OnCardSelected;
            _cardSelectOverlay.SelectionCanceled -= OnCardSelectionCanceled;
        }
        if (OutcomeOverlay != null)
            OutcomeOverlay.GuiInput -= OnOutcomeOverlayGuiInput;
        _outcomeDismissSource?.TrySetResult(false);
        _outcomeDismissSource = null;
    }

    public async void StartAnimation()
    {
        if (_assembled)
            return;
        _assembled = true;
        await PlayAssembleAnimationAsync();
        if (SkipEnterPrompt)
            await EnterOptionsAsync();
    }

    public void ApplyEventData(GameEvent gameEvent)
    {
        ThisEvent = gameEvent;
        if (ThisEvent == null)
            return;

        if (TitleLabel != null && !string.IsNullOrWhiteSpace(ThisEvent.EventName))
        {
            TitleLabel.Text = ThisEvent.IsRestSite
                || ThisEvent.IsTreasureChest
                || ThisEvent.IsStarterBonus
                || ThisEvent.IsBossRelicChoice
                ? ThisEvent.EventName
                : $"事件：{ThisEvent.EventName}";
        }
        if (StoryText != null && !string.IsNullOrWhiteSpace(ThisEvent.Text))
            StoryText.Text = ThisEvent.Text;
        if (SubtitleLabel != null)
        {
            SubtitleLabel.Text = ThisEvent.IsRestSite
                ? I18n.Tr("ui.rest.prompt", "要做点什么？")
                : ThisEvent.IsTreasureChest
                    ? I18n.Tr("ui.treasure.prompt", "领取遗物后继续冒险。")
                : ThisEvent.IsStarterBonus
                    ? I18n.Tr("ui.starter_bonus.subtitle", "本次冒险只能保留一项")
                : ThisEvent.IsBossRelicChoice
                    ? I18n.Tr("ui.boss_relic.subtitle", "区域二即将展开")
                    : string.IsNullOrWhiteSpace(SubtitleLabel.Text)
                        ? "——"
                        : SubtitleLabel.Text;
        }

        var options = ThisEvent.Options ?? Array.Empty<EventOption>();
        RollOptionResourceRewards(options);
        _optionTipTexts = new string[options.Length];
        for (int i = 0; i < OptionButtons.Count; i++)
        {
            var button = OptionButtons[i];
            bool exists = i < options.Length;
            button.Visible = exists;
            bool canUse = exists && CanUseOption(options[i]);
            button.Disabled = !exists || !canUse;
            if (exists)
            {
                button.Text = string.IsNullOrWhiteSpace(options[i].Text)
                    ? $"选项 {i + 1}"
                    : options[i].Text;
                _optionTipTexts[i] = BuildOptionTipText(
                    options[i],
                    GetRolledElectricityChange(options[i]),
                    GetRolledPartyHealPercent(options[i]),
                    GetRolledPropertyChangeCost(options[i]),
                    GetRolledRelicReward(options[i]),
                    WhichNode
                );
            }
        }
    }

    private void BindOptionButtons()
    {
        for (int i = 0; i < OptionButtons.Count; i++)
        {
            int capturedIndex = i;
            OptionButtons[i].Pressed += () => OnOptionPressed(capturedIndex);
            OptionButtons[i].MouseEntered += () => ShowOptionTip(capturedIndex);
            OptionButtons[i].MouseExited += HideOptionTip;
        }
    }

    private async void OnEnterPressed()
    {
        if (_isTransitioning)
            return;

        await EnterOptionsAsync();
    }

    private async Task EnterOptionsAsync()
    {
        if (_isTransitioning)
            return;

        _isTransitioning = true;
        EnterButton.Disabled = true;
        ExitButton.Disabled = true;

        var enterPart = CapturePart(EnterButton);
        var exitPart = CapturePart(ExitButton);
        var titlePart = CapturePart(OptionsTitle);
        var optionsPart = CapturePart(OptionsContainer);

        OptionsTitle.Visible = true;
        OptionsContainer.Visible = true;
        SetPartTopLevel(enterPart, true);
        SetPartTopLevel(exitPart, true);
        SetPartTopLevel(titlePart, true);
        SetPartTopLevel(optionsPart, true);

        EnterButton.GlobalPosition = enterPart.BaseGlobalPosition;
        ExitButton.GlobalPosition = exitPart.BaseGlobalPosition;
        OptionsTitle.GlobalPosition = titlePart.BaseGlobalPosition + new Vector2(0, 8);
        OptionsContainer.GlobalPosition = optionsPart.BaseGlobalPosition + new Vector2(0, 12);
        SetControlAlpha(OptionsTitle, 0.0f);
        SetControlAlpha(OptionsContainer, 0.0f);

        var tween = CreateTween();
        tween.SetParallel(true);
        tween.SetEase(Tween.EaseType.Out);
        tween.SetTrans(Tween.TransitionType.Quint);
        tween.TweenProperty(EnterButton, "modulate:a", 0.0f, 0.2f);
        tween.TweenProperty(
            EnterButton,
            "global_position",
            enterPart.BaseGlobalPosition + new Vector2(0, 6),
            0.2f
        );
        tween.TweenProperty(ExitButton, "modulate:a", 0.0f, 0.2f);
        tween.TweenProperty(
            ExitButton,
            "global_position",
            exitPart.BaseGlobalPosition + new Vector2(0, 6),
            0.2f
        );
        tween.TweenProperty(OptionsTitle, "modulate:a", 1.0f, 0.24f).SetDelay(0.1f);
        tween
            .TweenProperty(OptionsTitle, "global_position", titlePart.BaseGlobalPosition, 0.24f)
            .SetDelay(0.1f);
        tween.TweenProperty(OptionsContainer, "modulate:a", 1.0f, 0.28f).SetDelay(0.16f);
        tween
            .TweenProperty(
                OptionsContainer,
                "global_position",
                optionsPart.BaseGlobalPosition,
                0.26f
            )
            .SetDelay(0.16f);

        // Option buttons reveal in a short stagger for better rhythm.
        var optionAnimationBase = new List<(Button button, Vector2 pos)>();
        foreach (var button in OptionButtons)
        {
            if (!button.Visible)
                continue;
            optionAnimationBase.Add((button, button.Position));
            button.Position += new Vector2(0, 10);
            SetControlAlpha(button, 0.0f);
        }

        for (int i = 0; i < optionAnimationBase.Count; i++)
        {
            var (button, basePos) = optionAnimationBase[i];
            float delay = 0.2f + 0.05f * i;
            tween.TweenProperty(button, "position", basePos, 0.24f).SetDelay(delay);
            tween.TweenProperty(button, "modulate:a", 1.0f, 0.22f).SetDelay(delay);
        }

        await ToSignal(tween, Tween.SignalName.Finished);
        EnterButton.Visible = false;
        ExitButton.Visible = false;
        RestorePartTopLevel(enterPart);
        RestorePartTopLevel(exitPart);
        RestorePartTopLevel(titlePart);
        RestorePartTopLevel(optionsPart);
        _isTransitioning = false;
    }

    private void OnExitPressed()
    {
        if (
            _isTransitioning
            || _isShowingOutcomeOverlay
            || ThisEvent?.IsStarterBonus == true
            || ThisEvent?.IsBossRelicChoice == true
            || ThisEvent?.IsTreasureChest == true
        )
            return;

        HideOptionTip();
        HideTargetSelection();
        HideCardSelection();
        _ = PlayCloseAnimationAsync(false);
    }

    private async void OnOptionPressed(int optionIndex)
    {
        if (
            _isTransitioning
            || _isShowingOutcomeOverlay
            || TargetSelectOverlay.Visible
            || (_cardSelectOverlay?.Visible == true)
            || ThisEvent?.Options == null
        )
            return;
        if ((uint)optionIndex >= (uint)ThisEvent.Options.Length)
            return;

        HideOptionTip();
        var option = ThisEvent.Options[optionIndex];
        if (option == null)
            return;
        if (!CanUseOption(option))
            return;

        if (optionIndex < OptionButtons.Count)
            await PlayOptionPressFeedbackAsync(OptionButtons[optionIndex]);

        if (option.PropertyChange != null && option.PropertyChange.Count > 0)
        {
            if (option.RandomChange)
            {
                int randomIndex = PickRandomPlayerIndex();
                if (randomIndex >= 0)
                {
                    ApplyPropertyChangesToPlayer(randomIndex, option.PropertyChange);
                    RefreshPartyLifeResource();
                    ShowPropertyHint(randomIndex, option.PropertyChange, true);
                }

                await ResolveOptionOutcomeAsync(option, randomIndex, true);
                return;
            }

            ShowTargetSelection(option);
            return;
        }

        if (option.RequiresCardSelection)
        {
            ShowCardSelection(option);
            return;
        }

        if (option.ActionType == EventOptionActionType.RestSingleHeal)
        {
            ShowRestSingleHealTargetSelection(option);
            return;
        }

        if (
            option.ActionType == EventOptionActionType.StarterBonus
            && option.StarterBonusOption == StarterBonusOption.TransformTwoCards
        )
        {
            _ = RunStarterBonusCardSelectionAsync(option);
            return;
        }

        string actionOutcomeText = ApplyImmediateOptionAction(option);
        await ResolveOptionOutcomeAsync(option, actionOutcomeText: actionOutcomeText);
    }

    private void ShowTargetSelection(EventOption option)
    {
        _pendingTargetOption = option;
        var players = GameInfo.PlayerCharacters ?? Array.Empty<PlayerInfoStructure>();
        TargetSelectOverlay.ShowSelection(players, "请选择一名角色作为该选项的目标");
    }

    private void ShowRestSingleHealTargetSelection(EventOption option)
    {
        _pendingRestSingleHealOption = option;
        var players = GameInfo.PlayerCharacters ?? Array.Empty<PlayerInfoStructure>();
        TargetSelectOverlay.ShowSelection(
            players,
            I18n.Tr("ui.rest.single_heal_selection_hint", "请选择要回复生命的角色")
        );
    }

    private void HideTargetSelection()
    {
        _pendingTargetOption = null;
        _pendingRestSingleHealOption = null;
        TargetSelectOverlay.HideSelection();
    }

    private EventCardSelectOverlay EnsureCardSelectOverlay()
    {
        if (_cardSelectOverlay != null && GodotObject.IsInstanceValid(_cardSelectOverlay))
            return _cardSelectOverlay;

        _cardSelectOverlay = GetNodeOrNull<EventCardSelectOverlay>("CardSelectOverlay");
        if (_cardSelectOverlay == null)
        {
            _cardSelectOverlay = new EventCardSelectOverlay { Name = "CardSelectOverlay" };
            AddChild(_cardSelectOverlay);
            MoveChild(_cardSelectOverlay, GetChildCount() - 1);
        }

        _cardSelectOverlay.CardSelected -= OnCardSelected;
        _cardSelectOverlay.SelectionCanceled -= OnCardSelectionCanceled;
        _cardSelectOverlay.CardSelected += OnCardSelected;
        _cardSelectOverlay.SelectionCanceled += OnCardSelectionCanceled;
        return _cardSelectOverlay;
    }

    private async Task RunStarterBonusCardSelectionAsync(EventOption option)
    {
        if (option == null || !CanUseOption(option))
            return;

        var overlay = EnsureCardSelectOverlay();
        MoveChild(overlay, GetChildCount() - 1);

        var entries = BuildSelectableCardEntries(EventOptionActionType.TransformCard);
        string hint = I18n.Tr(
            "starter_bonus.transform_two_cards.selection_hint",
            "请选择要变化的卡牌"
        );
        IReadOnlyList<EventCardSelection> selections = await overlay.SelectManyAsync(
            entries,
            hint,
            GameInfo.TransformTwoCardsCount
        );

        if (selections == null || selections.Count == 0)
            return;

        overlay.DetachSelectionCardsForDeckOperations(this, selections);
        overlay.HideSelection();

        string transformOutcome = await ApplyStarterBonusTransformCardsAsync(selections);
        ApplyStarterBonusOption(option);
        await ResolveOptionOutcomeAsync(option, actionOutcomeText: transformOutcome);
    }

    private async Task<string> ApplyStarterBonusTransformCardsAsync(
        IReadOnlyList<EventCardSelection> selections
    )
    {
        if (selections == null || selections.Count == 0)
            return string.Empty;

        _resourceRandom ??= CreateResourceRandom();
        var requests = selections
            .Select(selection => new TransformDeckCardRequest(
                selection.PlayerIndex,
                selection.SkillId,
                selection.SourceCard,
                selection.Snapshot
            ))
            .ToArray();
        IReadOnlyList<BattleReadyDeckOperationResult> results = await BattleReady.TransformDeckCardsAsync(
            this,
            requests,
            _resourceRandom
        );
        return string.Join(
            "\n",
            results
                .Select(result => result.Message)
                .Where(message => !string.IsNullOrWhiteSpace(message))
        );
    }

    private void ShowCardSelection(EventOption option)
    {
        _pendingCardOption = option;
        var overlay = EnsureCardSelectOverlay();
        MoveChild(overlay, GetChildCount() - 1);
        overlay.ShowSelection(
            BuildSelectableCardEntries(option.ActionType),
            GetCardSelectionHint(option.ActionType)
        );
    }

    private void HideCardSelection()
    {
        _pendingCardOption = null;
        _cardSelectOverlay?.HideSelection();
    }

    private void OnTargetSelectionCanceled()
    {
        _pendingTargetOption = null;
        _pendingRestSingleHealOption = null;
    }

    private void OnCardSelectionCanceled()
    {
        _pendingCardOption = null;
    }

    private async void OnTargetCharacterSelected(int index)
    {
        if (_pendingRestSingleHealOption != null)
        {
            if (!IsValidPlayerIndex(index))
                return;

            EventOption restOption = _pendingRestSingleHealOption;
            HideTargetSelection();
            string actionOutcomeText = ApplyRestSingleHeal(index);
            await ResolveOptionOutcomeAsync(restOption, index, actionOutcomeText: actionOutcomeText);
            return;
        }

        if (_pendingTargetOption == null)
            return;
        if (!IsValidPlayerIndex(index))
            return;

        var option = _pendingTargetOption;
        HideTargetSelection();
        ApplyPropertyChangesToPlayer(index, option.PropertyChange);
        RefreshPartyLifeResource();
        ShowPropertyHint(index, option.PropertyChange, false);
        await ResolveOptionOutcomeAsync(option, index, false);
    }

    private async void OnCardSelected(EventCardSelection selection)
    {
        if (_pendingCardOption == null)
            return;

        var option = _pendingCardOption;
        if (option.ActionType == EventOptionActionType.TransformCard)
        {
            var overlay = EnsureCardSelectOverlay();
            overlay.DetachSelectionCardsForDeckOperations(this, new[] { selection });
            HideCardSelection();
        }

        string actionOutcomeText = await ApplyCardOptionActionAsync(option, selection);
        if (option.ActionType != EventOptionActionType.TransformCard)
            HideCardSelection();
        await ResolveOptionOutcomeAsync(option, actionOutcomeText: actionOutcomeText);
    }

    private void RefreshPartyLifeResource()
    {
        GetTree()?.Root.GetNodeOrNull<Map>("/root/Map")?.PlayerResourceState?.RefreshPartyLifeResource();
    }

    private static bool IsValidPlayerIndex(int index)
    {
        var players = GameInfo.PlayerCharacters;
        return players != null && (uint)index < (uint)players.Length;
    }

    private static void ApplyPropertyChangesToPlayer(
        int playerIndex,
        Dictionary<PropertyType, int> changes
    )
    {
        if (!IsValidPlayerIndex(playerIndex))
            return;
        if (changes == null || changes.Count == 0)
            return;

        var info = GameInfo.PlayerCharacters[playerIndex];
        foreach (var kv in changes)
        {
            switch (kv.Key)
            {
                case PropertyType.Power:
                    info.Power += kv.Value;
                    break;
                case PropertyType.Survivability:
                    info.Survivability += kv.Value;
                    break;
                case PropertyType.MaxLife:
                    info.LifeMax += kv.Value;
                    info.LifeMax = Math.Max(1, info.LifeMax);
                    info.Life = Math.Clamp(info.Life, 0, info.LifeMax);
                    info.LifeInitialized = true;
                    break;
            }
        }
        GameInfo.PlayerCharacters[playerIndex] = info;
    }

    private int PickRandomPlayerIndex()
    {
        var players = GameInfo.PlayerCharacters;
        if (players == null || players.Length == 0)
            return -1;
        _resourceRandom ??= CreateResourceRandom();
        return _resourceRandom.Next(players.Length);
    }

    private void RollOptionResourceRewards(EventOption[] options)
    {
        _electricityRolls.Clear();
        _partyHealPercentRolls.Clear();
        _propertyChangeCostRolls.Clear();
        _relicRewardRolls.Clear();
        if (options == null)
            return;

        foreach (var option in options)
        {
            if (option?.HasElectricityChange == true)
                _electricityRolls[option] = option.RollElectricityChange(_resourceRandom);
            if (option?.HasPartyHealPercent == true)
                _partyHealPercentRolls[option] = option.RollPartyHealPercent(_resourceRandom);
            if (option?.HasPropertyChangeElectricityCost == true)
                _propertyChangeCostRolls[option] = option.RollPropertyChangeElectricityCost(_resourceRandom);
            if (option?.ActionType == EventOptionActionType.GainRelic)
                _relicRewardRolls[option] = RollRelicReward(option);
        }
    }

    private int GetRolledElectricityChange(EventOption option)
    {
        if (option == null)
            return 0;
        if (_electricityRolls.TryGetValue(option, out int value))
            return value;
        _resourceRandom ??= CreateResourceRandom();
        return option.RollElectricityChange(_resourceRandom);
    }

    private int GetRolledPartyHealPercent(EventOption option)
    {
        if (option == null)
            return 0;
        if (_partyHealPercentRolls.TryGetValue(option, out int value))
            return value;
        _resourceRandom ??= CreateResourceRandom();
        return option.RollPartyHealPercent(_resourceRandom);
    }

    private int GetRolledPropertyChangeCost(EventOption option)
    {
        if (option == null)
            return 0;
        if (_propertyChangeCostRolls.TryGetValue(option, out int value))
            return value;
        _resourceRandom ??= CreateResourceRandom();
        return option.RollPropertyChangeElectricityCost(_resourceRandom);
    }

    private RelicID? GetRolledRelicReward(EventOption option)
    {
        if (option == null || option.ActionType != EventOptionActionType.GainRelic)
            return null;
        if (_relicRewardRolls.TryGetValue(option, out RelicID? relicId))
            return relicId;

        _resourceRandom ??= CreateResourceRandom();
        relicId = RollRelicReward(option);
        _relicRewardRolls[option] = relicId;
        return relicId;
    }

    private RelicID? RollRelicReward(EventOption option)
    {
        if (option?.RelicReward != null)
            return option.RelicReward.Value;

        return GameInfo.GetEventRelicOffer(WhichNode);
    }

    private bool CanUseOption(EventOption option)
    {
        if (option == null)
            return false;

        if (option.PropertyChange != null && option.PropertyChange.Count > 0)
        {
            int cost = GetRolledPropertyChangeCost(option);
            if (GameInfo.ElectricityCoin < cost)
                return false;
        }
        else if (
            option.ActionType == EventOptionActionType.StarterBonus
            && option.HasPropertyChangeElectricityCost
        )
        {
            int cost = GetRolledPropertyChangeCost(option);
            if (cost > 0 && GameInfo.ElectricityCoin < cost)
                return false;
        }

        return option.ActionType switch
        {
            EventOptionActionType.CopyCard
            or EventOptionActionType.TransformCard
            or EventOptionActionType.RemoveCard => BuildSelectableCardEntries(option.ActionType).Any(),
            EventOptionActionType.GainRelic => GetRolledRelicReward(option).HasValue,
            EventOptionActionType.GainTalentPoint => HasTalentPointCandidate(),
            EventOptionActionType.RestHeal => true,
            EventOptionActionType.RestSingleHeal => true,
            EventOptionActionType.RestTalentPoint =>
                GameInfo.PreviewRestTalentPointReward(WhichNode).Granted,
            EventOptionActionType.StarterBonus => CanUseStarterBonusOption(option),
            _ => true,
        };
    }

    private static bool CanUseStarterBonusOption(EventOption option)
    {
        return option?.StarterBonusOption switch
        {
            StarterBonusOption.RandomTalentPoints =>
                GameInfo.PreviewStarterBonusTalentPointReward().Granted,
            StarterBonusOption.TransformTwoCards =>
                CountTransformableDeckCards() >= GameInfo.TransformTwoCardsCount,
            _ => true,
        };
    }

    private static int CountTransformableDeckCards()
    {
        var players = GameInfo.PlayerCharacters ?? Array.Empty<PlayerInfoStructure>();
        return GameInfo
            .BuildSelectableDeckCardEntries()
            .Count(entry =>
                entry.PlayerIndex >= 0
                && entry.PlayerIndex < players.Length
                && BattleReady.CanTransformDeckCard(players[entry.PlayerIndex], entry.SkillId)
            );
    }

    private Random CreateResourceRandom()
    {
        return GameInfo.CreateRunRng(WhichNode, EventResourceRandomSalt);
    }

    private List<EventCardSelectionEntry> BuildSelectableCardEntries(
        EventOptionActionType actionType
    )
    {
        if (actionType != EventOptionActionType.TransformCard)
            return GameInfo.BuildSelectableDeckCardEntries();

        var entries = GameInfo.BuildSelectableDeckCardEntries();
        var players = GameInfo.PlayerCharacters ?? Array.Empty<PlayerInfoStructure>();
        return entries
            .Where(entry =>
                entry.PlayerIndex >= 0
                && entry.PlayerIndex < players.Length
                && BattleReady.CanTransformDeckCard(players[entry.PlayerIndex], entry.SkillId)
            )
            .ToList();
    }

    private static string GetCardSelectionHint(EventOptionActionType actionType)
    {
        return actionType switch
        {
            EventOptionActionType.CopyCard => "请选择要复制的卡牌",
            EventOptionActionType.TransformCard => "请选择要变化的卡牌",
            EventOptionActionType.RemoveCard => "请选择要删除的卡牌",
            _ => "请选择一张卡牌",
        };
    }

    private string ApplyImmediateOptionAction(EventOption option)
    {
        return option?.ActionType switch
        {
            EventOptionActionType.GainRelic => GrantRelicReward(option),
            EventOptionActionType.GainTalentPoint => GrantTalentPointReward(option),
            EventOptionActionType.RestHeal => ApplyRestHeal(),
            EventOptionActionType.RestTalentPoint => GrantRestTalentPointReward(),
            EventOptionActionType.StarterBonus => ApplyStarterBonusOption(option),
            _ => string.Empty,
        };
    }

    private string ApplyRestSingleHeal(int playerIndex)
    {
        int percent = (int)Math.Round(LevelProgress.RestSingleHealPercent * 100f);
        string name = GetPlayerDisplayName(playerIndex);
        int healed = GameInfo.HealPlayerByMaxLifePercent(
            playerIndex,
            LevelProgress.RestSingleHealPercent
        );
        GameInfo.AppendActiveLevelNodeNote(
            I18n.Format(
                "ui.rest.history_single_heal",
                "选择：{name} 恢复 {percent}% 生命",
                ("name", name),
                ("percent", percent)
            )
        );
        RefreshPartyLifeResource();
        return I18n.Format(
            "ui.rest.single_heal_description",
            "[b]{name}[/b]\n恢复 [color=#7dffb0]{percent}%[/color] 最大生命（+{healed}）。",
            ("name", name),
            ("percent", percent),
            ("healed", healed)
        );
    }

    private string ApplyRestHeal()
    {
        int percent = (int)Math.Round(LevelProgress.RestHealPercent * 100f);
        GameInfo.AppendActiveLevelNodeNote(
            I18n.Format(
                "ui.rest.history_heal",
                "选择：恢复 {percent}% 生命",
                ("percent", percent)
            )
        );
        GameInfo.HealPartyByMaxLifePercent(LevelProgress.RestHealPercent);
        RefreshPartyLifeResource();
        return I18n.Format(
            "ui.rest.heal_description",
            "恢复全队 [color=#7dffb0]{percent}%[/color] 最大生命。",
            ("percent", percent)
        );
    }

    private string GrantRestTalentPointReward()
    {
        var talentReward = GameInfo.TryGrantRestTalentPointReward(WhichNode);
        if (!talentReward.Granted)
            return "没有可获得天赋点的角色";

        GameInfo.AppendActiveLevelNodeNote(
            I18n.Format(
                "ui.rest.history_talent",
                "选择：{name} 获得 {amount} 点天赋点",
                ("name", talentReward.CharacterName),
                ("amount", talentReward.Amount)
            )
        );
        GD.Print(
            I18n.Format(
                "ui.rest.talent_reward_log",
                "休息奖励：{name} 获得 {amount} 点天赋点。",
                ("name", talentReward.CharacterName),
                ("amount", talentReward.Amount)
            )
        );
        _pendingRestTalentCharacterName = talentReward.CharacterName;
        return $"[b]{talentReward.CharacterName}[/b]\n天赋点 {FormatSigned(talentReward.Amount)}";
    }

    private async Task<string> ApplyCardOptionActionAsync(
        EventOption option,
        EventCardSelection selection
    )
    {
        BattleReadyDeckOperationResult result = option?.ActionType switch
        {
            EventOptionActionType.CopyCard => await BattleReady.CopyDeckCardAsync(
                this,
                selection.PlayerIndex,
                selection.SkillId,
                selection.SourceCard
            ),
            EventOptionActionType.TransformCard => await BattleReady.TransformDeckCardAsync(
                this,
                selection.PlayerIndex,
                selection.SkillId,
                selection.SourceCard,
                _resourceRandom,
                selection.Snapshot
            ),
            EventOptionActionType.RemoveCard => await BattleReady.RemoveDeckCardAsync(
                this,
                selection.PlayerIndex,
                selection.SkillId,
                selection.SourceCard
            ),
            _ => default,
        };

        return result.Message ?? string.Empty;
    }

    private string ApplyStarterBonusOption(EventOption option)
    {
        var choice = option?.StarterBonusOption ?? StarterBonusOption.None;
        if (choice == StarterBonusOption.None)
            return string.Empty;

        var resourceState = GetResourceState();
        if (choice == StarterBonusOption.Blessing && resourceState != null)
        {
            Relic.RelicAdd(resourceState, RelicID.Blessing);
            GameInfo.SelectedStarterBonus = StarterBonusOption.Blessing;
            GameInfo.PendingStarterBonusChoice = false;
        }
        else
        {
            GameInfo.ApplyStarterBonus(choice);
            if (
                choice == StarterBonusOption.RandomRelic
                && resourceState != null
                && GameInfo.LastStarterBonusGrantedRelic.HasValue
            )
            {
                Relic.RelicAdd(resourceState, GameInfo.LastStarterBonusGrantedRelic.Value);
            }

            if (choice == StarterBonusOption.RandomTalentPoints)
                return BuildStarterBonusTalentOutcomeText(GameInfo.LastStarterBonusTalentGrantResult);
        }

        return string.Empty;
    }

    private string BuildStarterBonusTalentOutcomeText(StarterBonusTalentPointResult result)
    {
        if (!result.Granted || result.CharacterNames.Length == 0)
            return "没有可获得天赋点的角色";

        _pendingStarterBonusTalentCharacterNames = result.CharacterNames.ToList();
        var lines = new List<string>(result.CharacterNames.Length);
        foreach (string name in result.CharacterNames)
        {
            lines.Add(
                $"[b]{name}[/b]\n天赋点 {FormatSigned(result.AmountPerCharacter)}"
            );
        }

        return string.Join("\n\n", lines);
    }

    private string GrantRelicReward(EventOption option)
    {
        RelicID? relicId = GetRolledRelicReward(option);
        if (!relicId.HasValue)
            return "没有可获得的遗物";

        var resourceState = GetResourceState();
        if (resourceState != null)
        {
            Relic.RelicAdd(resourceState, relicId.Value);
        }
        else
        {
            GameInfo.SetRelicCount(relicId.Value, Relic.GetAcquireAmount(relicId.Value));
        }

        if (ThisEvent?.IsBossRelicChoice == true)
        {
            GameInfo.PendingBossRelicChoice = false;
            SaveSystem.SaveRunCheckpoint(background: false);
        }

        return $"获得遗物：[b]{Relic.Create(relicId.Value).RelicName}[/b]";
    }

    private string GrantTalentPointReward(EventOption option)
    {
        var candidates = GetTalentPointCandidateIndices();
        if (candidates.Count == 0)
            return "没有可获得天赋点的角色";

        _resourceRandom ??= CreateResourceRandom();
        int characterIndex = candidates[_resourceRandom.Next(candidates.Count)];
        int amount = Math.Max(1, option?.TalentPointAmount ?? 1);
        var players = GameInfo.PlayerCharacters;
        var info = players[characterIndex];
        TalentTree.AddTalentPoints(ref info, amount);
        players[characterIndex] = info;
        GameInfo.PlayerCharacters = players;

        string name = GetPlayerDisplayName(characterIndex);
        return $"[b]{name}[/b]\n天赋点 {FormatSigned(amount)}";
    }

    private static bool HasTalentPointCandidate() => GetTalentPointCandidateIndices().Count > 0;

    private static List<int> GetTalentPointCandidateIndices()
    {
        GameInfo.NormalizePlayerCharacters();
        var players = GameInfo.PlayerCharacters ?? Array.Empty<PlayerInfoStructure>();
        var result = new List<int>();

        for (int i = 0; i < players.Length; i++)
        {
            if (!string.IsNullOrWhiteSpace(players[i].CharacterName) && CanGainTalentPoint(players[i]))
                result.Add(i);
        }

        return result;
    }

    private static bool CanGainTalentPoint(PlayerInfoStructure info)
    {
        var nodes = TalentTree.GetNodes(info);
        if (nodes.Count == 0)
            return false;

        int totalCost = nodes.Sum(node => node.Cost);
        if (totalCost <= 0)
            return false;

        var unlockedTalentIds = new HashSet<string>(
            info.UnlockedTalents ?? [],
            StringComparer.Ordinal
        );
        int spentCost = nodes
            .Where(node => unlockedTalentIds.Contains(node.Id))
            .Sum(node => node.Cost);
        int earnedTalentPoints = Math.Max(0, info.TalentPoints) + spentCost;
        return earnedTalentPoints < totalCost;
    }

    private PlayerResourceState GetResourceState()
    {
        return GetTree()?.Root.GetNodeOrNull<PlayerResourceState>("Map/PlayerResourceState")
            ?? GetTree()?.Root.GetNodeOrNull<PlayerResourceState>("/root/Map/PlayerResourceState");
    }

    private static string GetSkillDisplayName(SkillID skillId)
    {
        return Skill.GetSkill(skillId)?.SkillName ?? skillId.ToString();
    }

    private static string ExtractCharacterKeyFromScenePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return string.Empty;

        string[] parts = path.Split('/');
        return parts.Length >= 2 ? parts[^2] : string.Empty;
    }

    private void ShowPropertyHint(
        int playerIndex,
        Dictionary<PropertyType, int> changes,
        bool random
    )
    {
        if (!IsValidPlayerIndex(playerIndex) || changes == null || changes.Count == 0)
            return;

        var info = GameInfo.PlayerCharacters[playerIndex];
        string name = string.IsNullOrWhiteSpace(info.CharacterName)
            ? $"角色{playerIndex + 1}"
            : info.CharacterName;

        var sb = new StringBuilder();
        sb.Append($"[b]{name}[/b]");
        if (random)
            sb.Append(" [color=#ffd36b](随机)[/color]");
        sb.Append('\n');

        foreach (var kv in changes)
            sb.Append($"{Skill.GetColoredPropertyLabel(kv.Key)} {FormatSigned(kv.Value)}\n");

        BuffHintLabel.Spawn(
            this,
            GlobalFunction.ColorizeNumbers(sb.ToString().TrimEnd()),
            new Vector2(960, 640),
            randomOffset: true
        );
    }

    private async Task ResolveOptionOutcomeAsync(
        EventOption option,
        int targetIndex = -1,
        bool randomTarget = false,
        string actionOutcomeText = null
    )
    {
        var resourceChanges = ApplyResourceChanges(option);
        int propertyCost = 0;
        if (option?.HasPropertyChangeElectricityCost == true)
        {
            bool shouldCharge =
                option.PropertyChange != null && option.PropertyChange.Count > 0
                || option.ActionType == EventOptionActionType.StarterBonus;
            if (shouldCharge)
            {
                propertyCost = GetRolledPropertyChangeCost(option);
                if (propertyCost > 0)
                {
                    GameInfo.ElectricityCoin -= propertyCost;
                    resourceChanges = new AppliedResourceChanges(
                        resourceChanges.PartyHealPercent,
                        resourceChanges.ElectricityChange,
                        propertyCost
                    );
                    var resourceState = GetResourceState();
                    if (resourceState != null)
                        resourceState.ElectricityCoin = GameInfo.ElectricityCoin;
                }
            }
        }
        string outcomeText = BuildOutcomeSummaryText(
            option,
            targetIndex,
            randomTarget,
            resourceChanges,
            actionOutcomeText
        );
        if (!string.IsNullOrWhiteSpace(outcomeText))
            await ShowOutcomeOverlayAsync(outcomeText);

        if (
            option.ActionType == EventOptionActionType.RestTalentPoint
            && !string.IsNullOrWhiteSpace(_pendingRestTalentCharacterName)
        )
        {
            string characterName = _pendingRestTalentCharacterName;
            _pendingRestTalentCharacterName = null;
            var talentTreeClosed = new TaskCompletionSource<bool>();
            Reward.ShowStandaloneTalentTree(
                this,
                characterName,
                onClosed: () => talentTreeClosed.TrySetResult(true)
            );
            await talentTreeClosed.Task;
        }

        if (
            option.ActionType == EventOptionActionType.StarterBonus
            && option.StarterBonusOption == StarterBonusOption.RandomTalentPoints
            && _pendingStarterBonusTalentCharacterNames is { Count: > 0 } starterTalentNames
        )
        {
            foreach (string characterName in starterTalentNames.ToArray())
            {
                var talentTreeClosed = new TaskCompletionSource<bool>();
                Reward.ShowStandaloneTalentTree(
                    this,
                    characterName,
                    onClosed: () => talentTreeClosed.TrySetResult(true)
                );
                await talentTreeClosed.Task;
            }

            _pendingStarterBonusTalentCharacterNames = null;
        }

        if (option.Exit)
            await PlayCloseAnimationAsync(true);
    }

    private async Task ShowOutcomeOverlayAsync(string text)
    {
        if (OutcomeOverlay == null || OutcomeText == null || string.IsNullOrWhiteSpace(text))
            return;

        _outcomeDismissSource = new TaskCompletionSource<bool>();
        _isShowingOutcomeOverlay = true;
        OutcomeText.Text = text;
        OutcomeOverlay.Visible = true;
        SetControlAlpha(OutcomeOverlay, 0.0f);
        if (OutcomeBanner != null)
        {
            CacheOutcomeBannerBasePosition();
            OutcomeBanner.Scale = new Vector2(0.94f, 0.94f);
            OutcomeBanner.Position = _outcomeBannerBasePosition + new Vector2(0f, 28f);
            OutcomeBanner.Modulate = new Color(1.18f, 1.18f, 1.18f, 1f);
        }

        var tween = CreateTween();
        tween.SetParallel(true);
        tween.SetEase(Tween.EaseType.Out);
        tween.SetTrans(Tween.TransitionType.Cubic);
        tween.TweenProperty(OutcomeOverlay, "modulate:a", 1.0f, OutcomeOverlayFadeDuration);
        if (OutcomeBanner != null)
        {
            tween
                .TweenProperty(OutcomeBanner, "scale", Vector2.One, OutcomeBannerEnterDuration)
                .SetTrans(Tween.TransitionType.Cubic)
                .SetEase(Tween.EaseType.Out);
            tween
                .TweenProperty(OutcomeBanner, "position", _outcomeBannerBasePosition, OutcomeBannerEnterDuration)
                .SetTrans(Tween.TransitionType.Cubic)
                .SetEase(Tween.EaseType.Out);
            tween
                .TweenProperty(OutcomeBanner, "modulate", Colors.White, OutcomeBannerEnterDuration)
                .SetTrans(Tween.TransitionType.Cubic)
                .SetEase(Tween.EaseType.Out);
        }
        await ToSignal(tween, Tween.SignalName.Finished);

        await _outcomeDismissSource.Task;
    }

    private async void DismissOutcomeOverlay()
    {
        if (!_isShowingOutcomeOverlay || OutcomeOverlay == null)
            return;

        _isShowingOutcomeOverlay = false;
        var tween = CreateTween();
        tween.SetParallel(true);
        tween.SetEase(Tween.EaseType.In);
        tween.SetTrans(Tween.TransitionType.Cubic);
        tween.TweenProperty(OutcomeOverlay, "modulate:a", 0.0f, OutcomeOverlayDismissDuration);
        if (OutcomeBanner != null)
        {
            CacheOutcomeBannerBasePosition();
            tween.TweenProperty(
                OutcomeBanner,
                "scale",
                new Vector2(0.96f, 0.96f),
                OutcomeOverlayDismissDuration
            );
            tween.TweenProperty(
                OutcomeBanner,
                "position",
                _outcomeBannerBasePosition + new Vector2(0f, 10f),
                OutcomeOverlayDismissDuration
            );
        }
        await ToSignal(tween, Tween.SignalName.Finished);

        OutcomeOverlay.Visible = false;
        if (OutcomeBanner != null)
        {
            OutcomeBanner.Position = _outcomeBannerBasePosition;
            OutcomeBanner.Scale = Vector2.One;
            OutcomeBanner.Modulate = Colors.White;
        }
        _outcomeDismissSource?.TrySetResult(true);
        _outcomeDismissSource = null;
    }

    private async Task PlayOptionPressFeedbackAsync(Button button)
    {
        if (button == null || !GodotObject.IsInstanceValid(button) || !button.IsInsideTree())
            return;

        Vector2 baseScale = button.Scale == Vector2.Zero ? Vector2.One : button.Scale;
        Color baseModulate = button.Modulate;
        button.PivotOffset = button.Size * 0.5f;
        button.Modulate = new Color(1.18f, 1.12f, 0.78f, baseModulate.A);

        Tween tween = CreateTween();
        tween.SetParallel(false);
        tween
            .TweenProperty(button, "scale", baseScale * 0.97f, OptionPressPulseDuration * 0.42f)
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.Out);
        tween
            .TweenProperty(button, "scale", baseScale, OptionPressPulseDuration * 0.58f)
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.Out);
        tween
            .TweenProperty(button, "modulate", baseModulate, OptionPressPulseDuration)
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.Out);

        await ToSignal(tween, Tween.SignalName.Finished);
    }

    private void CacheOutcomeBannerBasePosition()
    {
        if (_outcomeBannerBasePositionCached || OutcomeBanner == null)
            return;

        _outcomeBannerBasePosition = OutcomeBanner.Position;
        _outcomeBannerBasePositionCached = true;
    }

    private void OnOutcomeOverlayGuiInput(InputEvent inputEvent)
    {
        if (!_isShowingOutcomeOverlay)
            return;

        bool shouldDismiss =
            (inputEvent is InputEventMouseButton mouseButton && mouseButton.Pressed)
            || (inputEvent is InputEventScreenTouch screenTouch && screenTouch.Pressed);

        if (!shouldDismiss)
            return;

        AcceptEvent();
        DismissOutcomeOverlay();
    }

    private AppliedResourceChanges ApplyResourceChanges(EventOption option)
    {
        if (option == null)
            return default;

        int electricityChange = GetRolledElectricityChange(option);
        int partyHealPercent = GetRolledPartyHealPercent(option);
        if (partyHealPercent == 0 && electricityChange == 0)
            return default;

        int previousElectricityCoin = GameInfo.ElectricityCoin;
        if (partyHealPercent > 0)
        {
            GameInfo.HealPartyByMaxLifePercent(partyHealPercent / 100f);
            GameInfo.AppendActiveLevelNodeNote($"全体恢复 {partyHealPercent}% 最大生命");
        }

        var map = GetTree().Root.GetNodeOrNull<Map>("/root/Map");
        if (map != null && map.PlayerResourceState != null)
        {
            if (partyHealPercent > 0)
                map.PlayerResourceState.RefreshPartyLifeResource();
            if (electricityChange != 0)
                map.PlayerResourceState.ElectricityCoin += electricityChange;
            return new AppliedResourceChanges(
                partyHealPercent,
                GameInfo.ElectricityCoin - previousElectricityCoin
            );
        }

        if (electricityChange != 0)
            GameInfo.ElectricityCoin += electricityChange;

        return new AppliedResourceChanges(
            partyHealPercent,
            GameInfo.ElectricityCoin - previousElectricityCoin
        );
    }

    private async Task PlayAssembleAnimationAsync()
    {
        if (_isTransitioning)
            return;

        _isTransitioning = true;
        Modulate = Modulate with { A = 0.0f };
        SetControlAlpha(Background, 0.0f);

        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

        var items = GetAssemblyItems();
        var prepared = new List<(AssemblyItem item, PartState part)>(items.Length);
        foreach (var item in items)
        {
            var part = CapturePart(item.Control);
            prepared.Add((item, part));
            SetPartTopLevel(part, true);
            item.Control.GlobalPosition = part.BaseGlobalPosition + item.Offset;
            SetControlAlpha(item.Control, 0.0f);
        }

        var tween = CreateTween();
        tween.SetParallel(true);
        tween.SetEase(Tween.EaseType.Out);
        tween.SetTrans(Tween.TransitionType.Quint);
        tween.TweenProperty(this, "modulate:a", 1.0f, IntroDuration(0.3f));
        tween.TweenProperty(Background, "modulate:a", 1.0f, IntroDuration(0.36f));

        foreach (var entry in prepared)
        {
            var item = entry.item;
            var part = entry.part;
            tween
                .TweenProperty(
                    item.Control,
                    "global_position",
                    part.BaseGlobalPosition,
                    IntroDuration(item.MoveDuration)
                )
                .SetDelay(IntroDuration(item.Delay));
            tween
                .TweenProperty(item.Control, "modulate:a", part.BaseAlpha, IntroDuration(item.FadeDuration))
                .SetDelay(IntroDuration(item.Delay));
        }

        await ToSignal(tween, Tween.SignalName.Finished);
        foreach (var entry in prepared)
            RestorePartTopLevel(entry.part);

        _isTransitioning = false;
    }

    private async Task PlayDisassembleAnimationAsync()
    {
        var items = GetDisassemblyItems();
        var prepared = new List<(AssemblyItem item, PartState part)>(items.Length);

        foreach (var item in items)
        {
            if (item.Control == null || !item.Control.Visible)
                continue;

            var part = CapturePart(item.Control);
            prepared.Add((item, part));
            SetPartTopLevel(part, true);
            item.Control.GlobalPosition = part.BaseGlobalPosition;
        }

        var tween = CreateTween();
        tween.SetParallel(true);
        tween.SetEase(Tween.EaseType.Out);
        tween.SetTrans(Tween.TransitionType.Quint);
        tween.TweenProperty(this, "modulate:a", 0.0f, 0.36f).SetDelay(0.12f);
        if (Background != null)
            tween.TweenProperty(Background, "modulate:a", 0.0f, 0.28f);

        foreach (var entry in prepared)
        {
            var item = entry.item;
            tween
                .TweenProperty(
                    item.Control,
                    "global_position",
                    entry.part.BaseGlobalPosition + item.Offset,
                    item.MoveDuration
                )
                .SetDelay(item.Delay);
            tween
                .TweenProperty(item.Control, "modulate:a", 0.0f, item.FadeDuration)
                .SetDelay(item.Delay);
        }

        await ToSignal(tween, Tween.SignalName.Finished);
        foreach (var entry in prepared)
            RestorePartTopLevel(entry.part);
    }

    private async Task PlayCloseAnimationAsync(bool callComplete)
    {
        if (_isTransitioning)
            return;

        _isTransitioning = true;
        HideTargetSelection();
        HideCardSelection();
        await PlayDisassembleAnimationAsync();
        if (callComplete)
        {
            WhichNode?.Completed();
            QueueFree();
            return;
        }
        else
        {
            QueueFree();
            ReleaseMapNodeLock();
        }
    }

    private void ReleaseMapNodeLock()
    {
        var levelProgress = WhichNode?.GetParent()?.GetParent<LevelProgress>();
        levelProgress?.UnlockAllNodes();
    }

    private AssemblyItem[] GetDisassemblyItems()
    {
        return
        [
            new AssemblyItem(OptionsContainer, new Vector2(0f, 18f), 0.00f, 0.24f, 0.18f),
            new AssemblyItem(OptionsTitle, new Vector2(0f, 12f), 0.04f, 0.22f, 0.18f),
            new AssemblyItem(ContentRow, new Vector2(0f, 26f), 0.08f, 0.26f, 0.2f),
            new AssemblyItem(Divider, new Vector2(0f, -10f), 0.12f, 0.22f, 0.18f),
            new AssemblyItem(SubtitleLabel, new Vector2(0f, -16f), 0.14f, 0.24f, 0.18f),
            new AssemblyItem(TitleLabel, new Vector2(0f, -22f), 0.18f, 0.26f, 0.2f),
            new AssemblyItem(LeftAccent, new Vector2(-14f, 0f), 0.22f, 0.22f, 0.18f),
            new AssemblyItem(RightAccent, new Vector2(14f, 0f), 0.24f, 0.22f, 0.18f),
            new AssemblyItem(HeaderAccent, new Vector2(-28f, 0f), 0.28f, 0.24f, 0.18f),
            new AssemblyItem(HeaderCode, new Vector2(24f, 0f), 0.30f, 0.24f, 0.18f),
            new AssemblyItem(HeaderTitle, new Vector2(-24f, 0f), 0.32f, 0.24f, 0.18f),
            new AssemblyItem(HeaderBar, new Vector2(0f, -20f), 0.36f, 0.26f, 0.2f),
            new AssemblyItem(EnterButton, new Vector2(0f, 12f), 0.02f, 0.22f, 0.16f),
            new AssemblyItem(ExitButton, new Vector2(0f, 16f), 0.06f, 0.22f, 0.16f),
        ];
    }

    private AssemblyItem[] GetAssemblyItems()
    {
        return
        [
            new AssemblyItem(HeaderBar, new Vector2(0f, -24f), 0.00f, 0.34f, 0.28f),
            new AssemblyItem(HeaderTitle, new Vector2(-26f, 0f), 0.06f, 0.30f, 0.24f),
            new AssemblyItem(HeaderCode, new Vector2(26f, 0f), 0.08f, 0.30f, 0.24f),
            new AssemblyItem(HeaderAccent, new Vector2(-36f, 0f), 0.10f, 0.28f, 0.22f),
            new AssemblyItem(LeftAccent, new Vector2(-16f, 0f), 0.14f, 0.26f, 0.22f),
            new AssemblyItem(RightAccent, new Vector2(16f, 0f), 0.16f, 0.26f, 0.22f),
            new AssemblyItem(TitleLabel, new Vector2(0f, -26f), 0.22f, 0.30f, 0.24f),
            new AssemblyItem(SubtitleLabel, new Vector2(0f, -16f), 0.28f, 0.28f, 0.22f),
            new AssemblyItem(Divider, new Vector2(0f, -12f), 0.34f, 0.26f, 0.2f),
            new AssemblyItem(ContentRow, new Vector2(0f, 24f), 0.42f, 0.32f, 0.26f),
        ];
    }

    private void EnsureTipLayer()
    {
        var root = GetTree().Root;
        var existingLayer = root.GetNodeOrNull<CanvasLayer>("TipLayer");

        if (existingLayer == null)
        {
            existingLayer = new CanvasLayer { Layer = 6, Name = "TipLayer" };
            root.CallDeferred(Node.MethodName.AddChild, existingLayer);
        }

        var tipScene = GD.Load<PackedScene>("res://battle/UIScene/Tip.tscn");
        if (tipScene == null)
            return;

        if (!existingLayer.HasNode("EventTip"))
        {
            var tip = tipScene.Instantiate<Tip>();
            tip.Name = "EventTip";
            tip.FollowMouse = true;
            tip.AnchorOffset = new Vector2(20f, 20f);
            existingLayer.AddChild(tip);
        }
    }

    private void ShowOptionTip(int optionIndex)
    {
        if (OptionTooltip == null || _optionTipTexts.Length == 0)
            return;
        if ((uint)optionIndex >= (uint)_optionTipTexts.Length)
            return;

        string text = _optionTipTexts[optionIndex];
        if (string.IsNullOrWhiteSpace(text))
            return;
        OptionTooltip.SetText(text);
    }

    private void HideOptionTip()
    {
        OptionTooltip?.HideTooltip();
    }

    private static string BuildOptionTipText(
        EventOption option,
        int electricityChange,
        int partyHealPercent,
        int propertyChangeCost,
        RelicID? relicReward,
        LevelNode whichNode = null
    )
    {
        if (option == null)
            return string.Empty;

        var sb = new StringBuilder(128);
        sb.Append("[b]效果[/b]\n");

        bool hasAny = false;
        if (option.PropertyChange != null && option.PropertyChange.Count > 0)
        {
            sb.Append(option.RandomChange ? "目标：随机一名角色\n" : "目标：选择一名角色\n");
            foreach (var kv in option.PropertyChange)
            {
                sb.Append($"{Skill.GetColoredPropertyLabel(kv.Key)} {FormatSigned(kv.Value)}\n");
            }
            if (propertyChangeCost > 0)
            {
                sb.Append($"[color=#ff6b6b]电力币 {FormatSigned(-propertyChangeCost)}[/color]\n");
            }
            hasAny = true;
        }

        if (option.ActionType != EventOptionActionType.None)
        {
            AppendActionTipText(sb, option, relicReward, whichNode);
            hasAny = true;
        }

        if (partyHealPercent > 0)
        {
            sb.Append($"全体恢复 {partyHealPercent}% 最大生命\n");
            hasAny = true;
        }

        if (option.HasElectricityChange)
        {
            sb.Append($"电力币 {FormatSigned(electricityChange)}\n");
            hasAny = true;
        }
        if (option.Exit)
        {
            sb.Append("事件结束\n");
            hasAny = true;
        }

        if (!hasAny)
            sb.Append("无额外效果\n");

        string text = sb.ToString().TrimEnd();
        text = GlobalFunction.ColorizeNumbers(text);
        text = GlobalFunction.ColorizeKeywords(text);
        return text;
    }

    private static void AppendActionTipText(
        StringBuilder sb,
        EventOption option,
        RelicID? relicReward,
        LevelNode whichNode = null
    )
    {
        switch (option.ActionType)
        {
            case EventOptionActionType.CopyCard:
                sb.Append("复制：选择一张已拥有卡牌，获得其副本\n");
                break;
            case EventOptionActionType.TransformCard:
                sb.Append("变化：选择一张卡牌，替换为同角色随机卡牌\n");
                break;
            case EventOptionActionType.RemoveCard:
                sb.Append("删除：选择一张卡牌，从牌组移除\n");
                break;
            case EventOptionActionType.GainRelic:
                if (relicReward.HasValue)
                {
                    var relic = Relic.Create(relicReward.Value);
                    sb.Append($"获得遗物：{relic.RelicName}\n");
                    sb.Append(GlobalFunction.ColorizeNumbers(relic.RelicDescription));
                    sb.Append('\n');
                }
                else
                    sb.Append("获得遗物：无可获得遗物\n");
                break;
            case EventOptionActionType.GainTalentPoint:
                int amount = Math.Max(1, option.TalentPointAmount);
                sb.Append($"随机角色 天赋点 {FormatSigned(amount)}\n");
                break;
            case EventOptionActionType.RestHeal:
                int healPercent = (int)MathF.Round(LevelProgress.RestHealPercent * 100f);
                sb.Append(
                    I18n.Format(
                        "ui.rest.heal_description",
                        "恢复全队 [color=#7dffb0]{percent}%[/color] 最大生命。",
                        ("percent", healPercent)
                    )
                );
                sb.Append('\n');
                break;
            case EventOptionActionType.RestSingleHeal:
                int singleHealPercent = (int)MathF.Round(LevelProgress.RestSingleHealPercent * 100f);
                sb.Append(
                    I18n.Format(
                        "ui.rest.single_heal_description_tip",
                        "选择一名角色，恢复其 [color=#7dffb0]{percent}%[/color] 最大生命。",
                        ("percent", singleHealPercent)
                    )
                );
                sb.Append('\n');
                break;
            case EventOptionActionType.RestTalentPoint:
                var talentPreview = GameInfo.PreviewRestTalentPointReward(whichNode);
                if (talentPreview.Granted)
                {
                    sb.Append(
                        I18n.Format(
                            "ui.rest.talent_description",
                            "随机一名角色获得 [color=#ffd987]1[/color] 点天赋点。\n预览：[color=#cfd6e6]{name}[/color]",
                            ("name", talentPreview.CharacterName)
                        )
                    );
                    sb.Append('\n');
                }
                else
                {
                    sb.Append("没有可获得天赋点的角色\n");
                }
                break;
            case EventOptionActionType.StarterBonus:
                AppendStarterBonusTipText(sb, option);
                break;
        }
    }

    private static void AppendStarterBonusTipText(StringBuilder sb, EventOption option)
    {
        switch (option?.StarterBonusOption ?? StarterBonusOption.None)
        {
            case StarterBonusOption.Blessing:
                var relic = Relic.Create(RelicID.Blessing);
                sb.Append(GlobalFunction.ColorizeNumbers(relic.RelicDescription));
                sb.Append('\n');
                break;
            case StarterBonusOption.ExtraBattleSkillRewards:
                sb.Append(
                    I18n.Tr(
                        "starter_bonus.extra_rewards.description",
                        "前三场战斗奖励时，额外多看一组卡牌。"
                    )
                );
                sb.Append('\n');
                break;
            case StarterBonusOption.RandomRareSkill:
                sb.Append(
                    I18n.Tr(
                        "starter_bonus.rare_skill.description",
                        "随机获得一张稀有卡牌加入牌组。"
                    )
                );
                sb.Append('\n');
                break;
            case StarterBonusOption.RandomRelic:
                sb.Append(
                    I18n.Tr(
                        "starter_bonus.random_relic.description",
                        "随机获得一件遗物。"
                    )
                );
                sb.Append('\n');
                break;
            case StarterBonusOption.RandomTalentPoints:
            {
                var preview = GameInfo.PreviewStarterBonusTalentPointReward();
                if (preview.Granted)
                {
                    string names = string.Join(
                        "、",
                        preview.CharacterNames.Where(name => !string.IsNullOrWhiteSpace(name))
                    );
                    sb.Append(
                        I18n.Format(
                            "starter_bonus.random_talent.description",
                            "随机2名队员各获得1点天赋点。\n预览：[color=#cfd6e6]{names}[/color]",
                            ("names", names)
                        )
                    );
                    sb.Append('\n');
                }
                else
                {
                    sb.Append("没有可获得天赋点的角色\n");
                }

                break;
            }
            case StarterBonusOption.TransformTwoCards:
                sb.Append(
                    I18n.Tr(
                        "starter_bonus.transform_two_cards.description",
                        "选择2张卡牌，各变化为同角色随机卡牌。"
                    )
                );
                sb.Append('\n');
                break;
        }

        int electricityCost = GameInfo.GetStarterBonusElectricityCost(
            option?.StarterBonusOption ?? StarterBonusOption.None
        );
        if (electricityCost > 0)
            sb.Append($"[color=#ff6b6b]电力币 {FormatSigned(-electricityCost)}[/color]\n");
    }

    private static string BuildOutcomeSummaryText(
        EventOption option,
        int targetIndex = -1,
        bool randomTarget = false,
        AppliedResourceChanges resourceChanges = default,
        string actionOutcomeText = null
    )
    {
        if (option == null)
            return string.Empty;

        var sb = new StringBuilder(128);
        bool hasAny = false;

        if (option.PropertyChange != null && option.PropertyChange.Count > 0)
        {
            string targetName = GetPlayerDisplayName(targetIndex);
            if (!string.IsNullOrWhiteSpace(targetName))
            {
                sb.Append($"[b]{targetName}[/b]");
                if (randomTarget)
                    sb.Append(" [color=#ffd36b](随机)[/color]");
                sb.Append('\n');
            }

            foreach (var kv in option.PropertyChange)
                sb.Append($"{Skill.GetColoredPropertyLabel(kv.Key)} {FormatSigned(kv.Value)}\n");

            hasAny = true;
        }

        if (!string.IsNullOrWhiteSpace(actionOutcomeText))
        {
            if (hasAny)
                sb.Append('\n');

            sb.Append(actionOutcomeText.TrimEnd());
            sb.Append('\n');
            hasAny = true;
        }

        if (resourceChanges.HasAny)
        {
            if (hasAny)
                sb.Append('\n');

            if (resourceChanges.PartyHealPercent > 0)
                sb.Append($"全体恢复 {resourceChanges.PartyHealPercent}% 最大生命\n");
            if (resourceChanges.ElectricityChange != 0)
                sb.Append($"电力币 {FormatSigned(resourceChanges.ElectricityChange)}\n");
            if (resourceChanges.PropertyChangeElectricityCost != 0)
                sb.Append($"[color=#ff6b6b]电力币 {FormatSigned(-resourceChanges.PropertyChangeElectricityCost)}[/color]\n");

            hasAny = true;
        }

        if (!hasAny)
            return string.Empty;

        string text = sb.ToString().TrimEnd();
        text = GlobalFunction.ColorizeNumbers(text);
        text = GlobalFunction.ColorizeKeywords(text);
        return text;
    }

    private static string GetPlayerDisplayName(int index)
    {
        if (!IsValidPlayerIndex(index))
            return string.Empty;

        var info = GameInfo.PlayerCharacters[index];
        return string.IsNullOrWhiteSpace(info.CharacterName) ? $"角色{index + 1}" : info.CharacterName;
    }

    private static string FormatSigned(int value)
    {
        return value >= 0 ? $"+{value}" : value.ToString();
    }

    private PartState CapturePart(Control node)
    {
        if (node == null)
            return null;

        if (!_partStates.TryGetValue(node, out var part))
        {
            part = new PartState { Node = node };
            _partStates[node] = part;
        }

        part.BasePosition = node.Position;
        part.BaseGlobalPosition = node.GlobalPosition;
        part.BaseTopLevel = node.TopLevel;
        part.BaseAlpha = node.Modulate.A;
        return part;
    }

    private static void SetPartTopLevel(PartState part, bool topLevel)
    {
        if (part?.Node == null || part.Node.TopLevel == topLevel)
            return;

        var globalPos = part.Node.GlobalPosition;
        part.Node.TopLevel = topLevel;
        part.Node.GlobalPosition = globalPos;
    }

    private static void RestorePartTopLevel(PartState part)
    {
        if (part?.Node == null)
            return;
        SetPartTopLevel(part, part.BaseTopLevel);
    }

    private static void SetControlAlpha(Control control, float alpha)
    {
        if (control == null)
            return;
        control.Modulate = control.Modulate with { A = alpha };
    }
}
