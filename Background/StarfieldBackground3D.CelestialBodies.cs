using System.Collections.Generic;
using Godot;

// Celestial bodies: slow-drifting planets for menu backdrops. Built only when
// CelestialBodyStrength > 0, so battle scenes skip the cost entirely.
public partial class StarfieldBackground3D
{
    [Export(PropertyHint.Range, "0.0,1.0,0.01")]
    public float CelestialBodyStrength = 0.0f;

    private Node3D _celestialBodyRoot;
    private readonly List<CelestialBodyState> _celestialBodies = new();

    private sealed class CelestialBodyState
    {
        public Node3D Pivot;
        public Node3D OrbitRoot;
        public Node3D RingSpinRoot;
        public Vector3 BasePosition;
        public float BobAmplitude;
        public float BobSpeed;
        public float BobPhase;
        public float MoonOrbitSpeed;
        public float MoonOrbitPhase;
    }

    private void BuildCelestialBodies()
    {
        if (
            _world == null
            || !GodotObject.IsInstanceValid(_world)
            || CelestialBodyStrength <= 0.001f
        )
        {
            return;
        }

        _celestialBodyRoot = _world.GetNodeOrNull<Node3D>("CelestialBodies");
        if (_celestialBodyRoot != null && GodotObject.IsInstanceValid(_celestialBodyRoot))
            return;

        _celestialBodyRoot = new Node3D { Name = "CelestialBodies" };
        _world.AddChild(_celestialBodyRoot, false, InternalMode.Disabled);

        float strength = Mathf.Clamp(CelestialBodyStrength, 0f, 1f);
        Shader planetShader = CreatePlanetShader();

        // Ringed gas giant anchoring the lower-right quadrant. Real NASA-derived
        // surface maps (see docs/THIRD_PARTY_ASSETS.md), graded to the palette.
        CelestialBodyState giant = AddCelestialBody(
            "GasGiant",
            planetShader,
            LoadCelestialTexture("2k_jupiter.png"),
            new Vector3(26f, -14f, -34f),
            8.0f,
            new Color(0.10f, 0.14f, 0.22f),
            new Color(0.82f, 0.72f, 0.54f),
            new Color(0.45f, 0.66f, 0.66f),
            strength,
            cityAmount: 0f,
            spinSpeed: 0.010f,
            bobAmplitude: 0.40f,
            bobSpeed: 0.11f,
            bobPhase: 0.7f
        );
        AddPlanetRing(giant, 16.4f, strength);
        AddMoon(giant, planetShader, strength);

        // Ice sentinel parked at azimuth ~-100 deg: the orbiting camera reaches
        // it late in the loop rather than sharing the opening frame.
        AddCelestialBody(
            "IceSentinel",
            planetShader,
            LoadCelestialTexture("2k_neptune.png"),
            new Vector3(-34.5f, 16f, 6.1f),
            3.4f,
            new Color(0.10f, 0.14f, 0.22f),
            new Color(0.55f, 0.68f, 0.78f),
            new Color(0.50f, 0.68f, 0.76f),
            strength * 0.75f,
            cityAmount: 0f,
            spinSpeed: 0.006f,
            bobAmplitude: 0.55f,
            bobSpeed: 0.07f,
            bobPhase: 3.4f
        );

        // Black hole at azimuth ~+135 deg: revealed mid-loop as the gas giant
        // slides out of frame.
        AddBlackHole(new Vector3(29.7f, 8f, 29.7f), 15f, strength);
    }

    private void AddBlackHole(Vector3 position, float size, float strength)
    {
        var pivot = new Node3D { Name = "BlackHole", Position = position };
        _celestialBodyRoot.AddChild(pivot, false, InternalMode.Disabled);

        var quad = new QuadMesh { Size = new Vector2(size, size) };
        var material = new ShaderMaterial { Shader = CreateBlackHoleShader() };
        material.SetShaderParameter("strength", Mathf.Clamp(strength, 0f, 1f));
        pivot.AddChild(
            new MeshInstance3D
            {
                Name = "Lens",
                Mesh = quad,
                MaterialOverride = material,
                CastShadow = GeometryInstance3D.ShadowCastingSetting.Off
            },
            false,
            InternalMode.Disabled
        );

        _celestialBodies.Add(
            new CelestialBodyState
            {
                Pivot = pivot,
                BasePosition = position,
                BobAmplitude = 0.5f,
                BobSpeed = 0.05f,
                BobPhase = 5.1f
            }
        );
    }

    private static Texture2D LoadCelestialTexture(string fileName)
    {
        return GD.Load<Texture2D>($"res://asset/Background/Celestial/{fileName}");
    }

    private CelestialBodyState AddCelestialBody(
        string name,
        Shader planetShader,
        Texture2D surfaceTexture,
        Vector3 position,
        float radius,
        Color shadowTint,
        Color lightTint,
        Color atmoColor,
        float strength,
        float cityAmount,
        float spinSpeed,
        float bobAmplitude,
        float bobSpeed,
        float bobPhase
    )
    {
        var pivot = new Node3D { Name = name, Position = position };
        _celestialBodyRoot.AddChild(pivot, false, InternalMode.Disabled);

        var sphere = new SphereMesh
        {
            Radius = radius,
            Height = radius * 2f,
            RadialSegments = MobilePlatform.IsMobile ? 40 : 72,
            Rings = MobilePlatform.IsMobile ? 20 : 36
        };
        var material = new ShaderMaterial { Shader = planetShader };
        if (surfaceTexture != null)
            material.SetShaderParameter("surface_tex", surfaceTexture);
        material.SetShaderParameter("shadow_tint", shadowTint);
        material.SetShaderParameter("light_tint", lightTint);
        material.SetShaderParameter("atmo_color", atmoColor);
        material.SetShaderParameter("strength", strength);
        material.SetShaderParameter("city_amount", cityAmount);
        material.SetShaderParameter("spin_speed", spinSpeed);
        material.SetShaderParameter("seed_offset", bobPhase * 3.7f);
        pivot.AddChild(
            new MeshInstance3D
            {
                Name = "Surface",
                Mesh = sphere,
                MaterialOverride = material,
                CastShadow = GeometryInstance3D.ShadowCastingSetting.Off
            },
            false,
            InternalMode.Disabled
        );

        var state = new CelestialBodyState
        {
            Pivot = pivot,
            BasePosition = position,
            BobAmplitude = bobAmplitude,
            BobSpeed = bobSpeed,
            BobPhase = bobPhase
        };
        _celestialBodies.Add(state);
        return state;
    }

    private void AddPlanetRing(CelestialBodyState host, float outerRadius, float strength)
    {
        var tilt = new Node3D
        {
            Name = "RingTilt",
            Rotation = new Vector3(1.28f, 0f, -0.26f)
        };
        host.Pivot.AddChild(tilt, false, InternalMode.Disabled);

        var plane = new PlaneMesh { Size = new Vector2(outerRadius * 2f, outerRadius * 2f) };
        var material = new ShaderMaterial { Shader = CreatePlanetRingShader() };
        Texture2D ringTexture = LoadCelestialTexture("2k_saturn_ring_alpha.png");
        if (ringTexture != null)
            material.SetShaderParameter("ring_tex", ringTexture);
        material.SetShaderParameter("strength", strength);
        tilt.AddChild(
            new MeshInstance3D
            {
                Name = "Ring",
                Mesh = plane,
                MaterialOverride = material,
                CastShadow = GeometryInstance3D.ShadowCastingSetting.Off
            },
            false,
            InternalMode.Disabled
        );

        // Industry reads as motion, not chrome: bright orbital stations
        // crawling along the ring, reusing the flow-head glow sprite.
        var spin = new Node3D { Name = "StationSpin" };
        tilt.AddChild(spin, false, InternalMode.Disabled);
        float stationRadius = outerRadius * 0.63f;
        var stationColors = new[]
        {
            new Color(0.95f, 0.74f, 0.42f, 0.6f),
            new Color(0.78f, 0.86f, 0.95f, 0.55f),
            new Color(0.95f, 0.74f, 0.42f, 0.5f),
        };
        for (int i = 0; i < stationColors.Length; i++)
        {
            Node3D station = CreateSphericalFlowHead(
                $"Station{i + 1}",
                0.10f,
                stationColors[i],
                Mathf.Clamp(strength, 0f, 1f)
            );
            float angle = Mathf.Tau * i / stationColors.Length + 0.5f;
            station.Position = new Vector3(
                Mathf.Cos(angle) * stationRadius,
                0f,
                Mathf.Sin(angle) * stationRadius
            );
            spin.AddChild(station, false, InternalMode.Disabled);
        }
        host.RingSpinRoot = spin;
    }

    private void AddMoon(CelestialBodyState host, Shader planetShader, float strength)
    {
        var orbit = new Node3D { Name = "MoonOrbit" };
        host.Pivot.AddChild(orbit, false, InternalMode.Disabled);

        var sphere = new SphereMesh
        {
            Radius = 1.15f,
            Height = 2.3f,
            RadialSegments = MobilePlatform.IsMobile ? 24 : 40,
            Rings = MobilePlatform.IsMobile ? 12 : 20
        };
        var material = new ShaderMaterial { Shader = planetShader };
        Texture2D moonTexture = LoadCelestialTexture("2k_moon.png");
        if (moonTexture != null)
            material.SetShaderParameter("surface_tex", moonTexture);
        material.SetShaderParameter("shadow_tint", new Color(0.12f, 0.14f, 0.18f));
        material.SetShaderParameter("light_tint", new Color(0.74f, 0.76f, 0.79f));
        material.SetShaderParameter("grade_amount", 0.4f);
        material.SetShaderParameter("atmo_color", new Color(0.44f, 0.52f, 0.60f));
        material.SetShaderParameter("strength", strength);
        material.SetShaderParameter("city_amount", 0.5f);
        material.SetShaderParameter("spin_speed", 0.008f);
        material.SetShaderParameter("seed_offset", 1.9f);
        orbit.AddChild(
            new MeshInstance3D
            {
                Name = "Moon",
                Mesh = sphere,
                MaterialOverride = material,
                Position = new Vector3(11.5f, 0f, 0f),
                CastShadow = GeometryInstance3D.ShadowCastingSetting.Off
            },
            false,
            InternalMode.Disabled
        );

        host.OrbitRoot = orbit;
        host.MoonOrbitSpeed = 0.045f;
        host.MoonOrbitPhase = 2.4f;
    }

    private void UpdateCelestialBodies()
    {
        if (_celestialBodyRoot == null || !GodotObject.IsInstanceValid(_celestialBodyRoot))
            return;

        foreach (CelestialBodyState state in _celestialBodies)
        {
            if (state.Pivot == null || !GodotObject.IsInstanceValid(state.Pivot))
                continue;

            // Two incommensurable sine drifts read as weightless easing.
            float bob = Mathf.Sin(_time * state.BobSpeed + state.BobPhase);
            float sway = Mathf.Sin(_time * state.BobSpeed * 0.63f + state.BobPhase * 1.7f);
            state.Pivot.Position = state.BasePosition
                + new Vector3(
                    sway * state.BobAmplitude * 0.55f,
                    bob * state.BobAmplitude,
                    0f
                );

            if (state.OrbitRoot != null && GodotObject.IsInstanceValid(state.OrbitRoot))
            {
                state.OrbitRoot.Rotation = new Vector3(
                    0.16f,
                    _time * state.MoonOrbitSpeed + state.MoonOrbitPhase,
                    0f
                );
            }

            if (state.RingSpinRoot != null && GodotObject.IsInstanceValid(state.RingSpinRoot))
                state.RingSpinRoot.Rotation = new Vector3(0f, _time * 0.05f, 0f);
        }
    }

    private static Shader CreatePlanetShader()
    {
        return new Shader
        {
            Code = """
                shader_type spatial;
                render_mode unshaded, cull_back, depth_draw_opaque;

                uniform sampler2D surface_tex : source_color, filter_linear_mipmap, repeat_enable;
                uniform vec4 shadow_tint : source_color = vec4(0.10, 0.14, 0.22, 1.0);
                uniform vec4 light_tint : source_color = vec4(0.80, 0.72, 0.55, 1.0);
                uniform float grade_amount = 0.55;
                uniform vec4 atmo_color : source_color = vec4(0.45, 0.66, 0.66, 1.0);
                uniform vec4 city_color : source_color = vec4(0.90, 0.71, 0.42, 1.0);
                uniform float city_amount = 0.0;
                uniform float strength = 1.0;
                uniform float lit_gain = 2.4;
                uniform float spin_speed = 0.004;
                uniform float seed_offset = 0.0;
                uniform vec3 light_dir_view = vec3(-0.78, 0.40, 0.30);

                varying vec3 v_local;

                float hash31(vec3 v) {
                    return fract(sin(dot(v, vec3(127.1, 311.7, 74.7))) * 43758.5453);
                }

                void vertex() {
                    v_local = normalize(VERTEX);
                }

                void fragment() {
                    vec3 n = normalize(v_local);
                    // Equirectangular sample; spin scrolls longitude slowly.
                    float lon = atan(n.z, n.x) / TAU;
                    float lat01 = 0.5 - asin(clamp(n.y, -1.0, 1.0)) / PI;
                    vec2 uv = vec2(lon + TIME * spin_speed + seed_offset, lat01);
                    vec2 ddx = dFdx(uv);
                    vec2 ddy = dFdy(uv);
                    ddx.x -= round(ddx.x);
                    ddy.x -= round(ddy.x);
                    vec3 tex = textureGrad(surface_tex, uv, ddx, ddy).rgb;

                    // Grade the photo texture into the UI palette while keeping
                    // its luminance detail (bands, craters, storms).
                    float lum = dot(tex, vec3(0.299, 0.587, 0.114));
                    vec3 graded = mix(
                        shadow_tint.rgb, light_tint.rgb, smoothstep(0.06, 0.92, lum));
                    vec3 surface = mix(tex, graded, grade_amount);

                    vec3 nv = normalize(NORMAL);
                    float facing = clamp(dot(nv, normalize(VIEW)), 0.0, 1.0);
                    float day = dot(nv, normalize(light_dir_view));
                    float lit = smoothstep(-0.08, 0.42, day);

                    // Limb darkening sells mass; a wide rim glow reads as vapour.
                    float limb = 0.40 + 0.60 * facing;
                    vec3 color = surface * (0.10 + lit_gain * lit) * limb;

                    // Night-side worklights (outpost bodies only).
                    vec3 cell = n * 24.0;
                    float cid = hash31(floor(cell) + seed_offset);
                    float cd = length(fract(cell) - 0.5);
                    float dots = smoothstep(0.36, 0.10, cd) * step(0.80, cid);
                    float flicker = 0.82 + 0.18 * sin(TIME * 1.3 + cid * 41.0);
                    float lights = dots * (1.0 - lit) * city_amount
                        * (0.30 + 0.70 * facing) * flicker;
                    color += city_color.rgb * lights * 1.9;

                    // Thin atmosphere line hugging the limb.
                    float rim = pow(1.0 - facing, 3.4);
                    color += atmo_color.rgb * rim * (0.28 + lit * 0.8);

                    color *= strength;
                    ALBEDO = color;
                    EMISSION = color * 2.0 + city_color.rgb * lights * 1.6;
                }
                """
        };
    }

    private static Shader CreatePlanetRingShader()
    {
        return new Shader
        {
            Code = """
                shader_type spatial;
                render_mode unshaded, cull_disabled, depth_draw_never;

                uniform sampler2D ring_tex : source_color, filter_linear_mipmap;
                uniform vec4 tint : source_color = vec4(0.82, 0.78, 0.70, 1.0);
                uniform float inner_frac = 0.56;
                uniform float outer_frac = 0.99;
                uniform float strength = 1.0;

                void fragment() {
                    // Radial sample of a real ring density strip (x = radius).
                    vec2 p = UV * 2.0 - 1.0;
                    float r = length(p);
                    float x = (r - inner_frac) / (outer_frac - inner_frac);
                    vec4 tex = texture(ring_tex, vec2(clamp(x, 0.001, 0.999), 0.5));

                    float mask = smoothstep(inner_frac, inner_frac + 0.015, r)
                        * (1.0 - smoothstep(outer_frac - 0.015, outer_frac, r));

                    vec3 col = tex.rgb * tint.rgb * 1.5;
                    ALBEDO = col;
                    EMISSION = col * 1.6;
                    ALPHA = tex.a * mask * strength * 0.92;
                }
                """
        };
    }

    private static Shader CreateBlackHoleShader()
    {
        return new Shader
        {
            Code = """
                shader_type spatial;
                render_mode unshaded, cull_disabled, depth_draw_never;

                uniform float strength = 1.0;
                uniform vec4 hot_color : source_color = vec4(1.0, 0.94, 0.85, 1.0);
                uniform vec4 disc_color : source_color = vec4(0.95, 0.64, 0.32, 1.0);
                uniform vec4 cool_color : source_color = vec4(0.50, 0.76, 0.92, 1.0);

                void vertex() {
                    // Billboard: the lensing illusion only holds facing the camera.
                    MODELVIEW_MATRIX = VIEW_MATRIX * mat4(
                        INV_VIEW_MATRIX[0],
                        INV_VIEW_MATRIX[1],
                        INV_VIEW_MATRIX[2],
                        MODEL_MATRIX[3]);
                }

                void fragment() {
                    vec2 p = UV * 2.0 - 1.0;
                    float r = length(p);
                    float t = TIME * 0.25;

                    const float rh = 0.145;
                    float horizon = 1.0 - smoothstep(rh * 0.90, rh, r);

                    // Photon ring: razor-thin, hottest element in the frame.
                    float photon = exp(-abs(r - rh * 1.32) * 52.0);

                    // Accretion disc seen edge-on, slow swirl banding.
                    vec2 q = vec2(p.x, p.y * 3.4);
                    float rd = length(q);
                    float ang = atan(q.y, q.x);
                    float swirl = 0.72 + 0.28 * sin(ang * 3.0 - rd * 8.0 + t * 2.4);
                    float disc = smoothstep(rh * 1.15, rh * 1.75, rd)
                        * (1.0 - smoothstep(0.62, 0.98, rd)) * swirl;
                    float heat = clamp(exp(-(rd - rh * 1.4) * 3.0), 0.0, 1.0);

                    // Lensed far side of the disc, bent above/below the shadow.
                    float vertical = smoothstep(0.25, 0.9, abs(p.y) / max(r, 1e-4));
                    float halo = smoothstep(rh * 1.02, rh * 1.35, r)
                        * (1.0 - smoothstep(rh * 1.85, rh * 2.7, r)) * vertical;

                    // Relativistic beaming: approaching side outshines receding.
                    float doppler = 1.0
                        + 0.55 * clamp(-p.x / max(r, 1e-4), -1.0, 1.0);

                    // Only the near side of the disc crosses in front of the shadow.
                    float front = smoothstep(0.0, 0.12, -p.y);
                    float disc_occl = mix(1.0 - horizon, 1.0, front);

                    vec3 disc_col = mix(disc_color.rgb, hot_color.rgb, heat);
                    vec3 col = hot_color.rgb * photon * (1.0 - horizon) * 1.9;
                    col += disc_col * disc * disc_occl * doppler;
                    col += mix(disc_color.rgb, cool_color.rgb, 0.4)
                        * halo * (1.0 - horizon) * doppler * 0.8;

                    float glow = clamp(
                        photon * 1.6 + disc * disc_occl + halo * 0.8, 0.0, 1.0);
                    ALBEDO = col * strength;
                    EMISSION = col * 1.7 * strength;
                    ALPHA = clamp(max(glow * strength, horizon), 0.0, 1.0);
                }
                """
        };
    }
}
