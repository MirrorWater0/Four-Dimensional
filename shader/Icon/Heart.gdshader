shader_type canvas_item;

uniform vec4 heart_color : source_color = vec4(0.98, 0.38, 0.52, 1.0);
uniform vec4 highlight_color : source_color = vec4(1.0, 0.84, 0.90, 1.0);
uniform vec4 glow_color : source_color = vec4(1.0, 0.56, 0.68, 0.28);
uniform float scale : hint_range(0.7, 1.5) = 1.0;
uniform float softness : hint_range(0.8, 2.5) = 1.45;

void fragment() {
    vec2 uv = (UV - vec2(0.5, 0.75)) * 2.0 / max(scale, 0.001);
    uv.y *= -1.1;
    uv.y += 0.1;

    float theta = atan(uv.y, abs(uv.x));
    float r = length(uv);
    float dist = 9999.0;
    float heart_mask = 0.0;

    if (theta > 0.0 && theta < 3.14159265) {
        float t = tan(theta);
        float t_safe = max(abs(t), 0.0001);
        float r_limit = exp(log(t_safe) / t_safe);

        dist = r - r_limit;
        float edge_softness = fwidth(dist) * softness;
        heart_mask = smoothstep(edge_softness, -edge_softness, dist);
    }

    float glow_mask = smoothstep(0.10, -0.14, dist);
    glow_mask *= smoothstep(1.25, 0.18, length(uv));

    float top_highlight = heart_mask * smoothstep(0.45, -0.18, uv.y + uv.x * 0.28);
    float core_highlight = heart_mask * smoothstep(0.54, 0.08, length(uv + vec2(0.0, 0.02)));

    vec3 color = heart_color.rgb;
    color = mix(color, highlight_color.rgb, top_highlight * 0.42 + core_highlight * 0.22);
    color += glow_color.rgb * glow_mask * 0.22;

    float alpha = heart_mask;
    alpha = max(alpha, glow_mask * glow_color.a);
    alpha = clamp(alpha, 0.0, 1.0);

    COLOR *= vec4(color, alpha);
}
