using Godot;

[Tool]
public partial class WorldLineProbabilityRow : HBoxContainer {
    [Export]
    public LevelNode.LevelType EncounterType { get; set; }

    private ProgressBar _bar;
    private Label _value;
    private Label _delta;
    private Label _name;
    private ShaderMaterial _icon;
    private StyleBoxFlat _fill;
    private Tween _valueTween;
    private float _displayedValue;

    public override void _Ready() {
        _bar = GetNode<ProgressBar>("Bar");
        _value = GetNode<Label>("Value");
        _delta = GetNode<Label>("Delta");
        _name = GetNode<Label>("Name");
        _icon = (ShaderMaterial)GetNode<ColorRect>("Icon").Material;
        _fill = (StyleBoxFlat)_bar.GetThemeStylebox("fill").Duplicate();
        _bar.AddThemeStyleboxOverride("fill", _fill);
        _name.Text = EncounterType switch {
            LevelNode.LevelType.Event => "事件",
            LevelNode.LevelType.Shop => "商店",
            LevelNode.LevelType.Rest => "休息",
            LevelNode.LevelType.Elite => "精英",
            LevelNode.LevelType.Treasure => "宝箱",
            _ => "战斗",
        };
        int initial = EncounterType == LevelNode.LevelType.Normal ? 100 : 0;
        ShowProbability(initial, initial, new Color("#AACFEC"), false);
    }

    public void ShowProbability(int probability, int currentProbability, Color color, bool animate, float delay = 0f) {
        _valueTween?.Kill();
        _value.Text = $"{probability}%";
        _icon.SetShaderParameter("ring_color", color);
        _fill.BgColor = new Color(color, 0.85f);
        int difference = probability - currentProbability;
        _delta.Text = difference == 0 ? "" : $"{difference:+0;-0}";
        _delta.AddThemeColorOverride("font_color", difference > 0
            ? new Color("#A8C9D8") : new Color("#9AA8B8"));
        _delta.TooltipText = difference == 0 ? "与当前世界线相同"
            : $"比当前世界线{(difference > 0 ? "高" : "低")} {System.Math.Abs(difference)} 个百分点";
        string encounterName = EncounterType == LevelNode.LevelType.Normal ? "普通战斗" : _name.Text;
        TooltipText = $"{encounterName}：{probability}%\n{_delta.TooltipText}";

        void SetValue(float value) {
            _displayedValue = value;
            _bar.Value = value;
        }
        // Opening protection must never briefly display a nonzero elite chance.
        if (!animate || probability == 0 || Engine.IsEditorHint()) {
            SetValue(probability);
            return;
        }
        _valueTween = CreateTween();
        _valueTween.TweenMethod(Callable.From<float>(SetValue), _displayedValue, (float)probability, 0.32f)
            .SetDelay(delay).SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
    }
}
