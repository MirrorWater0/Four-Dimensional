using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Godot;

public partial class Buff
{
    public enum BuffType
    {
        Dying,
        Hurt,
    }

    public enum BuffName
    {
        Rebirth,
        DamageImmune,
    }

    public Charater Owner;
    public BuffName ThisBuffName;
    public int Stack;
    public ColorRect BuffIcon;

    public Buff(Charater owner, BuffName name, int stack)
    {
        Owner = owner;
        ThisBuffName = name;
        Stack = stack;
    }
}

public class DyingBuff : Buff
{
    public DyingBuff(Charater owner, BuffName name, int stack)
        : base(owner, name, stack) { }

    public async void Trigger()
    {
        switch (ThisBuffName)
        {
            case BuffName.Rebirth:
                await Task.Delay(200);
                Owner.CreateTween().TweenProperty(Owner, "modulate", new Color(1, 1, 1, 1), 0.5f);
                Owner.Recovery(Owner.BattleLifemax);
                Owner.State = Charater.CharaterState.Normal;
                Stack--;
                GD.Print("Rebirth", Owner.Life, "array");
                break;
        }

        if (Stack == 0)
        {
            BuffIcon.QueueFree();
            Owner.DyingBuffs.Remove(this);
        }
    }

    public static void BuffAdd(BuffName name, Charater target, int stack)
    {
        DyingBuff buff = null;
        ColorRect icon = null;
        switch (name)
        {
            case BuffName.Rebirth:
                buff = new DyingBuff(target, BuffName.Rebirth, stack);
                target.DyingBuffs.Add(buff);
                icon =
                    GD.Load<PackedScene>("res://battle/buff/StateIcon/Rebirth.tscn").Instantiate()
                    as ColorRect;

                break;
            default:
                return;
        }
        buff.BuffIcon = icon;
        buff.BuffIcon.GetChild<Label>(0).Text = stack.ToString();
        target.StateIconContainer.AddChild(icon);
    }
}

public partial class HurtBuff : Buff
{
    public HurtBuff(Charater owner, BuffName name, int stack)
        : base(owner, name, stack) { }

    public void Trigger(ref float damage)
    {
        switch (ThisBuffName)
        {
            case BuffName.DamageImmune:
                damage = 0;
                Stack--;
                BuffIcon.GetChild<Label>(0).Text = Stack.ToString();
                break;
        }

        if (Stack == 0)
        {
            BuffIcon.QueueFree();
            Owner.HurtBuffs.Remove(this);
        }
    }

    public static void BuffAdd(BuffName name, Charater target, int stack)
    {
        HurtBuff buff = null;
        ColorRect icon = null;
        switch (name)
        {
            case BuffName.DamageImmune:
                buff = new HurtBuff(target, BuffName.DamageImmune, stack);
                target.HurtBuffs.Add(buff);
                icon =
                    GD.Load<PackedScene>("res://battle/buff/StateIcon/Buffer.tscn").Instantiate()
                    as ColorRect;
                break;
            default:
                return;
        }
        buff.BuffIcon = icon;
        buff.BuffIcon.GetChild<Label>(0).Text = stack.ToString();
        target.StateIconContainer.AddChild(icon);
    }
}
