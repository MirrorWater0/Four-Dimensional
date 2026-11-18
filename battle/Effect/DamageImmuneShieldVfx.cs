using Godot;

/// <summary>Orbiting shield panels split behind and in front of the character.</summary>
public partial class DamageImmuneShieldVfx : Node2D
{
    private const string ScenePath = "res://battle/Effect/DamageImmuneShieldVfx.tscn";
    private const float FadeInSeconds = 0.22f;
    private const float FadeOutSeconds = 0.28f;
    private const float ImpactSeconds = 0.55f;
    private static PackedScene _scene;

    [Export(PropertyHint.Range, "-3,3,0.01")]
    public float RotationSpeed { get; set; } = 0.85f;

    private Character _character;
    private ColorRect _backSurface;
    private ColorRect _frontSurface;
    private ShaderMaterial _backMaterial;
    private ShaderMaterial _frontMaterial;
    private bool _active = true;
    private float _presence;
    private float _elapsed;
    private float _impactAge = ImpactSeconds;
    private Vector2 _impactOrigin = new(0.92f, 0.58f);

    private ColorRect BackSurface => _backSurface ??= GetNode<ColorRect>("Back");
    private ColorRect FrontSurface => _frontSurface ??= GetNode<ColorRect>("Front");

    public static DamageImmuneShieldVfx Spawn(Character character)
    {
        if (!GodotObject.IsInstanceValid(character) || !character.IsInsideTree())
            return null;

        _scene ??= GD.Load<PackedScene>(ScenePath);
        var effect = _scene?.Instantiate<DamageImmuneShieldVfx>();
        if (effect == null)
            return null;

        effect._character = character;
        character.AddChild(effect);
        return effect;
    }

    public override void _Ready()
    {
        // Both passes share one clock but own their material and depth mask.
        _backMaterial = (ShaderMaterial)BackSurface.Material.Duplicate();
        _frontMaterial = (ShaderMaterial)FrontSurface.Material.Duplicate();
        BackSurface.Material = _backMaterial;
        FrontSurface.Material = _frontMaterial;
        RefreshBounds();
        ApplyShaderState();
    }

    public void SetActive(bool active, bool immediate = false)
    {
        _active = active;
        if (immediate)
        {
            _presence = active ? 1f : 0f;
            _impactAge = ImpactSeconds;
            Visible = active;
            ApplyShaderState();
            SetProcess(active);
            return;
        }

        if (active || _presence > 0f || _impactAge < ImpactSeconds)
        {
            Visible = true;
            SetProcess(true);
        }
    }

    public void PlayImpact(Character attacker = null)
    {
        RefreshBounds();
        Vector2 direction = new(_character?.IsPlayer == false ? -1f : 1f, 0f);
        if (GodotObject.IsInstanceValid(attacker) && attacker.IsInsideTree())
        {
            Vector2 localSource = GetGlobalTransformWithCanvas().AffineInverse()
                * attacker.GetVisualCenterGlobalPosition();
            Vector2 orbitCenter = BackSurface.Size * new Vector2(0f, 0.08f);
            Vector2 orbitDirection = (localSource - orbitCenter)
                / (BackSurface.Size * new Vector2(0.42f, 0.085f));
            if (orbitDirection.LengthSquared() > 0.001f)
                direction = orbitDirection.Normalized();
        }

        _impactOrigin = new Vector2(0.5f, 0.58f)
            + direction * new Vector2(0.42f, 0.085f);
        _impactAge = 0f;
        _presence = Mathf.Max(_presence, 0.75f);
        Visible = true;
        ApplyShaderState();
        SetProcess(true);
    }

    public override void _Process(double delta)
    {
        float step = (float)delta;
        _elapsed += step;
        _impactAge = Mathf.Min(_impactAge + step, ImpactSeconds);

        // Keep the final consumed layer visible long enough to read the impact.
        float target = _active || _impactAge < 0.12f ? 1f : 0f;
        float fadeSeconds = target > _presence ? FadeInSeconds : FadeOutSeconds;
        _presence = Mathf.MoveToward(_presence, target, step / fadeSeconds);
        RefreshBounds();
        ApplyShaderState();
        if (!_active && _presence <= 0f && _impactAge >= ImpactSeconds)
        {
            Visible = false;
            SetProcess(false);
        }
    }

    private void RefreshBounds()
    {
        if (!GodotObject.IsInstanceValid(_character))
            return;

        Control frame = _character.GetNodeOrNull<Control>("Hoverframe");
        if (frame == null || frame.Size.X <= 0f || frame.Size.Y <= 0f)
            return;

        // Follow the targeting bounds, independent of sprite squash, recoil, or Spine scale.
        Transform = frame.GetTransform() * new Transform2D(0f, frame.Size * 0.5f);
        Vector2 size = frame.Size + new Vector2(100f, 0f);
        BackSurface.Position = -size * 0.5f;
        BackSurface.Size = size;
        FrontSurface.Position = BackSurface.Position;
        FrontSurface.Size = size;
    }

    private void ApplyShaderState()
    {
        ApplyShaderStateTo(_backMaterial, -1f);
        ApplyShaderStateTo(_frontMaterial, 1f);
    }

    private void ApplyShaderStateTo(ShaderMaterial material, float depthLayer)
    {
        if (material == null)
            return;

        material.SetShaderParameter("canvas_size", BackSurface.Size);
        material.SetShaderParameter("depth_layer", depthLayer);
        material.SetShaderParameter("presence", _presence);
        material.SetShaderParameter("effect_time", _elapsed);
        material.SetShaderParameter("rotation_speed", RotationSpeed);
        material.SetShaderParameter("impact_age", _impactAge);
        material.SetShaderParameter("impact_origin", _impactOrigin);
    }
}
