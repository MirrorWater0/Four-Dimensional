using System.Collections.Generic;
using Godot;

/// <summary>
/// Miniature related-card previews anchored to the hovered <see cref="SkillCard"/> (left side).
/// </summary>
public partial class SkillRelatedCardPreview : Control
{
    public const float PreviewScale = 0.9f;
    private const float PreviewHoverScaleInfluence = 1f;
    private const float CardSeparation = 6f;
    private const float HostGap = 14f;
    private const float ViewportMargin = 10f;

    private VBoxContainer _cardColumn;
    private SkillCard _hostCard;
    private readonly List<SkillCard> _cards = new();
    private readonly Queue<SkillCard> _cardPool = new();
    private string _activePreviewKey = string.Empty;

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        ZIndex = 25;

        _cardColumn = new VBoxContainer
        {
            MouseFilter = MouseFilterEnum.Ignore,
        };
        _cardColumn.AddThemeConstantOverride("separation", (int)CardSeparation);
        AddChild(_cardColumn);
        Visible = false;
    }

    public override void _Process(double delta)
    {
        if (!Visible)
            return;

        SyncLayout();
    }

    public void ShowPreviews(
        IReadOnlyList<SkillID> skillIds,
        Skill sourceSkill,
        SkillCard hostCard
    )
    {
        string previewKey = BuildPreviewKey(skillIds, sourceSkill, hostCard);
        if (Visible && _hostCard == hostCard && _activePreviewKey == previewKey)
        {
            SyncLayout();
            return;
        }

        _hostCard = hostCard;
        _activePreviewKey = previewKey;
        ReleaseActiveCards();

        if (skillIds == null || skillIds.Count == 0)
        {
            Visible = false;
            return;
        }

        foreach (SkillID skillId in skillIds)
        {
            Skill previewSkill = CreatePreviewSkill(skillId, sourceSkill, hostCard);
            if (previewSkill == null)
                continue;

            SkillCard card = TakePreviewCard();
            if (card == null)
                continue;

            if (card.GetParent() != _cardColumn)
                _cardColumn.AddChild(card);
            else
                _cardColumn.MoveChild(card, _cardColumn.GetChildCount() - 1);
            ConfigurePreviewCard(card, previewSkill, hostCard);
            _cards.Add(card);
        }

        if (_cards.Count == 0)
        {
            Visible = false;
            return;
        }

        FinalizeLayout();
    }

    public void HidePreviews()
    {
        _hostCard = null;
        _activePreviewKey = string.Empty;
        ReleaseActiveCards();
        Visible = false;
    }

    private void FinalizeLayout()
    {
        if (_cards.Count == 0)
        {
            Visible = false;
            return;
        }

        Visible = true;
        SyncLayout();
    }

    private void SyncLayout()
    {
        if (_hostCard == null || !GodotObject.IsInstanceValid(_hostCard))
            return;

        Viewport viewport = GetViewport();
        if (viewport == null)
            return;

        Vector2 viewportSize = viewport.GetVisibleRect().Size;
        Rect2 hostRect = GetControlCanvasRect(_hostCard);
        float previewScale = GetPreviewScaleForHost(_hostCard);
        ApplyPreviewScale(previewScale);
        Vector2 previewSize = GetPreviewSize(previewScale);
        bool previewsOnRight = HasRightSpace(hostRect, previewSize.X, viewportSize.X);
        if (!previewsOnRight && !HasLeftSpace(hostRect, previewSize.X))
            previewsOnRight = hostRect.GetCenter().X <= viewportSize.X * 0.5f;

        Vector2 previewPosition = previewsOnRight
            ? new Vector2(hostRect.End.X + HostGap, hostRect.Position.Y)
            : new Vector2(hostRect.Position.X - previewSize.X - HostGap, hostRect.Position.Y);

        previewPosition.Y = ClampVertical(previewPosition.Y, previewSize.Y, viewportSize.Y);
        GlobalPosition = new Vector2(
            ClampHorizontal(previewPosition.X, previewSize.X, viewportSize.X),
            previewPosition.Y
        );
    }

    private static Rect2 GetControlCanvasRect(Control control)
    {
        Transform2D transform = control.GetGlobalTransformWithCanvas();
        Vector2 size = control.Size;
        Vector2[] points =
        [
            transform * Vector2.Zero,
            transform * new Vector2(size.X, 0f),
            transform * size,
            transform * new Vector2(0f, size.Y),
        ];

        Vector2 min = points[0];
        Vector2 max = points[0];
        for (int i = 1; i < points.Length; i++)
        {
            min = new Vector2(Mathf.Min(min.X, points[i].X), Mathf.Min(min.Y, points[i].Y));
            max = new Vector2(Mathf.Max(max.X, points[i].X), Mathf.Max(max.Y, points[i].Y));
        }

        return new Rect2(min, max - min);
    }

    private static float GetPreviewScaleForHost(SkillCard hostCard)
    {
        Vector2 baseScale = hostCard.ConfiguredDisplayScale;
        float baseMagnitude = Mathf.Max(0.001f, (Mathf.Abs(baseScale.X) + Mathf.Abs(baseScale.Y)) * 0.5f);
        float currentMagnitude =
            (Mathf.Abs(hostCard.Scale.X) + Mathf.Abs(hostCard.Scale.Y)) * 0.5f;
        float hoverMultiplier = Mathf.Max(0.01f, currentMagnitude / baseMagnitude);
        return PreviewScale * Mathf.Lerp(1f, hoverMultiplier, PreviewHoverScaleInfluence);
    }

    private void ApplyPreviewScale(float previewScale)
    {
        Vector2 targetScale = Vector2.One * previewScale;
        if (_cardColumn.Scale.DistanceSquaredTo(targetScale) < 0.000001f)
            return;

        _cardColumn.Scale = targetScale;
    }

    private Vector2 GetPreviewSize(float previewScale)
    {
        float maxWidth = 0f;
        float totalHeight = 0f;
        for (int i = 0; i < _cards.Count; i++)
        {
            Vector2 displaySize = GetCardDisplaySize(_cards[i]);
            maxWidth = Mathf.Max(maxWidth, displaySize.X);
            totalHeight += displaySize.Y;
            if (i > 0)
                totalHeight += CardSeparation;
        }

        Vector2 unscaledSize = new(maxWidth, totalHeight);
        Size = unscaledSize * previewScale;
        _cardColumn.Size = unscaledSize;
        return Size;
    }

    private static bool HasRightSpace(Rect2 hostRect, float previewWidth, float viewportWidth) =>
        hostRect.End.X + HostGap + previewWidth <= viewportWidth - ViewportMargin;

    private static bool HasLeftSpace(Rect2 hostRect, float previewWidth) =>
        hostRect.Position.X - HostGap - previewWidth >= ViewportMargin;

    private static float ClampVertical(float y, float height, float viewportHeight)
    {
        float maxY = viewportHeight - height - ViewportMargin;
        return Mathf.Clamp(y, ViewportMargin, Mathf.Max(ViewportMargin, maxY));
    }

    private static float ClampHorizontal(float x, float width, float viewportWidth)
    {
        float maxX = viewportWidth - width - ViewportMargin;
        return Mathf.Clamp(x, ViewportMargin, Mathf.Max(ViewportMargin, maxX));
    }

    private static Vector2 GetCardDisplaySize(SkillCard card) => CardPileOverlayUi.CardBaseSize;

    private static Skill CreatePreviewSkill(SkillID skillId, Skill sourceSkill, SkillCard hostCard)
    {
        Skill previewSkill = Skill.GetSkill(skillId);
        if (previewSkill == null)
            return null;

        previewSkill.OwnerCharater =
            sourceSkill?.OwnerCharater ?? hostCard?.CurrentSkill?.OwnerCharater;
        return previewSkill;
    }

    private static void ConfigurePreviewCard(SkillCard card, Skill previewSkill, SkillCard hostCard)
    {
        if (card == null || !GodotObject.IsInstanceValid(card) || previewSkill == null)
            return;

        card.PreviewCharacterKey = hostCard?.PreviewCharacterKey;
        card.PreviewCharacterName = hostCard?.PreviewCharacterName;
        card.HoverUiEnabled = false;
        card.UseDefaultHoverEffect = false;
        card.AutoPressEffect = false;
        card.MouseFilter = MouseFilterEnum.Ignore;
        card.Scale = Vector2.One;
        card.CustomMinimumSize = CardPileOverlayUi.CardBaseSize;
        card.Size = CardPileOverlayUi.CardBaseSize;

        if (card.Button != null)
            card.Button.MouseFilter = MouseFilterEnum.Ignore;

        card.SetSkill(previewSkill);
        card.Visible = true;
    }

    private SkillCard TakePreviewCard()
    {
        SkillCard card = null;
        while (_cardPool.Count > 0)
        {
            card = _cardPool.Dequeue();
            if (card != null && GodotObject.IsInstanceValid(card))
                break;

            card = null;
        }

        if (card == null)
            card = CardPileOverlayUi.SkillCardScene.Instantiate<SkillCard>();

        return card;
    }

    private void ReleaseActiveCards()
    {
        for (int i = 0; i < _cards.Count; i++)
        {
            SkillCard card = _cards[i];
            if (!GodotObject.IsInstanceValid(card))
                continue;

            card.Visible = false;
            card.SetSkill(null);
            card.PreviewCharacterKey = null;
            card.PreviewCharacterName = null;
            _cardPool.Enqueue(card);
        }

        _cards.Clear();
    }

    private static string BuildPreviewKey(
        IReadOnlyList<SkillID> skillIds,
        Skill sourceSkill,
        SkillCard hostCard
    )
    {
        if (skillIds == null || skillIds.Count == 0)
            return string.Empty;

        ulong ownerId = 0;
        Character owner = sourceSkill?.OwnerCharater ?? hostCard?.CurrentSkill?.OwnerCharater;
        if (owner != null && GodotObject.IsInstanceValid(owner))
            ownerId = owner.GetInstanceId();

        string characterKey = hostCard?.PreviewCharacterKey ?? string.Empty;
        string characterName = hostCard?.PreviewCharacterName ?? string.Empty;
        return $"{ownerId}|{characterKey}|{characterName}|{string.Join(',', skillIds)}";
    }
}
