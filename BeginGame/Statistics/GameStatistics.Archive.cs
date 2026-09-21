using System;
using System.Collections.Generic;
using Godot;

public partial class GameStatistics {
    private readonly List<Button> _archiveButtons = new();
    private Tween _archiveChromeTween;
    private const string ArchiveContentPath = "CenterPanel/Detail/Scroll/Content";

    private static string FormatArchiveDuration(long seconds) {
        seconds = Math.Max(0, seconds);
        return $"{seconds / 3600:00}:{seconds / 60 % 60:00}:{seconds % 60:00}";
    }

    private void LocalizeArchiveChrome() {
        GetNode<Label>("CenterPanel/Footer").Text = I18n.Tr("ui.archive.footer", "悬停查看详情 · 右键技能查看卡牌");
        GetNode<Label>(ArchiveContentPath + "/NodeSection/NodeMargin/NodeVBox/NodeTitle").Text = I18n.Tr("ui.archive.route", "01 / 行进路线");
        RelicTitleLabel.Text = I18n.Tr("ui.archive.relics", "02 / 遗物收藏");
        CharacterSelectHeaderLabel.Text = I18n.Tr("ui.archive.characters", "03 / 角色与技能");
        ExitButton.Text = I18n.Tr("ui.archive.back", "返回  ↗");
        PreviousHistoryButton.TooltipText = I18n.Tr("ui.archive.previous", "上一条记录");
        NextHistoryButton.TooltipText = I18n.Tr("ui.archive.next", "下一条记录");
    }

    private void BuildArchiveDirectory() {
        var list = GetNode<VBoxContainer>("CenterPanel/Sidebar/HistoryScroll/HistoryList");
        ClearChildren(list);
        GetNode<ScrollContainer>("CenterPanel/Sidebar/HistoryScroll").ScrollVertical = 0;
        _archiveButtons.Clear();
        var records = GetHistoryRecords();
        GetNode<Label>("CenterPanel/Sidebar/Count").Text = I18n.Format("ui.archive.count", "共 {count} 次航行 · 最近优先", ("count", records.Count));
        if (records.Count == 0) {
            list.AddChild(CreateEmptyLabel(I18n.Tr("ui.statistics.no_records", "暂无记录")));
            return;
        }
        for (int index = records.Count - 1; index >= 0; index--) {
            var record = records[index];
            string outcome = I18n.Tr(record.Victory ? "ui.common.victory" : "ui.common.defeat", record.Victory ? "胜利" : "战败");
            string run = I18n.Format("ui.archive.run", "航行 {number}", ("number", record.RunIndex.ToString("000")));
            var button = new Button {
                Name = "Run_" + index,
                Text = $"{run}   /   {outcome}\n{FormatArchiveDuration(record.SessionPlaySeconds)}",
                CustomMinimumSize = new Vector2(0f, 78f),
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                Alignment = HorizontalAlignment.Left,
                ToggleMode = true,
                ClipText = true,
                FocusMode = Control.FocusModeEnum.All,
            };
            button.AddThemeFontSizeOverride("font_size", 16);
            var selected = new StyleBoxFlat {
                BgColor = new Color(0.80f, 0.87f, 0.90f, 0.055f),
                BorderColor = new Color(0.77f, 0.90f, 0.91f, 0.9f), BorderWidthLeft = 2,
                ContentMarginLeft = 18f, ContentMarginRight = 10f,
                ContentMarginTop = 10f, ContentMarginBottom = 10f,
            };
            button.AddThemeStyleboxOverride("pressed", selected);
            button.SetMeta("history_index", index);
            int captured = index;
            button.Pressed += () => {
                if (_isSwitchingHistoryPage || captured == _selectedHistoryIndex) {
                    RefreshArchiveSelection();
                    return;
                }
                HideTooltip();
                PlayHistoryPageSwitch(captured, Math.Sign(captured - _selectedHistoryIndex));
            };
            list.AddChild(button);
            _archiveButtons.Add(button);
        }
    }

    private void RefreshArchiveSelection() {
        foreach (var button in _archiveButtons)
            button.SetPressedNoSignal(button.GetMeta("history_index").AsInt32() == _selectedHistoryIndex);
    }

    private void RefreshArchiveChrome() {
        RefreshArchiveSelection();
        bool hasRecord = _currentRecord != null;
        GetNode<Label>("CenterPanel/Detail/RecordTitle").Text = hasRecord
            ? I18n.Format("ui.archive.record", "航行档案 / {number}", ("number", _currentRecord.RunIndex.ToString("000")))
            : I18n.Tr("ui.archive.empty_title", "旅程尚未写下");
        GetNode<Label>("CenterPanel/Sidebar/PageIndex").Text = hasRecord
            ? $"{_selectedHistoryIndex + 1:00}  /  {GetHistoryRecords().Count:00}" : "— / —";
        foreach (string section in new[] { "NodeSection", "RelicSection", "SkillSection" })
            GetNode<Control>(ArchiveContentPath + "/" + section).Visible = hasRecord;
        GetNode<ScrollContainer>("CenterPanel/Detail/Scroll").ScrollVertical = 0;
        CallDeferred(nameof(RevealSelectedArchive));
    }

    private void RevealSelectedArchive() {
        foreach (var button in _archiveButtons) {
            if (GodotObject.IsInstanceValid(button) && button.ButtonPressed) {
                GetNode<ScrollContainer>("CenterPanel/Sidebar/HistoryScroll").EnsureControlVisible(button);
                break;
            }
        }
    }

    private void AnimateArchiveChrome(bool opening) {
        _archiveChromeTween?.Kill();
        _archiveChromeTween = CreateTween().SetParallel(true).SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
        foreach (string path in new[] { "FlowCurves", "SeedCopyButton", "ExitButton", "PreviousHistoryButton", "NextHistoryButton" }) {
            var control = GetNode<Control>(path);
            float opacity = path == "FlowCurves" ? 0.13f : 1f;
            if (opening) control.Modulate = control.Modulate with { A = 0f };
            _archiveChromeTween.TweenProperty(control, "modulate:a", opening ? opacity : 0f, opening ? 0.35f : 0.14f);
        }
    }
}
