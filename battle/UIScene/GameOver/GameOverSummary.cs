using System;
using System.Linq;
using Godot;

public partial class GameOverSummary : CanvasLayer
{
    private const float IntroOffsetY = 24f;
    private const float TypeDurationMin = 1.4f;
    private const float TypeDurationMax = 5.8f;
    private const float TypeSecondsPerCharacter = 0.018f;

    private static readonly PackedScene SummaryScene = GD.Load<PackedScene>(
        "res://battle/UIScene/GameOver/GameOverSummary.tscn"
    );

    private ColorRect BG => field ??= GetNodeOrNull<ColorRect>("BG");
    private Control CenterPanel => field ??= GetNodeOrNull<Control>("CenterPanel");
    private RichTextLabel SummaryText =>
        field ??= GetNodeOrNull<RichTextLabel>("CenterPanel/Margin/VBox/SummaryText");
    private Button ReturnButton =>
        field ??= GetNodeOrNull<Button>("CenterPanel/Margin/VBox/HeaderRow/ReturnButton");
    private Label ResultValue =>
        field ??= GetNodeOrNull<Label>("CenterPanel/Margin/VBox/StatRow/ResultChip/Margin/VBox/Value");
    private Label DurationValue =>
        field ??= GetNodeOrNull<Label>("CenterPanel/Margin/VBox/StatRow/DurationChip/Margin/VBox/Value");
    private Label NodeValue =>
        field ??= GetNodeOrNull<Label>("CenterPanel/Margin/VBox/StatRow/NodeChip/Margin/VBox/Value");
    private Label EnemyValue =>
        field ??= GetNodeOrNull<Label>("CenterPanel/Margin/VBox/StatRow/EnemyChip/Margin/VBox/Value");
    private Label CoinValue =>
        field ??= GetNodeOrNull<Label>("CenterPanel/Margin/VBox/StatRow/CoinChip/Margin/VBox/Value");
    private Label LootValue =>
        field ??= GetNodeOrNull<Label>("CenterPanel/Margin/VBox/StatRow/LootChip/Margin/VBox/Value");

    private Tween _transitionTween;
    private Tween _typeTween;
    private Vector2 _centerPanelBasePosition;

    public static GameOverSummary Show(Node caller)
    {
        var root = caller?.GetTree()?.Root;
        if (root == null || SummaryScene == null)
            return null;

        var existing = root.GetNodeOrNull<GameOverSummary>("GameOverSummary");
        if (existing != null)
        {
            existing.CallDeferred(nameof(Open));
            return existing;
        }

        var summary = SummaryScene.Instantiate<GameOverSummary>();
        summary.Name = "GameOverSummary";
        summary.Layer = 50;
        root.AddChild(summary);
        summary.CallDeferred(nameof(Open));
        return summary;
    }

    public override void _Ready()
    {
        Visible = false;
        _centerPanelBasePosition = CenterPanel?.Position ?? Vector2.Zero;

        if (ReturnButton != null)
            ReturnButton.Pressed += ReturnToTitle;
    }

    public override void _ExitTree()
    {
        _transitionTween?.Kill();
        _typeTween?.Kill();
        base._ExitTree();
    }

    public void Open()
    {
        RefreshSummaryContent();
        Visible = true;
        PlayIntro();
    }

    private void RefreshSummaryContent()
    {
        var record = GameInfo.GetLatestRunHistoryRecord();
        RefreshStatChips(record);
        RefreshSummaryText(record);
    }

    private void RefreshStatChips(RunHistoryRecord record)
    {
        if (record == null)
        {
            SetLabelText(ResultValue, "--");
            SetLabelText(DurationValue, "--:--");
            SetLabelText(NodeValue, "0");
            SetLabelText(EnemyValue, "0 / 0 / 0");
            SetLabelText(CoinValue, "0");
            SetLabelText(LootValue, "0 / 0");
            return;
        }

        SetLabelText(ResultValue, record.Victory ? "胜利" : "战败");
        SetLabelText(DurationValue, FormatDuration(record.SessionPlaySeconds));
        SetLabelText(NodeValue, record.NodesVisited.ToString());
        SetLabelText(
            EnemyValue,
            $"{record.EnemiesDefeated} / {record.EliteDefeated} / {record.BossDefeated}"
        );
        SetLabelText(CoinValue, record.ElectricityCoinGained.ToString());
        SetLabelText(LootValue, $"{CountRelics(record)} / {CountEquipments(record)}");
    }

    private void RefreshSummaryText(RunHistoryRecord record)
    {
        if (SummaryText == null)
            return;

        string text = GameInfo.BuildRunHistoryRecordText(record);
        text = GlobalFunction.ColorizeNumbers(text);
        text = GlobalFunction.ColorizeKeywords(text);
        SummaryText.Text = text;
        SummaryText.VisibleCharacters = -1;
        SummaryText.ScrollToLine(0);
    }

    private void PlayIntro()
    {
        _transitionTween?.Kill();
        _typeTween?.Kill();

        if (BG != null)
            BG.Modulate = BG.Modulate with { A = 0f };

        if (CenterPanel != null)
        {
            CenterPanel.Position = _centerPanelBasePosition + new Vector2(0f, IntroOffsetY);
            CenterPanel.Modulate = CenterPanel.Modulate with { A = 0f };
        }

        if (SummaryText != null)
        {
            SummaryText.VisibleCharacters = 0;
            SummaryText.ScrollToLine(0);
        }

        _transitionTween = CreateTween();
        _transitionTween.SetParallel(true);
        _transitionTween.SetEase(Tween.EaseType.Out);
        _transitionTween.SetTrans(Tween.TransitionType.Cubic);

        if (BG != null)
            _transitionTween.TweenProperty(BG, "modulate:a", 1f, 0.32f);
        if (CenterPanel != null)
        {
            _transitionTween.TweenProperty(CenterPanel, "position", _centerPanelBasePosition, 0.36f);
            _transitionTween.TweenProperty(CenterPanel, "modulate:a", 1f, 0.28f);
        }

        _transitionTween.Chain().TweenCallback(Callable.From(StartTypewriter));
    }

    private void StartTypewriter()
    {
        if (SummaryText == null)
            return;

        SummaryText.VisibleCharacters = 0;
        SummaryText.ScrollToLine(0);

        int totalCharacters = SummaryText.GetTotalCharacterCount();
        if (totalCharacters <= 0)
        {
            SummaryText.VisibleCharacters = -1;
            return;
        }

        double duration = Mathf.Clamp(
            totalCharacters * TypeSecondsPerCharacter,
            TypeDurationMin,
            TypeDurationMax
        );

        _typeTween?.Kill();
        _typeTween = CreateTween();
        _typeTween.SetEase(Tween.EaseType.Out);
        _typeTween.SetTrans(Tween.TransitionType.Linear);
        _typeTween.TweenProperty(SummaryText, "visible_characters", totalCharacters, duration);
        _typeTween.Finished += () =>
        {
            if (SummaryText != null && GodotObject.IsInstanceValid(SummaryText))
                SummaryText.VisibleCharacters = -1;
        };
    }

    private void ReturnToTitle()
    {
        if (ReturnButton != null)
            ReturnButton.Disabled = true;

        var transition = SceneTransitionLayer.Ensure(this);
        transition?.SwitchScene("res://BeginGame/StartInterface.tscn");
        QueueFree();
    }

    private static void SetLabelText(Label label, string text)
    {
        if (label != null)
            label.Text = text;
    }

    private static int CountRelics(RunHistoryRecord record)
    {
        return record?.RelicRecords?.Sum(relic => Math.Max(1, relic?.Count ?? 0)) ?? 0;
    }

    private static int CountEquipments(RunHistoryRecord record)
    {
        return record?.EquipmentRecords?.Sum(equipment => Math.Max(1, equipment?.Count ?? 0)) ?? 0;
    }

    private static string FormatDuration(long totalSeconds)
    {
        var duration = TimeSpan.FromSeconds(Math.Max(0, totalSeconds));
        return duration.TotalHours >= 1
            ? $"{(int)duration.TotalHours:00}:{duration.Minutes:00}:{duration.Seconds:00}"
            : $"{duration.Minutes:00}:{duration.Seconds:00}";
    }
}
