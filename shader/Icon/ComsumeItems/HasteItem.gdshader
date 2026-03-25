shader_type canvas_item;

uniform vec4 trail_color : source_color = vec4(0.18, 0.80, 1.0, 1.0);
uniform vec4 arrow_color : source_color = vec4(0.98, 0.92, 0.35, 1.0);
uniform vec4 glow_color : source_color = vec4(0.92, 1.0, 1.0, 1.0);

float sdSegment(vec2 p, vec2 a, vec2 b) {
    vec2 pa = p - a;
    vec2 ba = b - a;
    float h = clamp(dot(pa, ba) / dot(ba, ba), 0.0, 1.0);
    return length(pa - ba * h);
}

float arrowMask(vec2 p, vec2 offset, float scale) {
    p -= offset;
    p /= max(scale, 0.0001);

    float shaft = 1.0 - smoothstep(0.035, 0.055, max(abs(p.y) - 0.08, abs(p.x + 0.12) - 0.19));
    float head_a = 1.0 - smoothstep(0.032, 0.05, sdSegment(p, vec2(-0.02, -0.20), vec2(0.24, 0.0)));
    float head_b = 1.0 - smoothstep(0.032, 0.05, sdSegment(p, vec2(-0.02, 0.20), vec2(0.24, 0.0)));
    return max(shaft, max(head_a, head_b));
}

void fragment() {
    vec2 uv = (UV - 0.5) * 2.0;
    float flow = fract(TIME * 1.8);

    float aa = max(fwidth(uv.x), fwidth(uv.y)) * 1.4;

    float arrow_front = arrowMask(uv, vec2(0.12, 0.0), 1.0);
    float arrow_back = arrowMask(uv, vec2(-0.14, 0.0), 0.92) * 0.78;
    float arrows = max(arrow_front, arrow_back);

    float line_top = 1.0 - smoothstep(0.018, 0.035, sdSegment(uv, vec2(-0.72 + flow * 0.18, -0.23), vec2(0.00 + flow * 0.18, -0.23)));
    float line_mid = 1.0 - smoothstep(0.02, 0.038, sdSegment(uv, vec2(-0.84 + flow * 0.24, 0.00), vec2(-0.10 + flow * 0.24, 0.00)));
    float line_bot = 1.0 - smoothstep(0.018, 0.035, sdSegment(uv, vec2(-0.68 + flow * 0.14, 0.23), vec2(0.06 + flow * 0.14, 0.23)));
    float trails = max(line_top, max(line_mid, line_bot));

    float halo = smoothstep(0.62, 0.12, length(uv - vec2(0.08, 0.0))) * 0.26;

    vec3 color = mix(trail_color.rgb, arrow_color.rgb, arrows);
    color = mix(color, glow_color.rgb, trails * 0.35 + halo * 0.5);
    color += arrows * 0.1;

    float alpha = arrows * 0.95 + trails * 0.52 + halo;
    alpha = clamp(alpha, 0.0, 1.0);

    COLOR = vec4(color, alpha);
}
