shader_type canvas_item;
render_mode unshaded;

uniform vec4 face_color : source_color = vec4(0.97, 0.77, 0.26, 1.0);
uniform vec4 edge_color : source_color = vec4(0.16, 0.12, 0.055, 1.0);
uniform vec4 accent_color : source_color = vec4(1.0, 0.91, 0.55, 1.0);
uniform float outline_width : hint_range(0.0, 3.0, 0.1) = 1.2;
uniform float triangle_gap_pixels : hint_range(0.0, 16.0, 0.1) = 3.0;
uniform float tilt_degrees : hint_range(-180.0, 180.0, 0.1) = 0.0;
uniform vec2 offset_pixels = vec2(0.0, 0.0);
uniform float press_amount : hint_range(0.0, 1.0) = 0.0;
uniform float burst_amount : hint_range(0.0, 1.0) = 0.0;
uniform float motion_amount : hint_range(0.0, 1.0) = 0.0;

float segment_distance(vec2 p, vec2 a, vec2 b) {
    vec2 edge = b - a;
    return length(p - a - edge * clamp(dot(p - a, edge) / dot(edge, edge), 0.0, 1.0));
}

float cross_2d(vec2 a, vec2 b) {
    return a.x * b.y - a.y * b.x;
}

float triangle_distance(vec2 p, vec2 a, vec2 b, vec2 c) {
    float edge_distance = min(segment_distance(p, a, b),
        min(segment_distance(p, b, c), segment_distance(p, c, a)));
    float orientation = sign(cross_2d(b - a, c - a));
    bool inside = orientation * cross_2d(b - a, p - a) >= 0.0
        && orientation * cross_2d(c - b, p - b) >= 0.0
        && orientation * cross_2d(a - c, p - c) >= 0.0;
    return inside ? -edge_distance : edge_distance;
}

void vertex() {
    // Rotate the entire quad around the graphic's tip without clipping the shape.
    vec2 tip = vec2(18.5, 5.0);
    vec2 offset = VERTEX - tip;
    float angle = radians(tilt_degrees);
    float cosine = cos(angle);
    float sine = sin(angle);
    VERTEX = tip + vec2(
        cosine * offset.x - sine * offset.y,
        sine * offset.x + cosine * offset.y
    ) + offset_pixels;
}

void fragment() {
    // The graphic's reference tip is independent of the Cursor node's origin.
    vec2 tip = vec2(18.5, 5.0);
    vec2 offset = (UV * 48.0 - tip) / (1.0 - press_amount * 0.045);
    vec2 p = offset + vec2(-1.5, 0.0);
    float left_distance = triangle_distance(p,
        vec2(-1.5, 0.0), vec2(-10.0, 21.0), vec2(-1.5, 34.0));
    // Move only the right half to preserve the left tip's hotspot alignment.
    vec2 right_p = p - vec2(max(triangle_gap_pixels, 0.0) - 3.0, 0.0);
    float right_distance = triangle_distance(right_p,
        vec2(1.5, 0.0), vec2(10.0, 21.0), vec2(1.5, 34.0));
    float d = min(left_distance, right_distance);

    float aa = max(fwidth(d), 0.55);
    float mask = 1.0 - smoothstep(-aa * 0.5, aa * 0.5, d);
    // Inset outlines preserve the transparent gap between the two halves.
    float fill = 1.0 - smoothstep(-outline_width - aa * 0.5,
        -outline_width + aa * 0.5, d);
    vec3 face = mix(face_color.rgb, accent_color.rgb,
        press_amount * 0.08 + burst_amount * 0.05 + motion_amount * 0.015);
    COLOR = vec4(mix(edge_color.rgb, face, fill), mask * face_color.a);
}
