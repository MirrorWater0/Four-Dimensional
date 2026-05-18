using Godot;

public partial class CharacterSelectButton : Button
{
    private TextureRect IconRect =>
        field ??= GetNodeOrNull<TextureRect>("Content/Row/IconFrame/IconMargin/Icon");
    private Label NameLabel => field ??= GetNodeOrNull<Label>("Content/Row/TextStack/NameLabel");
    private Label StatsLabel => field ??= GetNodeOrNull<Label>("Content/Row/TextStack/StatsLabel");
    private Label SelectedBadge => field ??= GetNodeOrNull<Label>("SelectedBadge");

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Stop;
        FocusMode = FocusModeEnum.None;
        ActionMode = ActionModeEnum.Press;
        IgnoreChildMouseFilters(this);
    }

    public void SetCharacter(Texture2D icon, Texture2D fallbackPortrait, PlayerInfoStructure info)
    {
        if (IconRect != null)
            IconRect.Texture = icon ?? fallbackPortrait;

        if (NameLabel != null)
            NameLabel.Text = info.CharacterName ?? "Character";

        if (StatsLabel != null)
            StatsLabel.Text =
                $"生命 {info.LifeMax}  力量 {info.Power}  生存 {info.Survivability}  速度 {info.Speed}";
    }

    public void SetBadge(bool visible, string text)
    {
        if (SelectedBadge == null)
            return;

        SelectedBadge.Visible = visible;
        SelectedBadge.Text = visible ? text : string.Empty;
    }

    private static void IgnoreChildMouseFilters(Node parent)
    {
        if (parent == null)
            return;

        foreach (Node child in parent.GetChildren())
        {
            if (child is Control control)
                control.MouseFilter = MouseFilterEnum.Ignore;

            IgnoreChildMouseFilters(child);
        }
    }
}
