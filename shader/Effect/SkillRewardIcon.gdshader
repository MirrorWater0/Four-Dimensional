shader_type canvas_item;
render_mode blend_add;

uniform vec4 main_color : source_color = vec4(0.42, 0.92, 1.0, 1.0);
uniform vec4 accent_color : source_color = vec4(1.0, 1.0, 1.0, 1.0);
uniform float glow_strength : hint_range(0.0, 3.0) = 1.25;
uniform float line_width : hint_range(0.005, 0.08) = 0.025;

float sd_box(vec2 p, vec2 b) {
    vec2 d = abs(p) - b;
    return length(max(d, vec2(0.0))) + min(max(d.x, d.y), 0.0);
}

void fragment() {
    vec2 p = (UV - vec2(0.5)) * 2.0;
    p.x *= 0.78;

    float card = sd_box(p, vec2(0.38, 0.56));
    float border = 1.0 - smoothstep(line_width, line_width + 0.018, abs(card));
    float fill = 1.0 - smoothstep(-0.12, 0.18, card);

    vec2 rune_p = p;
    rune_p.y += 0.03;
    float diamond = abs(rune_p.x) + abs(rune_p.y) - 0.32;
    float rune = 1.0 - smoothstep(line_width, line_width + 0.016, abs(diamond));

    float center_line = 1.0 - smoothstep(line_width, line_width + 0.014, abs(p.x));
    center_line *= smoothstep(0.42, 0.18, abs(p.y));

    float glow = smoothstep(0.95, 0.0, length(p)) * glow_strength;
    float alpha = clamp(border + rune + center_line * 0.6 + fill * 0.16 + glow * 0.18, 0.0, 1.0);
    vec3 color = mix(main_color.rgb, accent_color.rgb, clamp(rune + center_line, 0.0, 1.0));
    color += main_color.rgb * glow * 0.35;

    COLOR = vec4(color, alpha * main_color.a) * COLOR;
}
