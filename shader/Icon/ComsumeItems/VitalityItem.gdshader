shader_type canvas_item;

uniform vec4 heart_color : source_color = vec4(0.18, 0.96, 0.54, 1.0);
uniform vec4 ring_color : source_color = vec4(0.76, 1.0, 0.86, 1.0);
uniform vec4 core_color : source_color = vec4(1.0, 1.0, 0.86, 1.0);

float heartField(vec2 p) {
    p.x *= 1.05;
    p.y += 0.14;
    float x = p.x;
    float y = p.y;
    float a = x * x + y * y - 0.28;
    return a * a * a - x * x * y * y * y;
}

void fragment() {
    vec2 uv = (UV - 0.5) * 2.0;
    float aa = max(fwidth(uv.x), fwidth(uv.y)) * 1.8;

    float heart = 1.0 - smoothstep(-0.01, 0.02 + aa, heartField(uv));
    float core = smoothstep(0.20, 0.0, length(uv + vec2(0.0, 0.08)));

    vec2 ring_uv = uv;
    ring_uv.x *= 0.88;
    float ring_outer = smoothstep(0.66, 0.58, length(ring_uv));
    float ring_inner = smoothstep(0.45, 0.52, length(ring_uv));
    float ring = ring_outer * ring_inner;

    float pulse = 0.84 + 0.16 * sin(TIME * 3.6);
    float spark = smoothstep(0.03, 0.0, abs(uv.x) - 0.02) * smoothstep(0.22, 0.04, abs(uv.y + 0.34));
    spark += smoothstep(0.03, 0.0, abs(uv.y + 0.34) - 0.02) * smoothstep(0.18, 0.03, abs(uv.x));
    spark = clamp(spark, 0.0, 1.0);

    vec3 color = mix(heart_color.rgb, core_color.rgb, core * 0.55);
    color = mix(color, ring_color.rgb, ring * 0.8 + spark * 0.8);
    color += heart * 0.08 * pulse;

    float alpha = heart * pulse + ring * 0.58 + spark * 0.7;
    alpha = clamp(alpha, 0.0, 1.0);

    COLOR = vec4(color, alpha);
}
