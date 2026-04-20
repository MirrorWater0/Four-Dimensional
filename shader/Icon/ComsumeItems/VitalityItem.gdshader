shader_type canvas_item;

uniform vec4 main_color : source_color = vec4(0.20, 0.94, 0.56, 1.0);
uniform vec4 accent_color : source_color = vec4(0.82, 1.0, 0.88, 1.0);
uniform vec4 glow_color : source_color = vec4(0.96, 1.0, 0.84, 1.0);

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
    uv.y += 0.02;

    float aa = max(fwidth(uv.x), fwidth(uv.y)) * 1.4;
    float pulse = 0.5 + 0.5 * sin(TIME * 2.4);

    float frame_outer = sdHexagon(uv, 0.58);
    float frame_inner = sdHexagon(uv, 0.44);
    float core_outer = sdHexagon(uv, 0.28);
    float core_inner = sdHexagon(uv, 0.16);

    float frame = smoothstep(aa, -aa, frame_outer) * (1.0 - smoothstep(aa, -aa, frame_inner));
    float core_ring = smoothstep(aa, -aa, core_outer) * (1.0 - smoothstep(aa, -aa, core_inner));
    float node = smoothstep(aa, -aa, core_inner);

    float plate_left = 1.0 - smoothstep(0.03, 0.055, sdSegment(uv, vec2(-0.34, -0.18), vec2(-0.12, 0.18)));
    float plate_right = 1.0 - smoothstep(0.03, 0.055, sdSegment(uv, vec2(0.34, -0.18), vec2(0.12, 0.18)));
    float plate_bottom = 1.0 - smoothstep(0.03, 0.055, sdSegment(uv, vec2(-0.18, 0.32), vec2(0.18, 0.32)));
    float plates = max(plate_left, max(plate_right, plate_bottom));

    float holo = smoothstep(-0.20, 0.10, uv.y) * (1.0 - smoothstep(0.10, 0.40, uv.y));
    holo *= 0.45 + 0.55 * pulse;

    vec3 color = main_color.rgb * (0.56 + 0.14 * pulse);
    color = mix(color, accent_color.rgb, frame * 0.66 + core_ring * 0.48);
    color = mix(color, glow_color.rgb, node * 0.88 + plates * 0.72);
    color += frame * holo * 0.14;

    float alpha = frame * 0.92 + core_ring * 0.56 + plates * 0.90 + node * 0.72;
    alpha += holo * frame * 0.16;
    alpha = clamp(alpha, 0.0, 1.0);

    COLOR = vec4(color, alpha);
}
