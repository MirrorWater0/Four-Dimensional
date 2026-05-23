shader_type canvas_item;

uniform vec4 beam_color : source_color = vec4(1.0, 0.83, 0.34, 1.0);
uniform vec4 core_color : source_color = vec4(1.0, 0.98, 0.72, 1.0);
uniform vec4 glass_color : source_color = vec4(0.36, 0.86, 1.0, 1.0);
uniform vec4 base_color : source_color = vec4(0.18, 0.30, 0.42, 1.0);
uniform float animation_speed : hint_range(0.0, 4.0) = 1.0;
uniform float glow_strength : hint_range(0.0, 2.0) = 0.9;

vec2 rotate2d(vec2 point, float angle) {
    float sine = sin(angle);
    float cosine = cos(angle);
    return vec2(point.x * cosine - point.y * sine, point.x * sine + point.y * cosine);
}

float sd_box(vec2 point, vec2 half_size) {
    vec2 delta = abs(point) - half_size;
    return length(max(delta, vec2(0.0))) + min(max(delta.x, delta.y), 0.0);
}

float box_mask(vec2 point, vec2 half_size, float blur) {
    float dist = sd_box(point, half_size);
    float aa = max(fwidth(dist), 0.0015);
    return 1.0 - smoothstep(0.0, max(blur, aa), dist);
}

float circle_mask(vec2 point, float radius, float blur) {
    float dist = length(point);
    float aa = max(fwidth(dist), 0.0015);
    return 1.0 - smoothstep(radius, radius + max(blur, aa), dist);
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
    float aa = max(fwidth(dist), 0.0015);

    float tower = box_mask(uv - vec2(0.0, 0.12), vec2(0.055, 0.20), 0.018);
    float roof = box_mask(rotate2d(uv - vec2(0.0, -0.12), 0.785398), vec2(0.095, 0.095), 0.018);
    roof *= smoothstep(0.08 + aa, -0.02 - aa, uv.y + 0.08);
    float base = box_mask(uv - vec2(0.0, 0.32), vec2(0.16, 0.045), 0.018);

    vec2 lantern_uv = uv - vec2(0.0, -0.05);
    float lantern = circle_mask(lantern_uv * vec2(1.0, 1.18), 0.125, 0.018);
    float core = circle_mask(lantern_uv, 0.058 + sin(time * 2.2) * 0.006, 0.016);

    vec2 sweep = rotate2d(uv - vec2(0.0, -0.05), sin(time * 0.75) * 0.32);
    float horizontal_beam = ray_mask(sweep, 0.030, 0.46);
    float vertical_beam = ray_mask(sweep.yx, 0.023, 0.30) * 0.65;
    vec2 diagonal = rotate2d(sweep, 0.785398);
    float diagonal_beam = max(ray_mask(diagonal, 0.016, 0.34), ray_mask(diagonal.yx, 0.016, 0.34)) * 0.55;
    float beams = (horizontal_beam + vertical_beam + diagonal_beam) * smoothstep(0.50, 0.10, dist);

    float ring = smoothstep(0.018 + aa, 0.0, abs(length(lantern_uv) - 0.17));
    float pulse = 0.68 + 0.32 * sin(time * 2.6);
    float glow = exp(-max(length(lantern_uv) - 0.06, 0.0) * 8.0) * glow_strength;

    vec3 rgb = vec3(0.0);
    rgb += beam_color.rgb * (beams * 0.72 + ring * 0.55 + glow * 0.32);
    rgb += glass_color.rgb * lantern * 0.62;
    rgb += core_color.rgb * (core * 1.15 + beams * 0.18 * pulse);
    rgb = mix(rgb, base_color.rgb, clamp(max(max(tower, roof), base) * 0.82, 0.0, 1.0));
    rgb += core_color.rgb * core * 0.75;

    float alpha = clamp(max(max(max(tower, roof), base), lantern) + beams * 0.58 + ring * 0.72 + glow * 0.20, 0.0, 1.0);
    alpha *= smoothstep(0.52, 0.44, dist);

    COLOR *= vec4(rgb, alpha);
}
