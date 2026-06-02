shader_type canvas_item;

uniform vec4 shell_color : source_color = vec4(0.18, 0.82, 1.0, 1.0);
uniform vec4 charge_color : source_color = vec4(0.98, 0.90, 0.36, 1.0);
uniform vec4 glow_color : source_color = vec4(0.20, 0.72, 1.0, 0.42);
uniform float charge_amount : hint_range(0.0, 1.0) = 0.82;
uniform float pulse_speed : hint_range(0.0, 6.0) = 2.2;

float sd_box(vec2 p, vec2 b) {
    vec2 d = abs(p) - b;
    return length(max(d, vec2(0.0))) + min(max(d.x, d.y), 0.0);
}

float sd_segment(vec2 p, vec2 a, vec2 b) {
    vec2 pa = p - a;
    vec2 ba = b - a;
    float h = clamp(dot(pa, ba) / max(dot(ba, ba), 0.0001), 0.0, 1.0);
    return length(pa - ba * h);
}

float aa(float d) {
    return max(fwidth(d) * 1.35, 0.001);
}

float fill(float d) {
    float w = aa(d);
    return smoothstep(w, -w, d);
}

float stroke(float d, float width) {
    float w = aa(d);
    return smoothstep(width + w, width - w, abs(d));
}

float bolt(vec2 p) {
    vec2 a = vec2(-0.06, -0.34);
    vec2 b = vec2(-0.25, 0.08);
    vec2 c = vec2(-0.04, 0.08);
    vec2 d = vec2(-0.15, 0.36);
    vec2 e = vec2(0.27, -0.12);
    vec2 f = vec2(0.06, -0.12);

    float line = stroke(sd_segment(p, a, b), 0.055);
    line = max(line, stroke(sd_segment(p, b, c), 0.055));
    line = max(line, stroke(sd_segment(p, c, d), 0.055));
    line = max(line, stroke(sd_segment(p, d, e), 0.055));
    line = max(line, stroke(sd_segment(p, e, f), 0.055));
    line = max(line, stroke(sd_segment(p, f, a), 0.055));
    return line;
}

void fragment() {
    vec2 uv = (UV - 0.5) * 2.0;
    uv.x *= 0.92;
    float pulse = 0.5 + 0.5 * sin(TIME * pulse_speed);

    float body_sdf = sd_box(uv + vec2(0.04, 0.0), vec2(0.48, 0.66));
    float cap_sdf = sd_box(uv - vec2(0.55, 0.0), vec2(0.10, 0.27));
    float body = fill(body_sdf);
    float cap = fill(cap_sdf);
    float frame = stroke(body_sdf, 0.030);
    frame = max(frame, stroke(cap_sdf, 0.024));

    float charge_limit = mix(-0.36, 0.38, charge_amount);
    float charge_box = fill(sd_box(uv + vec2(0.04, 0.0), vec2(0.36, 0.50)));
    float charge_clip = smoothstep(charge_limit + aa(uv.x), charge_limit - aa(uv.x), uv.x);
    float charge = charge_box * charge_clip;

    float bolt_shape = bolt(uv);
    float glow = smoothstep(1.10, 0.05, length(uv)) * (0.22 + pulse * 0.18);
    float scan = smoothstep(0.035, 0.0, abs(fract(uv.x * 4.0 - TIME * 1.4) - 0.5)) * charge * 0.18;

    vec3 color = shell_color.rgb * 0.72;
    color = mix(color, charge_color.rgb, clamp(charge + bolt_shape, 0.0, 1.0));
    color += glow_color.rgb * (glow + scan);

    float alpha = body * 0.30 + cap * 0.42 + frame * 0.96 + charge * 0.70 + bolt_shape * 0.95;
    alpha += glow * glow_color.a;
    alpha = clamp(alpha, 0.0, 1.0);

    COLOR *= vec4(color, alpha);
}
