using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public partial class Buff
{
    private const float CardPreviewHighlightScaleMultiplier = 1.22f;
    private const float CardPreviewHighlightDuration = 0.12f;
    private bool _cardPreviewHighlighted;
    private Vector2 _cardPreviewHighlightBaseScale = Vector2.One;
    private Color _cardPreviewHighlightBaseModulate = Colors.White;
    private int _cardPreviewHighlightBaseZIndex;
    private Tween _cardPreviewHighlightTween;

    public void SetCardPreviewHighlight(bool highlighted, bool instant = false)
    {
        if (_cardPreviewHighlighted == highlighted)
            return;

        _cardPreviewHighlighted = highlighted;
        if (BuffIcon == null || !GodotObject.IsInstanceValid(BuffIcon))
            return;

        _cardPreviewHighlightTween?.Kill();
        _cardPreviewHighlightTween = null;
        BuffIcon.PivotOffset = BuffIcon.Size * 0.5f;

        if (highlighted)
        {
            _cardPreviewHighlightBaseScale =
                BuffIcon.Scale == Vector2.Zero ? Vector2.One : BuffIcon.Scale;
            _cardPreviewHighlightBaseModulate = BuffIcon.Modulate;
            _cardPreviewHighlightBaseZIndex = BuffIcon.ZIndex;
        }

        Vector2 targetScale = highlighted
            ? _cardPreviewHighlightBaseScale * CardPreviewHighlightScaleMultiplier
            : _cardPreviewHighlightBaseScale;
        Color targetModulate = highlighted
            ? new Color(
                _cardPreviewHighlightBaseModulate.R * 1.34f,
                _cardPreviewHighlightBaseModulate.G * 1.22f,
                _cardPreviewHighlightBaseModulate.B * 0.76f,
                _cardPreviewHighlightBaseModulate.A
            )
            : _cardPreviewHighlightBaseModulate;

        BuffIcon.ZIndex = highlighted
            ? _cardPreviewHighlightBaseZIndex + 1
            : _cardPreviewHighlightBaseZIndex;

        if (instant || !BuffIcon.IsInsideTree())
        {
            BuffIcon.Scale = targetScale;
            BuffIcon.Modulate = targetModulate;
            return;
        }

        _cardPreviewHighlightTween = BuffIcon.CreateTween();
        _cardPreviewHighlightTween.SetParallel(true);
        _cardPreviewHighlightTween
            .TweenProperty(BuffIcon, "scale", targetScale, CardPreviewHighlightDuration)
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.Out);
        _cardPreviewHighlightTween
            .TweenProperty(BuffIcon, "modulate", targetModulate, CardPreviewHighlightDuration)
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.Out);
    }
}

public partial class SkillBuff
{
    public bool CanTriggerForCardPreview(Skill skill)
    {
        if (Stack <= 0 || IsOwnerUnavailableForTrigger())
            return false;

        return ThisBuffName switch
        {
            BuffName.Stun => true,
            BuffName.Echo => _echoTriggeredCountThisTurn < Stack,
            BuffName.Fear => skill?.SkillType == Skill.SkillTypes.Attack,
            _ => false,
        };
    }
}

public static class CardPreviewBuffHighlighter
{
    public static Buff[] FindTriggeredBuffs(Skill skill)
    {
        Character owner = skill?.OwnerCharater;
        if (
            owner == null
            || !GodotObject.IsInstanceValid(owner)
            || owner.State != Character.CharacterState.Normal
        )
        {
            return Array.Empty<Buff>();
        }

        var highlighted = new HashSet<Buff>();
        if (AddSkillBuffTriggers(highlighted, owner, skill))
            return highlighted.ToArray();

        CardEffectPreviewSnapshot snapshot = CardEffectPreviewEcs.GetSnapshot(skill);
        Skill.PreviewEffectEntry[] entries = snapshot.EffectEntries;
        bool hasHostileAttack = false;

        for (int i = 0; i < entries.Length; i++)
        {
            Skill.PreviewEffectEntry entry = entries[i];
            Character target = entry.Target;
            if (
                target == null
                || !GodotObject.IsInstanceValid(target)
                || target.State != Character.CharacterState.Normal
            )
            {
                continue;
            }

            switch (entry.Kind)
            {
                case Skill.PreviewEffectKind.Damage:
                    if (entry.Value > 0)
                    {
                        bool isHostileAttack = target.IsPlayer != owner.IsPlayer;
                        AddDamageTriggers(highlighted, owner, target, entry, isHostileAttack);
                        hasHostileAttack |= isHostileAttack;
                    }
                    break;
                case Skill.PreviewEffectKind.Heal:
                    if (entry.Value > 0)
                        AddTeamBuffs(
                            highlighted,
                            target.BattleNode,
                            target.IsPlayer,
                            Buff.BuffName.Sanctuary
                        );
                    break;
                case Skill.PreviewEffectKind.Block:
                    if (entry.Value > 0 && target.Block < 99999)
                        AddActiveBuffs(
                            highlighted,
                            target.SpecialBuffs,
                            Buff.BuffName.Beacon
                        );
                    break;
                case Skill.PreviewEffectKind.Property:
                    if (entry.Value > 0 && entry.PropertyType is PropertyType propertyType)
                    {
                        Buff.BuffName? triggerName = propertyType switch
                        {
                            PropertyType.Power => Buff.BuffName.ExtraPower,
                            PropertyType.Survivability => Buff.BuffName.ExtraSurvivability,
                            _ => null,
                        };
                        if (triggerName.HasValue)
                            AddActiveBuffs(highlighted, target.SpecialBuffs, triggerName.Value);
                    }
                    break;
                case Skill.PreviewEffectKind.Buff:
                    AddBuffApplicationTriggers(highlighted, owner, target, entry);
                    break;
            }
        }

        if (hasHostileAttack && owner.IsPlayer)
        {
            AddTeamBuffs(
                highlighted,
                owner.BattleNode,
                owner.IsPlayer,
                Buff.BuffName.Shadow,
                excludedOwner: owner
            );
        }

        if (skill.SkillType == Skill.SkillTypes.Survive && owner.TriggersSkillUseEvents)
        {
            AddTeamBuffs(
                highlighted,
                owner.BattleNode,
                owner.IsPlayer,
                Buff.BuffName.Void
            );
        }

        return highlighted.ToArray();
    }

    private static bool AddSkillBuffTriggers(HashSet<Buff> highlighted, Character owner, Skill skill)
    {
        if (owner.SkillBuffs == null)
            return false;

        SkillBuff stun = owner.SkillBuffs.FirstOrDefault(buff =>
            buff != null
            && buff.ThisBuffName == Buff.BuffName.Stun
            && buff.CanTriggerForCardPreview(skill)
        );
        if (stun != null)
        {
            highlighted.Add(stun);
            return true;
        }

        foreach (SkillBuff buff in owner.SkillBuffs)
        {
            if (buff != null && buff.CanTriggerForCardPreview(skill))
                highlighted.Add(buff);
        }

        return false;
    }

    private static void AddDamageTriggers(
        HashSet<Buff> highlighted,
        Character owner,
        Character target,
        Skill.PreviewEffectEntry entry,
        bool isHostileAttack
    )
    {
        if (isHostileAttack)
        {
            AddActiveBuffs(highlighted, owner.StartActionBuffs, Buff.BuffName.Divinity);
            AddActiveBuffs(highlighted, owner.AttackBuffs, Buff.BuffName.Weaken);

            bool hasCursePower = HasActiveBuff(owner.AttackBuffs, Buff.BuffName.CursePower);
            if (hasCursePower)
            {
                AddActiveBuffs(highlighted, owner.AttackBuffs, Buff.BuffName.CursePower);
                AddDebuffImmunityTrigger(highlighted, target);
                if (!HasActiveBuff(target.SpecialBuffs, Buff.BuffName.DebuffImmunity))
                    AddActiveBuffs(
                        highlighted,
                        owner.SpecialBuffs,
                        Buff.BuffName.WeakeningField
                    );
            }

            AddAttackHurtBuffTriggers(highlighted, target);
        }
        else
        {
            AddActiveBuffs(highlighted, target.HurtBuffs, Buff.BuffName.DamageImmune);
        }

        int immunityStacks = GetActiveStackCount(target.HurtBuffs, Buff.BuffName.DamageImmune);
        bool canChangeLife = immunityStacks < entry.HitCount && entry.Value > target.Block;
        if (canChangeLife)
        {
            AddTeamBuffs(
                highlighted,
                target.BattleNode,
                target.IsPlayer,
                Buff.BuffName.Sanctuary
            );
        }
    }

    private static void AddAttackHurtBuffTriggers(HashSet<Buff> highlighted, Character target)
    {
        if (target.HurtBuffs == null)
            return;

        bool damageCanStillBeAmplified = true;
        foreach (HurtBuff buff in target.HurtBuffs)
        {
            if (buff == null || buff.Stack <= 0)
                continue;

            switch (buff.ThisBuffName)
            {
                case Buff.BuffName.DamageImmune:
                    highlighted.Add(buff);
                    damageCanStillBeAmplified = false;
                    break;
                case Buff.BuffName.Vulnerable when damageCanStillBeAmplified:
                case Buff.BuffName.Thorn:
                case Buff.BuffName.AutoArmor:
                    highlighted.Add(buff);
                    break;
            }
        }
    }

    private static void AddBuffApplicationTriggers(
        HashSet<Buff> highlighted,
        Character owner,
        Character target,
        Skill.PreviewEffectEntry entry
    )
    {
        if (entry.Value <= 0 || !entry.BuffName.HasValue)
            return;

        Buff.BuffName addedBuff = entry.BuffName.Value;
        if (!Buff.IsDebuff(addedBuff))
            return;

        AddDebuffImmunityTrigger(highlighted, target);
        if (HasActiveBuff(target.SpecialBuffs, Buff.BuffName.DebuffImmunity))
            return;

        if (addedBuff == Buff.BuffName.Weaken)
        {
            Character source = entry.Source;
            if (source == null || !GodotObject.IsInstanceValid(source))
                source = owner;
            AddActiveBuffs(highlighted, source.SpecialBuffs, Buff.BuffName.WeakeningField);
        }
    }

    private static void AddDebuffImmunityTrigger(HashSet<Buff> highlighted, Character target)
    {
        AddActiveBuffs(highlighted, target.SpecialBuffs, Buff.BuffName.DebuffImmunity);
    }

    private static void AddTeamBuffs(
        HashSet<Buff> highlighted,
        Battle battle,
        bool isPlayer,
        Buff.BuffName name,
        Character excludedOwner = null
    )
    {
        if (battle == null || !GodotObject.IsInstanceValid(battle))
            return;

        foreach (Character character in battle.GetTeamCharacters(isPlayer, includeSummons: true))
        {
            if (
                character == null
                || character == excludedOwner
                || !GodotObject.IsInstanceValid(character)
                || character.State == Character.CharacterState.Dying
            )
            {
                continue;
            }

            AddActiveBuffs(highlighted, character.EndActionBuffs, name);
            AddActiveBuffs(highlighted, character.AttackBuffs, name);
        }
    }

    private static void AddActiveBuffs(
        HashSet<Buff> highlighted,
        IEnumerable<Buff> buffs,
        Buff.BuffName name
    )
    {
        if (buffs == null)
            return;

        foreach (Buff buff in buffs)
        {
            if (buff != null && buff.ThisBuffName == name && buff.Stack > 0)
                highlighted.Add(buff);
        }
    }

    private static bool HasActiveBuff(IEnumerable<Buff> buffs, Buff.BuffName name)
    {
        if (buffs == null)
            return false;

        foreach (Buff buff in buffs)
        {
            if (buff != null && buff.ThisBuffName == name && buff.Stack > 0)
                return true;
        }

        return false;
    }

    private static int GetActiveStackCount(IEnumerable<Buff> buffs, Buff.BuffName name)
    {
        if (buffs == null)
            return 0;

        int total = 0;
        foreach (Buff buff in buffs)
        {
            if (buff != null && buff.ThisBuffName == name && buff.Stack > 0)
                total += buff.Stack;
        }

        return total;
    }
}
