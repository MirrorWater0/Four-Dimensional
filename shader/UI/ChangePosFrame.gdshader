shader_type canvas_item;

// Keep these names for scene compatibility.
uniform vec4 bg_color : source_color = vec4(0.08, 0.13, 0.22, 0.62);
uniform vec4 line_color : source_color = vec4(0.72, 0.86, 0.98, 1.0);
uniform float border_thickness : hint_range(0.001, 0.06) = 0.005;
uniform float corner_size : hint_range(0.05, 0.48) = 0.2;

// Extra style controls.
uniform vec4 accent_color : source_color = vec4(0.42, 0.78, 1.0, 1.0);
uniform float pulse_speed : hint_range(0.0, 6.0) = 1.3;
uniform float sweep_speed : hint_range(0.0, 1.0) = 0.;
uniform float sweep_strength : hint_range(0.0, 1.0) = 0.0;
uniform float scanline_strength : hint_range(0.0, 0.2) = 0.04;

float sd_box(vec2 p, vec2 b) {
    vec2 d = abs(p) - b;
    return length(max(d, vec2(0.0))) + min(max(d.x, d.y), 0.0);
}

float aa_step(float d, float w) {
    float aa = max(fwidth(d), 0.0005);
    return smoothstep(w + aa, w - aa, d);
}

void fragment() {
    vec4 modulate_color = COLOR;
    vec2 uv = UV;
    vec2 p = uv - 0.5;
    float t = max(border_thickness, 0.001);
    float c = clamp(corner_size, 0.05, 0.48);

    // Base masks
    float panel = aa_step(sd_box(p, vec2(0.5 - t * 1.2)), 0.0);
    float outer0 = aa_step(sd_box(p, vec2(0.5 - t * 0.1)), 0.0);
    float outer1 = aa_step(sd_box(p, vec2(0.5 - t * 1.25)), 0.0);
    float outer_frame = clamp(outer0 - outer1, 0.0, 1.0);

    float inner0 = aa_step(sd_box(p, vec2(0.42)), 0.0);
    float inner1 = aa_step(sd_box(p, vec2(0.42 - t * 0.75)), 0.0);
    float inner_frame = clamp(inner0 - inner1, 0.0, 1.0);

    // Corner braces
    float edge_x = 0.5 - c;
    float edge_y = 0.5 - c;
    float corner_region = step(edge_x, abs(p.x)) * step(edge_y, abs(p.y));

    float v_line = aa_step(abs(abs(p.x) - (0.5 - t * 0.75)), t * 0.9) * step(edge_y, abs(p.y));
    float h_line = aa_step(abs(abs(p.y) - (0.5 - t * 0.75)), t * 0.9) * step(edge_x, abs(p.x));
    float corner_frame = clamp((v_line + h_line) * corner_region, 0.0, 1.0);

    float corner_dot = aa_step(length(abs(p) - vec2(0.5 - c * 0.25)), t * 1.6) * corner_region;

    // Sweep highlight disabled by design to keep a calmer frame style.
    float sweep = 0.0;

    // Scanline texture
    float scan = sin((uv.y + TIME * 0.03) * 220.0) * scanline_strength;

    // Pulse
    float pulse = 0.9 + 0.1 * sin(TIME * pulse_speed);

    // Color composition
    vec3 bg = bg_color.rgb * (0.9 + 0.1 * uv.y);
    bg *= (1.0 + scan);

    vec3 frame_col = line_color.rgb * pulse;
    vec3 accent_col = accent_color.rgb;

    vec3 rgb = vec3(0.0);
    rgb += bg * panel;
    rgb += frame_col * outer_frame;
    rgb += mix(frame_col, accent_col, 0.45) * inner_frame * 0.85;
    rgb += accent_col * corner_frame * (0.85 + 0.15 * pulse);
    rgb += accent_col * corner_dot * 0.9;
    rgb += accent_col * sweep;

    float alpha = max(panel * bg_color.a, outer_frame * line_color.a);
    alpha = max(alpha, inner_frame * line_color.a * 0.9);
    alpha = max(alpha, corner_frame * accent_color.a);
    alpha = clamp(alpha, 0.0, 1.0);

    COLOR = vec4(rgb, alpha) * modulate_color;
}
