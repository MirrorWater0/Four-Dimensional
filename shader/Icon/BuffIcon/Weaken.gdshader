shader_type canvas_item;

uniform vec4 base_color : source_color = vec4(0.65, 0.54, 0.21, 1.0);
uniform vec4 shadow_color : source_color = vec4(0.28, 0.18, 0.04, 1.0);
uniform vec4 crack_color : source_color = vec4(0.98, 0.87, 0.49, 1.0);

float sd_box(vec2 p, vec2 b) {
    vec2 d = abs(p) - b;
    return length(max(d, vec2(0.0))) + min(max(d.x, d.y), 0.0);
}

void fragment() {
    vec2 uv = UV - vec2(0.5);
    uv.y *= -1.0;

    float diamond = abs(uv.x) + abs(uv.y * 1.05);
    float mask = smoothstep(0.56, 0.50, diamond);

    float notch_left = smoothstep(0.09, 0.0, sd_box(uv - vec2(-0.18, -0.02), vec2(0.10, 0.07)));
    float notch_right = smoothstep(0.09, 0.0, sd_box(uv - vec2(0.18, -0.02), vec2(0.10, 0.07)));
    float top_cut = smoothstep(0.06, 0.0, sd_box(uv - vec2(0.0, 0.22), vec2(0.12, 0.05)));

    mask *= 1.0 - max(max(notch_left, notch_right), top_cut) * 0.92;

    float shade = smoothstep(-0.45, 0.35, uv.y + uv.x * 0.25);
    vec3 color = mix(shadow_color.rgb, base_color.rgb, shade);

    float crack_a = smoothstep(0.030, 0.0, abs(uv.x + uv.y * 0.36));
    float crack_b = smoothstep(0.026, 0.0, abs(uv.x - 0.18 + uv.y * 1.25));
    float crack_c = smoothstep(0.026, 0.0, abs(uv.x + 0.16 - uv.y * 1.15));
    float cracks = max(crack_a * 0.9, max(crack_b * 0.7, crack_c * 0.7)) * mask;

    color = mix(color, crack_color.rgb, cracks);

    float rim = smoothstep(0.56, 0.47, diamond) - smoothstep(0.48, 0.42, diamond);
    color += rim * 0.10;

    COLOR *= vec4(color, mask);
}
