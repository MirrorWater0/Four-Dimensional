using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Godot;

public partial class EventInterface : Control
{
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
    private Control LeftAccent => field ??= GetNode<Control>("Frame/LeftAccent");
    private Control RightAccent => field ??= GetNode<Control>("Frame/RightAccent");
    private Control Divider => field ??= GetNode<Control>("Frame/Divider");
    private Control ContentRow => field ??= GetNode<Control>("Frame/ContentGroup");
    private Tip OptionTooltip => field ??= GetTree().Root.GetNodeOrNull<Tip>("TipLayer/EventTip");
    private TargetSelectOverlay TargetSelectOverlay =>
        field ??= GetNode<TargetSelectOverlay>("Frame/TargetSelectOverlay");

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
    private string[] _optionTipTexts = Array.Empty<string>();
    private EventOption _pendingTargetOption;

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
        EnterButton.Pressed += OnEnterPressed;
        ExitButton.Pressed += OnExitPressed;
        BindOptionButtons();
        TargetSelectOverlay.CharacterSelected += OnTargetCharacterSelected;
        TargetSelectOverlay.SelectionCanceled += OnTargetSelectionCanceled;
        ApplyEventData(ThisEvent ?? GameEvent.Catalog?.FirstOrDefault());
        CallDeferred(nameof(StartAnimation));
    }

    public override void _ExitTree()
    {
        TargetSelectOverlay.CharacterSelected -= OnTargetCharacterSelected;
        TargetSelectOverlay.SelectionCanceled -= OnTargetSelectionCanceled;
    }

    public async void StartAnimation()
    {
        if (_assembled)
            return;
        _assembled = true;
        await PlayAssembleAnimationAsync();
    }

    public void ApplyEventData(GameEvent gameEvent)
    {
        ThisEvent = gameEvent;
        if (ThisEvent == null)
            return;

        if (TitleLabel != null && !string.IsNullOrWhiteSpace(ThisEvent.EventName))
            TitleLabel.Text = $"事件：{ThisEvent.EventName}";
        if (StoryText != null && !string.IsNullOrWhiteSpace(ThisEvent.Text))
            StoryText.Text = ThisEvent.Text;
        if (SubtitleLabel != null)
            SubtitleLabel.Text = string.IsNullOrWhiteSpace(SubtitleLabel.Text)
                ? "——"
                : SubtitleLabel.Text;

        var options = ThisEvent.Options ?? Array.Empty<EventOption>();
        _optionTipTexts = new string[options.Length];
        for (int i = 0; i < OptionButtons.Count; i++)
        {
            var button = OptionButtons[i];
            bool exists = i < options.Length;
            button.Visible = exists;
            button.Disabled = !exists;
            if (exists)
            {
                button.Text = string.IsNullOrWhiteSpace(options[i].Text)
                    ? $"选项 {i + 1}"
                    : options[i].Text;
                _optionTipTexts[i] = BuildOptionTipText(options[i]);
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
        if (_isTransitioning)
            return;

        HideOptionTip();
        HideTargetSelection();
        _ = PlayCloseAnimationAsync(false);
    }

    private void OnOptionPressed(int optionIndex)
    {
        if (_isTransitioning || TargetSelectOverlay.Visible || ThisEvent?.Options == null)
            return;
        if ((uint)optionIndex >= (uint)ThisEvent.Options.Length)
            return;

        HideOptionTip();
        var option = ThisEvent.Options[optionIndex];
        if (option == null)
            return;

        if (option.PropertyChange != null && option.PropertyChange.Count > 0)
        {
            if (option.RandomChange)
            {
                int randomIndex = PickRandomPlayerIndex();
                if (randomIndex >= 0)
                {
                    ApplyPropertyChangesToPlayer(randomIndex, option.PropertyChange);
                    ShowPropertyHint(randomIndex, option.PropertyChange, true);
                }

                ApplyNonPropertyOptionEffects(option);
                return;
            }

            ShowTargetSelection(option);
            return;
        }

        ApplyNonPropertyOptionEffects(option);
    }

    private void ShowTargetSelection(EventOption option)
    {
        _pendingTargetOption = option;
        var players = GameInfo.PlayerCharacters ?? Array.Empty<PlayerInfoStructure>();
        TargetSelectOverlay.ShowSelection(players, "请选择一名角色作为该选项的目标");
    }

    private void HideTargetSelection()
    {
        _pendingTargetOption = null;
        TargetSelectOverlay.HideSelection();
    }

    private void OnTargetSelectionCanceled()
    {
        _pendingTargetOption = null;
    }

    private void OnTargetCharacterSelected(int index)
    {
        if (_pendingTargetOption == null)
            return;
        if (!IsValidPlayerIndex(index))
            return;

        var option = _pendingTargetOption;
        HideTargetSelection();
        ApplyPropertyChangesToPlayer(index, option.PropertyChange);
        ShowPropertyHint(index, option.PropertyChange, false);
        ApplyNonPropertyOptionEffects(option);
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
                case PropertyType.Speed:
                    info.Speed += kv.Value;
                    break;
                case PropertyType.MaxLife:
                    info.LifeMax += kv.Value;
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
        return new Random().Next(players.Length);
    }

    private void ShowPropertyHint(
        int playerIndex,
        Dictionary<PropertyType, int> changes,
        bool random
    )
    {
        if (!IsValidPlayerIndex(playerIndex) || changes == null || changes.Count == 0)
            return;

        var hint = Buff.HintScene.Instantiate<BuffHintLabel>();
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

        hint.Text = GlobalFunction.ColorizeNumbers(sb.ToString().TrimEnd());
        hint.TargetPosition = new Vector2(960, 640);
        hint.RandomOffset = true;
        AddChild(hint);
    }

    private void ApplyNonPropertyOptionEffects(EventOption option)
    {
        ApplyResourceChanges(option);
        ApplyEquipmentReward(option.EquipmentReward);
        if (option.Exit)
            _ = PlayCloseAnimationAsync(true);
    }

    private void ApplyResourceChanges(EventOption option)
    {
        if (option.TransitionEnergyChange == 0 && option.ElectricityChange == 0)
            return;

        var map = GetTree().Root.GetNodeOrNull<Map>("/root/Map");
        if (map != null && map.PlayerResourceState != null)
        {
            if (option.TransitionEnergyChange != 0)
                map.PlayerResourceState.TransitionEnergy += option.TransitionEnergyChange;
            if (option.ElectricityChange != 0)
                map.PlayerResourceState.ElectricityCoin += option.ElectricityChange;
            return;
        }

        if (option.TransitionEnergyChange != 0)
        {
            GameInfo.TransitionEnergy = Math.Clamp(
                GameInfo.TransitionEnergy + option.TransitionEnergyChange,
                0,
                GameInfo.TransitionEnergyMax
            );
        }

        if (option.ElectricityChange != 0)
            GameInfo.ElectricityCoin += option.ElectricityChange;
    }

    private static void ApplyEquipmentReward(List<Equipment> equipmentReward)
    {
        if (equipmentReward == null || equipmentReward.Count == 0)
            return;

        foreach (var equipment in equipmentReward)
        {
            if (equipment == null)
                continue;
            GameInfo.OwnedEquipments.Add(Equipment.Clone(equipment));
        }
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
        await PlayDisassembleAnimationAsync();
        if (callComplete)
            WhichNode?.Completed();
        QueueFree();
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
            new AssemblyItem(EnterButton, new Vector2(0f, 18f), 0.52f, 0.30f, 0.24f),
            new AssemblyItem(ExitButton, new Vector2(0f, 24f), 0.58f, 0.28f, 0.22f),
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

    private static string BuildOptionTipText(EventOption option)
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
            hasAny = true;
        }

        if (option.TransitionEnergyChange != 0)
        {
            sb.Append($"过渡能量 {FormatSigned(option.TransitionEnergyChange)}\n");
            hasAny = true;
        }

        if (option.ElectricityChange != 0)
        {
            sb.Append($"电力币 {FormatSigned(option.ElectricityChange)}\n");
            hasAny = true;
        }

        if (option.EquipmentReward != null && option.EquipmentReward.Count > 0)
        {
            sb.Append("获得装备：");
            for (int i = 0; i < option.EquipmentReward.Count; i++)
            {
                var equip = option.EquipmentReward[i];
                if (equip == null)
                    continue;
                sb.Append(i == 0 ? equip.DisplayName : $"，{equip.DisplayName}");
            }
            sb.Append('\n');
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
