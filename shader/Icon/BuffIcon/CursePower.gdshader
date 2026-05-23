shader_type canvas_item;

uniform vec4 curse_color : source_color = vec4(0.42, 0.10, 0.72, 1.0);
uniform vec4 rune_color : source_color = vec4(0.95, 0.50, 1.0, 1.0);
uniform vec4 core_color : source_color = vec4(0.08, 0.01, 0.12, 1.0);
uniform vec4 spark_color : source_color = vec4(1.0, 0.90, 0.55, 1.0);
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

float slash_mask(vec2 point, float width, float length) {
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

    vec2 spin = rotate2d(uv, time * 0.72);
    float outer_ring = ring_mask(dist, 0.30, 0.025);
    float inner_ring = ring_mask(dist, 0.20 + sin(time * 1.7) * 0.01, 0.018);
    float runes = smoothstep(0.45, 0.95, sin(angle * 7.0 - time * 2.0)) * outer_ring;

    vec2 claw_a = rotate2d(uv - vec2(-0.01, 0.02), -0.60);
    vec2 claw_b = rotate2d(uv - vec2(0.06, -0.03), -0.60);
    vec2 claw_c = rotate2d(uv - vec2(-0.07, -0.04), -0.60);
    float claws = max(
        max(slash_mask(claw_a, 0.020, 0.30), slash_mask(claw_b, 0.018, 0.25)),
        slash_mask(claw_c, 0.016, 0.22)
    );
    claws *= smoothstep(0.39, 0.10, dist);

    float core = circle_mask(uv, 0.11, 0.025);
    float hollow = 1.0 - circle_mask(uv + vec2(0.03, -0.02), 0.055, 0.015);
    float curse_core = core * hollow;

    float spark_a = circle_mask(spin - vec2(0.19, -0.18), 0.028, 0.012);
    float spark_b = circle_mask(spin - vec2(-0.21, 0.14), 0.020, 0.010);
    float sparks = (spark_a + spark_b) * (0.65 + 0.35 * sin(time * 3.0));

    float halo = exp(-max(dist - 0.15, 0.0) * 8.0) * glow_strength;
    float alpha = clamp(outer_ring * 0.75 + inner_ring * 0.55 + runes * 0.65 + claws + curse_core + sparks + halo * 0.25, 0.0, 1.0);
    alpha *= smoothstep(0.52, 0.43, dist);

    vec3 color = core_color.rgb * curse_core;
    color += curse_color.rgb * (halo * 0.55 + inner_ring * 0.6 + claws * 0.75);
    color += rune_color.rgb * (outer_ring * 0.45 + runes + claws * 0.35);
    color += spark_color.rgb * sparks;

    COLOR *= vec4(color, alpha);
}
