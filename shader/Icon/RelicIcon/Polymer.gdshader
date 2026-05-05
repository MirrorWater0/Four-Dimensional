shader_type canvas_item;

uniform vec4 node_color : source_color = vec4(0.55, 0.95, 1.0, 1.0);
uniform vec4 bond_color : source_color = vec4(0.95, 0.98, 1.0, 1.0);
uniform vec4 core_color : source_color = vec4(0.12, 0.42, 0.95, 1.0);
uniform vec4 glow_color : source_color = vec4(0.05, 0.75, 0.95, 1.0);
uniform float scale : hint_range(0.25, 1.25) = 0.9;
uniform float node_radius : hint_range(0.02, 0.18) = 0.068;
uniform float bond_width : hint_range(0.004, 0.08) = 0.018;
uniform float soft_edge : hint_range(0.001, 0.08) = 0.018;

float sd_circle(vec2 p, float radius) {
    return length(p) - radius;
}

float sd_regular_polygon(vec2 p, float radius, float sides) {
    float angle = atan(p.y, p.x);
    float sector = 6.28318530718 / sides;
    return cos(floor(0.5 + angle / sector) * sector - angle) * length(p) - radius;
}

float sd_segment(vec2 p, vec2 a, vec2 b) {
    vec2 pa = p - a;
    vec2 ba = b - a;
    float h = clamp(dot(pa, ba) / dot(ba, ba), 0.0, 1.0);
    return length(pa - ba * h);
}

float fill_alpha(float dist, float smoothing) {
    return smoothstep(smoothing, 0.0, dist);
}

float stroke_alpha(float dist, float thickness, float smoothing) {
    return smoothstep(smoothing, 0.0, abs(dist) - thickness);
}

float bond_alpha(vec2 p, vec2 a, vec2 b) {
    return fill_alpha(sd_segment(p, a, b) - bond_width, soft_edge);
}

float node_alpha(vec2 p, vec2 center, float radius) {
    float core = fill_alpha(sd_circle(p - center, radius), soft_edge);
    float ring = stroke_alpha(sd_regular_polygon(p - center, radius * 1.28, 6.0), bond_width * 0.45, soft_edge);
    return max(core, ring);
}

void fragment() {
    vec2 p = (UV - 0.5) / scale;

    vec2 n0 = vec2(-0.26, 0.08);
    vec2 n1 = vec2(-0.08, -0.16);
    vec2 n2 = vec2(0.16, -0.10);
    vec2 n3 = vec2(0.28, 0.14);
    vec2 n4 = vec2(0.03, 0.23);
    vec2 n5 = vec2(-0.20, 0.30);

    float bonds = 0.0;
    bonds = max(bonds, bond_alpha(p, n0, n1));
    bonds = max(bonds, bond_alpha(p, n1, n2));
    bonds = max(bonds, bond_alpha(p, n2, n3));
    bonds = max(bonds, bond_alpha(p, n3, n4));
    bonds = max(bonds, bond_alpha(p, n4, n0));
    bonds = max(bonds, bond_alpha(p, n4, n5));
    bonds = max(bonds, bond_alpha(p, n0, n5));

    float nodes = 0.0;
    nodes = max(nodes, node_alpha(p, n0, node_radius));
    nodes = max(nodes, node_alpha(p, n1, node_radius * 0.92));
    nodes = max(nodes, node_alpha(p, n2, node_radius * 1.04));
    nodes = max(nodes, node_alpha(p, n3, node_radius * 0.95));
    nodes = max(nodes, node_alpha(p, n4, node_radius * 1.15));
    nodes = max(nodes, node_alpha(p, n5, node_radius * 0.82));

    float outer = stroke_alpha(sd_regular_polygon(p + vec2(0.01, -0.01), 0.42, 6.0), 0.012, soft_edge);
    float inner = stroke_alpha(sd_regular_polygon(p + vec2(0.01, -0.01), 0.29, 6.0), 0.006, soft_edge);

    float radial_glow = smoothstep(0.58, 0.04, length(p)) * 0.18;
    float structure_glow = max(nodes, bonds) * 0.28 + outer * 0.18;

    vec3 rgb = glow_color.rgb * (radial_glow + structure_glow);
    rgb = mix(rgb, bond_color.rgb, max(bonds, inner));
    rgb = mix(rgb, core_color.rgb, outer * 0.8);
    rgb = mix(rgb, node_color.rgb, nodes);

    float alpha = max(radial_glow * 0.85, max(max(nodes, bonds), max(outer, inner)));
    COLOR = vec4(rgb, alpha);
}
