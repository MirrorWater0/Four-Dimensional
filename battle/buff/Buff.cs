using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using Godot;

public partial class Buff
{
    public static PackedScene HintScene = GD.Load<PackedScene>(
        "res://LabelNode/BuffHintLabel.tscn"
    );

    public static string GetBuffEffectText(BuffName name)
    {
        string key = name switch
        {
            BuffName.RebirthI => "生命归零时，回复最大生命的50%，消耗1层。",
            BuffName.DamageImmune => "受到伤害时，伤害变为0，消耗1层。",
            BuffName.Vulnerable => "受到伤害时，伤害提高25%，消耗1层。",
            BuffName.Taunt => "敌方攻击更倾向于选择该目标；受到伤害时消耗1层。",
            BuffName.Stun => "无法行动；回合开始时消耗1层。",
            _ => string.Empty,
        };

        return string.IsNullOrWhiteSpace(key) ? string.Empty : TranslationServer.Translate(key);
    }

    public static void GhostExplode(Control node, Vector2 scale, Node parent = null)
    {
        // 1. 克隆节点
        var ghost = node.Duplicate() as Control;
        ghost.GetChild(0).QueueFree();
        ghost.SetAnchorsPreset(Control.LayoutPreset.TopLeft);
        if (ghost == null)
            return;

        // 2. 处理 Shader，将其变为 Add 模式
        // 注意：哪怕是普通的 Control，只要带 ShaderMaterial 就可以处理
        if (ghost.Material is ShaderMaterial originalMat)
        {
            // 必须 Duplicate 材质和 Shader 资源，否则会修改到原始节点
            var newMat = (ShaderMaterial)originalMat.Duplicate();
            newMat.Shader = (Shader)newMat.Shader.Duplicate();

            string code = newMat.Shader.Code;
            // 注入混合模式：blend_add
            if (code.Contains("render_mode"))
            {
                if (!code.Contains("blend_add"))
                    code = code.Replace("render_mode ", "render_mode blend_add, ");
            }
            else
            {
                code = code.Replace(
                    "shader_type canvas_item;",
                    "shader_type canvas_item;\nrender_mode blend_add;"
                );
            }

            newMat.Shader.Code = code;
            ghost.Material = newMat;
        }

        // 3. 设置层级与位置
        if (parent != null)
            parent.AddChild(ghost);
        else
            node.AddChild(ghost);

        // 设置缩放中心
        ghost.PivotOffset = ghost.Size / 2;

        ghost.Position = -ghost.Size / 2 + new Vector2(0, -40);

        // 4. 动画效果
        var tween = ghost.CreateTween(); // 建议直接由 ghost 创建

        // 并行执行：缩放变大 + 变透明
        tween.SetParallel(true);
        tween
            .TweenProperty(ghost, "scale", scale, 0.7f)
            .SetTrans(Tween.TransitionType.Quad)
            .SetEase(Tween.EaseType.Out);

        // 注意：Add 模式下，Alpha 也会参与叠加，modulate 依然有效
        tween
            .TweenProperty(ghost, "modulate:a", 0.0f, 0.7f)
            .SetTrans(Tween.TransitionType.Quad)
            .SetEase(Tween.EaseType.Out);

        // 5. 结束后自动销毁
        tween.SetParallel(false);
        tween.Chain().TweenCallback(Callable.From(ghost.QueueFree));
    }

    public enum BuffType
    {
        Dying,
        Hurt,
    }


    public enum BuffName
    {
        [Description("重生I")]
        RebirthI,

        [Description("免疫伤害")]
        DamageImmune,

        [Description("易伤")]
        Vulnerable,

        [Description("嘲讽")]
        Taunt,

        [Description("眩晕")]
        Stun,
    }

    public Character Owner;
    public BuffName ThisBuffName;
    public Nature BuffNature;
    public int Stack;
    public ColorRect BuffIcon;

    public Buff(Character owner, BuffName name, int stack)
    {
        Owner = owner;
        ThisBuffName = name;
        Stack = stack;
    }

    public void TweenLabel()
    {
        if (Stack == 0)
            return;
        // Check if BuffIcon is still valid (not disposed or queued for deletion)
        if (BuffIcon == null || !GodotObject.IsInstanceValid(BuffIcon))
            return;

        Tween tween = BuffIcon.CreateTween();
        BuffIcon.GetChild<Label>(0).PivotOffset = BuffIcon.GetChild<Label>(0).Size / 2;
        tween.TweenProperty(BuffIcon.GetChild<Label>(0), "scale", new Vector2(2f, 2f), 0.15f);
        tween.TweenProperty(BuffIcon.GetChild<Label>(0), "scale", new Vector2(1f, 1f), 0.35f);
    }

    public void BuffAddAnimation()
    {
        if (BuffIcon == null || !GodotObject.IsInstanceValid(BuffIcon))
            return;

        var depIcon = BuffIcon.Duplicate() as ColorRect;
        if (depIcon == null)
            return;

        // Avoid Godot warning: setting Size on Controls with stretched anchors gets overridden after _Ready.
        depIcon.SetAnchorsPreset(Control.LayoutPreset.TopLeft);
        depIcon.Size = new Vector2(200, 200);
        GhostExplode(depIcon, new Vector2(2f, 2f), Owner);
        depIcon.Free();
    }

    public void Hint(BuffName name, BuffHintLabel.Which which)
    {
        BuffHintLabel label = HintScene.Instantiate() as BuffHintLabel;
        label.TargetPosition = Owner.GlobalPosition;
        label.Initialize(which, name.GetDescription());
        label.Position = Vector2.Zero;
        Owner.AddChild(label);
    }
}

public class DyingBuff : Buff
{
    public DyingBuff(Character owner, BuffName name, int stack)
        : base(owner, name, stack) { }

    public async Task Trigger()
    {
        switch (ThisBuffName)
        {
            case BuffName.RebirthI:
                if (Stack >= 1)
                {
                    Owner.Recover(Owner.BattleLifemax / 2, true);
                    Stack--;
                }
                break;
        }
        TweenLabel();
        if (Stack == 0)
        {
            // Check if BuffIcon is still valid before queuing for deletion
            if (BuffIcon != null && GodotObject.IsInstanceValid(BuffIcon))
            {
                BuffIcon.QueueFree();
            }
            Owner.DyingBuffs.Remove(this);
        }
    }

    public static void BuffAdd(BuffName name, Character target, int stack)
    {
        if (target.DyingBuffs.Any(x => x.ThisBuffName == name))
        {
            Buff buff0 = target.DyingBuffs.First(x => x.ThisBuffName == name);
            buff0.Stack += stack;
            buff0.BuffIcon.GetChild<Label>(0).Text = buff0.Stack.ToString();
            buff0.TweenLabel();
            buff0.Hint(buff0.ThisBuffName, BuffHintLabel.Which.gain);
            buff0.BuffAddAnimation();
            return;
        }
        DyingBuff buff = null;
        ColorRect icon = null;
        switch (name)
        {
            case BuffName.RebirthI:
                buff = new DyingBuff(target, BuffName.RebirthI, stack);
                target.DyingBuffs.Add(buff);
                icon =
                    GD.Load<PackedScene>("res://battle/buff/StateIcon/Rebirth.tscn").Instantiate()
                    as ColorRect;

                break;
            default:
                return;
        }
        buff.BuffIcon = icon;
        buff.TweenLabel();
        buff.BuffIcon.GetChild<Label>(0).Text = stack.ToString();
        buff.Hint(buff.ThisBuffName, BuffHintLabel.Which.gain);
        target.StateIconContainer.AddChild(icon);

        buff.BuffAddAnimation();
    }
}

public partial class HurtBuff : Buff
{
    public HurtBuff(Character owner, BuffName name, int stack)
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
            case BuffName.Vulnerable:
                damage *= 1.25f;
                Stack--;

                BuffIcon.GetChild<Label>(0).Text = Stack.ToString();
                break;
            case BuffName.Taunt:
                Stack--;
                break;
        }
        TweenLabel();
        if (Stack == 0)
        {
            // Check if BuffIcon is still valid before queuing for deletion
            if (BuffIcon != null && GodotObject.IsInstanceValid(BuffIcon))
            {
                BuffIcon.QueueFree();
            }
            BuffIcon = null;
            Owner.HurtBuffs.Remove(this);
            Hint(ThisBuffName, BuffHintLabel.Which.vanish);
        }
    }

    public static void BuffAdd(BuffName name, Character target, int stack)
    {
        if (target.HurtBuffs.Any(x => x.ThisBuffName == name))
        {
            Buff buff0 = target.HurtBuffs.First(x => x.ThisBuffName == name);
            buff0.Stack += stack;
            buff0.BuffIcon.GetChild<Label>(0).Text = buff0.Stack.ToString();
            buff0.TweenLabel();
            buff0.Hint(buff0.ThisBuffName, BuffHintLabel.Which.gain);
            buff0.BuffAddAnimation();
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
            case BuffName.Vulnerable:
                buff = new HurtBuff(target, BuffName.Vulnerable, stack);
                target.HurtBuffs.Add(buff);
                icon =
                    GD.Load<PackedScene>("res://battle/buff/StateIcon/Vulnerable.tscn")
                        .Instantiate() as ColorRect;
                break;
            case BuffName.Taunt:
                buff = new HurtBuff(target, BuffName.Taunt, stack);
                target.HurtBuffs.Add(buff);
                icon =
                    GD.Load<PackedScene>("res://battle/buff/StateIcon/Aim.tscn").Instantiate()
                    as ColorRect;
                break;
            default:
                return;
        }
        buff.BuffIcon = icon;
        buff.TweenLabel();
        buff.Hint(buff.ThisBuffName, BuffHintLabel.Which.gain);
        buff.BuffIcon.GetChild<Label>(0).Text = stack.ToString();
        target.StateIconContainer.AddChild(icon);

        buff.BuffAddAnimation();
    }
}

public partial class StartActionBuff : Buff
{
    public StartActionBuff(Character owner, BuffName name, int stack)
        : base(owner, name, stack) { }

    public void Trigger()
    {
        if (Stack <= 0)
            return;

        switch (ThisBuffName)
        {
            case BuffName.Stun:
                Stack--;
                if (BuffIcon != null && GodotObject.IsInstanceValid(BuffIcon))
                    BuffIcon.GetChild<Label>(0).Text = Stack.ToString();
                break;
        }

        TweenLabel();
        if (Stack == 0)
        {
            if (BuffIcon != null && GodotObject.IsInstanceValid(BuffIcon))
            {
                BuffIcon.QueueFree();
            }
            BuffIcon = null;
            Owner.StartActionBuffs.Remove(this);
            Hint(ThisBuffName, BuffHintLabel.Which.vanish);
        }
    }

    public static void BuffAdd(BuffName name, Character target, int stack)
    {
        if (target.StartActionBuffs.Any(x => x.ThisBuffName == name))
        {
            Buff buff0 = target.StartActionBuffs.First(x => x.ThisBuffName == name);
            buff0.Stack += stack;
            buff0.BuffIcon.GetChild<Label>(0).Text = buff0.Stack.ToString();
            buff0.TweenLabel();
            buff0.Hint(buff0.ThisBuffName, BuffHintLabel.Which.gain);
            buff0.BuffAddAnimation();
            return;
        }
        StartActionBuff buff = null;
        ColorRect icon = null;
        switch (name)
        {
            case BuffName.Stun:
                buff = new StartActionBuff(target, BuffName.Stun, stack);
                target.StartActionBuffs.Add(buff);
                icon =
                    GD.Load<PackedScene>("res://battle/buff/StateIcon/Stun.tscn").Instantiate()
                    as ColorRect;
                break;
            default:
                return;
        }

        if (buff == null || icon == null)
            return;

        buff.BuffIcon = icon;
        buff.TweenLabel();
        buff.Hint(buff.ThisBuffName, BuffHintLabel.Which.gain);
        buff.BuffIcon.GetChild<Label>(0).Text = stack.ToString();
        target.StateIconContainer.AddChild(icon);

        buff.BuffAddAnimation();
    }
}

public enum Nature
{
    positive,
    negative,
}
