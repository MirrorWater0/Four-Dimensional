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
    public const float IconBaseSize = 40f;
    private const string TextureIconInsetMeta = "texture_icon_inset";

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
    public static PackedScene BuffGainParticleScene = GD.Load<PackedScene>(
        "res://battle/Effect/BuffGainParticle.tscn"
    );
    public static PackedScene BuffTriggerFlashVfxScene = GD.Load<PackedScene>(
        "res://battle/Effect/BuffTriggerFlashVfx.tscn"
    );
    private static readonly string IconTemplateScenePath = "res://battle/buff/IconTemplate.tscn";
    private static readonly Dictionary<BuffName, string> IconScenePaths = new()
    {
        [BuffName.RebirthI] = "res://battle/buff/StateIcon/Rebirth.tscn",
        [BuffName.Stun] = "res://battle/buff/StateIcon/Stun.tscn",
        [BuffName.Pursuit] = "res://battle/buff/StateIcon/Pursuit.tscn",
        [BuffName.EternalDark] = "res://battle/buff/StateIcon/EternalDark.tscn",
        [BuffName.Beacon] = "res://battle/buff/StateIcon/Beacon.tscn",
        [BuffName.Afterimage] = "res://battle/buff/StateIcon/Afterimage.tscn",
        [BuffName.Divinity] = "res://battle/buff/StateIcon/Divinity.tscn",
        [BuffName.Echo] = "res://battle/buff/StateIcon/Echo.tscn",
        [BuffName.CursePower] = "res://battle/buff/StateIcon/CursePower.tscn",
        [BuffName.WeakeningField] = "res://battle/buff/StateIcon/WeakeningField.tscn",
        [BuffName.Shadow] = "res://battle/buff/StateIcon/Shadow.tscn",
        [BuffName.Demon] = "res://battle/buff/StateIcon/Demon.tscn",
        [BuffName.Void] = "res://battle/buff/StateIcon/Void.tscn",
        [BuffName.Sanctuary] = "res://battle/buff/StateIcon/Sanctuary.tscn",
        [BuffName.Source] = "res://battle/buff/StateIcon/Source.tscn",
        [BuffName.NextEnergy] = "res://battle/buff/StateIcon/NextEnergy.tscn",
    };

    private readonly record struct TextureIconOverride(
        string TexturePath,
        Godot.Color Modulate,
        float Inset = 7.5f
    );

    private static readonly Dictionary<BuffName, TextureIconOverride> TextureIconOverrides = new()
    {
        [BuffName.AttackCount] = new(
            "res://asset/svg/BuffIcon/Kenney/sword.svg",
            new Godot.Color(1f, 0.72f, 0.3f, 1f),
            8f
        ),
        [BuffName.TemporaryAttackCount] = new(
            "res://asset/svg/BuffIcon/Kenney/dice_sword.svg",
            new Godot.Color(0.4f, 0.9f, 1f, 1f),
            8f
        ),
        [BuffName.DamageImmune] = new(
            "res://asset/svg/BuffIcon/Kenney/shield.svg",
            new Godot.Color(0.22f, 0.95f, 0.65f, 1f)
        ),
        [BuffName.Vulnerable] = new(
            "res://asset/svg/BuffIcon/Kenney/suit_hearts_broken.svg",
            new Godot.Color(1f, 0.22f, 0.2f, 1f),
            8.5f
        ),
        [BuffName.Weaken] = new(
            "res://asset/svg/BuffIcon/Kenney/sword.svg",
            new Godot.Color(0.55f, 0.55f, 0.72f, 1f),
            8f
        ),
        [BuffName.Fear] = new(
            "res://asset/svg/BuffIcon/Kenney/skull.svg",
            new Godot.Color(0.94f, 0.22f, 0.3f, 1f),
            8f
        ),
        [BuffName.Taunt] = new(
            "res://asset/svg/BuffIcon/Kenney/card_target.svg",
            new Godot.Color(1f, 0.78f, 0.28f, 1f)
        ),
        [BuffName.Thorn] = new(
            "res://asset/svg/BuffIcon/Kenney/dice_sword.svg",
            new Godot.Color(0.2f, 0.96f, 0.55f, 1f)
        ),
        [BuffName.DebuffImmunity] = new(
            "res://asset/BuffIcon/DebuffImmunity.png",
            new Godot.Color(1f, 0.86f, 0.16f, 1f),
            3f
        ),
        [BuffName.EnergyStorage] = new(
            "res://asset/third_party/kenney_board_game_icons/PNG/Default (64px)/hexagon_tile.png",
            Colors.White,
            4f
        ),
        [BuffName.Invisible] = new(
            "res://asset/svg/BuffIcon/Invisible.svg",
            Colors.White,
            3f
        ),
        [BuffName.Swift] = new(
            "res://asset/svg/BuffIcon/Kenney/arrow_right_curve.svg",
            new Godot.Color(0.32f, 0.78f, 1f, 1f),
            8f
        ),
        [BuffName.ExtraPower] = new(
            "res://asset/svg/BuffIcon/Kenney/sword.svg",
            new Godot.Color(1f, 0.45f, 0.22f, 1f),
            8f
        ),
        [BuffName.ExtraSurvivability] = new(
            "res://asset/svg/BuffIcon/Kenney/tag_shield.svg",
            new Godot.Color(0.26f, 0.78f, 1f, 1f)
        ),
        [BuffName.AutoArmor] = new(
            "res://asset/svg/BuffIcon/Kenney/dice_shield.svg",
            new Godot.Color(0.34f, 0.9f, 1f, 1f)
        ),
        [BuffName.Barricade] = new(
            "res://asset/svg/BuffIcon/Kenney/structure_wall.svg",
            new Godot.Color(0.78f, 0.86f, 1f, 1f),
            8f
        ),
        [BuffName.Disaster] = new(
            "res://asset/svg/BuffIcon/Kenney/fire.svg",
            new Godot.Color(1f, 0.38f, 0.18f, 1f),
            8f
        ),
        [BuffName.ExtraDraw] = new(
            "res://asset/svg/BuffIcon/Kenney/cards_take.svg",
            new Godot.Color(0.42f, 0.96f, 1f, 1f),
            8f
        ),
        [BuffName.Foresight] = new(
            "res://asset/svg/BuffIcon/Kenney/cards_seek_top.svg",
            new Godot.Color(0.78f, 0.62f, 1f, 1f),
            8f
        ),
        [BuffName.ExhaustShield] = new(
            "res://asset/third_party/kenney_board_game_icons/PNG/Default (64px)/cards_stack_cross.png",
            new Godot.Color(0.38f, 0.86f, 1f, 1f),
            7.5f
        ),
        [BuffName.NoDraw] = new(
            "res://asset/third_party/kenney_board_game_icons/PNG/Default (64px)/hand_cross.png",
            new Godot.Color(1f, 0.38f, 0.48f, 1f),
            7.5f
        ),
        [BuffName.Search] = new(
            "res://asset/third_party/kenney_board_game_icons/PNG/Default (64px)/cards_seek.png",
            new Godot.Color(1f, 0.82f, 0.3f, 1f),
            7.5f
        ),
        [BuffName.Prediction] = new(
            "res://asset/third_party/kenney_board_game_icons/PNG/Default (64px)/cards_order.png",
            new Godot.Color(0.72f, 0.58f, 1f, 1f),
            7.5f
        ),
        [BuffName.Recycling] = new(
            "res://asset/third_party/kenney_board_game_icons/Vector/Icons/card_rotate.svg",
            new Godot.Color(0.34f, 1f, 0.78f, 1f),
            8f
        ),
    };

    private static string GetBuffNameKey(BuffName name)
    {
        return $"buff.{I18n.ToSnakeCase(name.ToString())}.name";
    }

    public static string GetBuffDisplayName(BuffName name) =>
        I18n.Tr(GetBuffNameKey(name), GetLegacyBuffName(name));

    public static string GetBuffEffectText(BuffName name)
    {
        string fallback = name switch
        {
            BuffName.AttackCount => "回合结束时，按层数进行普通攻击；默认1次，持续至本场战斗结束。",
            BuffName.TemporaryAttackCount => "本回合结束时，每层额外进行1次普通攻击；攻击结算后清零。",
            BuffName.RebirthI => "濒死时，回复最大生命的50%，消耗1层。",
            BuffName.DamageImmune => "受到伤害时，伤害变为0，消耗1层。",
            BuffName.Vulnerable => "受到攻击时，伤害提高50%；阵营回合开始时减少1层。",
            BuffName.Fear => "使用攻击技能时，每层受到1点伤害。",
            BuffName.Taunt => "敌方攻击只能锁定该目标；对方阵营回合结束时减少1层。",
            BuffName.Thorn => "受到攻击时，每层对攻击者造成1点伤害。",
            BuffName.Stun => "下1次释放技能会被阻止，并固定失去1点能量；触发后消耗1层。",
            BuffName.Pursuit => $"阵营回合结束时，造成等同于{PropertyType.Power.GetDescription()}的伤害。",
            BuffName.DebuffImmunity => "抵消1次负面状态添加，消耗1层。",
            BuffName.Invisible => "其他角色存活时,无法被选为攻击目标；对方阵营回合结束时减少1层。",
            BuffName.Swift => "阵营回合开始时，每层抽1张牌。",
            BuffName.ExtraPower => "获得力量时，每层额外获得1点力量。",
            BuffName.ExtraSurvivability => "获得生存时，每层额外获得1点生存。",
            BuffName.AutoArmor => "受到攻击后，每层获得1点格挡。",
            BuffName.Barricade => "阵营回合开始时，保留你的格挡。",
            BuffName.Afterimage => "阵营回合开始时，格挡不会消失，减少1层。",
            BuffName.Weaken => "造成的伤害降低25%；阵营回合结束时减少1层。",
            BuffName.Disaster => "己方阵营回合结束时，每层受到1点伤害，并消耗1层。",
            BuffName.Divinity => "攻击伤害变为2倍；回合开始时消耗1层。",
            BuffName.Shadow => "其他己方角色攻击时，己方全阵每层获得1点力量。",
            BuffName.Demon =>
                "每有一张牌被消耗，己方全阵每层获得1点力量。",
            BuffName.Void =>
                "任意己方角色使用生存牌时，己方全阵每层获得1点力量。",
            BuffName.Echo => "每回合每层使前1张技能牌释放2次。",
            BuffName.Sanctuary => "每当有角色恢复或失去生命时，己方全阵每层获得1点力量。",
            BuffName.ExtraDraw => "回合开始时，消耗1层抽1张牌。",
            BuffName.Source => "己方阵营回合开始时，每层额外获得1点能量。",
            BuffName.EnergyStorage => "阵营回合结束时，每层少失去1点能量。",
            BuffName.EternalDark => "回合开始时，每层获得1层隐身。",
            BuffName.NextEnergy => "下回合开始时，消耗所有层数，每层获得1点能量。",
            BuffName.Beacon => "获得格挡时，其他队友每层获得1点格挡。",
            BuffName.CursePower => "每次攻击时，每层给予目标1层虚弱。",
            BuffName.WeakeningField => "每给予1层虚弱，每层使己方全阵获得{block}点格挡。",
            BuffName.ExhaustShield => "每有一张牌被消耗，每层获得1点格挡。",
            BuffName.Foresight => "每次使用生成卡牌的技能时，每层抽1张牌。",
            BuffName.Recycling => "每当有一张牌被消耗时，抽1张牌。",
            BuffName.NoDraw => "无法抽牌；回合结束时减少1层。",
            BuffName.Search => "洗牌时，从抽牌堆中选择1张牌加入手牌。",
            BuffName.Prediction =>
                "回合开始时，每层查看抽牌堆顶部1张牌，并可将其中任意张放入弃牌堆。",
            _ => string.Empty,
        };

        if (string.IsNullOrWhiteSpace(fallback))
            return string.Empty;

        string key = $"buff.{I18n.ToSnakeCase(name.ToString())}.effect";
        return name switch
        {
            BuffName.Pursuit => I18n.Format(
                key,
                fallback,
                ("power", PropertyType.Power.GetDescription())
            ),
            BuffName.WeakeningField => I18n.Format(
                key,
                fallback,
                ("block", SpecialBuff.WeakeningFieldBlock)
            ),
            _ => I18n.Tr(key, fallback),
        };
    }

    private static string GetLegacyBuffName(BuffName name)
    {
        if (name == BuffName.Void)
            return "虚空";

        var field = typeof(BuffName).GetField(name.ToString());
        return field
                ?.GetCustomAttributes(typeof(DescriptionAttribute), false)
                .OfType<DescriptionAttribute>()
                .FirstOrDefault()
                ?.Description ?? name.ToString();
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
        bool useOffsetMotion = false,
        bool removeFirstChild = true,
        float alphaScale = 1f
    )
    {
        if (node == null || !GodotObject.IsInstanceValid(node))
            return;
        if (!TryConsumeGhostExplodeBudget())
            return;

        var ghost = node.Duplicate() as Control;
        if (ghost == null)
            return;
        if (removeFirstChild)
            StripStackLabels(ghost);
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

        bool preserveSourcePosition =
            parent != null && node.IsInsideTree() && parent is Control;
        Vector2 sourceCenterInParent = Vector2.Zero;
        if (preserveSourcePosition)
        {
            var parentControl = parent as Control;
            sourceCenterInParent =
                node.GetGlobalRect().GetCenter() - parentControl.GetGlobalRect().Position;
        }

        if (parent != null)
            parent.AddChild(ghost);
        else
            node.AddChild(ghost);

        ghost.PivotOffset = ghost.Size / 2;
        Vector2 centeredPos = preserveSourcePosition
            ? sourceCenterInParent - ghost.Size / 2
            : -ghost.Size / 2;
        Vector2 basePos = centeredPos;
        Vector2 flashPos = basePos;
        Vector2 arcPos = basePos;
        Vector2 endPos = basePos;

        if (useOffsetMotion)
        {
            basePos = centeredPos + new Vector2(0, -22);
            float lateral = (float)GD.RandRange(-28.0, 28.0);
            float lift = (float)GD.RandRange(20.0, 34.0);
            float settle = (float)GD.RandRange(-10.0, 10.0);

            flashPos = basePos;
            arcPos = basePos + new Vector2(lateral * 0.55f, -lift);
            endPos = basePos + new Vector2(lateral + settle, -lift - 24f);
        }

        float spinDeg = (float)GD.RandRange(-14.0, 14.0);
        float spinRad = Mathf.DegToRad(spinDeg);
        float settleSpinRad = spinRad * 0.28f;

        // Brighter than white to emphasize additive glow.
        alphaScale = Mathf.Clamp(alphaScale, 0f, 1f);
        Godot.Color flashColor = new Godot.Color(1.35f, 1.28f, 1.55f, 1.0f * alphaScale);
        Godot.Color midColor = new Godot.Color(1.1f, 1.08f, 1.28f, 0.76f * alphaScale);
        Godot.Color fadeColor = new Godot.Color(0.8f, 0.86f, 1.12f, 0.0f);

        ghost.Position = basePos;
        ghost.Rotation = 0f;
        ghost.Modulate = new Godot.Color(1.1f, 1.1f, 1.2f, 0.95f * alphaScale);
        ghost.Scale = Vector2.One;

        var tween = ghost.CreateTween();

        // Stage 1: clean flash before the upward drift.
        tween.SetParallel(true);
        tween
            .TweenProperty(ghost, "position", flashPos, 0.06f)
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
            .SetTrans(Tween.TransitionType.Cubic)
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
            .SetTrans(Tween.TransitionType.Cubic)
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

        [Description("恐惧")]
        Fear,

        [Description("嘲讽")]
        Taunt,

        [Description("荆棘")]
        Thorn,

        [Description("晕眩")]
        Stun,

        [Description("追击")]
        Pursuit,

        [Description("减益免疫")]
        DebuffImmunity,

        [Description("隐身")]
        Invisible,

        [Description("永暗")]
        EternalDark,

        [Description("灯塔")]
        Beacon,

        [Description("额外力量")]
        ExtraPower,

        [Description("额外生存")]
        ExtraSurvivability,

        [Obsolete("Removed gameplay buff; kept as an enum placeholder.")]
        [Description("额外行动")]
        ExtraTurn,

        [Description("自动护盾")]
        AutoArmor,

        [Description("壁垒")]
        Barricade,

        [Description("残影")]
        Afterimage,

        [Description("灾厄")]
        Disaster,

        [Description("神格")]
        Divinity,

        [Description("暗影")]
        Shadow,

        [Description("恶魔")]
        Demon,

        [Description("虚无")]
        Void,

        [Description("回响")]
        Echo,

        [Description("咒力")]
        CursePower,

        [Description("虚弱立场")]
        WeakeningField,

        [Description("圣域")]
        Sanctuary,

        [Description("额外抽卡")]
        ExtraDraw,

        [Description("迅捷")]
        Swift,

        [Description("源泉")]
        Source,

        [Description("能量储存")]
        EnergyStorage,

        [Description("耗尽之盾")]
        ExhaustShield,

        [Description("预见")]
        Foresight,

        [Description("次回合能量")]
        NextEnergy,

        [Description("循环利用")]
        Recycling,

        [Description("无法抽牌")]
        NoDraw,

        [Description("搜寻")]
        Search,

        [Description("预测")]
        Prediction,

        [Description("攻击次数")]
        AttackCount,

        [Description("临时攻击")]
        TemporaryAttackCount,
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
            BuffName.AttackCount => Nature.positive,
            BuffName.TemporaryAttackCount => Nature.positive,
            BuffName.RebirthI => Nature.positive,
            BuffName.DamageImmune => Nature.positive,
            BuffName.Vulnerable => Nature.negative,
            BuffName.Weaken => Nature.negative,
            BuffName.Fear => Nature.negative,
            BuffName.Taunt => Nature.positive,
            BuffName.Thorn => Nature.positive,
            BuffName.Stun => Nature.negative,
            BuffName.Pursuit => Nature.positive,
            BuffName.DebuffImmunity => Nature.positive,
            BuffName.EternalDark => Nature.positive,
            BuffName.Swift => Nature.positive,
            BuffName.ExtraPower => Nature.positive,
            BuffName.ExtraSurvivability => Nature.positive,
            BuffName.AutoArmor => Nature.positive,
            BuffName.Barricade => Nature.positive,
            BuffName.Afterimage => Nature.positive,
            BuffName.Disaster => Nature.negative,
            BuffName.Divinity => Nature.positive,
            BuffName.Echo => Nature.positive,
            BuffName.CursePower => Nature.positive,
            BuffName.WeakeningField => Nature.positive,
            BuffName.Shadow => Nature.positive,
            BuffName.Demon => Nature.positive,
            BuffName.Void => Nature.positive,
            BuffName.Sanctuary => Nature.positive,
            BuffName.ExtraDraw => Nature.positive,
            BuffName.Source => Nature.positive,
            BuffName.EnergyStorage => Nature.positive,
            BuffName.Beacon => Nature.positive,
            BuffName.ExhaustShield => Nature.positive,
            BuffName.Foresight => Nature.positive,
            BuffName.NextEnergy => Nature.positive,
            BuffName.Recycling => Nature.positive,
            BuffName.NoDraw => Nature.negative,
            BuffName.Search => Nature.positive,
            BuffName.Prediction => Nature.positive,
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

        Label stackLabel = GetStackLabel();
        if (stackLabel == null)
            return;

        Tween tween = BuffIcon.CreateTween();
        stackLabel.PivotOffset = stackLabel.Size / 2;
        tween.TweenProperty(stackLabel, "scale", new Vector2(2f, 2f), 0.15f);
        tween.TweenProperty(stackLabel, "scale", new Vector2(1f, 1f), 0.35f);
    }

    public void FlashTrigger()
    {
        if (Stack <= 0)
            return;
        if (Owner == null || !GodotObject.IsInstanceValid(Owner))
            return;
        if (Owner.State == Character.CharacterState.Dying)
            return;
        if (!TryConsumeVisualBudget(Owner, consumeHint: false))
            return;

        FlashBuffIconPulse();
        Owner.PlayTargetLockPulse(new Godot.Color(1.36f, 1.08f, 1.82f, 1f), 0.82f);
        SpawnTriggerBodyVfx();
    }

    public static void FlashTriggersOnOwner(Character owner, BuffName name)
    {
        if (owner == null || !GodotObject.IsInstanceValid(owner))
            return;

        foreach (Buff buff in FindBuffsOnOwner(owner, name))
            buff.FlashTrigger();
    }

    private static IEnumerable<Buff> FindBuffsOnOwner(Character owner, BuffName name)
    {
        if (owner.StartActionBuffs != null)
        {
            foreach (StartActionBuff buff in owner.StartActionBuffs)
            {
                if (buff != null && buff.ThisBuffName == name && buff.Stack > 0)
                    yield return buff;
            }
        }

        if (owner.HurtBuffs != null)
        {
            foreach (HurtBuff buff in owner.HurtBuffs)
            {
                if (buff != null && buff.ThisBuffName == name && buff.Stack > 0)
                    yield return buff;
            }
        }

        if (owner.AttackBuffs != null)
        {
            foreach (AttackBuff buff in owner.AttackBuffs)
            {
                if (buff != null && buff.ThisBuffName == name && buff.Stack > 0)
                    yield return buff;
            }
        }

        if (owner.SkillBuffs != null)
        {
            foreach (SkillBuff buff in owner.SkillBuffs)
            {
                if (buff != null && buff.ThisBuffName == name && buff.Stack > 0)
                    yield return buff;
            }
        }

        if (owner.EndActionBuffs != null)
        {
            foreach (EndActionBuff buff in owner.EndActionBuffs)
            {
                if (buff != null && buff.ThisBuffName == name && buff.Stack > 0)
                    yield return buff;
            }
        }

        if (owner.SpecialBuffs != null)
        {
            foreach (SpecialBuff buff in owner.SpecialBuffs)
            {
                if (buff != null && buff.ThisBuffName == name && buff.Stack > 0)
                    yield return buff;
            }
        }

        if (owner.DyingBuffs != null)
        {
            foreach (DyingBuff buff in owner.DyingBuffs)
            {
                if (buff != null && buff.ThisBuffName == name && buff.Stack > 0)
                    yield return buff;
            }
        }
    }

    private void FlashBuffIconPulse()
    {
        if (BuffIcon == null || !GodotObject.IsInstanceValid(BuffIcon))
            return;

        BuffIcon.PivotOffset = BuffIcon.Size / 2;
        Tween tween = BuffIcon.CreateTween();
        tween
            .TweenProperty(BuffIcon, "scale", new Vector2(1.24f, 1.24f), 0.10f)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Cubic);
        tween
            .TweenProperty(BuffIcon, "scale", Vector2.One, 0.3f)
            .SetEase(Tween.EaseType.Out);
    }

    private void SpawnTriggerBodyVfx()
    {
        if (BuffTriggerFlashVfxScene == null || Owner == null || !GodotObject.IsInstanceValid(Owner))
            return;

        BuffTriggerFlashVfx.Spawn(this, Owner);
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
        RefreshTextureIconOverrideLayout(depIcon);
        GhostExplode(depIcon, new Vector2(2f, 2f), Owner, useOffsetMotion: false);
        PlayBuffIconGainPulse();
        PlayBuffGainParticle();
        depIcon.Free();
    }

    private void PlayBuffIconGainPulse()
    {
        if (BuffIcon == null || !GodotObject.IsInstanceValid(BuffIcon))
            return;

        BuffIcon.PivotOffset = BuffIcon.Size / 2;
        Vector2 baseScale = BuffIcon.Scale == Vector2.Zero ? Vector2.One : BuffIcon.Scale;
        Godot.Color baseModulate = BuffIcon.Modulate;

        Tween tween = BuffIcon.CreateTween();
        tween.SetParallel(true);
        tween
            .TweenProperty(BuffIcon, "scale", baseScale * 1.14f, 0.08f)
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.Out);
        tween
            .TweenProperty(
                BuffIcon,
                "modulate",
                new Godot.Color(1.32f, 1.24f, 1.78f, baseModulate.A),
                0.08f
            )
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.Out);
        tween.TweenProperty(BuffIcon, "scale", baseScale, 0.18f).SetDelay(0.08f);
        tween.TweenProperty(BuffIcon, "modulate", baseModulate, 0.18f).SetDelay(0.08f);
    }

    private void PlayBuffGainParticle()
    {
        if (Owner == null || !GodotObject.IsInstanceValid(Owner) || BuffGainParticleScene == null)
            return;

        BuffGainParticle.Spawn(Owner);
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
        BuffHintLabel.Spawn(Owner, $"{GetBuffDisplayName(name)}{suffix}", Owner.GlobalPosition);
    }

    protected Label GetStackLabel() => FindStackLabel(BuffIcon);

    private static Label FindStackLabel(Control icon)
    {
        if (icon == null || !GodotObject.IsInstanceValid(icon))
            return null;

        return icon.GetNodeOrNull<Label>("Label")
            ?? icon.GetChildren().OfType<Label>().FirstOrDefault();
    }

    private static void StripStackLabels(Node root)
    {
        if (root == null || !GodotObject.IsInstanceValid(root))
            return;

        foreach (Node child in root.GetChildren())
        {
            if (child is Label)
            {
                child.QueueFree();
                continue;
            }

            StripStackLabels(child);
        }
    }

    protected void UpdateStackLabel()
    {
        var label = GetStackLabel();
        if (label != null)
            label.Text = Stack.ToString();
        Owner?.InvalidateBuffTooltipCache();
        Owner?.BattleNode?.RefreshEnemyIntentionPreviews();
    }

    protected static ColorRect CreateBuffIcon(BuffName name)
    {
        if (TextureIconOverrides.ContainsKey(name))
        {
            var textureIcon = InstantiateBuffIconScene(IconTemplateScenePath);
            ApplyTextureIconOverride(textureIcon, name);
            return textureIcon;
        }

        if (!IconScenePaths.TryGetValue(name, out var scenePath))
            return null;

        return InstantiateBuffIconScene(scenePath);
    }

    private static ColorRect InstantiateBuffIconScene(string scenePath)
    {
        PackedScene scene = null;
        if (PreloadeScene.PreloadedScenes.TryGetValue(scenePath, out var cachedScene))
            scene = cachedScene;
        else
            scene = GD.Load<PackedScene>(scenePath);

        return scene?.Instantiate() as ColorRect;
    }

    private static void ApplyTextureIconOverride(ColorRect icon, BuffName name)
    {
        if (icon == null || !TextureIconOverrides.TryGetValue(name, out var config))
            return;

        Texture2D texture = GD.Load<Texture2D>(config.TexturePath);
        if (texture == null)
            return;

        icon.Material = null;
        icon.Color = Colors.Transparent;

        var existing = icon.GetNodeOrNull<TextureRect>("TextureIcon");
        existing?.QueueFree();

        var textureIcon = new TextureRect
        {
            Name = "TextureIcon",
            Texture = texture,
            ExpandMode = TextureRect.ExpandModeEnum.FitWidthProportional,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            SelfModulate = config.Modulate,
        };
        icon.SetMeta(TextureIconInsetMeta, config.Inset);
        textureIcon.SetMeta(TextureIconInsetMeta, config.Inset);
        textureIcon.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        textureIcon.OffsetLeft = config.Inset;
        textureIcon.OffsetTop = config.Inset;
        textureIcon.OffsetRight = -config.Inset;
        textureIcon.OffsetBottom = -config.Inset;
        icon.AddChild(textureIcon);
        icon.MoveChild(textureIcon, 0);
        FindStackLabel(icon)?.MoveToFront();
    }

    public static void RefreshTextureIconOverrideLayout(Control icon)
    {
        if (icon == null || !GodotObject.IsInstanceValid(icon))
            return;

        var textureIcon = icon.GetNodeOrNull<TextureRect>("TextureIcon");
        if (textureIcon == null)
            return;

        bool hasInsetMeta = icon.HasMeta(TextureIconInsetMeta)
            || textureIcon.HasMeta(TextureIconInsetMeta);
        if (!hasInsetMeta)
            return;

        float baseInset = icon.HasMeta(TextureIconInsetMeta)
            ? icon.GetMeta(TextureIconInsetMeta).AsSingle()
            : textureIcon.GetMeta(TextureIconInsetMeta).AsSingle();
        float size = Mathf.Max(IconBaseSize, Mathf.Min(icon.Size.X, icon.Size.Y));
        float inset = baseInset * (size / IconBaseSize);

        textureIcon.OffsetLeft = inset;
        textureIcon.OffsetTop = inset;
        textureIcon.OffsetRight = -inset;
        textureIcon.OffsetBottom = -inset;
    }

    public static string BuildTooltipIconTag(BuffName name) => $"[buff_icon={name}]";

    public static ColorRect CreateBuffTooltipIcon(BuffName name)
    {
        var icon = CreateBuffIcon(name);
        if (icon == null)
            return null;

        icon.MouseFilter = Control.MouseFilterEnum.Ignore;
        icon.SetAnchorsPreset(Control.LayoutPreset.TopLeft);
        icon.Position = Vector2.Zero;
        if (HasTextureIconChild(icon))
            icon.Color = Colors.Transparent;
        icon.CustomMinimumSize = Vector2.Zero;
        icon.Size = new Vector2(IconBaseSize, IconBaseSize);
        if (FindStackLabel(icon) is Label stackLabel)
            stackLabel.Visible = false;
        return icon;
    }

    private static bool HasTextureIconChild(Node root)
    {
        if (root == null || !GodotObject.IsInstanceValid(root))
            return false;

        foreach (Node child in root.GetChildren())
        {
            if (child is TextureRect)
                return true;

            if (HasTextureIconChild(child))
                return true;
        }

        return false;
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
        PlayBuffGainAudio(target, stack);
        if (stack > 0)
            target?.MarkBuffSeen(name);
        target?.InvalidateBuffTooltipCache();
        RecordBuffGain(target, name, stack, source);
        if (name == BuffName.Invisible && stack > 0)
            target?.BattleNode?.RetargetEnemySingleTargetDamageIntentionsForInvisible(target);
        if (name == BuffName.Taunt && stack > 0)
            target?.BattleNode?.RetargetEnemyIntentionsForTaunt();
        NotifyHandPreviewContextChanged(target);
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
        PlayBuffGainAudio(target, buff.Stack);
        if (buff.Stack > 0)
            target.MarkBuffSeen(buff.ThisBuffName);
        target.InvalidateBuffTooltipCache();
        RecordBuffGain(target, buff.ThisBuffName, buff.Stack, source);
        if (buff.ThisBuffName == BuffName.Invisible && buff.Stack > 0)
            target.BattleNode?.RetargetEnemySingleTargetDamageIntentionsForInvisible(target);
        if (buff.ThisBuffName == BuffName.Taunt && buff.Stack > 0)
            target.BattleNode?.RetargetEnemyIntentionsForTaunt();
        NotifyHandPreviewContextChanged(target);
    }

    private static void PlayBuffGainAudio(Character target, int stack)
    {
        if (stack <= 0 || target == null || !GodotObject.IsInstanceValid(target))
            return;

        AudioManager.PlayBuffGain(target);
    }

    protected bool IsOwnerUnavailableForTrigger() =>
        Owner == null
        || !GodotObject.IsInstanceValid(Owner)
        || Owner.State == Character.CharacterState.Dying;

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
        if (ThisBuffName == BuffName.Invisible)
            Owner?.BattleNode?.RetargetEnemyDamageIntentionsAfterInvisibleEnds(Owner);
        else
            Owner?.BattleNode?.RefreshEnemyIntentionPreviews();
        NotifyHandPreviewContextChanged(Owner);

        if (showVanishHint)
            Hint(ThisBuffName, BuffHintLabel.Which.vanish);

        return true;
    }

    private static void NotifyHandPreviewContextChanged(Character target)
    {
        Battle battle = target?.BattleNode;
        battle?.NotifyHandPreviewContextChanged();
        battle?.RefreshEnemyIntentionPreviews();
    }
}

public class DyingBuff : Buff
{
    public DyingBuff(Character owner, BuffName name, int stack)
        : base(owner, name, stack) { }

    public Task Trigger()
    {
        if (Owner == null || !GodotObject.IsInstanceValid(Owner))
            return Task.CompletedTask;

        using var _ = Owner?.BeginEffectSource(GetBuffDisplayName(ThisBuffName));
        switch (ThisBuffName)
        {
            case BuffName.RebirthI:
                if (Stack >= 1)
                {
                    if (Owner.IsPlayer && Owner.BattleNode?.CanReviveDyingPlayerNow() != true)
                        break;

                    Owner.Recover(Owner.BattleMaxLife / 2, true, Owner);
                    Stack--;
                    UpdateStackLabel();
                }
                break;
        }
        FlashTrigger();
        TryRemoveIfEmpty(Owner.DyingBuffs, showVanishHint: false);
        return Task.CompletedTask;
    }

    public static void BuffAdd(BuffName name, Character target, int stack, Character source = null)
    {
        if (
            name == BuffName.RebirthI
            && target?.IsPlayer == true
            && target.BattleNode?.CanReviveDyingPlayerNow() != true
        )
        {
            return;
        }

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
        Character.DamageKind damageKind = Character.DamageKind.Other
    )
    {
        if (IsOwnerUnavailableForTrigger())
            return damage;

        bool triggered = false;

        switch (ThisBuffName)
        {
            case BuffName.DamageImmune:
                damage = 0;
                Stack--;
                UpdateStackLabel();
                triggered = true;
                break;
            case BuffName.Vulnerable:
                if (damageKind == Character.DamageKind.Attack && damage > 0)
                {
                    damage *= 1.5f;
                    triggered = true;
                }
                break;
            case BuffName.Thorn:
                if (
                    damageKind == Character.DamageKind.Attack
                    && Owner != null
                    && attacker != null
                    && attacker != Owner
                    && attacker.State != Character.CharacterState.Dying
                    && Stack > 0
                )
                {
                    triggered = true;
                    using var _ = Owner.BeginEffectSource(GetBuffDisplayName(ThisBuffName));
                    await attacker.GetHurt(Stack, Owner);
                }
                break;
            case BuffName.AutoArmor:
                if (damageKind == Character.DamageKind.Attack && Owner != null && Stack > 0)
                {
                    triggered = true;
                    Owner.CallDeferred(nameof(Character.UpdataBlock), Stack, true, Owner);
                }
                break;
        }

        if (triggered)
            FlashTrigger();
        TryRemoveIfEmpty(Owner.HurtBuffs);
        return damage;
    }

    public void ConsumeTeamTurnStartStack()
    {
        if (
            ThisBuffName != BuffName.Vulnerable
            || Stack <= 0
            || Owner == null
            || !GodotObject.IsInstanceValid(Owner)
            || Owner.State == Character.CharacterState.Dying
        )
        {
            return;
        }

        Stack--;
        UpdateStackLabel();
        TweenLabel();
        TryRemoveIfEmpty(Owner.HurtBuffs);
    }

    public void ConsumeOpposingTeamTurnEndStack()
    {
        if (
            ThisBuffName != BuffName.Taunt
            || Stack <= 0
            || Owner == null
            || !GodotObject.IsInstanceValid(Owner)
            || Owner.State == Character.CharacterState.Dying
        )
        {
            return;
        }

        Stack--;
        UpdateStackLabel();
        TweenLabel();
        TryRemoveIfEmpty(Owner.HurtBuffs);
    }

    public static void BuffAdd(BuffName name, Character target, int stack, Character source = null)
    {
        if (IsDebuff(name) && SpecialBuff.TryConsumeDebuffImmunity(target, source, name))
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

    public static bool KeepsBlockOnTurnStart(BuffName name) =>
        name == BuffName.Barricade || name == BuffName.Afterimage;

    public void Trigger()
    {
        if (Stack <= 0 || IsOwnerUnavailableForTrigger())
            return;

        switch (ThisBuffName)
        {
            case BuffName.EternalDark:
                StartActionBuff.BuffAdd(BuffName.Invisible, Owner, Stack, Owner);
                break;
            case BuffName.Swift:
                if (Owner?.IsPlayer == true && Owner.BattleNode != null)
                    Owner.BattleNode.TryDrawPlayerTeamBattleCards(Stack);
                break;
            case BuffName.NextEnergy:
                int energyGain = Stack;
                if (Owner?.IsPlayer == true)
                    Owner.BattleNode?.UpdataEnergy(Owner, energyGain, Owner);
                Stack = 0;
                UpdateStackLabel();
                break;
            case BuffName.Prediction:
                if (Owner?.IsPlayer == true)
                    Owner.BattleNode?.QueuePredictionAtTurnStart(Owner, Stack);
                break;
            case BuffName.Barricade:
                // Passive effect: checked by Character.StartAction before block reset.
                break;
            case BuffName.Afterimage:
                Stack--;
                UpdateStackLabel();
                break;
            case BuffName.Divinity:
                Stack--;
                UpdateStackLabel();
                break;
        }

        FlashTrigger();
        TryRemoveIfEmpty(Owner.StartActionBuffs);
    }

    public void ConsumeOpposingTeamTurnEndStack()
    {
        if (
            ThisBuffName != BuffName.Invisible
            || Stack <= 0
            || Owner == null
            || !GodotObject.IsInstanceValid(Owner)
            || Owner.State == Character.CharacterState.Dying
        )
        {
            return;
        }

        Stack--;
        UpdateStackLabel();
        TweenLabel();
        TryRemoveIfEmpty(Owner.StartActionBuffs);
    }

    public static void BuffAdd(BuffName name, Character target, int stack, Character source = null)
    {
        if (IsDebuff(name) && SpecialBuff.TryConsumeDebuffImmunity(target, source, name))
            return;

        if (TryStackExisting(target?.StartActionBuffs, name, stack, target, source))
            return;

        if (
            target?.StartActionBuffs == null
            || (
                name != BuffName.Invisible
                && name != BuffName.EternalDark
                && name != BuffName.Swift
                && name != BuffName.Barricade
                && name != BuffName.Afterimage
                && name != BuffName.Divinity
                && name != BuffName.NextEnergy
                && name != BuffName.Prediction
            )
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
    private const float DivinityMultiplier = 2f;

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
        if (IsOwnerUnavailableForTrigger())
            return;

        int currentStack = GetCurrentStack(context.State);
        if (currentStack <= 0)
            return;

        bool isPreview = context.State != null;
        bool triggered = false;

        switch (ThisBuffName)
        {
            case BuffName.Weaken:
                if (context.Damage > 0)
                {
                    context.Damage = Math.Max(
                        (int)MathF.Floor(context.Damage * WeakenMultiplier),
                        0
                    );
                    triggered = true;
                }
                break;
            case BuffName.CursePower:
                if (!isPreview && context.Target != null)
                {
                    using var _ = Owner?.BeginEffectSource(GetBuffDisplayName(ThisBuffName));
                    AttackBuff.BuffAdd(BuffName.Weaken, context.Target, currentStack, Owner);
                    triggered = true;
                }
                break;
        }

        if (!isPreview && triggered)
            FlashTrigger();
    }

    public void ConsumeTeamTurnEndStack()
    {
        if (
            ThisBuffName != BuffName.Weaken
            || Stack <= 0
            || Owner == null
            || !GodotObject.IsInstanceValid(Owner)
            || Owner.State == Character.CharacterState.Dying
        )
        {
            return;
        }

        Stack--;
        UpdateStackLabel();
        TweenLabel();
        TryRemoveIfEmpty(Owner.AttackBuffs);
    }

    private static bool HasDivinity(Character attacker)
    {
        return attacker?.StartActionBuffs?.Any(x =>
                x != null && x.ThisBuffName == BuffName.Divinity && x.Stack > 0
            ) == true;
    }

    public static void Trigger(Character attacker, ref TriggerContext context)
    {
        if (attacker == null || attacker.State == Character.CharacterState.Dying)
            return;

        bool isPreview = context.State != null;

        if (!isPreview && HasDivinity(attacker))
        {
            context.Damage = Math.Max(
                (int)MathF.Floor(context.Damage * DivinityMultiplier),
                0
            );
            StartActionBuff divinity = attacker.StartActionBuffs?.FirstOrDefault(x =>
                x != null && x.ThisBuffName == BuffName.Divinity && x.Stack > 0
            );
            divinity?.FlashTrigger();
        }
        else if (isPreview && HasDivinity(attacker))
        {
            context.Damage = Math.Max(
                (int)MathF.Floor(context.Damage * DivinityMultiplier),
                0
            );
        }

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
        if (IsDebuff(name) && SpecialBuff.TryConsumeDebuffImmunity(target, source, name))
            return;

        if (TryStackExisting(target?.AttackBuffs, name, stack, target, source))
        {
            if (name == BuffName.Weaken)
                SpecialBuff.TriggerWeakeningFieldBlock(source, stack);
            return;
        }

        if (
            target?.AttackBuffs == null
            || (name != BuffName.Weaken && name != BuffName.Shadow && name != BuffName.CursePower)
        )
            return;

        var buff = new AttackBuff(target, name, stack) { BuffIcon = CreateBuffIcon(name) };
        if (buff.BuffIcon == null)
            return;

        target.AttackBuffs.Add(buff);
        FinalizeBuffAdd(buff, target, source);
        if (name == BuffName.Weaken)
            SpecialBuff.TriggerWeakeningFieldBlock(source, stack);
    }
}

public partial class SkillBuff : Buff
{
    private int _echoTriggeredCountThisTurn;

    public SkillBuff(Character owner, BuffName name, int stack)
        : base(owner, name, stack) { }

    public void ResetTurnState()
    {
        _echoTriggeredCountThisTurn = 0;
    }

    public async Task Trigger(Skill skill)
    {
        if (Stack <= 0 || IsOwnerUnavailableForTrigger())
            return;

        bool triggered = false;

        switch (ThisBuffName)
        {
            case BuffName.Stun:
                triggered = true;
                Stack--;
                UpdateStackLabel();

                if (skill?.OwnerCharater != null && skill.OwnerCharater.CurrentEnergy > 0)
                    skill.OwnerCharater.BattleNode?.UpdataEnergy(
                        skill.OwnerCharater,
                        -1,
                        skill.OwnerCharater
                    );

                if (Owner != null)
                {
                    BuffHintLabel.Spawn(
                        Owner,
                        "[color=yellow]无法行动[/color]",
                        Owner.GlobalPosition,
                        randomOffset: true
                    );
                }

                if (skill?.OwnerCharater?.CharacterEffectScene != null)
                {
                    var effect = CharacterEffect.Spawn(skill.OwnerCharater, "stun");
                    if (effect?.Animation != null)
                        await skill.OwnerCharater.ToSignal(effect.Animation, "animation_finished");
                }
                break;
            case BuffName.Echo:
                if (_echoTriggeredCountThisTurn < Stack)
                {
                    triggered = true;
                    _echoTriggeredCountThisTurn++;
                    skill?.QueueExtraSkillExecutions(1);
                }
                break;
            case BuffName.Fear:
                if (skill?.SkillType == Skill.SkillTypes.Attack && Owner != null && Stack > 0)
                {
                    triggered = true;
                    await Owner.GetHurt(Stack, Owner);
                }
                break;
        }

        if (triggered)
            FlashTrigger();

        TryRemoveIfEmpty(Owner.SkillBuffs);
    }

    public static void BuffAdd(BuffName name, Character target, int stack, Character source = null)
    {
        if (IsDebuff(name) && SpecialBuff.TryConsumeDebuffImmunity(target, source, name))
            return;

        if (target?.SkillBuffs == null)
            return;

        if (TryStackExisting(target.SkillBuffs, name, stack, target, source))
            return;

        if (name != BuffName.Stun && name != BuffName.Echo && name != BuffName.Fear)
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

    public bool ConsumeOneStack(bool showVanishHint = true)
    {
        if (Stack <= 0)
            return false;

        Stack--;
        UpdateStackLabel();
        TweenLabel();
        return TryRemoveIfEmpty(Owner.EndActionBuffs, showVanishHint);
    }

    public async Task Trigger()
    {
        if (Stack <= 0 || IsOwnerUnavailableForTrigger())
            return;

        if (ThisBuffName == BuffName.Demon)
            return;

        FlashTrigger();

        using var _ = Owner.BeginEffectSource(GetBuffDisplayName(ThisBuffName));

        switch (ThisBuffName)
        {
            case BuffName.Pursuit:
                ConsumeOneStack();
                var skill = Skill.CreatePlaceholder(Skill.SkillTypes.Attack);
                skill.OwnerCharater = Owner;
                await skill.Attack(Owner.BattlePower);
                break;
            case BuffName.NoDraw:
                Stack = 0;
                UpdateStackLabel();
                TweenLabel();
                TryRemoveIfEmpty(Owner.EndActionBuffs);
                break;
        }
    }

    public static bool TryBlockPlayerTeamDraw(Battle battle)
    {
        if (battle == null)
            return false;

        Character[] owners = battle
            .GetTeamCharacters(isPlayer: true, includeSummons: false)
            .Where(character =>
                character != null
                && character.State != Character.CharacterState.Dying
                && character.EndActionBuffs?.Any(buff =>
                    buff != null && buff.ThisBuffName == BuffName.NoDraw && buff.Stack > 0
                ) == true
            )
            .ToArray();
        if (owners.Length == 0)
            return false;

        foreach (Character owner in owners)
            FlashTriggersOnOwner(owner, BuffName.NoDraw);
        return true;
    }

    public static void TriggerDemonPowerOnExhaust(Battle battle, int exhaustedCardCount)
    {
        if (battle == null || exhaustedCardCount <= 0)
            return;

        _ = TriggerDemonPowerOnExhaustAsync(battle, exhaustedCardCount);
    }

    private static async Task TriggerDemonPowerOnExhaustAsync(Battle battle, int exhaustedCardCount)
    {
        Character[] team = battle
            .GetTeamCharacters(isPlayer: true, includeSummons: true)
            .Where(x => x != null && x.State != Character.CharacterState.Dying)
            .ToArray();
        if (team.Length == 0)
            return;

        Character[] formOwners = team
            .Where(character =>
                character.EndActionBuffs?.Any(buff =>
                    buff != null && buff.ThisBuffName == BuffName.Demon && buff.Stack > 0
                ) == true
            )
            .ToArray();

        foreach (Character formOwner in formOwners)
        {
            if (formOwner.EndActionBuffs == null)
                continue;

            int stacks = formOwner
                .EndActionBuffs.Where(x =>
                    x != null && x.ThisBuffName == BuffName.Demon && x.Stack > 0
                )
                .Sum(x => x.Stack);
            if (stacks <= 0)
                continue;

            int powerGain = stacks * exhaustedCardCount;
            FlashTriggersOnOwner(formOwner, BuffName.Demon);
            using var _ = formOwner.BeginEffectSource(GetBuffDisplayName(BuffName.Demon));
            foreach (Character ally in team)
            {
                if (ally == null || !GodotObject.IsInstanceValid(ally))
                    continue;

                await ally.IncreaseProperties(PropertyType.Power, powerGain, formOwner);
            }
        }
    }

    public static void BuffAdd(BuffName name, Character target, int stack, Character source = null)
    {
        if (IsDebuff(name) && SpecialBuff.TryConsumeDebuffImmunity(target, source, name))
            return;

        if (target?.EndActionBuffs == null)
            return;

        if (TryStackExisting(target.EndActionBuffs, name, stack, target, source))
            return;

        if (
            name != BuffName.Pursuit
            && name != BuffName.Disaster
            && name != BuffName.Demon
            && name != BuffName.Void
            && name != BuffName.Sanctuary
            && name != BuffName.NoDraw
        )
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
    private static bool _sharingBeaconBlock;
    internal const int WeakeningFieldBlock = 1;

    public SpecialBuff(Character owner, BuffName name, int stack)
        : base(owner, name, stack) { }

    public static void TriggerWeakeningFieldBlock(Character owner, int weakenStacks)
    {
        if (owner?.SpecialBuffs == null || weakenStacks <= 0)
            return;

        int fieldStacks = owner
            .SpecialBuffs.Where(x =>
                x != null && x.ThisBuffName == BuffName.WeakeningField && x.Stack > 0
            )
            .Sum(x => x.Stack);
        if (fieldStacks <= 0)
            return;

        var allies = owner
            .BattleNode?.GetTeamCharacters(owner.IsPlayer, includeSummons: true)
            .Where(x => x != null && x.State != Character.CharacterState.Dying)
            .ToArray();
        if (allies == null || allies.Length == 0)
            return;

        int block = WeakeningFieldBlock * weakenStacks * fieldStacks;
        using var _ = owner.BeginEffectSource(GetBuffDisplayName(BuffName.WeakeningField));
        FlashTriggersOnOwner(owner, BuffName.WeakeningField);
        foreach (var ally in allies)
            ally.UpdataBlock(block, source: owner);
    }

    public static void TriggerBeaconBlockShare(Character owner, int gainedBlock)
    {
        if (_sharingBeaconBlock || owner?.SpecialBuffs == null || gainedBlock <= 0)
            return;

        int beaconStacks = owner
            .SpecialBuffs.Where(x => x != null && x.ThisBuffName == BuffName.Beacon && x.Stack > 0)
            .Sum(x => x.Stack);
        if (beaconStacks <= 0)
            return;

        int sharedBlock = beaconStacks;

        var allies = owner
            .BattleNode?.GetTeamCharacters(owner.IsPlayer, includeSummons: true)
            .Where(x => x != null && x != owner && x.State != Character.CharacterState.Dying)
            .ToArray();
        if (allies == null || allies.Length == 0)
            return;

        _sharingBeaconBlock = true;
        try
        {
            using var _ = owner.BeginEffectSource(GetBuffDisplayName(BuffName.Beacon));
            FlashTriggersOnOwner(owner, BuffName.Beacon);
            foreach (var ally in allies)
                ally.UpdataBlock(sharedBlock, source: owner);
        }
        finally
        {
            _sharingBeaconBlock = false;
        }
    }

    public static void TriggerExhaustShieldBlock(Battle battle, int exhaustedCardCount)
    {
        if (battle == null || exhaustedCardCount <= 0)
            return;

        Character[] allies = battle
            .GetTeamCharacters(isPlayer: true, includeSummons: true)
            .Where(x => x != null && x.State != Character.CharacterState.Dying)
            .ToArray();
        if (allies.Length == 0)
            return;

        foreach (Character ally in allies)
        {
            if (ally.SpecialBuffs == null)
                continue;

            int stacks = ally
                .SpecialBuffs.Where(x =>
                    x != null && x.ThisBuffName == BuffName.ExhaustShield && x.Stack > 0
                )
                .Sum(x => x.Stack);
            if (stacks <= 0)
                continue;

            int block = stacks * exhaustedCardCount;
            using var _ = ally.BeginEffectSource(GetBuffDisplayName(BuffName.ExhaustShield));
            FlashTriggersOnOwner(ally, BuffName.ExhaustShield);
            ally.UpdataBlock(block, source: ally);
        }
    }

    public static void TriggerRecyclingDraw(Battle battle, int exhaustedCardCount)
    {
        if (
            battle == null
            || exhaustedCardCount <= 0
            || battle.GetPlayerTeamBattleHandEmptySlotCount() <= 0
        )
        {
            return;
        }

        Character[] owners = battle
            .GetTeamCharacters(isPlayer: true, includeSummons: false)
            .Where(x =>
                x != null
                && x.State != Character.CharacterState.Dying
                && x.SpecialBuffs?.Any(buff =>
                    buff != null && buff.ThisBuffName == BuffName.Recycling && buff.Stack > 0
                ) == true
            )
            .ToArray();
        if (owners.Length == 0)
            return;

        int drawCount = exhaustedCardCount * owners.Length;
        if (!battle.TryDrawPlayerTeamBattleCards(drawCount))
            return;

        foreach (Character owner in owners)
            FlashTriggersOnOwner(owner, BuffName.Recycling);
    }

    public static bool TryConsumeDebuffImmunity(
        Character target,
        Character source = null,
        BuffName? blockedBuffName = null
    )
    {
        if (target?.SpecialBuffs == null)
            return false;

        var immunity = target.SpecialBuffs.FirstOrDefault(x =>
            x != null && x.ThisBuffName == BuffName.DebuffImmunity && x.Stack > 0
        );
        if (immunity == null)
            return false;

        immunity.FlashTrigger();
        immunity.Stack--;
        immunity.UpdateStackLabel();
        immunity.TryRemoveIfEmpty(target.SpecialBuffs);
        target.BattleNode?.RecordDebuffImmunityConsume(target, blockedBuffName, source);

        return true;
    }

    public static bool TryConsumeCardRefresh(Character target)
    {
        if (target?.IsPlayer != true || target.SpecialBuffs == null)
            return false;

        var refresh = target.SpecialBuffs.FirstOrDefault(x =>
            x != null && x.ThisBuffName == BuffName.ExtraDraw && x.Stack > 0
        );
        if (refresh == null)
            return false;

        refresh.Stack--;
        refresh.UpdateStackLabel();
        refresh.FlashTrigger();
        refresh.TryRemoveIfEmpty(target.SpecialBuffs);
        return true;
    }

    public static int GetCardRefreshStack(Character target)
    {
        if (target?.IsPlayer != true)
            return 0;

        return target
                ?.SpecialBuffs?.FirstOrDefault(x =>
                    x != null && x.ThisBuffName == BuffName.ExtraDraw && x.Stack > 0
                )
                ?.Stack ?? 0;
    }

    public static int ConsumeCardRefresh(Character target, int count)
    {
        if (count <= 0 || target?.IsPlayer != true || target.SpecialBuffs == null)
            return 0;

        var refresh = target.SpecialBuffs.FirstOrDefault(x =>
            x != null && x.ThisBuffName == BuffName.ExtraDraw && x.Stack > 0
        );
        if (refresh == null)
            return 0;

        int consumed = Math.Min(count, refresh.Stack);
        refresh.Stack -= consumed;
        refresh.UpdateStackLabel();
        if (consumed > 0)
            refresh.FlashTrigger();
        refresh.TryRemoveIfEmpty(target.SpecialBuffs);
        return consumed;
    }

    public static int GetForesightStack(Character target)
    {
        if (target?.IsPlayer != true)
            return 0;

        return target
                ?.SpecialBuffs?.FirstOrDefault(x =>
                    x != null && x.ThisBuffName == BuffName.Foresight && x.Stack > 0
                )
                ?.Stack ?? 0;
    }

    public static int GetTotalForesightDrawCount(Battle battle)
    {
        if (battle == null)
            return 0;

        return battle
            .GetTeamCharacters(isPlayer: true, includeSummons: false)
            .Sum(x =>
                x is PlayerCharacter player && x.State != Character.CharacterState.Dying
                    ? GetForesightStack(player)
                    : 0
            );
    }

    public static PlayerCharacter[] GetSearchOwners(Battle battle)
    {
        if (battle == null)
            return Array.Empty<PlayerCharacter>();

        return battle
            .GetTeamCharacters(isPlayer: true, includeSummons: false)
            .OfType<PlayerCharacter>()
            .Where(player =>
                player.State != Character.CharacterState.Dying
                && player.SpecialBuffs?.Any(buff =>
                    buff != null && buff.ThisBuffName == BuffName.Search && buff.Stack > 0
                ) == true
            )
            .ToArray();
    }

    public static int GetEnergyStorageReduction(Character target)
    {
        if (target?.IsPlayer != true || target.SpecialBuffs == null)
            return 0;

        return target
            .SpecialBuffs.Where(x =>
                x != null && x.ThisBuffName == BuffName.EnergyStorage && x.Stack > 0
            )
            .Sum(x => x.Stack);
    }

    public static int GetSourceEnergyBonus(Character target)
    {
        if (target?.IsPlayer != true || target.SpecialBuffs == null)
            return 0;

        return target
            .SpecialBuffs.Where(x =>
                x != null && x.ThisBuffName == BuffName.Source && x.Stack > 0
            )
            .Sum(x => x.Stack);
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
            && name != BuffName.ExtraDraw
            && name != BuffName.Source
            && name != BuffName.EnergyStorage
            && name != BuffName.Beacon
            && name != BuffName.WeakeningField
            && name != BuffName.ExhaustShield
            && name != BuffName.Foresight
            && name != BuffName.Recycling
            && name != BuffName.Search
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
