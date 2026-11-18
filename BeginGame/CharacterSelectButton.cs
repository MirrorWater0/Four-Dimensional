using Godot;

public partial class CharacterSelectButton : Button
{
    private Tween _hoverTween;
    private Control Content => field ??= GetNode<Control>("Content");
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

    public void SetCharacter(Texture2D art, Texture2D fallback, PlayerInfoStructure info, string callsign)
    {
        // The four archive illustrations are landscape plates with the face in the upper centre.
        if (IconRect != null && art != null) {
            Vector2 size = art.GetSize();
            float side = size.Y * 0.56f;
            IconRect.Texture = new AtlasTexture {
                Atlas = art,
                Region = new Rect2((size.X - side) * 0.5f, size.Y * 0.015f, side, side),
                FilterClip = true,
            };
        } else if (IconRect != null) {
            IconRect.Texture = fallback;
        }

        if (NameLabel != null)
            NameLabel.Text = info.CharacterName ?? I18n.Tr("ui.common.character", "Character");
        if (StatsLabel != null)
            StatsLabel.Text = string.IsNullOrEmpty(callsign) ? "OPERATOR" : $"OPERATOR   /   {callsign.ToUpperInvariant()}";
    }

    public void SetHoverMotion(bool hovered) {
        _hoverTween?.Kill();
        _hoverTween = CreateTween().SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
        _hoverTween.TweenProperty(Content, "offset_left", hovered ? 7f : 0f, 0.16f);
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
