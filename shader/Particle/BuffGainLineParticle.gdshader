shader_type canvas_item;
render_mode blend_add;

uniform vec4 line_color : source_color = vec4(0.55, 0.95, 1.0, 1.0);
uniform float half_width : hint_range(0.002, 0.08) = 0.012;
uniform float edge_softness : hint_range(0.0005, 0.04) = 0.003;
uniform float end_softness : hint_range(0.0, 0.18) = 0.035;
uniform float glow_width : hint_range(0.0, 0.16) = 0.035;
uniform float glow_strength : hint_range(0.0, 1.0) = 0.18;

void fragment() {
    float x = abs(UV.x - 0.5);
    float rect = 1.0 - smoothstep(half_width, half_width + edge_softness, x);
    float glow = 1.0 - smoothstep(half_width, half_width + glow_width, x);
    float top = smoothstep(0.0, max(end_softness, 0.0001), UV.y);
    float bottom = 1.0 - smoothstep(1.0 - max(end_softness, 0.0001), 1.0, UV.y);
    float end_mask = mix(1.0, top * bottom, step(0.0001, end_softness));
    float alpha = (rect + glow * glow_strength) * end_mask * COLOR.a * line_color.a;

    COLOR = vec4(line_color.rgb * COLOR.rgb, alpha);
}
