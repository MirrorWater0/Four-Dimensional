shader_type canvas_item;

uniform vec4 main_color : source_color = vec4(1.0, 0.34, 0.12, 1.0);
uniform vec4 accent_color : source_color = vec4(1.0, 0.82, 0.22, 1.0);
uniform vec4 glow_color : source_color = vec4(1.0, 0.96, 0.76, 1.0);

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
    uv.y += 0.03;

    float aa = max(fwidth(uv.x), fwidth(uv.y)) * 1.5;
    float pulse = 0.5 + 0.5 * sin(TIME * 3.8);

    float core = 1.0 - smoothstep(aa, 0.05 + aa, sdDiamond(uv + vec2(0.0, 0.02), vec2(0.24, 0.34)));
    float outer = 1.0 - smoothstep(aa, 0.06 + aa, sdDiamond(uv + vec2(0.0, 0.01), vec2(0.38, 0.54)));
    float ring = max(outer - core, 0.0);

    float bolt_a = 1.0 - smoothstep(0.035, 0.065, sdSegment(uv, vec2(-0.12, -0.50), vec2(0.10, -0.06)));
    float bolt_b = 1.0 - smoothstep(0.035, 0.065, sdSegment(uv, vec2(0.10, -0.06), vec2(-0.02, -0.02)));
    float bolt_c = 1.0 - smoothstep(0.035, 0.065, sdSegment(uv, vec2(-0.02, -0.02), vec2(0.16, 0.44)));
    float bolt = max(bolt_a, max(bolt_b, bolt_c));

    float wing_left = 1.0 - smoothstep(0.04, 0.07, sdSegment(uv, vec2(-0.44, 0.22), vec2(-0.10, 0.04)));
    float wing_right = 1.0 - smoothstep(0.04, 0.07, sdSegment(uv, vec2(0.44, -0.02), vec2(0.12, -0.14)));
    float flares = max(wing_left, wing_right) * (0.72 + 0.28 * pulse);

    vec3 color = mix(main_color.rgb, accent_color.rgb, clamp((uv.y + 0.7) * 0.9, 0.0, 1.0));
    color = mix(color, glow_color.rgb, bolt * 0.88 + core * 0.46 + flares * 0.35);
    color += ring * 0.16 + pulse * core * 0.08;

    float alpha = ring * 0.54 + core * 0.82 + bolt * 1.0 + flares * 0.46;
    alpha = clamp(alpha, 0.0, 1.0);

    COLOR = vec4(color, alpha);
}
