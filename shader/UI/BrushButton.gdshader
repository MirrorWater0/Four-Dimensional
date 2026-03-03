shader_type canvas_item;

uniform vec4 line_color : source_color = vec4(1.0, 1.0, 1.0, 1.0);
uniform vec4 accent_color : source_color = vec4(0.35, 0.85, 1.0, 1.0);
uniform float thickness_px : hint_range(0.0, 30.0) = 2.0;
uniform float bevel_px : hint_range(0.0, 100.0) = 10.0;
uniform float cut_x : hint_range(0.0, 1.0) = 0.6;
uniform float cut_y : hint_range(0.0, 1.0) = 0.6;
uniform vec2 rect_size_px = vec2(256.0, 64.0);
uniform float glow_strength : hint_range(0.0, 3.0) = 1.1;
uniform float scan_strength : hint_range(0.0, 0.6) = 0.18;

float sd_aa(float dist, float aa) {
    return smoothstep(aa, -aa, dist);
}

float rect_mask(vec2 p, vec2 minv, vec2 maxv) {
    return step(minv.x, p.x) * step(p.x, maxv.x) * step(minv.y, p.y) * step(p.y, maxv.y);
}

void fragment() {
    vec2 size = max(rect_size_px, vec2(1.0));
    vec2 p = UV * size;

    float t = max(thickness_px, 0.001);
    float b = max(bevel_px, 0.001);
    float aa = 1.0;

    float tr_h_rect = max(max((size.x * (1.0 - cut_x)) - p.x, p.x - size.x), max(-p.y, p.y - t));
    float tr_h_bevel = ((size.x * (1.0 - cut_x)) - p.x) + (t - p.y) * (b / t);
    float d_tr_h = max(tr_h_rect, tr_h_bevel);

    float tr_v_rect = max(max((size.x - t) - p.x, p.x - size.x), max(-p.y, p.y - (size.y * cut_y)));
    float tr_v_bevel = (p.y - (size.y * cut_y)) + (size.x - p.x) * (b / t);
    float d_tr_v = max(tr_v_rect, tr_v_bevel);

    float bl_h_rect = max(max(-p.x, p.x - (size.x * cut_x)), max((size.y - t) - p.y, p.y - size.y));
    float bl_h_bevel = (p.x - (size.x * cut_x)) + (p.y - (size.y - t)) * (b / t);
    float d_bl_h = max(bl_h_rect, bl_h_bevel);

    float bl_v_rect = max(max(-p.x, p.x - t), max((size.y * (1.0 - cut_y)) - p.y, p.y - size.y));
    float bl_v_bevel = ((size.y * (1.0 - cut_y)) - p.y) + p.x * (b / t);
    float d_bl_v = max(bl_v_rect, bl_v_bevel);

    float final_dist = min(min(d_tr_h, d_tr_v), min(d_bl_h, d_bl_v));

    float alpha = sd_aa(final_dist, aa);

    float inner_band = sd_aa(abs(final_dist + t * 0.5) - t * 0.12, aa);
    float inner_line = sd_aa(abs(final_dist + t * 1.05) - t * 0.1, aa);
    float outer_glow = exp(-max(final_dist, 0.0) / (t * 1.9)) * glow_strength;

    float grad = clamp((p.x / size.x) * 0.65 + (p.y / size.y) * 0.35, 0.0, 1.0);
    float scan = 1.0 + scan_strength * sin(p.y * 0.28);

    float node_size = max(t * 1.4, 3.0);
    float node_tr = rect_mask(p, vec2(size.x - node_size, 0.0), vec2(size.x, node_size));
    float node_bl = rect_mask(p, vec2(0.0, size.y - node_size), vec2(node_size, size.y));
    float nodes = clamp(node_tr + node_bl, 0.0, 1.0);

    vec3 base = line_color.rgb;
    vec3 accent = accent_color.rgb;
    vec3 color = mix(base, accent, grad);
    color = mix(color, vec3(1.0), inner_band * 0.4);
    color = mix(color, accent, inner_line * 0.6);
    color += nodes * 0.4;
    color *= scan;
    color += outer_glow * 0.18;

    float out_alpha = clamp(alpha + inner_band * 0.8 + inner_line * 0.55 + outer_glow * 0.2, 0.0, 1.0) * line_color.a;
    COLOR = vec4(color, out_alpha);
}
