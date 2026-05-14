shader_type canvas_item;

uniform vec4 fill_color : source_color = vec4(0.10, 0.16, 0.24, 0.88);
uniform vec4 edge_color : source_color = vec4(0.34, 0.50, 0.72, 0.26);
uniform float horizontal_fade = 0.18;
uniform float vertical_softness = 0.16;

void fragment()
{
    float fade_left = smoothstep(0.0, horizontal_fade, UV.x);
    float fade_right = smoothstep(0.0, horizontal_fade, 1.0 - UV.x);
    float fade_top = smoothstep(0.0, vertical_softness, UV.y);
    float fade_bottom = smoothstep(0.0, vertical_softness, 1.0 - UV.y);
    float edge_mix = abs(UV.x - 0.5) * 2.0;
    vec4 tint = mix(fill_color, edge_color, edge_mix);
    float alpha = fade_left * fade_right * mix(0.72, 1.0, fade_top * fade_bottom);
    COLOR = vec4(tint.rgb, tint.a * alpha);
}
