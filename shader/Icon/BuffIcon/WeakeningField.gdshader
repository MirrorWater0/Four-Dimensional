shader_type canvas_item;

uniform vec4 field_color : source_color = vec4(0.25, 0.72, 1.0, 1.0);
uniform vec4 weak_color : source_color = vec4(0.55, 0.22, 0.92, 1.0);
uniform vec4 shield_color : source_color = vec4(0.90, 0.98, 1.0, 1.0);
uniform vec4 core_color : source_color = vec4(0.04, 0.03, 0.12, 1.0);
uniform float animation_speed : hint_range(0.0, 4.0) = 1.0;
uniform float glow_strength : hint_range(0.0, 2.0) = 0.85;

vec2 rotate2d(vec2 point, float angle) {
    float s = sin(angle);
    float c = cos(angle);
    return vec2(point.x * c - point.y * s, point.x * s + point.y * c);
}

float circle_mask(vec2 point, float radius, float blur) {
    float dist = length(point);
    float aa = max(fwidth(dist), 0.0015);
    return 1.0 - smoothstep(radius, radius + max(blur, aa), dist);
}

float ring_mask(float dist, float radius, float width) {
    float aa = max(fwidth(dist), 0.0015);
    return smoothstep(width + aa, 0.0, abs(dist - radius));
}

float ray_mask(vec2 point, float width, float length) {
    float aa = max(max(fwidth(point.x), fwidth(point.y)), 0.0015);
    float body = smoothstep(width + aa, 0.0, abs(point.y));
    float fade = smoothstep(length + aa, 0.02 - aa, abs(point.x));
    return body * fade;
}

void fragment() {
    vec2 uv = UV - 0.5;
    float time = TIME * animation_speed;
    float dist = length(uv);
    float angle = atan(uv.y, uv.x);

    vec2 spin = rotate2d(uv, time * 0.48);
    float outer_ring = ring_mask(dist, 0.31, 0.024);
    float inner_ring = ring_mask(dist, 0.22 + sin(time * 1.8) * 0.01, 0.018);
    float broken = outer_ring * (0.55 + 0.45 * smoothstep(-0.25, 0.9, sin(angle * 6.0 - time * 1.6)));

    float core = circle_mask(uv, 0.12, 0.026);
    float shield_top = circle_mask((uv - vec2(0.0, -0.02)) * vec2(0.85, 1.2), 0.13, 0.018);
    float shield_cut = smoothstep(-0.18, 0.03, uv.y);
    float shield = shield_top * shield_cut;

    vec2 slash = rotate2d(uv - vec2(0.02, 0.01), -0.62);
    float weakness_mark = ray_mask(slash, 0.024, 0.29) * smoothstep(0.37, 0.08, dist);
    vec2 slash_b = rotate2d(uv - vec2(-0.04, -0.04), -0.62);
    weakness_mark += ray_mask(slash_b, 0.014, 0.19) * smoothstep(0.32, 0.08, dist);

    float sparks = circle_mask(spin - vec2(0.20, -0.16), 0.020, 0.010);
    sparks += circle_mask(spin - vec2(-0.18, 0.18), 0.016, 0.010);
    sparks *= 0.65 + 0.35 * sin(time * 3.0);

    float halo = exp(-max(dist - 0.15, 0.0) * 8.0) * glow_strength;
    float alpha = clamp(broken + inner_ring * 0.65 + shield + weakness_mark + sparks + halo * 0.22, 0.0, 1.0);
    alpha *= smoothstep(0.52, 0.43, dist);

    vec3 color = core_color.rgb * core;
    color += field_color.rgb * (broken * 0.75 + inner_ring * 0.55 + halo * 0.38);
    color += shield_color.rgb * shield;
    color += weak_color.rgb * weakness_mark;
    color += shield_color.rgb * sparks;

    COLOR *= vec4(color, alpha);
}
