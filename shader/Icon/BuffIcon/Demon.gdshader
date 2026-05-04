shader_type canvas_item;

uniform vec4 ember_color : source_color = vec4(0.95, 0.20, 0.28, 1.0);
uniform vec4 horn_color : source_color = vec4(0.58, 0.08, 0.18, 1.0);
uniform vec4 eye_color : source_color = vec4(1.0, 0.72, 0.22, 1.0);
uniform vec4 void_color : source_color = vec4(0.10, 0.01, 0.03, 1.0);
uniform float animation_speed : hint_range(0.0, 4.0) = 1.1;
uniform float glow_strength : hint_range(0.0, 2.0) = 0.85;

vec2 rotate2d(vec2 point, float angle)
{
    float sine = sin(angle);
    float cosine = cos(angle);
    return vec2(
        point.x * cosine - point.y * sine,
        point.x * sine + point.y * cosine
    );
}

float circle_mask(vec2 point, float radius, float blur)
{
    return 1.0 - smoothstep(radius, radius + blur, length(point));
}

float sd_isosceles(vec2 point, vec2 size)
{
    point.x = abs(point.x);
    vec2 edge = point - size * clamp(dot(point, size) / dot(size, size), 0.0, 1.0);
    vec2 base = point - size * vec2(clamp(point.x / size.x, 0.0, 1.0), 1.0);
    float distance = sqrt(min(dot(edge, edge), dot(base, base)));
    float sign_mask = max(size.y * point.x - size.x * point.y, point.y - size.y);
    return distance * sign(sign_mask);
}

void fragment()
{
    vec2 uv = UV - 0.5;
    float time = TIME * animation_speed;
    float dist = length(uv);

    vec2 pulse_uv = rotate2d(uv, sin(time * 0.6) * 0.08);
    float face = circle_mask(pulse_uv + vec2(0.0, 0.03), 0.20, 0.02);
    float jaw = circle_mask(pulse_uv + vec2(0.0, 0.14), 0.11, 0.02);
    float head_shape = max(face, jaw * 0.92);

    vec2 left_horn_uv = rotate2d(uv - vec2(-0.11, -0.12), -0.42);
    vec2 right_horn_uv = rotate2d(uv - vec2(0.11, -0.12), 0.42);
    float left_horn = 1.0 - smoothstep(0.0, 0.018, sd_isosceles(left_horn_uv, vec2(0.07, 0.17)));
    float right_horn = 1.0 - smoothstep(0.0, 0.018, sd_isosceles(right_horn_uv, vec2(0.07, 0.17)));
    float horns = max(left_horn, right_horn);

    float left_eye = 1.0 - smoothstep(0.018, 0.040, length((uv - vec2(-0.07, 0.01)) * vec2(1.25, 0.75)));
    float right_eye = 1.0 - smoothstep(0.018, 0.040, length((uv - vec2(0.07, 0.01)) * vec2(1.25, 0.75)));
    float eyes = max(left_eye, right_eye);

    float brow = smoothstep(0.02, 0.0, abs(uv.y + 0.04 + sin(uv.x * 10.0 + time) * 0.01) - 0.01);
    brow *= smoothstep(0.17, 0.03, abs(uv.x));

    vec2 flame_uv = uv;
    flame_uv.y += 0.28;
    flame_uv.x += sin(flame_uv.y * 14.0 - time * 2.5) * 0.035;
    float flame = smoothstep(0.20, 0.0, length(flame_uv * vec2(0.85, 1.35)));
    flame *= smoothstep(0.42, 0.12, flame_uv.y);

    float ring = smoothstep(0.028, 0.0, abs(dist - 0.31));
    float runes = smoothstep(0.55, 0.95, sin(atan(uv.y, uv.x) * 5.0 - time * 1.4));
    float halo = ring * runes;

    float shape = max(max(head_shape, horns), halo * 0.9);
    float smoke = exp(-max(dist - 0.18, 0.0) * 7.0) * glow_strength;
    float alpha = clamp(shape + flame * 0.65 + smoke * 0.35, 0.0, 1.0);
    alpha *= smoothstep(0.53, 0.44, dist);

    vec3 color = void_color.rgb * head_shape;
    color += horn_color.rgb * horns;
    color += ember_color.rgb * (halo * 0.7 + flame * 0.9 + brow * 0.45);
    color += eye_color.rgb * eyes * 1.2;
    color += ember_color.rgb * smoke * 0.25;

    COLOR = vec4(color, alpha);
}
