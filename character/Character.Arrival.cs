using Godot;

public partial class Character {
    private const float ArrivalDuration = 0.76f;
    private static readonly Shader ArrivalShader = GD.Load<Shader>("res://shader/Effect/CharacterArrival.gdshader");
    private static readonly StringName ArrivalPhase = "arrival_phase";
    private static readonly StringName FxPhase = "phase";

    private void PlayBattleArrival() {
        var frame = Hoverframe;
        var shadow = CharacterShadow;
        Vector2 foot = shadow != null ? shadow.Position + shadow.Size * shadow.RingCenter
            : frame.Position + frame.Size * new Vector2(0.5f, 1f);
        float height = Mathf.Max(100f, foot.Y - frame.Position.Y);
        float width = Mathf.Clamp(frame.Size.X * 2.28f, height * 0.70f, height * 1.30f);
        Color ink = IsPlayer ? (shadow?.FillColor ?? new Color(0.30f, 0.68f, 1f)) : new Color(1f, 0.33f, 0.15f);
        ink = ink.Lerp(IsPlayer ? new Color(0.36f, 0.72f, 1f) : new Color(1f, 0.69f, 0.24f), 0.20f);

        var effect = new Node2D { Name = "ArrivalImpact", Position = foot };
        // The rift is behind the body. The impact layer only covers the area around the feet.
        var rift = CreateArrivalLayer(effect, "Rift", width, height, 1.18f, 0.22f, ink, false);
        var impact = CreateArrivalLayer(effect, "Impact", width, height, 0.44f, 0.22f, ink, true);
        AddChild(effect);

        // Some scenes export the Spine node even when only their illustration is visible.
        var illustration = LocalizeArrivalBody(GetNodeOrNull<Node2D>("Sprite"), false, ink);
        var spine = LocalizeArrivalBody(GetNodeOrNull<Node2D>("SpineSprite"), true, ink);
        float delay = IsSummon ? 0f : Mathf.Clamp(PositionIndex - 1, 0, 4) * 0.047f + (IsPlayer ? 0f : 0.085f);
        var tween = effect.CreateTween();
        tween.TweenMethod(Callable.From<float>(phase => {
            rift.SetShaderParameter(FxPhase, phase);
            impact.SetShaderParameter(FxPhase, phase);
            illustration?.SetShaderParameter(ArrivalPhase, phase);
            spine?.SetShaderParameter(ArrivalPhase, phase);
        }), 0f, 1f, ArrivalDuration).SetDelay(delay).SetTrans(Tween.TransitionType.Linear);
        tween.TweenCallback(Callable.From(effect.QueueFree));
    }

    private ShaderMaterial CreateArrivalLayer(Node2D effect, string name, float width, float height,
        float above, float below, Color ink, bool foreground) {
        var material = new ShaderMaterial { Shader = ArrivalShader };
        material.SetShaderParameter("ink", ink);
        material.SetShaderParameter("facing", IsPlayer ? 1f : -1f);
        material.SetShaderParameter("vertical_span", new Vector2(above, below));
        material.SetShaderParameter("impact_layer", foreground);
        effect.AddChild(new ColorRect {
            Name = name, Material = material, MouseFilter = Control.MouseFilterEnum.Ignore,
            Position = new Vector2(-width * 0.5f, -height * above),
            Size = new Vector2(width, height * (above + below)),
            ZIndex = foreground ? 0 : -1,
        });
        return material;
    }

    private static ShaderMaterial LocalizeArrivalBody(Node2D visual, bool skinned, Color ink) {
        if (visual == null || !visual.Visible || (skinned && !visual.IsClass("SpineSprite")))
            return null;
        ShaderMaterial source = skinned ? visual.Get("normal_material").AsGodotObject() as ShaderMaterial
            : visual.Material as ShaderMaterial;
        string expected = skinned ? "res://shader/Effect/SpineCharacter.gdshader" : "res://shader/Charater.gdshader";
        if (source?.Shader?.ResourcePath != expected)
            return null;
        var local = (ShaderMaterial)source.Duplicate();
        local.SetShaderParameter(ArrivalPhase, 0f);
        local.SetShaderParameter("arrival_ink", ink);
        if (skinned)
            visual.Set("normal_material", local);
        else
            visual.Material = local;
        return local;
    }
}
