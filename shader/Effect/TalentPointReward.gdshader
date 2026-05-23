shader_type canvas_item;
render_mode blend_add;

uniform vec4 glow_color : source_color = vec4(1.0, 0.78, 0.18, 1.0);
uniform float core_radius : hint_range(0.0, 0.5) = 0.14;
uniform float glow_radius : hint_range(0.0, 0.9) = 0.44;
uniform float glow_strength : hint_range(0.0, 3.0) = 1.45;
uniform float ray_length : hint_range(0.1, 1.2) = 0.86;
uniform float ray_width : hint_range(0.001, 0.08) = 0.018;
uniform float twinkle_speed : hint_range(0.0, 8.0) = 2.6;

vec2 rotate2d(vec2 p, float a) {
    float s = sin(a);
    float c = cos(a);
    return vec2(c * p.x - s * p.y, s * p.x + c * p.y);
}

float hash21(vec2 p) {
    p = fract(p * vec2(123.34, 456.21));
    p += dot(p, p + 45.32);
    return fract(p.x * p.y);
}

float star_ray(vec2 p, vec2 dir, float width, float length_scale) {
    vec2 n = vec2(-dir.y, dir.x);
    float along = abs(dot(p, dir));
    float cross = abs(dot(p, n));
    float thin = smoothstep(width, 0.0, cross);
    float fade = smoothstep(ray_length * length_scale, 0.0, along);
    return thin * fade;
}

float sparkle(vec2 uv, vec2 center, float phase) {
    vec2 p = uv - center;
    float d = length(p);
    float pulse = 0.45 + 0.55 * sin(TIME * twinkle_speed + phase);
    float point = smoothstep(0.045, 0.0, d) * pulse;
    float rays =
        star_ray(p, normalize(vec2(1.0, 0.0)), 0.006, 0.18)
        + star_ray(p, normalize(vec2(0.0, 1.0)), 0.006, 0.18);
    return point + rays * 0.35 * pulse;
}

void fragment() {
    float breath = 0.88 + 0.12 * sin(TIME * twinkle_speed);
    float spin = TIME * 0.45;
    vec2 uv = UV;
    vec2 p = rotate2d(uv - vec2(0.5), spin);
	p *= 1.5;
    float d = length(p);

    float core = smoothstep(core_radius, core_radius * 0.28, d);
    float hot_core = smoothstep(core_radius * 0.42, 0.0, d);
    float halo = smoothstep(glow_radius * breath, 0.0, d) * glow_strength;

    float main_rays =
        star_ray(p, normalize(vec2(1.0, 0.0)), ray_width, 1.0)
        + star_ray(p, normalize(vec2(0.0, 1.0)), ray_width, 1.0);
    float diagonal_rays =
        star_ray(p, normalize(vec2(1.0, 1.0)), ray_width * 0.58, 0.34)
        + star_ray(p, normalize(vec2(1.0, -1.0)), ray_width * 0.58, 0.34);

    float grain = hash21(floor(uv * 32.0));
    float glitter = smoothstep(0.84, 1.0, grain) * smoothstep(0.42, 0.1, d);
    glitter *= 0.35 + 0.65 * sin(TIME * (twinkle_speed + grain * 3.0) + grain * 6.28318);

    float small_sparkles =
        sparkle(uv, vec2(0.24, 0.30), 0.0)
        + sparkle(uv, vec2(0.74, 0.36), 2.1)
        + sparkle(uv, vec2(0.31, 0.70), 4.0)
        + sparkle(uv, vec2(0.68, 0.73), 5.3);

    float star = main_rays * 0.9 + diagonal_rays * 0.45;
    float alpha = clamp(core + halo * 0.4 + star + glitter * 0.32 + small_sparkles * 0.42, 0.0, 1.0);
    vec3 warm_gold = glow_color.rgb;
    vec3 hot_white = vec3(1.0, 0.95, 0.72);
    vec3 color = mix(warm_gold, hot_white, clamp(hot_core + star * 0.55, 0.0, 1.0));
    color *= 1.0 + halo * 0.26 + star * 0.5 + small_sparkles * 0.35;
    COLOR = vec4(color, glow_color.a * alpha) * COLOR;
}
