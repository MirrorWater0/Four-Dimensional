shader_type canvas_item;

uniform vec4 main_color : source_color = vec4(0.16, 0.78, 1.0, 1.0);
uniform vec4 accent_color : source_color = vec4(0.84, 0.97, 1.0, 1.0);
uniform vec4 glow_color : source_color = vec4(0.24, 0.58, 1.0, 1.0);

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
    vec2 uv = (UV - 0.5) * 2.0;
    uv.y += 0.04;
    uv.x *= 0.92;

    float aa = max(fwidth(uv.x), fwidth(uv.y)) * 1.4;
    float pulse = 0.5 + 0.5 * sin(TIME * 2.6);

    float outer_hex = sdHexagon(uv + vec2(0.0, -0.05), 0.58);
    float mid_hex = sdHexagon(uv + vec2(0.0, -0.03), 0.42);
    float inner_hex = sdHexagon(uv + vec2(0.0, 0.00), 0.22);

    float shell = smoothstep(aa, -aa, outer_hex) * (1.0 - smoothstep(aa, -aa, mid_hex));
    float core = smoothstep(aa, -aa, mid_hex) * (1.0 - smoothstep(aa, -aa, inner_hex));
    float node = smoothstep(aa, -aa, inner_hex);

    float brace_left = 1.0 - smoothstep(0.03, 0.06, sdSegment(uv, vec2(-0.34, -0.06), vec2(-0.06, 0.28)));
    float brace_right = 1.0 - smoothstep(0.03, 0.06, sdSegment(uv, vec2(0.34, -0.06), vec2(0.06, 0.28)));
    float brace_top = 1.0 - smoothstep(0.03, 0.06, sdSegment(uv, vec2(-0.18, -0.26), vec2(0.18, -0.26)));
    float braces = max(brace_top, max(brace_left, brace_right));

    float sweep = smoothstep(-0.42, -0.06, uv.y) * (1.0 - smoothstep(-0.06, 0.24, uv.y));
    sweep *= 0.55 + 0.45 * pulse;

    vec3 color = main_color.rgb * (0.54 + 0.16 * pulse);
    color = mix(color, glow_color.rgb, shell * 0.72 + core * 0.30);
    color = mix(color, accent_color.rgb, braces * 0.85 + node * 0.80);
    color += shell * sweep * 0.16;

    float alpha = shell * 0.95 + core * 0.48 + braces * 0.92 + node * 0.70;
    alpha += shell * sweep * 0.18;
    alpha = clamp(alpha, 0.0, 1.0);

    COLOR = vec4(color, alpha);
}
