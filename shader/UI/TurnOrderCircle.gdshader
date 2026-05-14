shader_type canvas_item;

uniform vec4 fill_color : source_color = vec4(0.08, 0.18, 0.28, 0.82);
uniform vec4 rim_color : source_color = vec4(0.45, 0.88, 1.0, 1.0);
uniform vec4 glow_color : source_color = vec4(0.45, 0.88, 1.0, 0.7);
uniform float ring_width : hint_range(0.01, 0.3) = 0.09;
uniform float glow_size : hint_range(0.0, 0.4) = 0.12;
uniform float soft_edge : hint_range(0.001, 0.08) = 0.02;
uniform float center_brightness : hint_range(0.0, 1.5) = 0.18;

void fragment()
{
    vec2 p = UV - vec2(0.5);
    float dist = length(p);

    float outer = 0.5;
    float inner = outer - ring_width;

    float fill = 1.0 - smoothstep(inner, inner + soft_edge, dist);
    float ring = smoothstep(inner - soft_edge, inner, dist)
        * (1.0 - smoothstep(outer - soft_edge, outer, dist));
    float glow = 1.0 - smoothstep(outer, outer + glow_size, dist);
    float core = 1.0 - smoothstep(0.0, inner, dist);

    vec3 color = fill_color.rgb * fill;
    color += rim_color.rgb * ring;
    color += glow_color.rgb * glow * 0.45;
    color += rim_color.rgb * core * center_brightness;

    float alpha = max(fill * fill_color.a + ring * rim_color.a, glow * glow_color.a * 0.55);
    COLOR *= vec4(color, alpha);
}
