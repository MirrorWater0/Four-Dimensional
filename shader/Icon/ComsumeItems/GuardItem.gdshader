shader_type canvas_item;

uniform vec4 base_color : source_color = vec4(0.18, 0.78, 1.0, 1.0);
uniform vec4 accent_color : source_color = vec4(0.82, 0.96, 1.0, 1.0);
uniform vec4 glow_color : source_color = vec4(0.20, 0.55, 1.0, 1.0);

float sdHexagon(vec2 p, float r) {
    const vec3 k = vec3(-0.8660254, 0.5, 0.57735027);
    p = abs(p);
    p -= 2.0 * min(dot(k.xy, p), 0.0) * k.xy;
    p -= vec2(clamp(p.x, -k.z * r, k.z * r), r);
    return length(p) * sign(p.y);
}

float sdSegment(vec2 p, vec2 a, vec2 b) {
    vec2 pa = p - a;
    vec2 ba = b - a;
    float h = clamp(dot(pa, ba) / dot(ba, ba), 0.0, 1.0);
    return length(pa - ba * h);
}

void fragment() {
    vec2 uv = UV - 0.5;
    uv.y += 0.02;
    uv *= 2.0;
    uv.x *= 0.92;
    uv.y *= 1.06;

    float aa = max(fwidth(uv.x), fwidth(uv.y)) * 1.5;

    float outer_hex = sdHexagon(uv + vec2(0.0, -0.03), 0.52);
    float inner_hex = sdHexagon(uv + vec2(0.0, -0.03), 0.34);

    float crest = 1.0 - smoothstep(aa, -aa, max(abs(uv.x) - 0.12, abs(uv.y + 0.02) - 0.24));
    float spine = 1.0 - smoothstep(aa, -aa, sdSegment(uv, vec2(0.0, -0.30), vec2(0.0, 0.20)) - 0.05);
    float cross = max(crest, spine);

    float ring = smoothstep(aa, -aa, outer_hex) * (1.0 - smoothstep(aa, -aa, inner_hex));
    float core = smoothstep(aa, -aa, inner_hex);

    float sweep = clamp(0.5 + 0.5 * sin(TIME * 2.8 + uv.y * 14.0), 0.0, 1.0);
    float scan = smoothstep(-0.26, -0.02, uv.y) * (1.0 - smoothstep(-0.02, 0.22, uv.y));
    float shine = scan * sweep;

    vec3 color = base_color.rgb * (0.48 + 0.16 * shine);
    color = mix(color, glow_color.rgb, ring * 0.55);
    color = mix(color, accent_color.rgb, cross * 0.85);
    color += ring * shine * 0.22;

    float alpha = max(ring, core * 0.28 + cross * 0.85);
    alpha += ring * 0.16 + shine * ring * 0.12;
    alpha = clamp(alpha, 0.0, 1.0);

    COLOR = vec4(color, alpha);
}
