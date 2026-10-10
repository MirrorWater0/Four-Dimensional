using System;
using System.Linq;
using Godot;

/// <summary>Authoritative attack counts and their status-bar icons; normal count may be zero.</summary>
public partial class AttackCountBuff : SpecialBuff
{
    private AttackCountBuff(Character owner, BuffName name, int count) : base(owner, name, count) { }

    public static int GetCount(Character owner, bool temporary = false)
    {
        BuffName name = temporary ? BuffName.TemporaryAttackCount : BuffName.AttackCount;
        return owner?.SpecialBuffs?.FirstOrDefault(buff => buff.ThisBuffName == name)?.Stack
            ?? (temporary ? 0 : 1);
    }

    public static void Initialize(Character owner)
    {
        EnsureBuff(owner, BuffName.AttackCount, 1);
    }

    public static void Modify(Character owner, int delta, bool temporary = false, Character source = null)
    {
        if (owner == null || !GodotObject.IsInstanceValid(owner) || owner.SpecialBuffs == null || delta == 0)
            return;

        BuffName name = temporary ? BuffName.TemporaryAttackCount : BuffName.AttackCount;
        int previous = GetCount(owner, temporary);
        int next = (int)Math.Clamp((long)previous + delta, 0, int.MaxValue);
        if (next == previous)
            return;

        AttackCountBuff buff = EnsureBuff(owner, name, previous);
        buff.Stack = next;
        buff.UpdateStackLabel();
        if (temporary && next == 0)
            buff.TryRemoveIfEmpty(owner.SpecialBuffs);
        else
        {
            buff.TweenLabel();
            if (next > previous)
            {
                buff.Hint(name, BuffHintLabel.Which.gain);
                buff.BuffAddAnimation();
                owner.MarkBuffSeen(name);
            }
        }

        owner.InvalidateBuffTooltipCache();
        RecordBuffGain(owner, name, next - previous, source);
        owner.BattleNode?.NotifyHandPreviewContextChanged();
    }

    public static void ClearTemporary(Character owner)
    {
        if (owner == null || !GodotObject.IsInstanceValid(owner))
            return;

        if (owner.SpecialBuffs?.FirstOrDefault(buff => buff.ThisBuffName == BuffName.TemporaryAttackCount)
            is AttackCountBuff temporary)
        {
            temporary.Stack = 0;
            temporary.TryRemoveIfEmpty(owner.SpecialBuffs, showVanishHint: false);
        }
    }

    private static AttackCountBuff EnsureBuff(Character owner, BuffName name, int count)
    {
        if (owner.SpecialBuffs.FirstOrDefault(buff => buff.ThisBuffName == name) is AttackCountBuff existing)
            return existing;

        var buff = new AttackCountBuff(owner, name, count) { BuffIcon = CreateBuffIcon(name) };
        owner.SpecialBuffs.Add(buff);
        if (buff.BuffIcon != null)
        {
            owner.StateIconContainer.AddChild(buff.BuffIcon);
            buff.UpdateStackLabel();
        }
        owner.InvalidateBuffTooltipCache();
        return buff;
    }
}
