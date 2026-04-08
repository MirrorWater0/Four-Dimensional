shader_type canvas_item;
render_mode unshaded;

uniform float progress : hint_range(0.0, 1.0) = 0.0;
uniform vec4 veil_color : source_color = vec4(0.03, 0.08, 0.12, 1.0);
uniform vec4 accent_color : source_color = vec4(0.32, 0.82, 0.92, 1.0);
uniform vec4 glow_color : source_color = vec4(0.98, 0.86, 0.62, 1.0);
uniform float veil_strength : hint_range(0.0, 1.0) = 0.58;
uniform float frame_strength : hint_range(0.0, 1.0) = 0.24;
uniform float core_strength : hint_range(0.0, 1.0) = 0.12;
uniform float pattern_strength : hint_range(0.0, 1.0) = 0.035;
uniform float edge_width : hint_range(0.005, 0.08) = 0.018;
uniform float edge_softness : hint_range(0.005, 0.08) = 0.028;

void fragment()
{
    float intensity = clamp(progress, 0.0, 1.0);

    vec2 centered = UV - vec2(0.5);
    float radius = length(centered * vec2(1.08, 0.96)) * 1.26;
    float core = 1.0 - smoothstep(0.0, 0.72, radius);
    float side = min(min(UV.x, 1.0 - UV.x), min(UV.y, 1.0 - UV.y));
    float frame = 1.0 - smoothstep(edge_width, edge_width + edge_softness, side);
    float pattern_wave = sin((UV.x * 13.0 + UV.y * 9.0) * 3.14159265);
    float pattern = smoothstep(0.72, 1.0, pattern_wave * 0.5 + 0.5) * pattern_strength;

    float vignette = 1.0 - smoothstep(0.24, 0.92, length(centered * vec2(1.1, 0.96)) * 1.45);

    vec3 rgb = veil_color.rgb;
    rgb += accent_color.rgb * (frame * frame_strength + pattern) * intensity;
    rgb += glow_color.rgb * core * core_strength * intensity;
    rgb += accent_color.rgb * vignette * 0.08 * intensity;

    float alpha = intensity * veil_strength;
    alpha += frame * frame_strength * intensity * 0.42;
    alpha += core * core_strength * intensity * 0.56;
    alpha += pattern * intensity * 0.24;
    alpha += vignette * 0.06 * intensity;
    alpha = clamp(alpha, 0.0, 1.0);

    COLOR = vec4(rgb, alpha) * COLOR;
}
