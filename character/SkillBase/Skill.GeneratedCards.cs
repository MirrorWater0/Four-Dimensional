using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Godot;

public partial class Skill {
    protected SkillStep PlayDrawPileCardStep(SkillTypes skillType, bool random = false) =>
        new PlayDrawPileCardSkillStep(skillType, random);

    private sealed class PlayDrawPileCardSkillStep : SkillStep {
        private readonly SkillTypes _skillType;
        private readonly bool _random;

        public PlayDrawPileCardSkillStep(SkillTypes skillType, bool random) {
            _skillType = skillType;
            _random = random;
        }

        public override async Task Execute(Skill skill) {
            Battle battle = skill?.OwnerCharater?.BattleNode;
            if (battle == null)
                return;
            Skill card = skill.OwnerCharater is PlayerCharacter
                ? battle.TakeDrawPileSkill(_skillType, _random)
                : battle.DrawCharacterSkill(skill.OwnerCharater, _skillType, _random);
            if (card == null)
                return;

            Character actor = card.OwnerCharater;
            Relic.ApplyDrawPilePlayRelicEffects(actor);
            CharacterControl control = battle.CharacterControl;
            if (actor is PlayerCharacter && control != null && GodotObject.IsInstanceValid(control) && control.IsInsideTree())
                await control.QueueDrawPileCardAsync(actor, card);
            else {
                SetRandomManualTarget(card, actor, battle);
                using (card.BeginEnergyCostWaiver())
                    await card.Effect();
                if (actor is PlayerCharacter && battle.IsActionCardInstalled(card) != true)
                    battle.DiscardBattleSkill(actor, card);
                await battle.ResolvePendingBattleEffectsAsync();
            }
        }

        private static void SetRandomManualTarget(Skill card, Character actor, Battle battle) {
            Random random = battle.GetTargetRandom(actor) ?? new Random();
            if (card.RequiresManualHostileTarget()) {
                Character[] targets = card.GetManualHostileTargetCandidates();
                if (targets.Length > 0)
                    card.SetManualTargetSelection(targets[random.Next(targets.Length)]);
                return;
            }

            if (!card.RequiresManualFriendlyTarget())
                return;

            Character[] friendlyCandidates = battle.GetTeamCharacters(actor.IsPlayer, includeSummons: true)
                .Where(target => target != null
                    && GodotObject.IsInstanceValid(target)
                    && target.Life > 0)
                .ToArray();
            var validTargets = new List<Character>();
            foreach (Character target in friendlyCandidates) {
                card.SetManualTargetSelection(target);
                if (card.HasManualTargetSelection())
                    validTargets.Add(target);
            }
            if (validTargets.Count > 0)
                card.SetManualTargetSelection(validTargets[random.Next(validTargets.Count)]);
            else
                card.ClearManualTargetSelection();
        }

        public override IEnumerable<string> Describe(Skill skill) {
            string type = _skillType switch {
                SkillTypes.Attack => I18n.Tr("skill.step.skill_type.attack", "攻击卡"),
                SkillTypes.Permanent => I18n.Tr("skill.step.skill_type.permanent", "常驻卡"),
                SkillTypes.Special => I18n.Tr("skill.step.skill_type.special", "特殊卡"),
                SkillTypes.Ability => I18n.Tr("skill.step.skill_type.ability", "能力卡"),
                _ => I18n.Tr("skill.step.skill_type.any", "任意卡"),
            };
            yield return I18n.Format(_random ? "skill.step.play_draw_pile.random" : "skill.step.play_draw_pile.first",
                _random ? "随机打出抽牌堆的1张{type}。" : "打出抽牌堆中1张{type}。", ("type", type));
        }

        public override IEnumerable<PreviewEffectEntry> PreviewEffects(Skill skill, PreviewDamageContext context) {
            Battle battle = skill?.OwnerCharater?.BattleNode;
            bool hasCard = false;
            if (battle != null) {
                hasCard = skill.OwnerCharater is PlayerCharacter
                    ? battle.HasDrawablePlayerCard(_skillType)
                    : (skill.OwnerCharater?.Skills ?? Array.Empty<Skill>())
                        .Any(candidate => candidate != null
                            && candidate.SkillType != SkillTypes.none
                            && !candidate.IsStatusCard
                            && (_skillType == SkillTypes.none || candidate.SkillType == _skillType));
            }
            if (!hasCard)
                yield return PreviewEffectEntry.Message(skill?.OwnerCharater,
                    I18n.Tr("battle.draw_pile.no_matching_card", "抽牌堆没有符合类型的卡"), skill?.OwnerCharater);
        }
    }

    protected SkillStep PlayCardsStep(SkillID skillId, int count,
        [CallerArgumentExpression(nameof(count))] string countExpression = null) =>
        new PlayCardsSkillStep(skillId, count, TuningProviderFromExpression(countExpression));

    private sealed class PlayCardsSkillStep : SkillStep {
        private readonly SkillID _skillId;
        private readonly int _count;
        private readonly Func<Skill, int> _countProvider;

        public PlayCardsSkillStep(SkillID skillId, int count, Func<Skill, int> countProvider) {
            _skillId = skillId;
            _count = count;
            _countProvider = countProvider;
        }

        private int ResolveCount(Skill skill) => Math.Max(0, _countProvider?.Invoke(skill) ?? _count);

        private Skill CreateCard(Skill source) {
            Skill card = GetSkill(_skillId);
            if (card == null || source == null)
                return card;
            card.OwnerCharater = source.OwnerCharater;
            card.SetPreviewStats(source.OwnerPower, source.OwnerSurvivability, source.OwnerEnergy);
            card._actionPreview = source._actionPreview;
            card.UpdateDescription();
            return card;
        }

        public override async Task Execute(Skill skill) {
            Character actor = skill?.OwnerCharater;
            Battle battle = actor?.BattleNode;
            if (battle == null)
                return;
            int count = ResolveCount(skill);
            for (int i = 0; i < count && !ShouldAbortStepExecution(skill); i++) {
                Skill card = CreateCard(skill);
                if (card == null)
                    break;
                CharacterControl control = battle.CharacterControl;
                if (control != null && GodotObject.IsInstanceValid(control) && control.IsInsideTree())
                    await control.QueueDrawPileCardAsync(actor, card);
                else {
                    using (card.BeginEnergyCostWaiver())
                        await card.Effect();
                    battle.DiscardBattleSkill(actor, card);
                    await battle.ResolvePendingBattleEffectsAsync();
                }
            }
        }

        public override IEnumerable<string> Describe(Skill skill) {
            yield return I18n.Format("skill.step.play_generated_cards", "直接打出{count}张{card}。",
                ("count", ResolveCount(skill)), ("card", GetSkill(_skillId)?.SkillName ?? _skillId.ToString()));
        }

        public override IEnumerable<Character> PreviewTargets(Skill skill) =>
            ResolveCount(skill) > 0 ? CreateCard(skill)?.GetPreviewHostileTargets() ?? Array.Empty<Character>()
                : Array.Empty<Character>();

        public override IEnumerable<PreviewDamageEntry> PreviewDamage(Skill skill, PreviewDamageContext context) {
            for (int i = 0; i < ResolveCount(skill); i++)
                foreach (PreviewDamageEntry entry in CreateCard(skill)?.BuildPreviewHostileDamageEntriesRaw()
                    ?? Array.Empty<PreviewDamageEntry>())
                    yield return entry;
        }

        public override IEnumerable<PreviewEffectEntry> PreviewEffects(Skill skill, PreviewDamageContext context) {
            for (int i = 0; i < ResolveCount(skill); i++)
                foreach (PreviewEffectEntry entry in CreateCard(skill)?.BuildPreviewEffectEntriesRaw()
                    ?? Array.Empty<PreviewEffectEntry>())
                    yield return entry;
        }
    }
}
