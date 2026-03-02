using Godot;
using System.Threading.Tasks;

public partial class CardSlot : PanelContainer
{
    [Signal]
    public delegate void ClickedEventHandler();
    public Label label => field ??= GetNodeOrNull<Label>("Card1Label") ?? GetNodeOrNull<Label>("Label");

    public override void _GuiInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true })
            EmitSignal(SignalName.Clicked);
    }

    public override void _Ready()
    {
        if (label != null)
            label.MouseFilter = MouseFilterEnum.Ignore;

        Clicked += Select;
    }

    public void Select()
    {
        var styleBox = GetThemeStylebox("panel").Duplicate() as StyleBoxFlat;
        styleBox.BorderColor = Colors.Red;
        AddThemeStyleboxOverride("panel", styleBox);
    }

    public void Unselect()
    {
        var styleBox = GetThemeStylebox("panel").Duplicate() as StyleBoxFlat;
        styleBox.BorderColor = new Color("#a7d6ff52");
        AddThemeStyleboxOverride("panel", styleBox);
    }

    public async Task PlayRemoveAnimation(float moveDistance, float duration)
    {
        if (!Visible)
            return;

        Vector2 baseGlobal = GlobalPosition;
        bool originalTopLevel = TopLevel;
        SetTopLevelPreservePosition(true);
        SetAlpha(1.0f);

        var tween = CreateTween();
        tween.SetParallel(true);
        tween.SetEase(Tween.EaseType.In);
        tween.SetTrans(Tween.TransitionType.Cubic);
        tween.TweenProperty(this, "global_position", baseGlobal + new Vector2(moveDistance, 0), duration);
        tween.TweenProperty(this, "modulate:a", 0.0f, duration * 0.95f);

        await ToSignal(tween, Tween.SignalName.Finished);

        SetTopLevelPreservePosition(originalTopLevel);
        GlobalPosition = baseGlobal;
        SetAlpha(1.0f);
    }

    public async Task PlayInsertAnimation(float moveDistance, float duration)
    {
        if (!Visible)
            return;

        Vector2 baseGlobal = GlobalPosition;
        bool originalTopLevel = TopLevel;
        SetTopLevelPreservePosition(true);
        GlobalPosition = baseGlobal - new Vector2(moveDistance, 0);
        SetAlpha(0.0f);

        var tween = CreateTween();
        tween.SetParallel(true);
        tween.SetEase(Tween.EaseType.Out);
        tween.SetTrans(Tween.TransitionType.Cubic);
        tween.TweenProperty(this, "global_position", baseGlobal, duration);
        tween.TweenProperty(this, "modulate:a", 1.0f, duration * 0.95f);

        await ToSignal(tween, Tween.SignalName.Finished);

        SetTopLevelPreservePosition(originalTopLevel);
        GlobalPosition = baseGlobal;
        SetAlpha(1.0f);
    }

    private void SetTopLevelPreservePosition(bool topLevel)
    {
        if (TopLevel == topLevel)
            return;

        Vector2 globalPos = GlobalPosition;
        TopLevel = topLevel;
        GlobalPosition = globalPos;
    }

    private void SetAlpha(float alpha)
    {
        Modulate = Modulate with { A = alpha };
    }
}
