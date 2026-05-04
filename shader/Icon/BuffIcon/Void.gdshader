shader_type canvas_item;

uniform vec4 void_color : source_color = vec4(0.14, 0.06, 0.28, 1.0);
uniform vec4 rim_color : source_color = vec4(0.42, 0.26, 0.88, 1.0);
uniform vec4 star_color : source_color = vec4(0.84, 0.78, 1.0, 1.0);
uniform vec4 core_color : source_color = vec4(0.02, 0.01, 0.06, 1.0);
uniform float animation_speed : hint_range(0.0, 4.0) = 1.0;
uniform float swirl_strength : hint_range(0.0, 6.0) = 2.2;
uniform float halo_strength : hint_range(0.0, 2.0) = 0.9;

vec2 rotate2d(vec2 point, float angle)
{
    float sine = sin(angle);
    float cosine = cos(angle);
    return vec2(
        point.x * cosine - point.y * sine,
        point.x * sine + point.y * cosine
    );
}

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

    vec2 swirl_uv = rotate2d(uv, dist * swirl_strength - time * 0.9);
    float outer_ring = ring_mask(dist, 0.31, 0.03);
    float inner_ring = ring_mask(dist, 0.21, 0.045);

    float rune_a = smoothstep(0.55, 0.96, sin(angle * 6.0 - time * 1.4));
    float rune_b = smoothstep(0.45, 0.95, sin(angle * 3.0 + dist * 18.0 + time * 0.8));
    float runes = outer_ring * rune_a + inner_ring * rune_b * 0.7;

    float spiral = smoothstep(0.18, 0.0, abs(swirl_uv.x * swirl_uv.y * 5.5 + dist - 0.16));
    spiral *= smoothstep(0.36, 0.08, dist);

    float core = 1.0 - smoothstep(0.10, 0.18, dist + sin(time * 2.0 + angle * 4.0) * 0.012);
    float hole = 1.0 - smoothstep(0.03, 0.08, dist);
    float stars = smoothstep(0.02, 0.0, abs(fract(angle / 3.14159 * 6.0 + time * 0.2) - 0.5));
    stars *= smoothstep(0.34, 0.24, dist) * smoothstep(0.18, 0.22, dist);

    float halo = exp(-max(dist - 0.16, 0.0) * 8.5) * halo_strength;
    float alpha = clamp(outer_ring * 0.75 + inner_ring * 0.55 + spiral * 0.8 + halo * 0.35 + stars * 0.5, 0.0, 1.0);
    alpha *= smoothstep(0.53, 0.43, dist);

    vec3 color = core_color.rgb * core * hole;
    color += void_color.rgb * (spiral + inner_ring * 0.45 + halo * 0.5);
    color += rim_color.rgb * (outer_ring * 0.8 + runes * 0.6);
    color += star_color.rgb * (stars + core * 0.35);

    COLOR = vec4(color, alpha);
}
