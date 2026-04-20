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
    private sealed class VisualBudgetState
    {
        public ulong WindowStartMsec;
        public int HintCount;
        public int GhostCount;
    }

    private const ulong VisualBudgetWindowMsec = 90;
    private const int MaxHintsPerWindow = 5;
    private const int MaxGhostBurstsPerWindow = 3;
    private const ulong GhostExplodeBudgetWindowMsec = 90;
    private const int MaxGhostExplodesPerWindowGlobal = 8;
    private static readonly Dictionary<ulong, VisualBudgetState> VisualBudgetByOwner = new();
    private static readonly Dictionary<Rid, Shader> AdditiveGhostShaderCache = new();
    private static ulong _ghostExplodeWindowStartMsec;
    private static int _ghostExplodeCountInWindow;

    public static PackedScene HintScene = GD.Load<PackedScene>(
        "res://LabelNode/BuffHintLabel.tscn"
    );
    private static readonly Dictionary<BuffName, string> IconScenePaths = new()
    {
        [BuffName.RebirthI] = "res://battle/buff/StateIcon/Rebirth.tscn",
        [BuffName.DamageImmune] = "res://battle/buff/StateIcon/Buffer.tscn",
        [BuffName.Vulnerable] = "res://battle/buff/StateIcon/Vulnerable.tscn",
        [BuffName.Weaken] = "res://battle/buff/StateIcon/Weaken.tscn",
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
            BuffName.ExtraTurn => "回合结束时消耗1层，触发1次额外出手（不触发回合开始/结束效果）。",
            BuffName.AutoArmor => "受到伤害后获得等同层数的格挡。",
            BuffName.Barricade => "回合开始时，保留你的格挡。",
            BuffName.Weaken => "造成的伤害降低25%，每次攻击后消耗1层。",
            _ => string.Empty,
        };

        return string.IsNullOrWhiteSpace(key) ? string.Empty : TranslationServer.Translate(key);
    }

    private static bool TryConsumeGhostExplodeBudget()
    {
        ulong now = Time.GetTicksMsec();
        if (
            _ghostExplodeWindowStartMsec == 0
            || now - _ghostExplodeWindowStartMsec > GhostExplodeBudgetWindowMsec
        )
        {
            _ghostExplodeWindowStartMsec = now;
            _ghostExplodeCountInWindow = 0;
        }

        if (_ghostExplodeCountInWindow >= MaxGhostExplodesPerWindowGlobal)
            return false;

        _ghostExplodeCountInWindow++;
        return true;
    }

    public static void GhostExplode(
        Control node,
        Vector2 scale,
        Node parent = null,
        bool useOffsetMotion = false
    )
    {
        if (node == null || !GodotObject.IsInstanceValid(node))
            return;
        if (!TryConsumeGhostExplodeBudget())
            return;

        var ghost = node.Duplicate() as Control;
        if (ghost == null)
            return;
        if (ghost.GetChildCount() > 0)
            ghost.GetChild(0).QueueFree();
        ghost.SetAnchorsPreset(Control.LayoutPreset.TopLeft);

        if (ghost.Material is ShaderMaterial originalMat && originalMat.Shader != null)
        {
            var newMat = (ShaderMaterial)originalMat.Duplicate();
            Rid sourceShaderRid = originalMat.Shader.GetRid();
            if (
                !AdditiveGhostShaderCache.TryGetValue(sourceShaderRid, out Shader additiveShader)
                || additiveShader == null
                || !GodotObject.IsInstanceValid(additiveShader)
            )
            {
                additiveShader = (Shader)originalMat.Shader.Duplicate();
                string code = additiveShader.Code;
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

                additiveShader.Code = code;
                AdditiveGhostShaderCache[sourceShaderRid] = additiveShader;
            }

            newMat.Shader = additiveShader;
            ghost.Material = newMat;
        }

        if (parent != null)
            parent.AddChild(ghost);
        else
            node.AddChild(ghost);

        ghost.PivotOffset = ghost.Size / 2;
        Vector2 centeredPos = -ghost.Size / 2;
        Vector2 basePos = centeredPos;
        Vector2 jitterPos = basePos;
        Vector2 arcPos = basePos;
        Vector2 endPos = basePos;

        if (useOffsetMotion)
        {
            basePos = centeredPos + new Vector2(0, -22);
            float lateral = (float)GD.RandRange(-28.0, 28.0);
            float lift = (float)GD.RandRange(20.0, 34.0);
            float settle = (float)GD.RandRange(-10.0, 10.0);
            float jitterX = (float)GD.RandRange(-8.0, 8.0);
            float jitterY = (float)GD.RandRange(-4.0, 4.0);

            jitterPos = basePos + new Vector2(jitterX, jitterY);
            arcPos = basePos + new Vector2(lateral * 0.55f, -lift);
            endPos = basePos + new Vector2(lateral + settle, -lift - 24f);
        }

        float spinDeg = (float)GD.RandRange(-14.0, 14.0);
        float spinRad = Mathf.DegToRad(spinDeg);
        float settleSpinRad = spinRad * 0.28f;

        // Brighter than white to emphasize additive glow.
        Godot.Color flashColor = new Godot.Color(1.35f, 1.28f, 1.55f, 1.0f);
        Godot.Color midColor = new Godot.Color(1.1f, 1.08f, 1.28f, 0.76f);
        Godot.Color fadeColor = new Godot.Color(0.8f, 0.86f, 1.12f, 0.0f);

        ghost.Position = basePos;
        ghost.Rotation = 0f;
        ghost.Modulate = new Godot.Color(1.1f, 1.1f, 1.2f, 0.95f);
        ghost.Scale = Vector2.One;

        var tween = ghost.CreateTween();

        // Stage 1: small jitter + flash pop.
        tween.SetParallel(true);
        tween
            .TweenProperty(ghost, "position", jitterPos, 0.06f)
            .SetTrans(Tween.TransitionType.Sine)
            .SetEase(Tween.EaseType.Out);
        tween
            .TweenProperty(ghost, "rotation", spinRad * 0.35f, 0.06f)
            .SetTrans(Tween.TransitionType.Sine)
            .SetEase(Tween.EaseType.Out);
        tween
            .TweenProperty(ghost, "modulate", flashColor, 0.06f)
            .SetTrans(Tween.TransitionType.Quad)
            .SetEase(Tween.EaseType.Out);
        tween
            .TweenProperty(ghost, "scale", scale * 0.92f, 0.06f)
            .SetTrans(Tween.TransitionType.Back)
            .SetEase(Tween.EaseType.Out);

        // Stage 2: arc lift with slight rotation and glow.
        tween.SetParallel(false);
        tween.SetParallel(true);
        tween
            .TweenProperty(ghost, "position", arcPos, 0.22f)
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.Out);
        tween
            .TweenProperty(ghost, "rotation", spinRad, 0.22f)
            .SetTrans(Tween.TransitionType.Quad)
            .SetEase(Tween.EaseType.Out);
        tween
            .TweenProperty(ghost, "modulate", midColor, 0.22f)
            .SetTrans(Tween.TransitionType.Quad)
            .SetEase(Tween.EaseType.Out);
        tween
            .TweenProperty(ghost, "scale", scale * 1.08f, 0.22f)
            .SetTrans(Tween.TransitionType.Back)
            .SetEase(Tween.EaseType.Out);

        // Stage 3: curved drift + fade out.
        tween.SetParallel(false);
        tween.SetParallel(true);
        tween
            .TweenProperty(ghost, "position", endPos, 0.44f)
            .SetTrans(Tween.TransitionType.Quad)
            .SetEase(Tween.EaseType.Out);
        tween
            .TweenProperty(ghost, "rotation", settleSpinRad, 0.44f)
            .SetTrans(Tween.TransitionType.Sine)
            .SetEase(Tween.EaseType.InOut);
        tween
            .TweenProperty(ghost, "modulate", fadeColor, 0.44f)
            .SetTrans(Tween.TransitionType.Quad)
            .SetEase(Tween.EaseType.In);
        tween
            .TweenProperty(ghost, "scale", scale * 1.22f, 0.44f)
            .SetTrans(Tween.TransitionType.Quad)
            .SetEase(Tween.EaseType.Out);

        tween.SetParallel(false);
        tween.TweenCallback(Callable.From(ghost.QueueFree));
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

        [Description("虚弱")]
        Weaken,

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
            BuffName.Weaken => Nature.negative,
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

    private static void CleanupVisualBudget(ulong now)
    {
        if (VisualBudgetByOwner.Count <= 96)
            return;

        const ulong staleThresholdMsec = VisualBudgetWindowMsec * 12;
        var staleKeys = new List<ulong>();
        foreach (var pair in VisualBudgetByOwner)
        {
            bool invalidOwner = !GodotObject.IsInstanceIdValid(pair.Key);
            bool stale = now - pair.Value.WindowStartMsec > staleThresholdMsec;
            if (invalidOwner || stale)
                staleKeys.Add(pair.Key);
        }

        for (int i = 0; i < staleKeys.Count; i++)
            VisualBudgetByOwner.Remove(staleKeys[i]);
    }

    private static bool TryConsumeVisualBudget(Character owner, bool consumeHint)
    {
        if (owner == null || !GodotObject.IsInstanceValid(owner))
            return false;

        ulong now = Time.GetTicksMsec();
        ulong ownerId = owner.GetInstanceId();
        if (!VisualBudgetByOwner.TryGetValue(ownerId, out var state))
        {
            state = new VisualBudgetState { WindowStartMsec = now };
            VisualBudgetByOwner[ownerId] = state;
        }

        if (now - state.WindowStartMsec > VisualBudgetWindowMsec)
        {
            state.WindowStartMsec = now;
            state.HintCount = 0;
            state.GhostCount = 0;
        }

        bool allow;
        if (consumeHint)
        {
            allow = state.HintCount < MaxHintsPerWindow;
            if (allow)
                state.HintCount++;
        }
        else
        {
            allow = state.GhostCount < MaxGhostBurstsPerWindow;
            if (allow)
                state.GhostCount++;
        }

        CleanupVisualBudget(now);
        return allow;
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
        if (!TryConsumeVisualBudget(Owner, consumeHint: false))
            return;

        if (BuffIcon == null || !GodotObject.IsInstanceValid(BuffIcon))
            return;

        var depIcon = BuffIcon.Duplicate() as ColorRect;
        if (depIcon == null)
            return;

        // Avoid Godot warning: setting Size on Controls with stretched anchors gets overridden after _Ready.
        depIcon.SetAnchorsPreset(Control.LayoutPreset.TopLeft);
        depIcon.Size = new Vector2(200, 200);
        GhostExplode(depIcon, new Vector2(2f, 2f), Owner, useOffsetMotion: false);
        depIcon.Free();
    }

    public void Hint(BuffName name, BuffHintLabel.Which which)
    {
        if (!TryConsumeVisualBudget(Owner, consumeHint: true))
            return;

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
        Owner?.InvalidateBuffTooltipCache();
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
        target?.InvalidateBuffTooltipCache();
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
        target.InvalidateBuffTooltipCache();
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
        Owner?.InvalidateBuffTooltipCache();

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
                    UpdateStackLabel();
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
                UpdateStackLabel();
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

public partial class AttackBuff : Buff
{
    private const float WeakenMultiplier = 0.75f;

    public sealed class PreviewState
    {
        private readonly Dictionary<AttackBuff, int> _stacks = new();

        public int GetStack(AttackBuff buff)
        {
            if (buff == null)
                return 0;

            return _stacks.TryGetValue(buff, out int stack) ? stack : buff.Stack;
        }

        public void SetStack(AttackBuff buff, int stack)
        {
            if (buff == null)
                return;

            _stacks[buff] = Math.Max(stack, 0);
        }
    }

    public struct TriggerContext
    {
        public int Damage;
        public Character Target;
        public bool ConsumeStack;
        public PreviewState State;
    }

    public AttackBuff(Character owner, BuffName name, int stack)
        : base(owner, name, stack) { }

    private int GetCurrentStack(PreviewState state) => state?.GetStack(this) ?? Stack;

    private void SetCurrentStack(ref TriggerContext context, int stack)
    {
        stack = Math.Max(stack, 0);
        if (context.State != null)
        {
            context.State.SetStack(this, stack);
            return;
        }

        Stack = stack;
        UpdateStackLabel();
        TweenLabel();
        TryRemoveIfEmpty(Owner.AttackBuffs);
    }

    public void Trigger(ref TriggerContext context)
    {
        int currentStack = GetCurrentStack(context.State);
        if (currentStack <= 0)
            return;

        switch (ThisBuffName)
        {
            case BuffName.Weaken:
                context.Damage = Math.Max(
                    (int)MathF.Floor(context.Damage * WeakenMultiplier),
                    0
                );
                if (context.ConsumeStack)
                {
                    SetCurrentStack(ref context, currentStack - 1);
                }
                break;
        }
    }

    public static void Trigger(Character attacker, ref TriggerContext context)
    {
        if (attacker?.AttackBuffs == null)
            return;

        foreach (var buff in attacker.AttackBuffs.Where(x => x != null).ToArray())
        {
            buff.Trigger(ref context);
        }

        context.Damage = Math.Max(context.Damage, 0);
    }

    public static int ApplyOutgoingDamageModifiers(
        Character attacker,
        int damage,
        Character target = null,
        bool consumeStacks = false,
        PreviewState previewState = null
    )
    {
        var context = new TriggerContext
        {
            Damage = damage,
            Target = target,
            ConsumeStack = consumeStacks,
            State = previewState,
        };
        Trigger(attacker, ref context);
        return context.Damage;
    }

    public static void BuffAdd(BuffName name, Character target, int stack, Character source = null)
    {
        if (IsDebuff(name) && SpecialBuff.TryConsumeDebuffImmunity(target))
            return;

        if (TryStackExisting(target?.AttackBuffs, name, stack, target, source))
            return;

        if (target?.AttackBuffs == null || name != BuffName.Weaken)
            return;

        var buff = new AttackBuff(target, name, stack) { BuffIcon = CreateBuffIcon(name) };
        if (buff.BuffIcon == null)
            return;

        target.AttackBuffs.Add(buff);
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
                await skill.Attack(Owner.BattlePower);
                break;
            case BuffName.ExtraTurn:
                Stack--;
                UpdateStackLabel();
                Owner.BattleNode?.RequestExtraAction(Owner);
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


