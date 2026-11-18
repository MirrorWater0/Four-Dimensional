using System;
using System.Threading.Tasks;
using Godot;

public partial class CharacterControl {
    public void PlayActionCardDiscardAnimation(SkillCard sourceCard, Skill skill) {
        SkillCard animationCard = CreateActionCardRemovalAnimationCard(sourceCard, skill);
        if (animationCard == null) return;
        StartBattleCardAnimation(() => PlayActionCardRemovalAsync(animationCard, exhaust: false), "行动牌卸下动画");
    }

    public void PlayActionCardExhaustAnimation(SkillCard sourceCard, Skill skill) {
        SkillCard animationCard = CreateActionCardRemovalAnimationCard(sourceCard, skill);
        if (animationCard == null) return;
        animationCard.Name = "InstalledActionExhaustCard";
        StartBattleCardAnimation(() => PlayActionCardRemovalAsync(animationCard, exhaust: true), "行动牌消耗动画");
    }

    private async Task PlayActionCardRemovalAsync(SkillCard card, bool exhaust) {
        try {
            if (exhaust) {
                card.PlayExhaustEffect(CardPlayVanishDuration);
                await ToSignal(GetTree().CreateTimer(CardPlayVanishDuration), SceneTreeTimer.SignalName.Timeout);
            }
            else {
                // Batch removals keep the trails but avoid starting several particle emitters together.
                await PlayCardDiscardFlyAsync(card, enableTrailParticles: false);
            }
        }
        finally {
            if (IsInsideTree())
                ReturnBattleCardToPool(card);
            else
                FreeAnimationCard(card);
        }
    }

    private SkillCard CreateActionCardRemovalAnimationCard(SkillCard sourceCard, Skill skill) {
        if (!IsInsideTree() || sourceCard == null || !GodotObject.IsInstanceValid(sourceCard))
            return null;
        CanvasLayer overlay = EnsureCardPlayOverlay();
        if (overlay == null || !GodotObject.IsInstanceValid(overlay) || skill == null)
            return null;
        SkillCard animationCard = TakeBattleCardFromPool(-1);
        animationCard.Name = "InstalledActionRemovalCard";
        if (animationCard.GetParent() != overlay) {
            animationCard.GetParent()?.RemoveChild(animationCard);
            overlay.AddChild(animationCard);
        }
        animationCard.SetSkill(skill);
        animationCard.CharacterName.Text = sourceCard.CharacterName.Text;
        animationCard.GlobalPosition = sourceCard.GetGlobalTransformWithCanvas().Origin;
        animationCard.Rotation = GetCanvasRotation(sourceCard);
        animationCard.PivotOffset = sourceCard.PivotOffset;
        animationCard.ZIndex = PlayedCardZIndex;
        animationCard.Visible = true;
        animationCard.MouseFilter = MouseFilterEnum.Ignore;
        animationCard.SetHoverUiEnabled(false);
        animationCard.SetRelatedCardPreviewSuppressed(true);
        animationCard.SetHandIndexBadge(0, visible: false);
        SetCardButtonInputEnabled(animationCard, false);
        // Rack cards are scaled by both their own transform and the battle UI's canvas transform.
        animationCard.Scale = sourceCard.GetGlobalTransformWithCanvas().Scale;
        animationCard.SetPlayableHighlight(false, instant: true);
        sourceCard.HideSkillPreview();
        return animationCard;
    }

    private async Task PlayActionCardInstallationAsync(QueuedCardPlay play) {
        if (play.Card == null || !GodotObject.IsInstanceValid(play.Card) || play.Actor is not PlayerCharacter owner)
            return;
        var entries = BattleNode.GetInstalledActionCards(owner);
        int index = Array.FindIndex(entries, entry => ReferenceEquals(entry.Skill, play.Skill));
        var rack = BattleNode.ActionCardsUi?.GetRack(owner);
        if (index < 0 || rack == null)
            return;
        rack.BeginInstallation(index);
        play.AbilityActivationVisualInProgress = true;
        try {
            var ui = BattleNode.ActionCardsUi;
            bool flew = await play.Card.FlyInstallationWithImpactAsync(rack.GetSlotGlobalCenter(index), new CardInstallationOptions {
                WindupDuration = ui.InstallWindupDuration,
                FlyDuration = ui.InstallFlyDuration,
                ArcHeight = ui.InstallArcHeight,
                Pullback = ui.InstallPullback,
                TrailWidth = ui.InstallTrailWidth,
                FlightScale = ui.InstallFlightScale,
                OnArrival = (rotation, scale) => {
                    if (GodotObject.IsInstanceValid(rack))
                        rack.RevealInstallation(index, ui.InstallImpactScale, rotation, scale);
                },
            });
            if (flew)
                play.AbilityVisualConsumed = true;
        }
        finally {
            play.AbilityActivationVisualInProgress = false;
            if (GodotObject.IsInstanceValid(rack))
                rack.RevealInstallation(index);
        }
    }

}
