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
    private static readonly Dictionary<BuffName, string> IconScenePaths = new()
    {
        [BuffName.RebirthI] = "res://battle/buff/StateIcon/Rebirth.tscn",
        [BuffName.DamageImmune] = "res://battle/buff/StateIcon/Buffer.tscn",
        [BuffName.Vulnerable] = "res://battle/buff/StateIcon/Vulnerable.tscn",
        [BuffName.Taunt] = "res://battle/buff/StateIcon/Aim.tscn",
        [BuffName.Thorn] = "res://battle/buff/StateIcon/Thorn.tscn",
        [BuffName.Stun] = "res://battle/buff/StateIcon/Stun.tscn",
        [BuffName.Pursuit] = "res://battle/buff/StateIcon/Pursuit.tscn",
        [BuffName.DebuffImmunity] = "res://battle/buff/StateIcon/DebuffImmunity.tscn",
        [BuffName.Invisible] = "res://battle/buff/StateIcon/Invisible.tscn",
        [BuffName.ExtraPower] = "res://battle/buff/StateIcon/ExtraPower.tscn",
        [BuffName.ExtraSurvivability] = "res://battle/buff/StateIcon/ExtraSurvivability.tscn",
        [BuffName.ExtraTurn] = "res://battle/buff/StateIcon/ExtraTurn.tscn",
        [BuffName.AutoArmor] = "res://battle/buff/StateIcon/AutoArmor.tscn",
        [BuffName.Barricade] = "res://battle/buff/StateIcon/Barricade.tscn",
    };

    public static string GetBuffEffectText(BuffName name)
    {
        string key = name switch
        {
            BuffName.RebirthI => "生命归零时，回复最大生命的50%，消耗1层。",
            BuffName.DamageImmune => "受到伤害时，伤害变为0，消耗1层。",
            BuffName.Vulnerable => "受到伤害时，伤害提高50%，消耗1层。",
            BuffName.Taunt => "敌方只能锁定该目标；受到伤害时消耗1层。",
            BuffName.Thorn => "受到伤害时，对攻击者造成等同层数的伤害。",
            BuffName.Stun => "无法释放技能；释放技能时消耗1层。",
            BuffName.Pursuit => "回合结束时：造成一次伤害。",
            BuffName.DebuffImmunity => "抵消1次负面状态添加，消耗1层。",
            BuffName.Invisible => "无法被选为攻击目标；回合开始时消耗1层。",
            BuffName.ExtraPower => "获得力量或生存时额外获得等同层数的力量。",
            BuffName.ExtraSurvivability => "获得力量或生存时额外获得等同层数的生存。",
            BuffName.ExtraTurn => "回合结束时消耗1层，触发一次额外行动（仅触发行动开始效果）。",
            BuffName.AutoArmor => "受到伤害后获得等同层数的格挡。",
            BuffName.Barricade => "回合开始时，保留你的格挡。",
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

        [Description("荆棘")]
        Thorn,

        [Description("眩晕")]
        Stun,

        [Description("追击")]
        Pursuit,

        [Description("减益免疫")]
        DebuffImmunity,

        [Description("隐身")]
        Invisible,

        [Description("额外力量")]
        ExtraPower,

        [Description("额外生存")]
        ExtraSurvivability,

        [Description("额外行动")]
        ExtraTurn,

        [Description("自动护盾")]
        AutoArmor,

        [Description("壁垒")]
        Barricade,
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
        BuffNature = GetNature(name);
        Stack = stack;
    }

    public static Nature GetNature(BuffName name)
    {
        return name switch
        {
            BuffName.RebirthI => Nature.positive,
            BuffName.DamageImmune => Nature.positive,
            BuffName.Vulnerable => Nature.negative,
            BuffName.Taunt => Nature.positive,
            BuffName.Thorn => Nature.positive,
            BuffName.Stun => Nature.negative,
            BuffName.Pursuit => Nature.positive,
            BuffName.DebuffImmunity => Nature.positive,
            BuffName.ExtraPower => Nature.positive,
            BuffName.ExtraSurvivability => Nature.positive,
            BuffName.ExtraTurn => Nature.positive,
            BuffName.AutoArmor => Nature.positive,
            BuffName.Barricade => Nature.positive,
            _ => Nature.positive,
        };
    }

    public static bool IsDebuff(BuffName name) => GetNature(name) == Nature.negative;

    protected static void RecordBuffGain(
        Character target,
        BuffName name,
        int stack,
        Character source = null
    )
    {
        if (target?.BattleNode == null || stack == 0)
            return;

        target.BattleNode.RecordBuffGain(target, name, stack, source);
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
        string suffix = which switch
        {
            BuffHintLabel.Which.vanish => "[color=yellow]消失[/color]",
            BuffHintLabel.Which.gain => "[color=yellow]获得[/color]",
            _ => string.Empty,
        };
        BuffHintLabel.Spawn(Owner, $"{name.GetDescription()}{suffix}", Owner.GlobalPosition);
    }

    protected Label GetStackLabel() =>
        BuffIcon != null && GodotObject.IsInstanceValid(BuffIcon)
            ? BuffIcon.GetChildOrNull<Label>(0)
            : null;

    protected void UpdateStackLabel()
    {
        var label = GetStackLabel();
        if (label != null)
            label.Text = Stack.ToString();
    }

    protected static ColorRect CreateBuffIcon(BuffName name)
    {
        if (!IconScenePaths.TryGetValue(name, out var scenePath))
            return null;

        return GD.Load<PackedScene>(scenePath).Instantiate() as ColorRect;
    }

    protected static bool TryStackExisting<TBuff>(
        List<TBuff> buffs,
        BuffName name,
        int stack,
        Character target,
        Character source = null
    )
        where TBuff : Buff
    {
        var existingBuff = buffs?.FirstOrDefault(x => x != null && x.ThisBuffName == name);
        if (existingBuff == null)
            return false;

        existingBuff.Stack += stack;
        existingBuff.UpdateStackLabel();
        existingBuff.TweenLabel();
        existingBuff.Hint(existingBuff.ThisBuffName, BuffHintLabel.Which.gain);
        existingBuff.BuffAddAnimation();
        RecordBuffGain(target, name, stack, source);
        return true;
    }

    protected static void FinalizeBuffAdd(Buff buff, Character target, Character source = null)
    {
        if (buff?.BuffIcon == null || target?.StateIconContainer == null)
            return;

        buff.TweenLabel();
        buff.Hint(buff.ThisBuffName, BuffHintLabel.Which.gain);
        buff.UpdateStackLabel();
        target.StateIconContainer.AddChild(buff.BuffIcon);
        buff.BuffAddAnimation();
        RecordBuffGain(target, buff.ThisBuffName, buff.Stack, source);
    }

    protected bool TryRemoveIfEmpty<TBuff>(List<TBuff> buffs, bool showVanishHint = true)
        where TBuff : Buff
    {
        if (Stack != 0)
            return false;

        if (BuffIcon != null && GodotObject.IsInstanceValid(BuffIcon))
        {
            BuffIcon.QueueFree();
        }

        BuffIcon = null;
        buffs?.Remove((TBuff)this);

        if (showVanishHint)
            Hint(ThisBuffName, BuffHintLabel.Which.vanish);

        return true;
    }
}

public class DyingBuff : Buff
{
    public DyingBuff(Character owner, BuffName name, int stack)
        : base(owner, name, stack) { }

    public Task Trigger()
    {
        using var _ = Owner?.BeginEffectSource(ThisBuffName.GetDescription());
        switch (ThisBuffName)
        {
            case BuffName.RebirthI:
                if (Stack >= 1)
                {
                    Owner.Recover(Owner.BattleMaxLife / 2, true, Owner);
                    Stack--;
                }
                break;
        }
        TweenLabel();
        TryRemoveIfEmpty(Owner.DyingBuffs, showVanishHint: false);
        return Task.CompletedTask;
    }

    public static void BuffAdd(BuffName name, Character target, int stack, Character source = null)
    {
        if (TryStackExisting(target?.DyingBuffs, name, stack, target, source))
            return;

        if (name != BuffName.RebirthI || target?.DyingBuffs == null)
            return;

        var buff = new DyingBuff(target, name, stack) { BuffIcon = CreateBuffIcon(name) };
        if (buff.BuffIcon == null)
            return;

        target.DyingBuffs.Add(buff);
        FinalizeBuffAdd(buff, target, source);
    }
}

public partial class HurtBuff : Buff
{
    public HurtBuff(Character owner, BuffName name, int stack)
        : base(owner, name, stack) { }

    public async Task<float> Trigger(
        float damage,
        Character attacker = null,
        bool canTriggerThorn = true
    )
    {
        switch (ThisBuffName)
        {
            case BuffName.DamageImmune:
                damage = 0;
                Stack--;
                UpdateStackLabel();
                break;
            case BuffName.Vulnerable:
                damage *= 1.5f;
                Stack--;
                UpdateStackLabel();
                break;
            case BuffName.Taunt:
                Stack--;
                break;
            case BuffName.Thorn:
                if (
                    canTriggerThorn
                    && Owner != null
                    && attacker != null
                    && attacker != Owner
                    && attacker.State != Character.CharacterState.Dying
                    && Stack > 0
                )
                {
                    using var _ = Owner.BeginEffectSource(ThisBuffName.GetDescription());
                    await attacker.GetHurt(Stack, Owner, canTriggerThorn: false);
                }
                break;
            case BuffName.AutoArmor:
                if (Owner != null && Stack > 0)
                {
                    Owner.CallDeferred(nameof(Character.UpdataBlock), Stack, true, Owner);
                }
                break;
        }
        TweenLabel();
        TryRemoveIfEmpty(Owner.HurtBuffs);
        return damage;
    }

    public static void BuffAdd(BuffName name, Character target, int stack, Character source = null)
    {
        if (IsDebuff(name) && SpecialBuff.TryConsumeDebuffImmunity(target))
            return;

        if (TryStackExisting(target?.HurtBuffs, name, stack, target, source))
            return;

        if (
            target?.HurtBuffs == null
            || (
                name != BuffName.DamageImmune
                && name != BuffName.Vulnerable
                && name != BuffName.Taunt
                && name != BuffName.Thorn
                && name != BuffName.AutoArmor
            )
        )
            return;

        var buff = new HurtBuff(target, name, stack) { BuffIcon = CreateBuffIcon(name) };
        if (buff.BuffIcon == null)
            return;

        target.HurtBuffs.Add(buff);
        FinalizeBuffAdd(buff, target, source);
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
            case BuffName.Invisible:
                Stack--;
                UpdateStackLabel();
                break;
            case BuffName.Barricade:
                // Passive effect: checked by Character.StartAction before block reset.
                break;
        }

        TweenLabel();
        TryRemoveIfEmpty(Owner.StartActionBuffs);
    }

    public static void BuffAdd(BuffName name, Character target, int stack, Character source = null)
    {
        if (IsDebuff(name) && SpecialBuff.TryConsumeDebuffImmunity(target))
            return;

        if (TryStackExisting(target?.StartActionBuffs, name, stack, target, source))
            return;

        if (
            target?.StartActionBuffs == null
            || (name != BuffName.Invisible && name != BuffName.Barricade)
        )
            return;

        var buff = new StartActionBuff(target, name, stack) { BuffIcon = CreateBuffIcon(name) };
        if (buff.BuffIcon == null)
            return;

        target.StartActionBuffs.Add(buff);
        FinalizeBuffAdd(buff, target, source);
    }
}

public partial class SkillBuff : Buff
{
    public SkillBuff(Character owner, BuffName name, int stack)
        : base(owner, name, stack) { }

    public async Task Trigger(Skill skill)
    {
        if (Stack <= 0)
            return;

        switch (ThisBuffName)
        {
            case BuffName.Stun:
                Stack--;
                UpdateStackLabel();

                if (Owner != null)
                {
                    BuffHintLabel.Spawn(
                        Owner,
                        "[color=yellow]\u65e0\u6cd5\u884c\u52a8[/color]",
                        Owner.GlobalPosition,
                        randomOffset: true
                    );
                }

                if (skill?.OwnerCharater?.CharacterEffectScene != null)
                {
                    var effect =
                        skill.OwnerCharater.CharacterEffectScene.Instantiate<CharacterEffect>();
                    skill.OwnerCharater.AddChild(effect);
                    effect.Animation.Play("stun");
                    await skill.OwnerCharater.ToSignal(effect.Animation, "animation_finished");
                }
                break;
        }

        TweenLabel();
        TryRemoveIfEmpty(Owner.SkillBuffs);
    }

    public static void BuffAdd(BuffName name, Character target, int stack, Character source = null)
    {
        if (IsDebuff(name) && SpecialBuff.TryConsumeDebuffImmunity(target))
            return;

        if (target?.SkillBuffs == null)
            return;

        if (TryStackExisting(target.SkillBuffs, name, stack, target, source))
            return;

        if (name != BuffName.Stun)
            return;

        var buff = new SkillBuff(target, name, stack) { BuffIcon = CreateBuffIcon(name) };
        if (buff.BuffIcon == null)
            return;

        target.SkillBuffs.Add(buff);
        FinalizeBuffAdd(buff, target, source);
    }
}

public partial class EndActionBuff : Buff
{
    public EndActionBuff(Character owner, BuffName name, int stack)
        : base(owner, name, stack) { }

    public async Task Trigger()
    {
        if (Stack <= 0 || Owner == null)
            return;

        using var _ = Owner.BeginEffectSource(ThisBuffName.GetDescription());

        switch (ThisBuffName)
        {
            case BuffName.Pursuit:
                Stack--;
                UpdateStackLabel();

                var skill = new Skill(Skill.SkillTypes.Attack) { OwnerCharater = Owner };
                await skill.Attack1(Owner.BattlePower);
                break;
            case BuffName.ExtraTurn:
                Stack--;
                UpdateStackLabel();
                Owner.OnTurnStart();
                break;
        }

        TweenLabel();
        TryRemoveIfEmpty(Owner.EndActionBuffs);
    }

    public static void BuffAdd(BuffName name, Character target, int stack, Character source = null)
    {
        if (target?.EndActionBuffs == null)
            return;

        if (TryStackExisting(target.EndActionBuffs, name, stack, target, source))
            return;

        if (name != BuffName.Pursuit && name != BuffName.ExtraTurn)
            return;

        var buff = new EndActionBuff(target, name, stack) { BuffIcon = CreateBuffIcon(name) };
        if (buff.BuffIcon == null)
            return;

        target.EndActionBuffs.Add(buff);
        FinalizeBuffAdd(buff, target, source);
    }
}

public partial class SpecialBuff : Buff
{
    public SpecialBuff(Character owner, BuffName name, int stack)
        : base(owner, name, stack) { }

    public static bool TryConsumeDebuffImmunity(Character target)
    {
        if (target?.SpecialBuffs == null)
            return false;

        var immunity = target.SpecialBuffs.FirstOrDefault(x =>
            x != null && x.ThisBuffName == BuffName.DebuffImmunity && x.Stack > 0
        );
        if (immunity == null)
            return false;

        immunity.Stack--;
        immunity.UpdateStackLabel();

        immunity.TweenLabel();
        immunity.TryRemoveIfEmpty(target.SpecialBuffs);

        return true;
    }

    public static void BuffAdd(BuffName name, Character target, int stack, Character source = null)
    {
        if (target?.SpecialBuffs == null)
            return;

        if (TryStackExisting(target.SpecialBuffs, name, stack, target, source))
            return;

        if (
            name != BuffName.DebuffImmunity
            && name != BuffName.ExtraPower
            && name != BuffName.ExtraSurvivability
        )
            return;

        var buff = new SpecialBuff(target, name, stack) { BuffIcon = CreateBuffIcon(name) };
        if (buff.BuffIcon == null)
            return;

        target.SpecialBuffs.Add(buff);
        FinalizeBuffAdd(buff, target, source);
    }
}

public enum Nature
{
    positive,
    negative,
}
