shader_type canvas_item;

uniform vec4 shadow_color : source_color = vec4(0.30, 0.08, 0.45, 1.0);
uniform vec4 rim_color : source_color = vec4(0.72, 0.52, 1.0, 1.0);
uniform vec4 core_color : source_color = vec4(0.06, 0.02, 0.10, 1.0);
uniform float animation_speed : hint_range(0.0, 4.0) = 1.2;
uniform float ring_radius : hint_range(0.10, 0.40) = 0.27;
uniform float ring_width : hint_range(0.01, 0.08) = 0.04;
uniform float crescent_offset : hint_range(0.02, 0.30) = 0.12;
uniform float swirl_strength : hint_range(0.0, 4.0) = 1.6;

vec2 rotate2d(vec2 point, float angle) {
    float s = sin(angle);
    float c = cos(angle);
    return vec2(point.x * c - point.y * s, point.x * s + point.y * c);
}

float circle_mask(vec2 point, float radius, float blur) {
    return 1.0 - smoothstep(radius, radius + blur, length(point));
}

void fragment() {
    vec2 uv = UV - 0.5;
    float time = TIME * animation_speed;
    float dist = length(uv);
    float angle = atan(uv.y, uv.x);

    vec2 swirl_uv = rotate2d(uv, dist * swirl_strength - time * 0.75);

    float outer_ring = smoothstep(ring_width, 0.0, abs(dist - ring_radius));
    float inner_ring = smoothstep(ring_width * 0.8, 0.0, abs(dist - (ring_radius - 0.08)));

    float shard_a = smoothstep(0.028, 0.0, abs(swirl_uv.x * 1.3 + swirl_uv.y) - 0.06);
    float shard_b = smoothstep(0.028, 0.0, abs(swirl_uv.x * 1.3 - swirl_uv.y) - 0.06);
    float shard_fade = smoothstep(0.38, 0.12, dist);
    float shards = max(shard_a, shard_b) * shard_fade;

    vec2 crescent_uv = rotate2d(uv, 0.35 + sin(time * 0.8) * 0.12);
    float moon_outer = circle_mask(crescent_uv + vec2(-0.03, 0.0), 0.16, 0.01);
    float moon_inner = circle_mask(crescent_uv + vec2(crescent_offset, 0.0), 0.15, 0.012);
    float crescent = clamp(moon_outer - moon_inner, 0.0, 1.0);

    float pulse = 0.55 + 0.45 * sin(time * 2.2 + dist * 12.0);
    float void_core = circle_mask(uv, 0.11 + pulse * 0.015, 0.02);
    float halo = exp(-max(dist - 0.16, 0.0) * 8.0) * 0.35;

    float rune = smoothstep(0.45, 0.95, sin(angle * 6.0 - time * 1.5)) * outer_ring;
    float shape = max(max(outer_ring * 0.8 + rune * 0.65, inner_ring * 0.5 + shards), crescent);
    shape = max(shape, halo * 0.7);

    vec3 color = core_color.rgb * void_core * 0.8;
    color += shadow_color.rgb * (inner_ring * 0.55 + shards * 0.8 + halo);
    color += rim_color.rgb * (crescent + outer_ring * 0.35 + rune * 0.55);

    float alpha = clamp(shape + void_core * 0.45, 0.0, 1.0);
    alpha *= smoothstep(0.5, 0.43, dist);

    COLOR = vec4(color, alpha);
}
