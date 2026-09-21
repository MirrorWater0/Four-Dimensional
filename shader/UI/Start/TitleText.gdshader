shader_type canvas_item;

// Wordmark material: neutral silver vertical gradient, plus a white sweep and
// a subtle entrance scanline displacement. Label glyph UVs are atlas coordinates,
// so the gradient is computed from vertex-local position passed through a varying.
// Only COLOR.rgb is touched; glyph alpha is preserved.

uniform vec4 grad_top : source_color = vec4(0.94, 0.95, 0.97, 1.0);
uniform vec4 grad_mid : source_color = vec4(0.72, 0.74, 0.78, 1.0);
uniform vec4 grad_bottom : source_color = vec4(0.47, 0.49, 0.54, 1.0);
uniform vec4 sweep_color : source_color = vec4(1.0, 1.0, 1.0, 1.0);
uniform vec2 text_size = vec2(1150.0, 150.0);
uniform float sweep_t = -0.3;
uniform float sweep_width = 0.07;
uniform float sweep_strength = 0.32;
uniform float glitch_amount : hint_range(0.0, 1.0) = 0.0;

varying vec2 v_pos;

float hash1(float n) {
    return fract(sin(n * 127.1) * 43758.5453);
}

void vertex() {
    v_pos = VERTEX;
    VERTEX.x += (hash1(floor(VERTEX.y / 6.0) + floor(TIME * 24.0) * 7.0) - 0.5)
        * 14.0 * glitch_amount;
}

void fragment() {
    float ny = clamp(v_pos.y / text_size.y, 0.0, 1.0);
    vec3 grad = ny < 0.5
        ? mix(grad_top.rgb, grad_mid.rgb, ny * 2.0)
        : mix(grad_mid.rgb, grad_bottom.rgb, ny * 2.0 - 1.0);
    float nx = v_pos.x / text_size.x;
    float sweep = smoothstep(sweep_width, 0.0, abs(nx - sweep_t)) * sweep_strength;
    COLOR.rgb = mix(grad, sweep_color.rgb, sweep);
}
