using System;
using System.Threading.Tasks;
using Godot;

public partial class CharacterControl {
    public void PlayHandOverflowAnimation(
        Skill skill,
        HandCardEntryOrigin origin,
        Character source = null,
        SkillCard sourceCard = null,
        int order = 0
    ) {
        if (skill == null || !IsInsideTree() || !_uiBuilt
            || _cardRow == null || !GodotObject.IsInstanceValid(_cardRow))
            return;

        CanvasLayer overlay = EnsureCardPlayOverlay();
        if (overlay == null || !GodotObject.IsInstanceValid(overlay))
            return;

        Vector2 startCenter = GetHandOverflowStartCenter(skill, origin, source, sourceCard);
        Vector2 cardSize = BattleCardBaseSize * BattleCardScale;
        int handCount = PlayerCharacter.MaxBattleHandSize;
        float rowWidth = Math.Max(0f, _cardRow.Size.X);
        float step = HandLayout.GetCardStep(handCount, cardSize, rowWidth);
        float totalWidth = cardSize.X + step * (handCount - 1);
        Vector2 lastSlotPosition = new((rowWidth - totalWidth) * 0.5f + step * (handCount - 1),
            GetHandCardY(Math.Max(_cardRow.Size.Y, cardSize.Y), cardSize));
        Vector2 targetCenter = GetCardRowGlobalPosition(lastSlotPosition + cardSize * 0.5f);

        SkillCard card = TakeBattleCardFromPool(-1);
        if (card.GetParent() != overlay) {
            card.GetParent()?.RemoveChild(card);
            overlay.AddChild(card);
        }
        card.Name = "HandOverflowCard";
        card.RestoreDisplayState();
        card.SetSkill(skill);
        card.CharacterName.Text = GetSkillOwnerDisplayName(skill);
        card.PivotOffset = BattleCardBaseSize * 0.5f;
        Vector2 handScale = _cardRow.GetGlobalTransformWithCanvas().Scale * BattleCardScale;
        bool hasSourceCard = sourceCard != null && GodotObject.IsInstanceValid(sourceCard)
            && sourceCard.IsInsideTree();
        card.Scale = hasSourceCard ? sourceCard.GetGlobalTransformWithCanvas().Scale : handScale;
        card.Rotation = hasSourceCard ? GetCanvasRotation(sourceCard) : 0f;
        card.ZIndex = PlayedCardZIndex;
        card.Modulate = Colors.White;
        card.Visible = false;
        card.MouseFilter = MouseFilterEnum.Ignore;
        card.SetHoverUiEnabled(false);
        card.SetRelatedCardPreviewSuppressed(true);
        card.SetHandIndexBadge(0, visible: false);
        card.SetPlayableHighlight(false, instant: true);
        SetCardButtonInputEnabled(card, false);
        SetCardPivotCenterAt(card, startCenter);
        StartBattleCardAnimation(() => PlayHandOverflowAsync(card, startCenter, targetCenter,
            handScale, order), "满手溢出弃牌动画");
    }

    private Vector2 GetHandOverflowStartCenter(Skill skill, HandCardEntryOrigin origin,
        Character source, SkillCard sourceCard) {
        if (sourceCard != null && GodotObject.IsInstanceValid(sourceCard) && sourceCard.IsInsideTree())
            return sourceCard.GetGlobalTransformWithCanvas() * (sourceCard.Size * 0.5f);
        if (origin == HandCardEntryOrigin.PlayedCard
            && GetPlayedCardDrawEntryStartGlobalCenter() is Vector2 playedCenter)
            return playedCenter;
        if (origin == HandCardEntryOrigin.DiscardPile && _discardPileButton != null
            && GodotObject.IsInstanceValid(_discardPileButton) && _discardPileButton.IsInsideTree())
            return GetPileButtonVisualCenter(_discardPileButton);
        if (origin != HandCardEntryOrigin.DrawPile) {
            Character actor = source ?? skill.OwnerCharater;
            if (actor != null && GodotObject.IsInstanceValid(actor) && actor.IsInsideTree())
                return actor.GetGlobalTransformWithCanvas().Origin;
        }
        return GetDrawEntryStartCenter();
    }

    private async Task PlayHandOverflowAsync(SkillCard card, Vector2 startCenter,
        Vector2 targetCenter, Vector2 handScale, int order) {
        Task trailFade = Task.CompletedTask;
        try {
            float delay = Math.Min(Math.Max(0, order) * HandDrawEntryStagger,
                HandDrawEntryMaxTotalStaggerDuration);
            if (delay > 0f)
                await ToSignal(GetTree().CreateTimer(delay), SceneTreeTimer.SignalName.Timeout);
            if (!IsInsideTree() || !GodotObject.IsInstanceValid(card))
                return;

            card.Visible = true;
            // Begin discarding just before arrival, without occupying the last hand slot.
            float distance = startCenter.DistanceTo(targetCenter);
            if (distance > 48f) {
                Vector2 nearCenter = startCenter.Lerp(targetCenter, 1f - 48f / distance);
                float duration = GetDrawEntryTweenDuration(startCenter, nearCenter, 1);
                PrepareDrawEntryPreviewTrail(card, out Line trail, out GpuParticles2D particles,
                    enableParticles: false);
                Tween tween = card.CreateTween().SetParallel(true);
                tween.TweenProperty(card, "scale", handScale, duration);
                tween.TweenProperty(card, "rotation", 0f, duration);
                tween.TweenMethod(Callable.From<float>(t => {
                    if (GodotObject.IsInstanceValid(card))
                        SetCardPivotCenterAt(card, startCenter.Lerp(nearCenter, t));
                }), 0f, 1f, duration);
                await ToSignal(tween, Tween.SignalName.Finished);
                trailFade = FadeAndHideDrawEntryPreviewTrailAsync(trail, particles);
            }
            if (IsInsideTree() && GodotObject.IsInstanceValid(card))
                await PlayCardDiscardFlyAsync(card, enableTrailParticles: false);
        }
        finally {
            await trailFade;
            if (IsInsideTree())
                ReturnBattleCardToPool(card);
            else
                FreeAnimationCard(card);
        }
    }
}
