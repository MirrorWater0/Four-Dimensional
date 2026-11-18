using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public partial class Encyclopedia {
    private const string TuningPanelPath = "ScreenMargin/Root/Body/Content/Split/TuningPanel";
    private Control _tuningPanel;
    private OptionButton _tuningCardChoice;
    private VBoxContainer _tuningFields;
    private RichTextLabel _tuningPreview;
    private Label _tuningMessage;
    private Button _tuningApply;
    private Button _tuningSave;
    private Button _tuningRestore;
    private CardTuningResource _tuningDraft;
    private readonly List<string> _tuningCardKeys = new();
    private readonly Dictionary<string, SpinBox> _tuningInputs = new();
    private bool _tuningDraftDirty;

    private void BindTuningEditor() {
        _tuningPanel = GetNode<Control>(TuningPanelPath);
        string root = TuningPanelPath + "/Margin/Root/";
        _tuningCardChoice = GetNode<OptionButton>(root + "CardChoice");
        _tuningFields = GetNode<VBoxContainer>(root + "FieldsScroll/Fields");
        _tuningPreview = GetNode<RichTextLabel>(root + "Preview");
        _tuningMessage = GetNode<Label>(root + "Message");
        _tuningApply = GetNode<Button>(root + "Actions/Apply");
        _tuningSave = GetNode<Button>(root + "Actions/Save");
        _tuningRestore = GetNode<Button>(root + "Restore");
        _tuningSave.Text = SkillTuning.CanSaveProject ? "保存到项目" : "保存到本地";

        foreach (var card in SkillTuning.GetCards()) {
            _tuningCardKeys.Add(card.SkillKey);
            _tuningCardChoice.AddItem($"{card.DisplayName} · {card.SkillKey}");
        }
        _tuningCardChoice.ItemSelected += index => SelectTuningCard(_tuningCardKeys[(int)index], force: true);
        _tuningApply.Pressed += () => CommitTuningDraft(save: false);
        _tuningSave.Pressed += () => CommitTuningDraft(save: true);
        _tuningRestore.Pressed += RestoreTuningDefault;
        if (_tuningCardKeys.Count > 0)
            SelectTuningCard(SkillTuning.ResolveKey(Skill.GetPlayerSkillPool(_selectedSkillCharacter).First()));
    }

    private void SelectTuningCard(string key, bool force = false) {
        if (_tuningDraft?.SkillKey == key && !force)
            return;
        bool discarded = _tuningDraftDirty;
        // Constructing the skill can discover and repair new tuning fields.
        Skill skill = Skill.GetSkill(Enum.Parse<SkillID>(key));
        _tuningDraft = SkillTuning.GetCard(key);
        _tuningDraftDirty = false;
        _tuningCardChoice.Select(_tuningCardKeys.IndexOf(key));
        _tuningInputs.Clear();
        foreach (Node child in _tuningFields.GetChildren()) {
            _tuningFields.RemoveChild(child);
            child.QueueFree();
        }
        bool player = skill != null && IsPlayerTuningCard(skill);
        foreach (var pair in _tuningDraft.Values.OrderBy(pair => pair.Key)) {
            string parameter = pair.Key;
            if (parameter == "EnemyEnergyCost")
                continue;
            if (!player && parameter == "EnergyCost")
                continue;
            var row = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            var label = new Label {
                Text = CardTuningParameters.GetLabel(parameter),
                TooltipText = parameter,
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
                CustomMinimumSize = new Vector2(180, 36),
            };
            label.AddThemeFontSizeOverride("font_size", 15);
            var input = new SpinBox {
                MinValue = CardTuningParameters.GetMinimum(parameter), MaxValue = int.MaxValue,
                Step = 1, Value = pair.Value, CustomMinimumSize = new Vector2(108, 36),
                TooltipText = parameter, Rounded = true,
            };
            input.ValueChanged += value => {
                _tuningDraft.Values[parameter] = checked((int)value);
                _tuningDraftDirty = true;
                _tuningMessage.Text = "已修改，点击应用或保存后生效。";
            };
            row.AddChild(label);
            row.AddChild(input);
            _tuningFields.AddChild(row);
            _tuningInputs.Add(parameter, input);
        }
        _tuningMessage.Text = discarded ? "切换卡牌时已放弃未应用的输入。" : string.Empty;
        RefreshTuningPreview();
    }

    private void CommitTuningDraft(bool save) {
        if (_tuningDraft == null)
            return;
        // Apply the text still being edited, even if the SpinBox has not lost focus yet.
        foreach (var pair in _tuningInputs) {
            pair.Value.Apply();
            _tuningDraft.Values[pair.Key] = checked((int)pair.Value.Value);
        }
        string message;
        bool success = save ? SkillTuning.Save(_tuningDraft, SkillTuning.CanSaveProject, out message)
            : SkillTuning.Apply(_tuningDraft, out message);
        _tuningMessage.Text = message;
        if (!success)
            return;
        _tuningDraftDirty = false;
        RefreshTuningPreview();
    }

    private void RestoreTuningDefault() {
        if (_tuningDraft == null)
            return;
        string key = _tuningDraft.SkillKey;
        if (!SkillTuning.RestoreDefault(key, out string message)) {
            _tuningMessage.Text = message;
            return;
        }
        _tuningDraftDirty = false;
        SelectTuningCard(key, force: true);
        _tuningMessage.Text = message;
    }

    private void RefreshTuningPreview() {
        if (_tuningDraft == null)
            return;
        Skill skill = Skill.GetSkill(Enum.Parse<SkillID>(_tuningDraft.SkillKey));
        if (skill == null)
            return;
        bool player = IsPlayerTuningCard(skill);
        skill.SetPreviewStats(0, 0, 1, isPlayer: player);
        skill.UpdateDescription();
        string costLine = player ? $"费用：{skill.CardEnergyCostText}\n" : string.Empty;
        _tuningPreview.Text = $"[b]{EscapeBbcode(skill.SkillName)}[/b]\n{costLine}{skill.Description}";
    }

    private static bool IsPlayerTuningCard(Skill skill) {
        return Skill.TryGetPlayerCharacterKey(skill.SkillId.Value, out _)
            || skill.IsColorless || skill.SkillId.Value.ToString().StartsWith("Basic") || skill.IsStatusCard;
    }

    private void RefreshTuningEditorRevision() {
        if (_tuningDraft == null)
            return;
        if (_tuningDraftDirty) {
            _tuningMessage.Text = "当前数值已刷新；未应用的输入仍保留。";
            RefreshTuningPreview();
            return;
        }
        string message = _tuningMessage.Text;
        SelectTuningCard(_tuningDraft.SkillKey, force: true);
        _tuningMessage.Text = message;
    }
}
