shader_type canvas_item;

uniform vec4 bg_color : source_color = vec4(0.0, 0.0, 0.0, 0.0);
uniform vec4 fill_color : source_color = vec4(0.47, 0.74, 1.0, 1.0);
uniform vec4 edge_color : source_color = vec4(0.85, 0.94, 1.0, 1.0);
uniform float card_size : hint_range(0.40, 0.95) = 0.54;
uniform float edge_width : hint_range(0.0, 0.08) = 0.02;
uniform float offset_step : hint_range(0.02, 0.20) = 0.085;

float rect_mask(vec2 uv, vec2 center, vec2 half_size) {
    vec2 d = abs(uv - center) - half_size;
    float outside = max(d.x, d.y);
    float aa = fwidth(outside);
    return smoothstep(aa, -aa, outside);
}

void fragment() {
    vec2 uv = UV;
    vec2 half_size = vec2(card_size * 0.34, card_size * 0.22);

    vec2 c0 = vec2(0.5 - offset_step, 0.5 + offset_step);
    vec2 c1 = vec2(0.5, 0.5);
    vec2 c2 = vec2(0.5 + offset_step, 0.5 - offset_step);

    float m0 = rect_mask(uv, c0, half_size);
    float m1 = rect_mask(uv, c1, half_size);
    float m2 = rect_mask(uv, c2, half_size);

    float mask = max(max(m0, m1), m2);

    vec2 inner_half = max(half_size - vec2(edge_width), vec2(0.001));
    float i0 = rect_mask(uv, c0, inner_half);
    float i1 = rect_mask(uv, c1, inner_half);
    float i2 = rect_mask(uv, c2, inner_half);
    float inner = max(max(i0, i1), i2);

    float edge = clamp(mask - inner, 0.0, 1.0);
    vec3 rgb = mix(fill_color.rgb, edge_color.rgb, edge);
    float alpha = mask * fill_color.a;

    vec4 col = vec4(rgb, alpha);
    COLOR = mix(bg_color, col, mask);
}

