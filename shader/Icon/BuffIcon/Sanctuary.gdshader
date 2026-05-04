shader_type canvas_item;

uniform vec4 ring_color : source_color = vec4(0.98, 0.82, 0.38, 1.0);
uniform vec4 glow_color : source_color = vec4(1.0, 0.97, 0.74, 1.0);
uniform vec4 core_color : source_color = vec4(0.29, 0.18, 0.04, 1.0);
uniform float animation_speed : hint_range(0.0, 5.0) = 1.0;
uniform float pulse_strength : hint_range(0.0, 2.0) = 0.85;

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
    float pulse = 1.0 + sin(time * 2.1) * 0.035 * pulse_strength;

    float outer_ring = ring_mask(dist, 0.29 * pulse, 0.018);
    float inner_ring = ring_mask(dist, 0.20 * pulse, 0.028);

    vec2 rotated = rotate2d(uv, 0.7853982);
    float vertical = smoothstep(0.075, 0.0, abs(rotated.x)) * smoothstep(0.19, 0.045, abs(rotated.y));
    float horizontal = smoothstep(0.075, 0.0, abs(rotated.y)) * smoothstep(0.19, 0.045, abs(rotated.x));
    float cross = max(vertical, horizontal);

    float core = 1.0 - smoothstep(0.08, 0.16, dist);
    float aura = exp(-max(dist - 0.12, 0.0) * 7.5);

    float angle = atan(uv.y, uv.x);
    float sigils = smoothstep(0.45, 0.95, sin(angle * 6.0 - time * 1.1));
    sigils *= outer_ring;

    float rays = 0.0012 / (abs(uv.x * uv.y) + 0.00075);
    rays *= smoothstep(0.42, 0.11, dist) * 0.45;

    float alpha = clamp(
        outer_ring * 0.75
            + inner_ring * 0.6
            + cross * 0.95
            + core * 0.85
            + aura * 0.3
            + sigils * 0.55
            + rays,
        0.0,
        1.0
    );
    alpha *= smoothstep(0.52, 0.40, dist);

    vec3 color = core_color.rgb * core * 0.8;
    color += ring_color.rgb * (outer_ring * 0.85 + inner_ring * 0.55 + sigils * 0.7);
    color += glow_color.rgb * (cross + aura * 0.6 + rays);

    COLOR = vec4(color, alpha);
}
