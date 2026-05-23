shader_type canvas_item;

uniform vec4 stream_color : source_color = vec4(0.22, 0.84, 1.0, 1.0);
uniform vec4 highlight_color : source_color = vec4(0.90, 0.98, 1.0, 1.0);
uniform vec4 pool_color : source_color = vec4(0.06, 0.20, 0.34, 1.0);
uniform vec4 sparkle_color : source_color = vec4(0.62, 1.0, 0.96, 1.0);
uniform float animation_speed : hint_range(0.0, 4.0) = 1.0;
uniform float flow_strength : hint_range(0.0, 2.0) = 0.85;

float circle_mask(vec2 point, float radius, float blur)
{
    float aa = max(fwidth(length(point)), 0.0015);
    return 1.0 - smoothstep(radius, radius + max(blur, aa), length(point));
}

float ring_mask(float dist, float radius, float width)
{
    float aa = max(fwidth(dist), 0.0015);
    return smoothstep(width + aa, 0.0, abs(dist - radius));
}

float capsule_mask(vec2 point, vec2 half_extents, float radius, float blur)
{
    vec2 q = abs(point) - half_extents;
    float dist = length(max(q, vec2(0.0))) - radius;
    float aa = max(fwidth(dist), 0.0015);
    return 1.0 - smoothstep(0.0, max(blur, aa), dist);
}

void fragment()
{
    vec2 uv = UV - 0.5;
    float time = TIME * animation_speed;
    float dist = length(uv);

    vec2 pool_uv = uv + vec2(0.0, 0.20);
    float pool = circle_mask(pool_uv * vec2(1.0, 0.72), 0.24, 0.028);
    float pool_ring = ring_mask(length(pool_uv * vec2(1.0, 0.74)), 0.24, 0.022);

    vec2 stem_uv = uv + vec2(0.0, 0.02);
    float stream = capsule_mask(stem_uv, vec2(0.050, 0.14), 0.022, 0.020);
    float inner_stream = capsule_mask(stem_uv + vec2(0.0, 0.015), vec2(0.020, 0.11), 0.018, 0.018);

    vec2 top_uv = uv - vec2(0.0, 0.17);
    float blossom = circle_mask(top_uv, 0.10, 0.020);
    float blossom_cut = circle_mask(top_uv + vec2(0.0, 0.035), 0.055, 0.018);
    blossom *= 1.0 - blossom_cut;

    float flow = sin((uv.y + 0.5) * 13.0 - time * 3.2) * 0.5 + 0.5;
    float shimmer = smoothstep(0.58, 1.0, flow) * stream * flow_strength;

    float ripple_a = ring_mask(length(pool_uv * vec2(1.0, 0.78)) + sin(time * 1.6) * 0.012, 0.13, 0.015);
    float ripple_b = ring_mask(length(pool_uv * vec2(1.0, 0.82)) - sin(time * 1.15 + 1.2) * 0.010, 0.19, 0.013);

    float sparkle = 0.0;
    sparkle += circle_mask(uv - vec2(-0.17, -0.03), 0.020 + sin(time * 2.0) * 0.004, 0.010);
    sparkle += circle_mask(uv - vec2(0.16, 0.04), 0.016 + sin(time * 2.4 + 1.1) * 0.003, 0.010);
    sparkle += circle_mask(uv - vec2(0.0, -0.18), 0.014 + sin(time * 1.8 + 2.2) * 0.003, 0.010);
    sparkle *= smoothstep(0.52, 0.12, dist);

    float glow = exp(-max(dist - 0.12, 0.0) * 7.5) * 0.32;

    vec3 color = vec3(0.0);
    color += pool_color.rgb * pool * 0.95;
    color += stream_color.rgb * (pool_ring * 0.55 + stream * 0.9 + ripple_a * 0.50 + ripple_b * 0.36);
    color += highlight_color.rgb * (inner_stream * 0.95 + blossom * 0.82 + shimmer * 0.72);
    color += sparkle_color.rgb * sparkle * 0.9;
    color += highlight_color.rgb * glow;

    float alpha = clamp(
        pool * 0.88
            + pool_ring * 0.65
            + stream
            + inner_stream * 0.45
            + blossom * 0.75
            + ripple_a * 0.34
            + ripple_b * 0.26
            + sparkle * 0.45
            + glow * 0.25,
        0.0,
        1.0
    );
    alpha *= smoothstep(0.53, 0.44, dist);

    COLOR *= vec4(color, alpha);
}
