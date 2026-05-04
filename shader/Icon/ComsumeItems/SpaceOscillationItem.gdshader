shader_type canvas_item;

uniform vec4 ring_color : source_color = vec4(0.45, 0.72, 1.0, 1.0);
uniform vec4 void_color : source_color = vec4(0.72, 0.35, 1.0, 1.0);
uniform vec4 fracture_color : source_color = vec4(0.92, 1.0, 1.0, 1.0);
uniform float oscillation_speed : hint_range(0.0, 8.0) = 2.0;
uniform float ring_width : hint_range(0.005, 0.08) = 0.023;

vec2 rotate(vec2 p, float a) {
    float s = sin(a);
    float c = cos(a);
    return vec2(p.x * c - p.y * s, p.x * s + p.y * c);
}

float sd_segment(vec2 p, vec2 a, vec2 b) {
    vec2 pa = p - a;
    vec2 ba = b - a;
    float h = clamp(dot(pa, ba) / dot(ba, ba), 0.0, 1.0);
    return length(pa - ba * h);
}

float stroke(float d, float width, float aa) {
    return 1.0 - smoothstep(width, width + aa, d);
}

float warped_ring(vec2 p, float radius, float width, float phase, float aa) {
    float angle = atan(p.y, p.x);
    float warp = sin(angle * 5.0 + phase) * 0.030 + sin(angle * 9.0 - phase * 0.7) * 0.014;
    return 1.0 - smoothstep(width, width + aa, abs(length(p) - radius - warp));
}

void fragment() {
    vec2 uv = (UV - 0.5) * 2.0;
    uv *= 0.88;

    float aa = max(fwidth(uv.x), fwidth(uv.y)) * 1.8;
    float pulse = 0.5 + 0.5 * sin(TIME * oscillation_speed);
    vec2 p = rotate(uv, sin(TIME * 0.8) * 0.10);

    float phase = TIME * oscillation_speed;
    float rings = 0.0;
    rings += warped_ring(p, 0.26 + pulse * 0.018, ring_width * 1.25, phase, aa);
    rings += warped_ring(p, 0.43 - pulse * 0.012, ring_width, phase * 1.35 + 1.7, aa) * 0.82;
    rings += warped_ring(p, 0.61 + pulse * 0.010, ring_width * 0.86, phase * 1.8 + 3.1, aa) * 0.62;

    vec2 core_p = rotate(p, -0.72);
    float diamond = abs(core_p.x) + abs(core_p.y * 1.18);
    float core = 1.0 - smoothstep(0.18, 0.18 + aa, diamond);
    float core_hole = smoothstep(0.040, 0.040 + aa, length(p));
    core *= core_hole;

    float fractures = 0.0;
    fractures += stroke(sd_segment(p, vec2(-0.50, -0.25), vec2(-0.16, -0.06)), 0.017, aa);
    fractures += stroke(sd_segment(p, vec2(-0.16, -0.06), vec2(0.05, 0.22)), 0.017, aa);
    fractures += stroke(sd_segment(p, vec2(0.10, -0.42), vec2(0.00, -0.12)), 0.014, aa);
    fractures += stroke(sd_segment(p, vec2(0.00, -0.12), vec2(0.36, 0.11)), 0.014, aa);
    fractures += stroke(sd_segment(p, vec2(0.28, 0.38), vec2(0.08, 0.12)), 0.012, aa);
    fractures *= 0.72 + 0.28 * pulse;

    float shear = abs(sin((uv.x * 16.0 - uv.y * 11.0) + TIME * 8.0));
    float distortion = rings * smoothstep(0.82, 1.0, shear) * 0.22;
    float glow = smoothstep(0.96, 0.05, length(uv)) * 0.20;

    vec3 color = mix(ring_color.rgb, void_color.rgb, 0.36 + 0.28 * pulse);
    color = mix(color, fracture_color.rgb, clamp(fractures + core * 0.45, 0.0, 1.0) * 0.82);
    color += ring_color.rgb * distortion * 0.35;

    float alpha = rings * 0.82 + core * 0.78 + fractures * 0.94 + distortion + glow;
    COLOR = vec4(color, clamp(alpha, 0.0, 1.0));
}
