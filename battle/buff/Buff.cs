using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using Godot;

public partial class Buff
{
    public PackedScene HintScene = GD.Load<PackedScene>("res://LabelNode/BuffHintLabel.tscn");
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

    public void TweenLabel()
    {
        Tween tween = BuffIcon.CreateTween();
        BuffIcon.GetChild<Label>(0).PivotOffset = BuffIcon.GetChild<Label>(0).Size / 2;
        tween.TweenProperty(BuffIcon.GetChild<Label>(0), "scale", new Vector2(2f, 2f), 0.15f);
        tween.TweenProperty(BuffIcon.GetChild<Label>(0), "scale", new Vector2(1f, 1f), 0.35f);
    }

    public void Hint(BuffName name,BuffHintLabel.Which which)
    {
        BuffHintLabel label = HintScene.Instantiate() as BuffHintLabel;
        label.Initialize(which, name.ToString());
        label.Position = Vector2.Zero;
        Owner.AddChild(label);
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
                Owner.CreateTween().TweenProperty(Owner, "modulate", new Godot.Color(1, 1, 1, 1), 0.5f);
                Owner.Recovery(Owner.BattleLifemax);
                Owner.State = Charater.CharaterState.Normal;
                Stack--;
                TweenLabel();
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
                TweenLabel();
                break;
        }

        if (Stack == 0)
        {
            BuffIcon.QueueFree();
            BuffIcon = null;
            Owner.HurtBuffs.Remove(this);
            Hint(BuffName.DamageImmune,BuffHintLabel.Which.vanish);
        }
    }

    public static void BuffAdd(BuffName name, Charater target, int stack)
    {
        if(target.HurtBuffs.Any(x => x.ThisBuffName == name))
        {
            Buff buff0 = target.HurtBuffs.First(x => x.ThisBuffName == name);
            buff0.Stack += stack;
            buff0.BuffIcon.GetChild<Label>(0).Text = buff0.Stack.ToString();
            buff0.TweenLabel();
            buff0.Hint(BuffName.DamageImmune,BuffHintLabel.Which.gain);
            return;
        }
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
        buff.Hint(buff.ThisBuffName,BuffHintLabel.Which.gain);
        buff.BuffIcon.GetChild<Label>(0).Text = stack.ToString();
        target.StateIconContainer.AddChild(icon);
    }
}
