shader_type canvas_item;
render_mode unshaded;

uniform vec4 base_color : source_color = vec4(0.03, 0.08, 0.11, 0.36);
uniform vec4 accent_color : source_color = vec4(0.22, 0.78, 0.9, 0.75);
uniform vec4 glow_color : source_color = vec4(0.95, 0.76, 0.46, 0.62);
uniform float line_density : hint_range(4.0, 64.0) = 24.0;
uniform float scan_speed : hint_range(0.0, 1.5) = 0.16;
uniform float scan_width : hint_range(0.02, 0.45) = 0.14;
uniform float scan_intensity : hint_range(0.0, 1.0) = 0.22;
uniform float grid_intensity : hint_range(0.0, 1.0) = 0.1;
uniform float frame_intensity : hint_range(0.0, 1.0) = 0.32;
uniform float corner_size : hint_range(0.02, 0.3) = 0.14;
uniform float slant : hint_range(-1.0, 1.0) = 0.28;
uniform float alpha_scale : hint_range(0.0, 1.0) = 1.0;

float edge_mask(float v, float width) {
    return 1.0 - smoothstep(0.0, width, v);
}

void fragment() {
    vec4 modulate = COLOR;
    vec2 uv = UV;

    float diag = clamp(uv.x * 0.72 + uv.y * 0.48, 0.0, 1.0);
    vec3 rgb = mix(base_color.rgb * 0.82, accent_color.rgb, diag * 0.16);

    float stripe_wave = sin((uv.x + uv.y * slant) * line_density + TIME * 0.35) * 0.5 + 0.5;
    float stripes = smoothstep(0.72, 0.98, stripe_wave);
    rgb += accent_color.rgb * stripes * 0.045;

    vec2 coarse_grid = abs(fract(vec2(uv.x * 18.0 + TIME * 0.05, uv.y * 10.0)) - 0.5);
    float grid = (1.0 - smoothstep(0.46, 0.5, min(coarse_grid.x, coarse_grid.y))) * grid_intensity;
    rgb += accent_color.rgb * grid * 0.28;

    float scan_pos = fract(TIME * scan_speed);
    float scan = 1.0 - smoothstep(scan_width, scan_width + 0.035, abs(uv.x + uv.y * 0.18 - scan_pos));
    rgb += mix(accent_color.rgb, glow_color.rgb, 0.4) * scan * scan_intensity;

    float side = min(min(uv.x, 1.0 - uv.x), min(uv.y, 1.0 - uv.y));
    float frame = edge_mask(side, 0.018) * frame_intensity;

    float corner_bar_width = 0.024;
    float tl = (1.0 - step(corner_size, uv.x)) * (1.0 - step(corner_bar_width, uv.y))
        + (1.0 - step(corner_bar_width, uv.x)) * (1.0 - step(corner_size, uv.y));
    float tr = step(1.0 - corner_size, uv.x) * (1.0 - step(corner_bar_width, uv.y))
        + step(1.0 - corner_bar_width, uv.x) * (1.0 - step(corner_size, uv.y));
    float bl = (1.0 - step(corner_size, uv.x)) * step(1.0 - corner_bar_width, uv.y)
        + (1.0 - step(corner_bar_width, uv.x)) * step(1.0 - corner_size, uv.y);
    float br = step(1.0 - corner_size, uv.x) * step(1.0 - corner_bar_width, uv.y)
        + step(1.0 - corner_bar_width, uv.x) * step(1.0 - corner_size, uv.y);
    float corners = min(tl + tr + bl + br, 1.0);

    rgb += accent_color.rgb * frame * 0.26;
    rgb += glow_color.rgb * corners * 0.18;

    float alpha = base_color.a + stripes * 0.04 + grid * 0.12 + scan * 0.1 + frame * 0.12 + corners * 0.08;
    alpha = clamp(alpha * alpha_scale, 0.0, 1.0);

    COLOR = vec4(rgb, alpha) * modulate;
}
