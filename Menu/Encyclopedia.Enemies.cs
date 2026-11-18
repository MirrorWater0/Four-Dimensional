using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public partial class Encyclopedia {
    private const string EnemyPanelPath = "ScreenMargin/Root/Body/Content/Split/EnemyPanel";
    private Control _enemyPanel;
    private TextureRect _enemyPortrait;
    private RichTextLabel _enemyDetail;
    private HFlowContainer _enemyEditSections;
    private Label _enemyEditHeading;
    private VBoxContainer _enemyFields;
    private Label _enemyMessage;
    private Button _enemyApply;
    private Button _enemySave;
    private Button _enemyRestore;
    private EnemyTuningResource _enemyDraft;
    private CardTuningResource _enemySkillDraft;
    private string _enemyKey;
    private bool _enemyDraftDirty;
    private int _observedEnemyTuningRevision;
    private int _enemyEditSection;
    private readonly List<SkillID> _enemyEditSkillIds = new();
    private readonly List<Button> _enemyEditButtons = new();
    private readonly Dictionary<string, SpinBox> _enemyInputs = new();

    private static SkillID[] GetEnemySkillIds(EnemyRegedit definition) =>
        definition.GetAllSkillIds().Where(Skill.IsSkillRegistered).ToArray();

    private void BindEnemyEditor() {
        _enemyPanel = GetNode<Control>(EnemyPanelPath);
        string root = EnemyPanelPath + "/Margin/Root/";
        _enemyPortrait = GetNode<TextureRect>(root + "Overview/Portrait");
        _enemyDetail = GetNode<RichTextLabel>(root + "Overview/Detail");
        _enemyEditSections = GetNode<HFlowContainer>(root + "EditSections");
        _enemyEditHeading = GetNode<Label>(root + "EditHeading");
        _enemyFields = GetNode<VBoxContainer>(root + "FieldsScroll/Fields");
        _enemyMessage = GetNode<Label>(root + "Message");
        _enemyApply = GetNode<Button>(root + "Actions/Apply");
        _enemySave = GetNode<Button>(root + "Actions/Save");
        _enemyRestore = GetNode<Button>(root + "Restore");
        GetNode<Label>(root + "Heading").Text = I18n.Tr("ui.encyclopedia.enemy.editor_heading", "敌人档案与数值");
        GetNode<Label>(root + "Hint").Text = I18n.Tr("ui.encyclopedia.enemy.editor_hint",
            "点击下方技能名称编辑伤害、层数等数值。\n应用后生效，保存后下次启动仍有效。");
        _enemySave.Text = EnemyTuning.CanSaveProject ? "保存到项目" : "保存到本地";
        _enemyApply.Pressed += () => CommitEnemyDraft(save: false);
        _enemySave.Pressed += () => CommitEnemyDraft(save: true);
        _enemyRestore.Pressed += RestoreEnemyDraft;
    }

    private void SelectEnemyEntry(EncyclopediaEntry entry) {
        _enemyDetail.Text = entry?.Detail ?? "没有找到匹配的敌人。";
        bool available = entry?.EnemyKey != null;
        _enemyApply.Disabled = _enemySave.Disabled = _enemyRestore.Disabled = !available;
        if (_enemyKey == entry?.EnemyKey)
            return;

        bool discarded = _enemyDraftDirty;
        _enemyKey = entry?.EnemyKey;
        _enemyDraft = null;
        _enemySkillDraft = null;
        _enemyDraftDirty = false;
        foreach (Node child in _enemyEditSections.GetChildren()) {
            _enemyEditSections.RemoveChild(child);
            child.QueueFree();
        }
        _enemyEditButtons.Clear();
        _enemyEditSection = 0;
        _enemyEditSkillIds.Clear();
        _enemyPortrait.Texture = null;
        ClearEnemyFields();
        if (!available) {
            _enemyEditHeading.Text = string.Empty;
            _enemyMessage.Text = string.Empty;
            return;
        }

        EnemyRegedit definition = EnemyCatalog.Create(_enemyKey);
        if (!string.IsNullOrEmpty(definition.PortaitPath) && ResourceLoader.Exists(definition.PortaitPath))
            _enemyPortrait.Texture = GD.Load<Texture2D>(definition.PortaitPath);
        AddEnemyEditButton(I18n.Tr("ui.encyclopedia.enemy.base_and_passive", "基础属性与被动"));
        foreach (SkillID id in GetEnemySkillIds(definition)) {
            _enemyEditSkillIds.Add(id);
            AddEnemyEditButton(SkillTuning.GetCard(id).DisplayName);
        }
        SelectEnemyEditSection(0);
        if (discarded)
            _enemyMessage.Text = "切换敌人时已放弃未应用的输入。";
    }

    private void AddEnemyEditButton(string text) {
        int index = _enemyEditButtons.Count;
        Button button = CreateButton(text, new Vector2(0, 36));
        button.ClipText = false;
        button.SizeFlagsHorizontal = SizeFlags.Fill;
        button.AddThemeFontSizeOverride("font_size", 15);
        button.Pressed += () => SelectEnemyEditSection(index);
        _enemyEditSections.AddChild(button);
        _enemyEditButtons.Add(button);
    }

    private void ClearEnemyFields() {
        _enemyInputs.Clear();
        foreach (Node child in _enemyFields.GetChildren()) {
            _enemyFields.RemoveChild(child);
            child.QueueFree();
        }
    }

    private void SelectEnemyEditSection(int index) {
        if (_enemyKey == null)
            return;
        bool discarded = _enemyDraftDirty;
        _enemyDraftDirty = false;
        _enemyEditSection = index;
        for (int i = 0; i < _enemyEditButtons.Count; i++)
            _enemyEditButtons[i].Disabled = i == index;
        _enemyDraft = index == 0 ? EnemyTuning.GetEnemy(_enemyKey) : null;
        _enemySkillDraft = index > 0 ? SkillTuning.GetCard(_enemyEditSkillIds[index - 1]) : null;
        _enemyEditHeading.Text = index == 0
            ? I18n.Tr("ui.encyclopedia.enemy.base_and_passive", "基础属性与被动")
            : I18n.Format("ui.encyclopedia.enemy.skill_values", "技能数值 · {name}",
                ("name", _enemySkillDraft.DisplayName));
        ClearEnemyFields();
        var values = _enemyDraft?.Values ?? _enemySkillDraft.Values;
        foreach (var pair in values.OrderBy(pair => _enemyDraft != null ? EnemyTuningParameters.GetSortOrder(pair.Key) : 0)
            .ThenBy(pair => pair.Key)) {
            string parameter = pair.Key;
            if (_enemySkillDraft != null && (parameter == "EnergyCost" || parameter == "EnemyEnergyCost"))
                continue;
            bool stats = _enemyDraft != null;
            var row = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            var label = new Label {
                Text = stats ? EnemyTuningParameters.GetLabel(parameter) : CardTuningParameters.GetLabel(parameter),
                TooltipText = parameter, SizeFlagsHorizontal = SizeFlags.ExpandFill,
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
                CustomMinimumSize = new Vector2(180, 36),
            };
            label.AddThemeFontSizeOverride("font_size", 15);
            var input = new SpinBox {
                MinValue = stats ? EnemyTuningParameters.GetMinimum(parameter) : CardTuningParameters.GetMinimum(parameter),
                MaxValue = int.MaxValue, Step = 1, Value = pair.Value, Rounded = true,
                CustomMinimumSize = new Vector2(108, 36), TooltipText = parameter,
            };
            input.ValueChanged += value => {
                values[parameter] = checked((int)value);
                _enemyDraftDirty = true;
                _enemyMessage.Text = "已修改，点击应用或保存后生效。";
            };
            row.AddChild(label);
            row.AddChild(input);
            _enemyFields.AddChild(row);
            _enemyInputs.Add(parameter, input);
        }
        _enemyMessage.Text = discarded ? "切换编辑项时已放弃未应用的输入。" : string.Empty;
    }

    private void CommitEnemyDraft(bool save) {
        if (_enemyKey == null)
            return;
        var values = _enemyDraft?.Values ?? _enemySkillDraft.Values;
        foreach (var pair in _enemyInputs) {
            pair.Value.Apply();
            values[pair.Key] = checked((int)pair.Value.Value);
        }
        string message;
        bool success;
        if (_enemyDraft != null)
            success = save ? EnemyTuning.Save(_enemyDraft, EnemyTuning.CanSaveProject, out message)
                : EnemyTuning.Apply(_enemyDraft, out message);
        else
            success = save ? SkillTuning.Save(_enemySkillDraft, SkillTuning.CanSaveProject, out message)
                : SkillTuning.Apply(_enemySkillDraft, out message);
        if (success) {
            _enemyDraftDirty = false;
            RefreshEnemyTuningCatalog();
        }
        _enemyMessage.Text = message;
    }

    private void RestoreEnemyDraft() {
        if (_enemyKey == null)
            return;
        string message;
        bool success = _enemyDraft != null ? EnemyTuning.RestoreDefault(_enemyKey, out message)
            : SkillTuning.RestoreDefault(_enemySkillDraft.SkillKey, out message);
        if (success) {
            _enemyDraftDirty = false;
            RefreshEnemyTuningCatalog();
        }
        _enemyMessage.Text = message;
    }

    private void RefreshEnemyTuningCatalog() {
        _entries[EncyclopediaModule.Enemies] = BuildEnemyEntries();
        if (_currentModule != EncyclopediaModule.Enemies)
            return;
        string selectedKey = _enemyKey;
        string message = _enemyMessage.Text;
        RefreshResults();
        if (_enemyKey == null)
            return;
        if (!_enemyDraftDirty) {
            SelectEnemyEditSection(_enemyEditSection);
            if (_enemyKey == selectedKey)
                _enemyMessage.Text = message;
        }
        else
            _enemyMessage.Text = "当前数值已刷新；未应用的输入仍保留。";
    }
}
