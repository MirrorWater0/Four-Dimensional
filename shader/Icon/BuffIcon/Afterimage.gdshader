shader_type canvas_item;

uniform vec4 bg_color : source_color = vec4(0.0, 0.0, 0.0, 0.0);
uniform vec4 core_color : source_color = vec4(0.56, 0.82, 1.0, 1.0);
uniform vec4 ghost_color : source_color = vec4(0.28, 0.6, 1.0, 1.0);
uniform vec4 edge_color : source_color = vec4(0.92, 0.98, 1.0, 1.0);
uniform vec4 glow_color : source_color = vec4(0.45, 0.78, 1.0, 0.72);
uniform float shape_radius : hint_range(0.10, 0.30) = 0.19;
uniform float trail_offset : hint_range(0.04, 0.20) = 0.12;
uniform float shear : hint_range(-0.35, 0.35) = 0.18;
uniform float edge_width : hint_range(0.0, 0.08) = 0.035;
uniform float highlight_width : hint_range(0.02, 0.18) = 0.09;

float diamond_mask(vec2 uv, vec2 center, float radius, float skew) {
    vec2 p = uv - center;
    p.x += p.y * skew;
    float dist = (abs(p.x) + abs(p.y) * 1.12) / max(radius, 0.001);
    float aa = fwidth(dist);
    return smoothstep(1.0 + aa, 1.0 - aa, dist);
}

void add_layer(
    vec2 uv,
    vec2 center,
    float radius,
    float skew,
    float alpha_scale,
    inout vec3 rgb,
    inout float alpha,
    inout float edge_accum,
    inout float glow_accum
) {
    float outer = diamond_mask(uv, center, radius, skew);
    float inner = diamond_mask(uv, center, max(radius - edge_width, 0.001), skew);
    float edge = clamp(outer - inner, 0.0, 1.0);

    vec2 p = uv - center;
    p.x += p.y * skew;
    float highlight_line = abs((p.x - p.y * 0.28) + radius * 0.18);
    float highlight = smoothstep(highlight_width, 0.0, highlight_line) * inner;

    vec3 layer_rgb = mix(ghost_color.rgb, core_color.rgb, inner);
    layer_rgb = mix(layer_rgb, edge_color.rgb, clamp(edge + highlight * 0.85, 0.0, 1.0));

    float layer_alpha = outer * alpha_scale;
    rgb = mix(rgb, layer_rgb, layer_alpha);
    alpha = max(alpha, layer_alpha);
    edge_accum = max(edge_accum, edge + highlight * 0.6);
    glow_accum = max(
        glow_accum,
        diamond_mask(uv, center, radius + edge_width * 2.2, skew) * alpha_scale
    );
}

void fragment() {
    vec2 uv = UV;

    vec3 rgb = vec3(0.0);
    float alpha = 0.0;
    float edge_accum = 0.0;
    float glow_accum = 0.0;

    add_layer(
        uv,
        vec2(0.34, 0.66),
        shape_radius * 0.94,
        shear,
        0.34,
        rgb,
        alpha,
        edge_accum,
        glow_accum
    );
    add_layer(
        uv,
        vec2(0.5, 0.5),
        shape_radius,
        shear,
        0.62,
        rgb,
        alpha,
        edge_accum,
        glow_accum
    );
    add_layer(
        uv,
        vec2(0.5 + trail_offset, 0.5 - trail_offset),
        shape_radius * 1.02,
        shear,
        1.0,
        rgb,
        alpha,
        edge_accum,
        glow_accum
    );

    vec2 sweep_uv = uv - vec2(0.5, 0.5);
    float sweep = smoothstep(0.18, 0.0, abs(sweep_uv.x + sweep_uv.y * 0.72 - 0.02));
    float glow = glow_accum * (0.42 + sweep * 0.3);

    vec3 final_rgb = rgb + glow_color.rgb * glow * glow_color.a;
    float final_alpha = clamp(alpha + glow * 0.32 + edge_accum * 0.08, 0.0, 1.0);

    COLOR = mix(bg_color, vec4(final_rgb, final_alpha), final_alpha);
}
