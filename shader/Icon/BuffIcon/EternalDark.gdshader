shader_type canvas_item;

uniform vec4 abyss_color : source_color = vec4(0.015, 0.005, 0.035, 1.0);
uniform vec4 dark_violet : source_color = vec4(0.18, 0.035, 0.34, 1.0);
uniform vec4 rim_color : source_color = vec4(0.72, 0.46, 1.0, 1.0);
uniform vec4 star_color : source_color = vec4(0.96, 0.86, 1.0, 1.0);
uniform float animation_speed : hint_range(0.0, 4.0) = 1.0;
uniform float eclipse_radius : hint_range(0.12, 0.36) = 0.24;
uniform float ring_width : hint_range(0.01, 0.08) = 0.032;
uniform float star_strength : hint_range(0.0, 2.0) = 1.0;

vec2 rotate2d(vec2 point, float angle) {
    float sine = sin(angle);
    float cosine = cos(angle);
    return vec2(point.x * cosine - point.y * sine, point.x * sine + point.y * cosine);
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

float star_mask(vec2 point, float scale) {
    point /= scale;
    float horizontal = ray_mask(point, 0.020, 0.28);
    float vertical = ray_mask(point.yx, 0.020, 0.28);
    vec2 diagonal = rotate2d(point, 0.785398);
    float diag_a = ray_mask(diagonal, 0.012, 0.20);
    float diag_b = ray_mask(diagonal.yx, 0.012, 0.20);
    float core = circle_mask(point, 0.045, 0.018);
    return max(max(horizontal, vertical), max(diag_a, diag_b)) * 0.78 + core;
}

void fragment() {
    vec2 uv = UV - 0.5;
    float time = TIME * animation_speed;
    float dist = length(uv);
    float angle = atan(uv.y, uv.x);

    vec2 slow_spin = rotate2d(uv, time * 0.32);
    vec2 counter_spin = rotate2d(uv, -time * 0.58);

    float outer_ring = ring_mask(dist, eclipse_radius + 0.055, ring_width);
    float inner_ring = ring_mask(dist, eclipse_radius - 0.045, ring_width * 0.75);
    float broken_ring = outer_ring * (0.62 + 0.38 * smoothstep(-0.35, 0.85, sin(angle * 5.0 - time * 1.35)));

    float eclipse = circle_mask(uv, eclipse_radius, 0.028);
    float bite = circle_mask(uv + vec2(0.075 + sin(time * 0.7) * 0.012, -0.015), eclipse_radius * 0.92, 0.026);
    float corona = clamp(eclipse - bite, 0.0, 1.0);
    float black_core = circle_mask(uv, eclipse_radius * 0.68, 0.035);

    float top_star = star_mask(counter_spin - vec2(-0.15, -0.17), 0.82);
    float small_star = star_mask(slow_spin - vec2(0.18, 0.13), 0.58);
    float star_pulse = 0.62 + 0.38 * sin(time * 2.8);
    float stars = (top_star * star_pulse + small_star * (1.0 - star_pulse * 0.35)) * star_strength;

    float smoke = exp(-max(dist - 0.16, 0.0) * 7.0);
    float veil = smoothstep(0.90, 0.25, dist) * (0.45 + 0.25 * sin(angle * 4.0 + time));

    float shape = max(max(broken_ring, inner_ring * 0.55), max(corona, stars));
    shape = max(shape, smoke * 0.45 + veil * 0.20);

    vec3 color = abyss_color.rgb * (black_core + veil * 0.45);
    color += dark_violet.rgb * (smoke * 0.65 + inner_ring * 0.55 + corona * 0.45);
    color += rim_color.rgb * (broken_ring * 0.85 + corona * 0.75);
    color += star_color.rgb * stars * 1.35;

    float alpha = clamp(shape + black_core * 0.35, 0.0, 1.0);
    alpha *= smoothstep(0.52, 0.44, dist);

    COLOR *= vec4(color, alpha);
}
