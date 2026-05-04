shader_type canvas_item;

uniform vec4 wave_color : source_color = vec4(0.18, 0.88, 1.0, 1.0);
uniform vec4 spark_color : source_color = vec4(0.94, 1.0, 0.72, 1.0);
uniform vec4 core_color : source_color = vec4(0.62, 0.96, 1.0, 1.0);
uniform float pulse_speed : hint_range(0.0, 8.0) = 2.6;
uniform float line_width : hint_range(0.005, 0.08) = 0.026;

float sd_segment(vec2 p, vec2 a, vec2 b) {
    vec2 pa = p - a;
    vec2 ba = b - a;
    float h = clamp(dot(pa, ba) / dot(ba, ba), 0.0, 1.0);
    return length(pa - ba * h);
}

float stroke(float d, float width, float aa) {
    return 1.0 - smoothstep(width, width + aa, d);
}

float ring(vec2 p, float radius, float width, float aa) {
    return 1.0 - smoothstep(width, width + aa, abs(length(p) - radius));
}

float wave_arc(vec2 p, float radius, float side, float aa) {
    float a = atan(p.y, p.x);
    float facing = side * cos(a);
    float gate = smoothstep(0.42, 0.78, facing);
    return ring(p, radius, line_width, aa) * gate;
}

void fragment() {
    vec2 uv = (UV - 0.5) * 2.0;
    uv.y += 0.03;
    uv *= 0.92;

    float aa = max(fwidth(uv.x), fwidth(uv.y)) * 1.8;
    float pulse = 0.5 + 0.5 * sin(TIME * pulse_speed);
    float jitter = sin((uv.x - uv.y) * 38.0 + TIME * 12.0) * 0.010;

    vec2 p = uv + vec2(jitter, -jitter * 0.6);
    float body = ring(p, 0.28 + pulse * 0.014, 0.052, aa);
    body += stroke(sd_segment(p, vec2(-0.16, 0.22), vec2(0.16, 0.22)), 0.026, aa);
    body += stroke(sd_segment(p, vec2(-0.10, 0.29), vec2(0.10, 0.29)), 0.022, aa);
    body += stroke(sd_segment(p, vec2(0.0, 0.30), vec2(0.0, 0.48)), 0.024, aa);

    float waves = 0.0;
    waves += wave_arc(p, 0.45 + pulse * 0.018, 1.0, aa) * 0.85;
    waves += wave_arc(p, 0.62 - pulse * 0.014, 1.0, aa) * 0.68;
    waves += wave_arc(p, 0.45 + pulse * 0.018, -1.0, aa) * 0.85;
    waves += wave_arc(p, 0.62 - pulse * 0.014, -1.0, aa) * 0.68;

    float bolt = 0.0;
    bolt += stroke(sd_segment(p, vec2(-0.08, -0.42), vec2(0.05, -0.12)), 0.025, aa);
    bolt += stroke(sd_segment(p, vec2(0.05, -0.12), vec2(-0.03, -0.12)), 0.025, aa);
    bolt += stroke(sd_segment(p, vec2(-0.03, -0.12), vec2(0.11, 0.17)), 0.025, aa);
    bolt *= 0.82 + 0.18 * sin(TIME * 18.0);

    float noise_line = step(0.68, sin((uv.x + uv.y) * 32.0 + TIME * 20.0));
    float interference = waves * noise_line * 0.22;
    float glow = smoothstep(0.95, 0.02, length(uv)) * 0.18;

    vec3 color = wave_color.rgb * (0.70 + 0.20 * pulse);
    color = mix(color, core_color.rgb, clamp(body, 0.0, 1.0) * 0.72);
    color = mix(color, spark_color.rgb, clamp(bolt, 0.0, 1.0) * 0.86);
    color += wave_color.rgb * interference * 0.30;

    float alpha = body * 0.95 + waves * 0.72 + bolt + interference + glow;
    COLOR = vec4(color, clamp(alpha, 0.0, 1.0));
}
