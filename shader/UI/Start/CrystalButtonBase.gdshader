shader_type canvas_item;
render_mode unshaded;

uniform vec2 panel_size = vec2(520.0, 78.0);
uniform vec4 base_tint : source_color = vec4(0.39, 0.41, 0.42, 1.0);
uniform float prominence = 1.0;
uniform float disabled_amount = 0.0;

float rounded_box(vec2 p, vec2 half_size, float radius) {
    vec2 q = abs(p) - half_size + radius;
    return length(max(q, vec2(0.0))) + min(max(q.x, q.y), 0.0) - radius;
}

void fragment() {
    vec4 tint = COLOR;
    vec2 size = max(panel_size, vec2(1.0));
    vec2 p = (UV - 0.5) * size;
    float sd = rounded_box(p, size * 0.5 - 3.0, 5.0);
    float aa = max(fwidth(sd), 0.65);
    float mask = 1.0 - smoothstep(-aa, aa, sd);
    // A thin neutral tint leaves the scenery visible through the center.
    float vertical = smoothstep(0.0, 1.0, UV.y);
    vec3 base = base_tint.rgb * mix(0.70, 0.48, vertical);
    vec2 pixel = UV * size;
    float line_aa = max(fwidth(pixel.x), 0.65);
    float is_wide = step(300.0, size.x);
    float separator = (1.0 - smoothstep(0.35, 0.35 + line_aa, abs(pixel.x - (size.x - 76.0))))
        * smoothstep(17.0, 19.0, pixel.y) * (1.0 - smoothstep(size.y - 19.0, size.y - 17.0, pixel.y));
    float underline = (1.0 - smoothstep(0.25, 0.25 + line_aa, abs(pixel.y - (size.y - 7.0))))
        * smoothstep(13.0, 15.0, pixel.x) * (1.0 - smoothstep(35.0, 37.0, pixel.x));
    float upper_rule = (1.0 - smoothstep(0.25, 0.25 + line_aa, abs(pixel.y - 7.0)))
        * smoothstep(13.0, 15.0, pixel.x) * (1.0 - smoothstep(size.x - 18.0, size.x - 16.0, pixel.x));
    float muted = dot(base, vec3(0.21, 0.72, 0.07));
    base = mix(base, vec3(muted) * 0.50, disabled_amount * 0.86);
    float alpha = mix(0.10, 0.18, prominence) * (1.0 - disabled_amount * 0.30);
    float line_alpha = (separator * is_wide * 0.17 + underline * 0.23 + upper_rule * 0.035)
        * (1.0 - disabled_amount * 0.72);
    vec3 premultiplied = vec3(0.76, 0.79, 0.81) * line_alpha + base * alpha * (1.0 - line_alpha);
    alpha = line_alpha + alpha * (1.0 - line_alpha);
    COLOR = vec4(premultiplied / max(alpha, 0.0001), alpha * mask) * tint;
}
