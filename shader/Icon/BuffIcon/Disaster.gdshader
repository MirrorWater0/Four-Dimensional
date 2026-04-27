shader_type canvas_item;

uniform vec4 bg_color : source_color = vec4(0.0, 0.0, 0.0, 0.0);
uniform vec4 core_color : source_color = vec4(0.42, 0.0, 0.7, 1.0);
uniform vec4 fissure_color : source_color = vec4(1.0, 0.36, 0.84, 1.0);
uniform vec4 ember_color : source_color = vec4(1.0, 0.72, 0.28, 1.0);
uniform vec4 rim_color : source_color = vec4(0.96, 0.86, 1.0, 1.0);
uniform float radius : hint_range(0.18, 0.45) = 0.34;
uniform float ring_width : hint_range(0.02, 0.14) = 0.07;
uniform float crack_width : hint_range(0.01, 0.12) = 0.045;
uniform float pulse : hint_range(0.0, 0.4) = 0.12;
uniform float swirl : hint_range(0.0, 3.0) = 1.4;

float ring_mask(vec2 p, float r0, float r1) {
    float d = length(p);
    float aa = fwidth(d) * 1.5;
    float outer = smoothstep(r1 + aa, r1 - aa, d);
    float inner = smoothstep(r0 + aa, r0 - aa, d);
    return clamp(outer - inner, 0.0, 1.0);
}

float disc_mask(vec2 p, float r) {
    float d = length(p);
    float aa = fwidth(d) * 1.5;
    return smoothstep(r + aa, r - aa, d);
}

void fragment() {
    vec2 p = UV - vec2(0.5);
    float r = length(p);
    float angle = atan(p.y, p.x);

    float disc = disc_mask(p, radius);
    float outer_ring = ring_mask(p, max(radius - ring_width, 0.01), radius + pulse * 0.25);

    float crack_wave = sin(angle * 3.0 + r * 18.0 + swirl)
        + 0.55 * sin(angle * 7.0 - r * 24.0)
        + 0.3 * sin((p.x - p.y) * 30.0);
    float crack = smoothstep(crack_width, 0.0, abs(crack_wave)) * disc;

    float core_glow = smoothstep(radius * 0.95, 0.0, r) * disc;
    float ember = smoothstep(0.22, 0.0, abs(r - radius * 0.46 + crack_wave * 0.012)) * disc;

    vec3 rgb = core_color.rgb * (0.35 + core_glow * 0.65);
    rgb = mix(rgb, fissure_color.rgb, clamp(crack * 0.9, 0.0, 1.0));
    rgb += ember_color.rgb * ember * 0.55;
    rgb = mix(rgb, rim_color.rgb, outer_ring * 0.85);

    float alpha = max(disc * 0.92, outer_ring);
    alpha = clamp(alpha + crack * 0.25 + ember * 0.12, 0.0, 1.0);

    COLOR *= mix(bg_color, vec4(rgb, alpha), alpha);
}
