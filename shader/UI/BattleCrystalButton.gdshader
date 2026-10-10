shader_type canvas_item;
render_mode unshaded;

uniform vec2 panel_size = vec2(216.0, 80.0);
uniform vec4 line_color : source_color = vec4(0.82, 0.94, 0.93, 1.0);
uniform float line_width : hint_range(0.5, 3.0) = 1.0;
uniform float corner_cut : hint_range(4.0, 24.0) = 16.0;
uniform float hover_amount : hint_range(0.0, 1.0) = 0.0;
uniform float press_amount : hint_range(0.0, 1.0) = 0.0;
uniform float disabled_amount : hint_range(0.0, 1.0) = 0.0;

float chamfered_box(vec2 p, vec2 half_size, float cut) {
    vec2 q = abs(p) - half_size;
    // A signed distance for a rectangular gate with clipped corners.
    return max(max(q.x, q.y), abs(p.x) + abs(p.y) - (half_size.x + half_size.y - cut));
}

void fragment() {
    vec2 size = max(panel_size, vec2(1.0));
    vec2 p = (UV - 0.5) * size;
    float d = chamfered_box(p, size * 0.5 - 2.0, corner_cut);
    float aa = max(fwidth(d), 0.6);
    // Only the contour contributes alpha; the entire interior stays transparent.
    float outline = 1.0 - smoothstep(line_width * 0.5, line_width * 0.5 + aa, abs(d));
    vec2 corner_distance = size * 0.5 - abs(p);
    float corner_accent = 1.0 - smoothstep(corner_cut, corner_cut + 10.0,
        max(corner_distance.x, corner_distance.y));
    float opacity = (0.48 + corner_accent * 0.22 + hover_amount * 0.28 + press_amount * 0.12);
    opacity *= 1.0 - disabled_amount * 0.58;
    vec3 color = mix(line_color.rgb, vec3(1.0), hover_amount * 0.35);
    color = mix(color, vec3(0.58, 0.88, 0.80), press_amount * 0.55);
    COLOR = vec4(color, outline * clamp(opacity, 0.0, 1.0) * line_color.a);
}
