shader_type canvas_item;

uniform vec4 fill_color : source_color = vec4(0.76, 0.95, 1.0, 0.96);
uniform vec4 outline_color : source_color = vec4(0.01, 0.04, 0.06, 0.78);
uniform float outline_width : hint_range(0.0, 0.35, 0.005) = 0.11;
uniform float edge_softness : hint_range(0.0, 0.08, 0.001) = 0.01;

float triangle_edge(vec2 uv, float inset) {
    float half_width = max(0.0, (1.0 - uv.x) * 0.5 - inset);
    float diagonal_edge = half_width - abs(uv.y - 0.5);
    float base_edge = uv.x - inset;
    return min(base_edge, diagonal_edge);
}

void fragment() {
    float outer_edge = triangle_edge(UV, 0.0);
    float aa = max(fwidth(outer_edge), edge_softness);
    float outer = smoothstep(0.0, aa, outer_edge);

    float inner_edge = triangle_edge(UV, outline_width);
    float inner = smoothstep(0.0, aa, inner_edge);

    vec4 color = mix(outline_color, fill_color, inner);
    color.a *= outer;
    COLOR = color;
}
