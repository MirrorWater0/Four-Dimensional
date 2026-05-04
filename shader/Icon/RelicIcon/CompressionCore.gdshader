shader_type canvas_item;

uniform vec4 core_color : source_color = vec4(0.18, 0.95, 1.0, 1.0);
uniform vec4 accent_color : source_color = vec4(0.95, 0.98, 1.0, 1.0);
uniform vec4 glow_color : source_color = vec4(0.10, 0.55, 0.75, 1.0);
uniform float scale : hint_range(0.2, 1.2) = 0.92;
uniform float line_thickness : hint_range(0.002, 0.08) = 0.018;
uniform float soft_edge : hint_range(0.001, 0.08) = 0.012;
uniform float diamond_size : hint_range(0.05, 0.45) = 0.24;
uniform float ring_radius : hint_range(0.05, 0.45) = 0.31;
uniform float ring_thickness : hint_range(0.003, 0.08) = 0.018;

float sd_box(vec2 p, vec2 b) {
    vec2 d = abs(p) - b;
    return length(max(d, 0.0)) + min(max(d.x, d.y), 0.0);
}

float sd_diamond(vec2 p, float radius) {
    p = abs(p);
    return (p.x + p.y - radius) * 0.70710678;
}

float stroke_alpha(float dist, float thickness, float smoothing) {
    float edge = abs(dist) - thickness;
    return smoothstep(smoothing, 0.0, edge);
}

void fragment() {
    vec2 uv = (UV - 0.5) / scale;
    float r = length(uv);

    float outer_ring = stroke_alpha(r - ring_radius, ring_thickness, soft_edge);
    float inner_ring = stroke_alpha(r - ring_radius * 0.63, ring_thickness * 0.8, soft_edge);
    float diamond = stroke_alpha(sd_diamond(uv, diamond_size), line_thickness, soft_edge);

    float vertical_bar = stroke_alpha(
        sd_box(uv, vec2(line_thickness * 0.55, diamond_size * 0.82)),
        line_thickness * 0.55,
        soft_edge
    );
    float horizontal_bar = stroke_alpha(
        sd_box(uv, vec2(diamond_size * 0.82, line_thickness * 0.55)),
        line_thickness * 0.55,
        soft_edge
    );

    float corner_a = stroke_alpha(sd_box(uv - vec2(0.18, 0.18), vec2(0.035, 0.035)), 0.008, soft_edge);
    float corner_b = stroke_alpha(sd_box(uv + vec2(0.18, 0.18), vec2(0.035, 0.035)), 0.008, soft_edge);
    float corner_c = stroke_alpha(sd_box(vec2(uv.x - 0.18, uv.y + 0.18), vec2(0.035, 0.035)), 0.008, soft_edge);
    float corner_d = stroke_alpha(sd_box(vec2(uv.x + 0.18, uv.y - 0.18), vec2(0.035, 0.035)), 0.008, soft_edge);

    float corner_links = 0.0;
    corner_links = max(corner_links, stroke_alpha(sd_box(uv - vec2(0.18, 0.0), vec2(0.012, 0.10)), 0.006, soft_edge));
    corner_links = max(corner_links, stroke_alpha(sd_box(uv + vec2(0.18, 0.0), vec2(0.012, 0.10)), 0.006, soft_edge));
    corner_links = max(corner_links, stroke_alpha(sd_box(uv - vec2(0.0, 0.18), vec2(0.10, 0.012)), 0.006, soft_edge));
    corner_links = max(corner_links, stroke_alpha(sd_box(uv + vec2(0.0, 0.18), vec2(0.10, 0.012)), 0.006, soft_edge));

    float structure = max(max(outer_ring, inner_ring), max(diamond, max(vertical_bar, horizontal_bar)));
    float nodes = max(max(corner_a, corner_b), max(corner_c, corner_d));
    float links = max(corner_links, 0.0);

    float glow = smoothstep(0.42, 0.0, abs(r - ring_radius)) * 0.22;
    glow += smoothstep(0.25, 0.0, abs(sd_diamond(uv, diamond_size + 0.02))) * 0.18;

    vec3 rgb = vec3(0.0);
    rgb += glow_color.rgb * glow;
    rgb = mix(rgb, core_color.rgb, structure);
    rgb = mix(rgb, accent_color.rgb, max(nodes, links));

    float alpha = max(glow * 0.8, max(structure, max(nodes, links)));
    COLOR = vec4(rgb, alpha);
}
