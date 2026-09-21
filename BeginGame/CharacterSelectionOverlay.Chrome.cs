using System.Collections.Generic;
using Godot;

/// <summary>
/// Console chrome for the squad-assembly screen: 1px clean lines, column guides,
/// leaving the central hero portrait clear while ensuring columns sit on the clean DimensionalField background.
/// </summary>
public partial class CharacterSelectionOverlay
{
    private bool _chromeBuilt;
    private Control _chromeLayer;
    private readonly List<(Control Column, Control Guide)> _columnGuides = [];

    private void BuildChrome()
    {
        if (_chromeBuilt)
            return;

        _chromeBuilt = true;

        if (Shade != null)
            Shade.Color = new Color(0f, 0f, 0f, 0f);

        _chromeLayer = new Control
        {
            Name = "ChromeLayer",
            AnchorRight = 1f,
            AnchorBottom = 1f,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        AddChild(_chromeLayer);
        if (Shade != null)
            MoveChild(_chromeLayer, Shade.GetIndex() + 1);

        AddHeaderRule();
        TrackColumnGuide(RosterColumn);
        TrackColumnGuide(DossierColumn);
        CallDeferred(nameof(SyncColumnGuides));
    }

    private void AddHeaderRule()
    {
        if (HeaderBlock is not BoxContainer header)
            return;

        var rule = new ColorRect
        {
            Name = "Header1pxRule",
            CustomMinimumSize = new Vector2(160f, 1f),
            Color = new Color(0.78f, 0.80f, 0.82f, 0.35f),
            MouseFilter = MouseFilterEnum.Ignore,
        };
        header.AddChild(rule);
    }

    private void TrackColumnGuide(Control column)
    {
        if (column == null || _chromeLayer == null)
            return;

        var guide = new Control { MouseFilter = MouseFilterEnum.Ignore };
        // 1px top border line
        guide.AddChild(
            new ColorRect
            {
                AnchorRight = 1f,
                OffsetLeft = 16f,
                OffsetTop = 16f,
                OffsetRight = -16f,
                OffsetBottom = 17f,
                Color = new Color(0.78f, 0.80f, 0.82f, 0.18f),
                MouseFilter = MouseFilterEnum.Ignore,
            }
        );
        // 1px left border guide (replacing old gold line)
        guide.AddChild(
            new ColorRect
            {
                OffsetLeft = 16f,
                OffsetTop = 16f,
                OffsetRight = 17f,
                AnchorBottom = 1f,
                OffsetBottom = -16f,
                Color = new Color(0.78f, 0.82f, 0.88f, 0.25f),
                MouseFilter = MouseFilterEnum.Ignore,
            }
        );
        _chromeLayer.AddChild(guide);
        _columnGuides.Add((column, guide));
        column.Resized += SyncColumnGuides;
    }

    private void SyncColumnGuides()
    {
        if (_chromeLayer == null || !GodotObject.IsInstanceValid(_chromeLayer))
            return;

        Vector2 origin = _chromeLayer.GetGlobalRect().Position;
        foreach ((Control column, Control guide) in _columnGuides)
        {
            if (
                !GodotObject.IsInstanceValid(column)
                || !GodotObject.IsInstanceValid(guide)
                || !column.IsInsideTree()
            )
            {
                continue;
            }

            Rect2 rect = column.GetGlobalRect();
            guide.Position = rect.Position - origin;
            guide.Size = rect.Size;
        }
    }
}
