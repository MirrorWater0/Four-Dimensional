shader_type canvas_item;

uniform vec4 iris_color : source_color = vec4(0.98, 0.84, 0.38, 1.0);
uniform vec4 pupil_color : source_color = vec4(0.18, 0.10, 0.32, 1.0);
uniform vec4 glow_color : source_color = vec4(1.0, 0.95, 0.72, 1.0);
uniform vec4 card_color : source_color = vec4(0.92, 0.90, 0.84, 1.0);
uniform vec4 ray_color : source_color = vec4(0.98, 0.88, 0.48, 1.0);
uniform float animation_speed : hint_range(0.0, 4.0) = 1.15;

float rect_mask(vec2 uv, vec2 center, vec2 half_size, float blur) {
    vec2 delta = abs(uv - center) - half_size;
    float dist = length(max(delta, vec2(0.0))) + min(max(delta.x, delta.y), 0.0);
    float aa = max(fwidth(dist), 0.0015);
    return 1.0 - smoothstep(0.0, max(blur, aa), dist);
}

float rounded_box_sdf(vec2 p, vec2 half_size, float radius) {
    vec2 q = abs(p) - half_size + vec2(radius);
    return length(max(q, vec2(0.0))) + min(max(q.x, q.y), 0.0) - radius;
}

float card_mask(vec2 uv, vec2 center, float angle, vec2 half_size) {
    float s = sin(-angle);
    float c = cos(-angle);
    vec2 p = vec2(c * (uv.x - center.x) - s * (uv.y - center.y), s * (uv.x - center.x) + c * (uv.y - center.y));
    float d = rounded_box_sdf(p, half_size, 0.03);
    float aa = max(fwidth(d), 0.0015);
    return smoothstep(aa, -aa, d);
}

void fragment() {
    vec2 uv = UV;
    vec2 centered = uv - 0.5;
    float time = TIME * animation_speed;
    float dist = length(centered);

    float card = card_mask(uv, vec2(0.52, 0.72), -0.22, vec2(0.10, 0.14));
    card = max(card, card_mask(uv, vec2(0.44, 0.68), -0.34, vec2(0.09, 0.12)) * 0.72);

    float eye_outer = 1.0 - smoothstep(0.17, 0.19, abs(centered.y - 0.02) * 1.35 + abs(centered.x) * 0.72 - 0.18);
    float eye_white = 1.0 - smoothstep(0.11, 0.13, length(centered - vec2(0.0, 0.02)));
    float iris = 1.0 - smoothstep(0.055, 0.065, length(centered - vec2(0.0, 0.02)));
    float pupil = 1.0 - smoothstep(0.022, 0.028, length(centered - vec2(0.01, 0.03)));
    float highlight = exp(-dot(centered - vec2(-0.03, -0.01), centered - vec2(-0.03, -0.01)) * 260.0);

    float angle = atan(centered.y, centered.x);
    float rays = smoothstep(0.72, 0.98, sin(angle * 5.0 - time * 1.4));
    rays *= smoothstep(0.34, 0.12, dist);
    rays *= 1.0 - eye_white * 0.85;

    float pulse = 0.5 + 0.5 * sin(time * 2.2);
    float aura = exp(-max(dist - 0.14, 0.0) * 8.0) * (0.35 + pulse * 0.15);

    vec3 rgb = vec3(0.0);
    rgb = mix(rgb, card_color.rgb, card * 0.85);
    rgb = mix(rgb, glow_color.rgb, aura);
    rgb = mix(rgb, ray_color.rgb, rays * 0.75);
    rgb = mix(rgb, vec3(0.98), eye_white * 0.9);
    rgb = mix(rgb, iris_color.rgb, iris * 0.95);
    rgb = mix(rgb, pupil_color.rgb, pupil);
    rgb += vec3(1.0) * highlight * 0.85;

    float alpha = clamp(
        max(max(card, eye_outer), max(rays, aura))
            + eye_white * 0.85
            + iris * 0.35,
        0.0,
        1.0
    );
    alpha *= smoothstep(0.52, 0.42, dist);

    COLOR *= vec4(rgb, alpha);
}
