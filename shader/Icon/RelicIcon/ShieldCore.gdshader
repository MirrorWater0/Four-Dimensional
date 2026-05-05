shader_type canvas_item;

uniform vec4 shield_color : source_color = vec4(0.42, 0.78, 1.0, 1.0);
uniform vec4 edge_color : source_color = vec4(0.92, 0.98, 1.0, 1.0);
uniform vec4 core_color : source_color = vec4(0.08, 0.35, 0.92, 1.0);
uniform vec4 glow_color : source_color = vec4(0.12, 0.62, 1.0, 1.0);
uniform float scale : hint_range(0.25, 1.2) = 0.92;
uniform float soft_edge : hint_range(0.001, 0.08) = 0.014;
uniform float line_width : hint_range(0.003, 0.08) = 0.018;

float sd_regular_polygon(vec2 p, float radius, float sides) {
    float angle = atan(p.y, p.x);
    float sector = 6.28318530718 / sides;
    return cos(floor(0.5 + angle / sector) * sector - angle) * length(p) - radius;
}

float sd_box(vec2 p, vec2 b) {
    vec2 d = abs(p) - b;
    return length(max(d, 0.0)) + min(max(d.x, d.y), 0.0);
}

float stroke_alpha(float dist, float thickness, float smoothing) {
    return smoothstep(smoothing, 0.0, abs(dist) - thickness);
}

float fill_alpha(float dist, float smoothing) {
    return smoothstep(smoothing, 0.0, dist);
}

vec2 rotate_uv(vec2 p, float angle) {
    float s = sin(angle);
    float c = cos(angle);
    return vec2(c * p.x - s * p.y, s * p.x + c * p.y);
}

void fragment() {
    vec2 p = (UV - 0.5) / scale;
    vec2 rp = rotate_uv(p, 0.52359878);
    float r = length(p);

    float outer_hex = stroke_alpha(sd_regular_polygon(rp, 0.40, 6.0), line_width, soft_edge);
    float mid_hex = stroke_alpha(sd_regular_polygon(rp, 0.30, 6.0), line_width * 0.72, soft_edge);
    float core_hex = fill_alpha(sd_regular_polygon(rp, 0.18, 6.0), soft_edge);
    float core_ring = stroke_alpha(sd_regular_polygon(rp, 0.21, 6.0), line_width * 0.65, soft_edge);

    float vertical = stroke_alpha(sd_box(p, vec2(line_width * 0.45, 0.31)), line_width * 0.45, soft_edge);
    float diagonal_a = stroke_alpha(sd_box(rotate_uv(p, 1.04719755), vec2(line_width * 0.42, 0.28)), line_width * 0.42, soft_edge);
    float diagonal_b = stroke_alpha(sd_box(rotate_uv(p, -1.04719755), vec2(line_width * 0.42, 0.28)), line_width * 0.42, soft_edge);
    float braces = max(vertical, max(diagonal_a, diagonal_b)) * 0.72;

    float shine = fill_alpha(sd_box(rotate_uv(p - vec2(-0.07, -0.08), -0.72), vec2(0.012, 0.105)), soft_edge);
    shine = max(shine, fill_alpha(sd_box(rotate_uv(p - vec2(0.08, 0.08), -0.72), vec2(0.009, 0.065)), soft_edge));

    float glow = smoothstep(0.54, 0.05, r) * 0.18;
    glow += max(outer_hex, mid_hex) * 0.20;
    glow += core_hex * 0.16;

    float shield = max(outer_hex, max(mid_hex, braces));
    vec3 rgb = glow_color.rgb * glow;
    rgb = mix(rgb, shield_color.rgb, shield);
    rgb = mix(rgb, core_color.rgb, core_hex * 0.86);
    rgb = mix(rgb, edge_color.rgb, max(core_ring, shine));

    float alpha = max(glow * 0.75, max(shield, max(core_hex, max(core_ring, shine))));
    COLOR = vec4(rgb, alpha);
}
