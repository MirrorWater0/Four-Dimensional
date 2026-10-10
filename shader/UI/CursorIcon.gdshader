shader_type canvas_item;
render_mode unshaded;

uniform vec4 face_color : source_color = vec4(0.93, 0.96, 0.98, 1.0);
uniform vec4 facet_color : source_color = vec4(0.45, 0.7, 0.82, 1.0);
uniform vec4 edge_color : source_color = vec4(0.08, 0.14, 0.2, 1.0);
uniform vec4 accent_color : source_color = vec4(0.93, 0.82, 0.59, 1.0);
uniform float press_amount : hint_range(0.0, 1.0) = 0.0;
uniform float burst_amount : hint_range(0.0, 1.0) = 0.0;
uniform float motion_amount : hint_range(0.0, 1.0) = 0.0;

float cross_2d(vec2 a, vec2 b) {
    return a.x * b.y - a.y * b.x;
}

float segment_distance(vec2 p, vec2 a, vec2 b) {
    vec2 edge = b - a;
    return length(p - a - edge * clamp(dot(p - a, edge) / dot(edge, edge), 0.0, 1.0));
}

float triangle_distance(vec2 p, vec2 a, vec2 b, vec2 c) {
    float distance_to_edge = min(segment_distance(p, a, b),
        min(segment_distance(p, b, c), segment_distance(p, c, a)));
    float orientation = sign(cross_2d(b - a, c - a));
    float inside = min(orientation * cross_2d(b - a, p - a),
        min(orientation * cross_2d(c - b, p - b), orientation * cross_2d(a - c, p - c)));
    return inside >= 0.0 ? -distance_to_edge : distance_to_edge;
}

void fragment() {
    // All geometry is measured in the 48px control. (8, 8) is the exact hotspot.
    vec2 tip = vec2(8.0, 8.0);
    vec2 p = tip + (UV * 48.0 - tip) / (1.0 - press_amount * 0.07);
    vec2 heel = vec2(10.5, 33.0);
    vec2 notch = vec2(17.0, 25.0);
    vec2 wing = vec2(29.0, 26.0);
    float d = min(triangle_distance(p, tip, heel, notch), triangle_distance(p, tip, notch, wing));
    float aa = max(fwidth(d), 0.65);
    float mask = 1.0 - smoothstep(-aa * 0.5, aa * 0.5, d);
    float edge = smoothstep(-1.4 - aa, -1.4 + aa, d);

    float split = cross_2d(notch - tip, p - tip) / length(notch - tip);
    vec3 face = mix(facet_color.rgb, face_color.rgb, smoothstep(-0.6, 0.6, split));
    face = mix(face, accent_color.rgb, press_amount * 0.24);
    float seam = (1.0 - smoothstep(0.15, 0.8, abs(split)))
        * smoothstep(9.0, 15.0, p.y) * (1.0 - smoothstep(23.0, 26.0, p.y));
    face = mix(face, accent_color.rgb, seam * (0.6 + burst_amount * 0.3));
    face += motion_amount * 0.035;

    float halo = exp(-max(d, 0.0) * 1.0) * (1.0 - mask) * (0.09 + burst_amount * 0.08);
    vec3 ink = mix(face, edge_color.rgb, edge);
    float alpha = mask * face_color.a + halo;
    vec3 color = (ink * mask * face_color.a + facet_color.rgb * halo) / max(alpha, 0.001);
    COLOR = vec4(color, alpha);
}
