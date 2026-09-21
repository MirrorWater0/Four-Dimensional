shader_type canvas_item;
render_mode unshaded;

uniform vec2 panel_size = vec2(216.0, 80.0);
uniform float hover_amount : hint_range(0.0, 1.0) = 0.0;
uniform float press_amount : hint_range(0.0, 1.0) = 0.0;
uniform float disabled_amount : hint_range(0.0, 1.0) = 0.0;

float rounded_box(vec2 p, vec2 half_size, float radius) {
    vec2 q = abs(p) - half_size + radius;
    return length(max(q, vec2(0.0))) + min(max(q.x, q.y), 0.0) - radius;
}

void fragment() {
    vec2 size = max(panel_size, vec2(1.0));
    vec2 p = (UV - 0.5) * size;
    float d = rounded_box(p, size * 0.5 - 1.0, 9.0);
    float aa = max(fwidth(d), 0.6);
    float mask = 1.0 - smoothstep(-aa, aa, d);
    float rim = 1.0 - smoothstep(0.5, 1.5 + aa, -d);
    float inset = exp(-pow((d + 3.4) / 0.7, 2.0));
    float bevel = 1.0 - smoothstep(1.0, 7.0, -d);
    float strength = (1.0 - disabled_amount * 0.75) * (1.0 - press_amount * 0.3);
    float corner = exp(-length((UV - vec2(0.06, 0.03)) * vec2(2.7, 1.0)) * 3.5);
    corner += exp(-length((UV - vec2(0.94, 0.97)) * vec2(2.7, 1.0)) * 4.0);
    float crown = exp(-UV.y * 22.0);
    float lip = smoothstep(0.7, 1.0, UV.y);
    float cap_edge = 0.24 - UV.x * 0.10;
    float cap = (1.0 - smoothstep(cap_edge - 0.008, cap_edge + 0.008, UV.y));
    float streak = exp(-pow((UV.x + UV.y * 0.22 - 0.22) / 0.15, 2.0));
    vec3 dark_glass = mix(vec3(0.035, 0.075, 0.09), vec3(0.10, 0.18, 0.19), 1.0 - UV.y);
    vec3 pearl = vec3(0.79, 0.94, 0.93);
    vec3 color = dark_glass + pearl * (cap * 0.075 + streak * 0.035 + hover_amount * 0.045) * strength;
    float edge_light = rim * (0.23 + crown * 0.55 + corner * 0.65 + lip * 0.14 + hover_amount * 0.22);
    color += pearl * (edge_light + inset * (0.09 + lip * 0.15) + bevel * 0.04) * strength;
    color = mix(color, vec3(dot(color, vec3(0.2126, 0.7152, 0.0722))), disabled_amount * 0.5);
    COLOR = vec4(color, mask * (0.85 + rim * 0.15));
}
