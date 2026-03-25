shader_type canvas_item;

uniform vec4 core_color : source_color = vec4(1.0, 0.34, 0.10, 1.0);
uniform vec4 flame_color : source_color = vec4(1.0, 0.76, 0.14, 1.0);
uniform vec4 spark_color : source_color = vec4(1.0, 0.96, 0.72, 1.0);

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
    uv.y += 0.05;

    float aa = max(fwidth(uv.x), fwidth(uv.y)) * 1.5;

    float angle = atan(uv.y, uv.x);
    float radius = length(uv);
    float pulse = sin(TIME * 4.2 + angle * 5.0) * 0.035;

    float flame = 1.0 - smoothstep(0.52 + pulse, 0.60 + pulse, radius + uv.y * 0.24 - abs(uv.x) * 0.08);
    flame *= 1.0 - smoothstep(-0.68, -0.22, uv.y);

    float slash_a = 1.0 - smoothstep(0.04, 0.07, sdSegment(uv, vec2(-0.46, 0.34), vec2(0.34, -0.38)));
    float slash_b = 1.0 - smoothstep(0.04, 0.07, sdSegment(uv, vec2(-0.26, 0.46), vec2(0.46, -0.10)));
    float slashes = max(slash_a, slash_b);

    float ember = 1.0 - smoothstep(aa, -aa, sdDiamond(uv + vec2(0.0, 0.03), vec2(0.18, 0.24)));
    ember *= 0.85 + 0.15 * sin(TIME * 6.0);

    float spark_ring = smoothstep(0.48, 0.44, radius) * smoothstep(0.28, 0.32, radius);
    spark_ring *= clamp(0.45 + 0.55 * sin(TIME * 3.0 - angle * 6.0), 0.0, 1.0);

    vec3 color = mix(core_color.rgb, flame_color.rgb, clamp(radius * 1.5 + 0.2, 0.0, 1.0));
    color = mix(color, spark_color.rgb, slashes * 0.8 + ember * 0.55 + spark_ring * 0.5);
    color += flame * 0.14;

    float alpha = flame * 0.75 + slashes * 0.82 + ember * 0.9 + spark_ring * 0.35;
    alpha = clamp(alpha, 0.0, 1.0);

    COLOR = vec4(color, alpha);
}
