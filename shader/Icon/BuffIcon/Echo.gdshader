shader_type canvas_item;

uniform vec4 wave_color : source_color = vec4(0.18, 0.82, 0.98, 1.0);
uniform vec4 ring_color : source_color = vec4(0.74, 0.95, 1.0, 1.0);
uniform vec4 core_color : source_color = vec4(0.04, 0.15, 0.24, 1.0);
uniform float animation_speed : hint_range(0.0, 4.0) = 1.1;
uniform float ring_spacing : hint_range(0.08, 0.22) = 0.115;
uniform float pulse_strength : hint_range(0.0, 2.0) = 0.8;
uniform float thickness : hint_range(0.5, 2.5) = 1.0;

float ring_mask(float dist, float radius, float width)
{
    return smoothstep(width, 0.0, abs(dist - radius));
}

void fragment()
{
    vec2 uv = UV - 0.5;
    float time = TIME * animation_speed;
    float dist = length(uv);
    float angle = atan(uv.y, uv.x);
    float ring_thickness = max(thickness, 0.001);
    float arc_threshold = mix(0.72, 0.56, clamp((ring_thickness - 0.5) / 2.0, 0.0, 1.0));

    float pulse = 0.5 + 0.5 * sin(time * 2.3);
    float ring_a = ring_mask(dist, 0.12 + pulse * 0.02, 0.018 * ring_thickness);
    float ring_b = ring_mask(dist, 0.12 + ring_spacing + pulse * 0.028, 0.022 * ring_thickness);
    float ring_c = ring_mask(
        dist,
        0.12 + ring_spacing * 2.0 + pulse * 0.035,
        0.026 * ring_thickness
    );

    float radial_wave = sin(dist * 34.0 - time * 5.2);
    float angular_wave = sin(angle * 6.0 + time * 2.0);
    float echo_arcs = smoothstep(0.36, arc_threshold, radial_wave * 0.65 + angular_wave * 0.35);
    echo_arcs *= smoothstep(0.43, 0.08, dist);

    float core = 1.0 - smoothstep(0.05, 0.16, dist);
    float halo = exp(-max(dist - 0.08, 0.0) * 7.0) * pulse_strength;

    float alpha = clamp(
        ring_a * 0.95 + ring_b * 0.82 + ring_c * 0.68 + echo_arcs * 0.48 + halo * 0.18,
        0.0,
        1.0
    );
    alpha *= smoothstep(0.54, 0.46, dist);

    vec3 color = core_color.rgb * (core + halo * 0.28);
    color += wave_color.rgb * (ring_a * 0.7 + ring_b * 0.85 + echo_arcs * 0.6);
    color += ring_color.rgb * (ring_c * 0.95 + core * 0.32);

    COLOR *= vec4(color, alpha);
}
