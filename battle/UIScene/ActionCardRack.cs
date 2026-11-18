using System;
using System.Threading;
using System.Threading.Tasks;
using Godot;

/// <summary>Compact display for installed cards. SetCards supplies UI data only; the rack never executes skills.</summary>

public partial class ActionCardRack : Control {
    public sealed record CardEntry(Skill Skill, string Title = null, string Description = null, Texture2D Art = null);

    [ExportGroup("Card Layout")]
    [Export(PropertyHint.Range, "0.1,0.8,0.01")] public float CardScale { get; set; } = 0.29f;
    [Export(PropertyHint.Range, "1,240,1")] public float CardStep { get; set; } = 48;
    [Export] public bool FitCardsToWidth { get; set; } = true;
    [Export] public Vector2 ContentOffset { get; set; } = Vector2.Zero;
    [Export] public bool ShowShortNames { get; set; } = false;
    [Export(PropertyHint.Range, "8,32,1")] public int ShortNameFontSize { get; set; } = 13;
    [Export] public float ShortNameGap { get; set; } = 0;
    [Export] public float ShortNameHeight { get; set; } = 18;
    [Export(PropertyHint.Range, "0,1,0.01")] public float LayoutMoveDuration { get; set; } = 0.24f;
    [ExportGroup("Hover and Preview")]
    [Export] public float HoverLift { get; set; } = 10;
    [Export(PropertyHint.Range, "0,1,0.01")] public float HoverDuration { get; set; } = 0.16f;
    [Export] public Vector2 PreviewOffset { get; set; } = Vector2.Zero;
    [Export(PropertyHint.Range, "0.3,1.5,0.01")] public float PreviewScale { get; set; } = 1;
    [Export] public float PreviewHintGap { get; set; } = 5;
    [ExportGroup("Editor Preview")]
    [Export(PropertyHint.Range, "0,5,1")]
    public int PreviewCardCount { get; set; } = 3;
    [Export] public string OwnerKey { get; set; } = "Echo";
    [Export] public string OwnerName { get; set; } = "角色";

    public int HoveredIndex { get; private set; } = -1;
    public int CardCount { get; private set; }
    public event Action<int> CardPressed;

    private CardEntry[] _entries;
    private readonly Control[] _slots = new Control[5];
    private readonly Tween[] _tweens = new Tween[5];
    private readonly Tween[] _installationTweens = new Tween[5];
    private int _installingIndex = -1;
    private readonly Vector2[] _positions = new Vector2[5];
    private readonly SkillCard[] _cards = new SkillCard[5];
    private readonly Control[] _cardControls = new Control[5];
    private SkillCard _detailCard;
    private Control _detail;
    private int _editorCount = -1;

    public CharacterControl HandControl { get; set; }
    public bool HideEnergyCost { get; set; }
    private Vector2 _pointerPosition = new(-10000, -10000);
    private float CardWidth => 240 * Math.Max(0.1f, CardScale);
    private float CardHeight => 370 * Math.Max(0.1f, CardScale) + (ShowShortNames ? ShortNameGap + ShortNameHeight : 0);
    private int _layoutHash;
    private bool _layoutInitialized;
    private bool _uninstallPreview;
    public bool IsChoosingReplacement => false;

    public override void _Ready() {
        GetNode<CanvasLayer>("DetailLayer").Layer = GetCanvasLayerNode()?.Layer ?? 1;
        _detail = GetNode<Control>("DetailLayer/Detail");
        _detail.Visible = false;
        GetNode<Label>("DetailLayer/Detail/Hint").Visible = false;
        if (!Engine.IsEditorHint()) {
            _detailCard = _detail.GetNode<SkillCard>("Card");
            ConfigureCard(_detailCard, PreviewScale);
        }
        for (int i = 0; i < _slots.Length; i++) {
            _slots[i] = GetNode<Control>($"Cards/Card{i}");
            _cardControls[i] = _slots[i].GetNode<Control>("Card");
            // Non-Tool C# scripts have placeholders in the editor, so access their native Control type.
            if (!Engine.IsEditorHint()) {
                _cards[i] = _slots[i].GetNode<SkillCard>("Card");
                ConfigureCard(_cards[i], CardScale);
            }
        }
        if (_entries != null)
            SetCards(_entries);
        else
            ApplyLayout(PreviewCardCount);
    }

    public void SetCards(CardEntry[] entries) {
        if (entries == null)
            throw new ArgumentNullException(nameof(entries));
        if (entries.Length > _slots.Length)
            throw new ArgumentOutOfRangeException(nameof(entries), "This compact rack supports at most five cards.");
        // A flight can overlap effect callbacks that remove or replace its installed card.
        if (_installingIndex >= 0 && (_entries == null || _installingIndex >= _entries.Length
            || _installingIndex >= entries.Length || !IsSameCard(entries[_installingIndex], _entries[_installingIndex])))
            _installingIndex = -1;
        // Capture rendered positions by card identity, before indices change after an insertion/removal.
        // An interrupted transition resumes from its current position, never its previous destination.
        var previousPositions = new Vector2?[entries.Length];
        if (IsNodeReady() && _entries != null) {
            var used = new bool[_entries.Length];
            for (int i = 0; i < entries.Length; i++)
                for (int oldIndex = 0; oldIndex < _entries.Length; oldIndex++) {
                    if (used[oldIndex] || !IsSameCard(entries[i], _entries[oldIndex]))
                        continue;
                    previousPositions[i] = _slots[oldIndex].Position;
                    used[oldIndex] = true;
                    break;
                }
        }
        if (IsNodeReady())
            SetHovered(-1);
        if (IsNodeReady() && _entries != null)
            for (int i = 0; i < _slots.Length; i++)
                if (i >= entries.Length || i >= _entries.Length || !IsSameCard(entries[i], _entries[i]))
                    _installationTweens[i]?.Kill();
        _entries = (CardEntry[])entries.Clone();
        if (!IsNodeReady())
            return;
        for (int i = 0; i < entries.Length; i++) {
            ApplyCard(_cards[i], entries[i]);
            _slots[i].GetNode<Label>("ShortTitle").Text = entries[i].Title ?? entries[i].Skill?.SkillName ?? "";
        }
        ApplyLayout(entries.Length, previousPositions);
        if (_uninstallPreview)
            UpdateUninstallPreviewVisual();
    }

    private static bool IsSameCard(CardEntry first, CardEntry second) => ReferenceEquals(first, second)
        || (first.Skill != null && ReferenceEquals(first.Skill, second.Skill))
        // The static UI study recreates placeholder skills; its titles identify the sample cards.
        || (first.Skill?.SkillId == null && second.Skill?.SkillId == null
            && !string.IsNullOrEmpty(first.Title) && first.Title == second.Title);

    private void ApplyLayout(int count, Vector2?[] previousPositions = null) {
        CardCount = Math.Clamp(count, 0, _slots.Length);
        SetHovered(-1);
        float step = CardCount > 1 ? Math.Max(1, CardStep) : 0;
        if (FitCardsToWidth && CardCount > 1)
            step = Math.Min(step, Math.Max(1, (Size.X - CardWidth) / (CardCount - 1)));
        float width = CardCount == 0 ? 0 : CardWidth + step * (CardCount - 1);
        for (int i = 0; i < _slots.Length; i++) {
            bool wasVisible = _slots[i].Visible;
            Vector2 start = previousPositions != null && i < previousPositions.Length
                && previousPositions[i].HasValue ? previousPositions[i].Value : _slots[i].Position;
            bool existing = previousPositions != null
                ? i < previousPositions.Length && previousPositions[i].HasValue
                : _layoutInitialized && wasVisible;
            _tweens[i]?.Kill();
            _slots[i].Visible = i < CardCount && i != _installingIndex;
            _positions[i] = ContentOffset + new Vector2((Size.X - width) / 2 + i * step, 0);
            if (!Engine.IsEditorHint() && existing && i < CardCount && LayoutMoveDuration > 0
                && start.DistanceSquaredTo(_positions[i]) > 0.01f) {
                _slots[i].Position = start;
                MoveSlot(i, _positions[i], LayoutMoveDuration);
            }
            else
                _slots[i].Position = _positions[i];
            _slots[i].ZIndex = i;
            _slots[i].Size = new Vector2(CardWidth, CardHeight);
            var name = _slots[i].GetNode<Label>("ShortTitle");
            name.Visible = ShowShortNames;
            name.Position = new Vector2(0, 370 * CardScale + ShortNameGap);
            name.Size = new Vector2(i < CardCount - 1 ? Math.Min(CardWidth, step) : CardWidth, ShortNameHeight);
            name.ClipText = true;
            name.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
            name.AddThemeFontSizeOverride("font_size", ShortNameFontSize);
            if (Engine.IsEditorHint()) {
                _cardControls[i].Scale = new Vector2(CardScale, CardScale);
                _cardControls[i].PivotOffset = Vector2.Zero;
                _cardControls[i].PivotOffsetRatio = Vector2.Zero;
            }
            else {
                // Slot motion and the card's arrival pose are independent animations.
                // Reflow must not cancel a tiny incoming card's rotation/growth.
                if (_installationTweens[i]?.IsValid() != true || !_installationTweens[i].IsRunning()) {
                    _cards[i].Position = Vector2.Zero;
                    _cards[i].Rotation = 0;
                    ConfigureCard(_cards[i], CardScale);
                }
            }
        }
        _layoutHash = LayoutHash();
        _layoutInitialized = true;

        _editorCount = PreviewCardCount;
    }

    public override void _Process(double delta) {
        if (_layoutHash != LayoutHash() || (Engine.IsEditorHint() && _editorCount != PreviewCardCount))
            ApplyLayout(Engine.IsEditorHint() ? PreviewCardCount : _entries?.Length ?? PreviewCardCount);
        if (Engine.IsEditorHint())
            return;
        UpdateUninstallPreviewFrame();
        if (!IsVisibleInTree() || IsInteractionSuppressed()) {
            SetHovered(-1);
            return;
        }
        SetHovered(ResolveHoveredIndex(_pointerPosition));
        if (HoveredIndex >= 0)
            _cards[HoveredIndex]?.RefreshSkillPreviewIfStale();
        if (_detail?.Visible == true)
            UpdateDetailLayout();
    }

    private bool IsInteractionSuppressed() => !IsChoosingReplacement
        && HandControl?.ShouldSuppressInstalledCardInteraction(_pointerPosition) == true;

    private int ResolveHoveredIndex(Vector2 pointerPosition) {
        Vector2 mouse = GetGlobalTransformWithCanvas().AffineInverse() * pointerPosition;
        // Keep the lifted compact card hovered while moving over its displayed face.
        if (HoveredIndex >= 0 && IsPointerOverCard(HoveredIndex, pointerPosition))
            return HoveredIndex;
        int hovered = -1;
        for (int i = CardCount - 1; i >= 0; i--)
            if (_slots[i].Visible && !IsInstallationAnimating(i)
                && new Rect2(_slots[i].Position, new Vector2(CardWidth, CardHeight)).HasPoint(mouse)) {
                hovered = i;
                break;
            }
        return hovered;
    }

    private bool IsPointerOverCard(int index, Vector2 pointerPosition) =>
        _slots[index].Visible && !IsInstallationAnimating(index)
        && new Rect2(Vector2.Zero, new Vector2(240, 370)).HasPoint(
            _cardControls[index].GetGlobalTransformWithCanvas().AffineInverse() * pointerPosition);

    private bool IsInstallationAnimating(int index) =>
        _installationTweens[index]?.IsValid() == true && _installationTweens[index].IsRunning();

    public override void _Input(InputEvent input) {
        if (input is InputEventMouseMotion motion)
            _pointerPosition = motion.Position;
        else if (input is InputEventMouseButton button)
            _pointerPosition = button.Position;
        if (IsInteractionSuppressed())
            SetHovered(-1);
    }

    private void SetHovered(int index) {
        if (HoveredIndex == index)
            return;
        int previous = HoveredIndex;
        HoveredIndex = index;
        if (previous >= 0)
            AnimateCard(previous, false);
        if (index >= 0)
            AnimateCard(index, true);
        UpdateDetail();
    }

    private void AnimateCard(int index, bool lifted) {
        bool uninstalling = _uninstallPreview && index == 0;
        bool raised = lifted || uninstalling;
        _tweens[index]?.Kill();
        _slots[index].ZIndex = raised ? 10 : index;
        if (Engine.IsEditorHint()) {
            _slots[index].Position = _positions[index];
            return;
        }
        SkillCard card = _cards[index];
        card.SetPlayableHighlight(raised, instant: true);
        card.HoverHint.Visible = lifted;
        MoveSlot(index, _positions[index] + (raised ? new Vector2(0, -Math.Abs(HoverLift)) : Vector2.Zero), HoverDuration);
        _slots[index].GetNode<Label>("ShortTitle").Visible = ShowShortNames;
        if (lifted) {
            AudioManager.PlayCardHover(card);
            card.ShowSkillPreview();
        }
        else
            card.HideSkillPreview();
    }

    private void UpdateDetail() {
        if (_detail == null)
            return;
        _detail.Visible = HoveredIndex >= 0 && _entries != null;
        if (!_detail.Visible || _detailCard == null)
            return;
        ConfigureCard(_detailCard, PreviewScale);
        ApplyCard(_detailCard, _entries[HoveredIndex]);
        UpdateDetailLayout();
    }

    private void UpdateDetailLayout() {
        Vector2 cardSize = _detailCard.Size * PreviewScale;
        _detail.Size = cardSize;
        _detail.Position = GetViewportRect().GetCenter() + PreviewOffset - cardSize / 2;
        var hint = GetNode<Label>("DetailLayer/Detail/Hint");
        hint.Position = new Vector2(0, cardSize.Y + PreviewHintGap);
        hint.Size = new Vector2(cardSize.X, hint.Size.Y);
    }

    private void MoveSlot(int index, Vector2 position, float duration) {
        _tweens[index]?.Kill();
        _tweens[index] = CreateTween().SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
        _tweens[index].TweenProperty(_slots[index], "position", position, Math.Max(0.001f, duration));
    }

    private static void ConfigureCard(SkillCard card, float scale) {
        card.StopBattleMotion();
        card.UseDefaultHoverEffect = false;
        card.AutoPressEffect = false;
        card.AutoAdjustDescriptionTextSize = false;
        card.ConfigureDisplayScale(new Vector2(scale, scale));
        card.PivotOffset = Vector2.Zero;
        card.PivotOffsetRatio = Vector2.Zero;
        card.Position = Vector2.Zero;
        IgnoreCardInput(card);
        card.SetHoverUiEnabled(false);
        card.HoverHint.Visible = false;
        card.SetRelatedCardPreviewSuppressed(true);
        card.ConfigurePilePreviewVisuals();
    }

    private static void IgnoreCardInput(Node node) {
        if (node is Control control)
            control.MouseFilter = MouseFilterEnum.Ignore;
        foreach (Node child in node.GetChildren())
            IgnoreCardInput(child);
    }

    private void ApplyCard(SkillCard card, CardEntry entry) {
        card.PreviewCharacterName = OwnerName;
        card.PreviewCharacterKey = OwnerKey;
        card.DisplayNameOverride = entry.Title;
        card.SetSkill(entry.Skill);
        if (entry.Skill == null && entry.Title != null) {
            card.NameLabel.Text = entry.Title;
            card.CharacterName.Text = OwnerName;
        }
        card.EnergyBadge.Visible = !HideEnergyCost;
        if (entry.Description != null)
            card.Description.Text = entry.Description;
        if (entry.Art != null) {
            card.SkillPicture.Texture = entry.Art;
            card.SkillPicture.Visible = true;
            card.SkillIcon.Visible = false;
            if (card.ArtFill != null)
                card.ArtFill.Visible = false;
        }
    }

    public void RefreshCardText() {
        if (!IsNodeReady() || _entries == null)
            return;
        for (int i = 0; i < _entries.Length; i++)
            ApplyCard(_cards[i], _entries[i]);
        UpdateDetail();
    }

    public void Pulse(int index) {
        if (index < 0 || index >= CardCount || Engine.IsEditorHint())
            return;
        SkillCard card = _cards[index];
        Tween tween = CreateTween();
        tween.TweenProperty(card, "modulate", new Color(1.7f, 1.7f, 1.7f, 1), 0.12);
        tween.TweenProperty(card, "modulate", Colors.White, 0.3);
    }

    private int LayoutHash() {
        var hash = new HashCode();
        hash.Add(Size); hash.Add(CardScale); hash.Add(CardStep); hash.Add(FitCardsToWidth);
        hash.Add(ContentOffset); hash.Add(ShowShortNames); hash.Add(ShortNameFontSize);
        hash.Add(ShortNameGap); hash.Add(ShortNameHeight); hash.Add(HoverLift); hash.Add(HoverDuration);
        hash.Add(LayoutMoveDuration);
        hash.Add(PreviewOffset); hash.Add(PreviewScale); hash.Add(PreviewHintGap);
        return hash.ToHashCode();
    }

    public Vector2 GetSlotGlobalCenter(int index) => index >= 0 && index < CardCount
        ? GetGlobalTransformWithCanvas() * (_positions[index] + new Vector2(CardWidth / 2, 370 * CardScale / 2))
        : GetGlobalRect().GetCenter();

    public void BeginInstallation(int index) {
        if (index < 0 || index >= CardCount)
            return;
        _installationTweens[index]?.Kill();
        _installingIndex = index;
        _slots[index].Visible = false;
    }

    public void RevealInstallation(int index, float impactScale = 1.35f,
        float arrivalGlobalRotation = 0, float arrivalScale = 0.035f) {
        if (_installingIndex != index || index < 0 || index >= CardCount || !IsInsideTree())
            return;
        _installingIndex = -1;
        _slots[index].Visible = true;
        var card = _cards[index];
        var baseSize = new Vector2(240, 370);
        float startScale = Math.Min(arrivalScale, CardScale);
        float startRotation = Mathf.Wrap(arrivalGlobalRotation
            - _slots[index].GetGlobalTransformWithCanvas().X.Angle(), -Mathf.Pi, Mathf.Pi);
        SetInstallationPose(card, baseSize, startScale, startRotation);
        var impact = new ActionCardInstallationImpact {
            Position = _positions[index] + baseSize * CardScale / 2,
        };
        AddChild(impact);
        var tween = CreateTween();
        _installationTweens[index] = tween;
        float peak = CardScale * Math.Clamp(impactScale, 1, 2);
        // Preserve the flight heading at the exact landing point, then turn and expand in place.
        tween.TweenInterval(0.025);
        tween.TweenMethod(Callable.From<float>(t => SetInstallationPose(card, baseSize,
            Mathf.Lerp(startScale, peak, t), Mathf.Lerp(startRotation, 0.09f, t))), 0f, 1f, 0.11)
            .SetTrans(Tween.TransitionType.Expo).SetEase(Tween.EaseType.Out);
        tween.TweenMethod(Callable.From<float>(t => SetInstallationPose(card, baseSize,
            Mathf.Lerp(peak, CardScale * 0.94f, t), Mathf.Lerp(0.09f, -0.025f, t))), 0f, 1f, 0.055)
            .SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.In);
        tween.TweenMethod(Callable.From<float>(t => SetInstallationPose(card, baseSize,
            Mathf.Lerp(CardScale * 0.94f, CardScale, t), Mathf.Lerp(-0.025f, 0, t))), 0f, 1f, 0.09)
            .SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
        Pulse(index);
    }

    private void SetInstallationPose(SkillCard card, Vector2 baseSize, float scale, float rotation) {
        if (!GodotObject.IsInstanceValid(card)) return;
        card.Scale = Vector2.One * scale;
        card.Rotation = rotation;
        card.PivotOffset = Vector2.Zero;
        card.Position = baseSize * CardScale / 2 - (baseSize * scale / 2).Rotated(rotation);
    }

    // Compatibility for old callers; full racks now replace the oldest card without a modal.
    public Task<int> SelectReplacementAsync(Skill incoming, CancellationToken token) =>
        Task.FromResult(token.IsCancellationRequested || CardCount == 0 ? -1 : 0);

    public SkillCard GetInstalledCardVisual(Skill skill) {
        if (_entries == null) return null;
        for (int i = 0; i < _entries.Length; i++)
            if (ReferenceEquals(_entries[i].Skill, skill))
                return _cards[i];
        return null;
    }

    public void SetUninstallPreview(bool visible) {
        visible &= CardCount > 0 && !Engine.IsEditorHint();
        if (_uninstallPreview == visible) return;
        _uninstallPreview = visible;
        UpdateUninstallPreviewVisual();
    }

    private void UpdateUninstallPreviewVisual() {
        if (!IsNodeReady() || Engine.IsEditorHint()) return;
        var hint = GetNode<Label>("ReplacementHint");
        hint.Visible = _uninstallPreview && CardCount > 0;
        if (hint.Visible)
            hint.Text = $"即将卸载：{_entries?[0].Skill?.SkillName ?? _entries?[0].Title ?? "行动牌"}";
        if (CardCount > 0)
            AnimateCard(0, HoveredIndex == 0);
        UpdateUninstallPreviewFrame();
    }

    private void UpdateUninstallPreviewFrame() {
        var frame = GetNode<Control>("UninstallPreviewFrame");
        frame.Visible = _uninstallPreview && CardCount > 0 && _slots[0].Visible;
        if (!frame.Visible) return;
        frame.Position = _slots[0].Position - new Vector2(3, 3);
        frame.Size = new Vector2(CardWidth + 6, 370 * CardScale + 6);
    }

    public void ClearHoverPreview() => SetHovered(-1);

    // Retained for callers of the original rack preview API.
    public void ClearPinnedPreview() => ClearHoverPreview();

    public override void _ExitTree() {
        foreach (SkillCard card in _cards)
            if (card != null && GodotObject.IsInstanceValid(card))
                card.HideSkillPreview();
        foreach (Tween tween in _tweens)
            tween?.Kill();
        foreach (Tween tween in _installationTweens)
            tween?.Kill();
    }
}
