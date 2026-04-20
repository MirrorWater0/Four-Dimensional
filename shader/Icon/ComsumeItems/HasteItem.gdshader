shader_type canvas_item;

uniform vec4 main_color : source_color = vec4(0.18, 0.78, 1.0, 1.0);
uniform vec4 accent_color : source_color = vec4(0.98, 0.94, 0.38, 1.0);
uniform vec4 glow_color : source_color = vec4(0.90, 1.0, 1.0, 1.0);

float sdSegment(vec2 p, vec2 a, vec2 b) {
    vec2 pa = p - a;
    vec2 ba = b - a;
    float h = clamp(dot(pa, ba) / dot(ba, ba), 0.0, 1.0);
    return length(pa - ba * h);
}

float sdDiamond(vec2 p, vec2 size) {
    p = abs(p);
    return (p.x / size.x + p.y / size.y - 1.0) * min(size.x, size.y);
}

void fragment() {
    vec2 uv = (UV - 0.5) * 2.0;
    float aa = max(fwidth(uv.x), fwidth(uv.y)) * 1.3;
    float flow = fract(TIME * 1.4);

    float core = 1.0 - smoothstep(aa, 0.05 + aa, sdDiamond(uv + vec2(0.02, 0.0), vec2(0.16, 0.24)));

    float wing_l1 = 1.0 - smoothstep(0.03, 0.055, sdSegment(uv, vec2(-0.56, 0.00), vec2(-0.08, -0.22)));
    float wing_l2 = 1.0 - smoothstep(0.03, 0.055, sdSegment(uv, vec2(-0.52, 0.22), vec2(-0.08, -0.02)));
    float wing_l3 = 1.0 - smoothstep(0.03, 0.055, sdSegment(uv, vec2(-0.42, 0.42), vec2(-0.02, 0.16)));
    float wing_r1 = 1.0 - smoothstep(0.03, 0.055, sdSegment(uv, vec2(0.56, 0.00), vec2(0.08, -0.22)));
    float wing_r2 = 1.0 - smoothstep(0.03, 0.055, sdSegment(uv, vec2(0.52, 0.22), vec2(0.08, -0.02)));
    float wing_r3 = 1.0 - smoothstep(0.03, 0.055, sdSegment(uv, vec2(0.42, 0.42), vec2(0.02, 0.16)));
    float wings = max(max(wing_l1, wing_l2), max(wing_l3, max(wing_r1, max(wing_r2, wing_r3))));

    float stream_top = 1.0 - smoothstep(0.02, 0.04, sdSegment(uv, vec2(-0.72 + flow * 0.22, -0.36), vec2(0.14 + flow * 0.22, -0.36)));
    float stream_mid = 1.0 - smoothstep(0.02, 0.04, sdSegment(uv, vec2(-0.84 + flow * 0.28, -0.04), vec2(0.02 + flow * 0.28, -0.04)));
    float stream_low = 1.0 - smoothstep(0.02, 0.04, sdSegment(uv, vec2(-0.62 + flow * 0.18, 0.30), vec2(0.20 + flow * 0.18, 0.30)));
    float streams = max(stream_top, max(stream_mid, stream_low));

    vec3 color = mix(main_color.rgb, accent_color.rgb, wings * 0.55 + core * 0.45);
    color = mix(color, glow_color.rgb, streams * 0.45 + core * 0.35);
    color += wings * 0.08;

    float alpha = wings * 0.95 + core * 0.86 + streams * 0.44;
    alpha = clamp(alpha, 0.0, 1.0);

    COLOR = vec4(color, alpha);
}
