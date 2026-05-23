shader_type canvas_item;

uniform vec4 card_color : source_color = vec4(0.16, 0.58, 0.88, 0.92);
uniform vec4 card_edge_color : source_color = vec4(0.86, 0.98, 1.0, 1.0);
uniform vec4 wind_color : source_color = vec4(0.56, 0.95, 1.0, 0.95);
uniform vec4 spark_color : source_color = vec4(1.0, 0.94, 0.5, 1.0);
uniform vec4 glow_color : source_color = vec4(0.18, 0.78, 1.0, 0.5);
uniform float edge_width : hint_range(0.005, 0.06) = 0.022;
uniform float speed_line_width : hint_range(0.01, 0.08) = 0.035;

float rounded_box_sdf(vec2 p, vec2 half_size, float radius) {
    vec2 q = abs(p) - half_size + vec2(radius);
    return length(max(q, vec2(0.0))) + min(max(q.x, q.y), 0.0) - radius;
}

float mask_from_sdf(float d) {
    float aa = max(fwidth(d) * 1.6, 0.001);
    return smoothstep(aa, -aa, d);
}

vec2 rotate_point(vec2 p, float angle) {
    float s = sin(angle);
    float c = cos(angle);
    return vec2(c * p.x - s * p.y, s * p.x + c * p.y);
}

float card_mask(vec2 uv, vec2 center, float angle, vec2 half_size) {
    vec2 p = rotate_point(uv - center, -angle);
    return mask_from_sdf(rounded_box_sdf(p, half_size, 0.045));
}

float card_edge(vec2 uv, vec2 center, float angle, vec2 half_size) {
    vec2 p = rotate_point(uv - center, -angle);
    float outer = mask_from_sdf(rounded_box_sdf(p, half_size, 0.045));
    float inner = mask_from_sdf(
        rounded_box_sdf(p, half_size - vec2(edge_width), max(0.045 - edge_width, 0.001))
    );
    return clamp(outer - inner, 0.0, 1.0);
}

float line_mask(vec2 uv, vec2 center, float angle, float length_scale) {
    vec2 p = rotate_point(uv - center, -angle);
    float body = smoothstep(speed_line_width, 0.0, abs(p.y));
    float length = smoothstep(0.24 * length_scale, 0.0, abs(p.x));
    float taper = smoothstep(0.24 * length_scale, -0.04, p.x);
    return body * length * taper;
}

float chevron_mask(vec2 uv, vec2 center, float angle) {
    vec2 p = rotate_point(uv - center, -angle);
    float upper = smoothstep(0.035, 0.0, abs(p.y - p.x * 0.72)) * step(0.0, p.x);
    float lower = smoothstep(0.035, 0.0, abs(p.y + p.x * 0.72)) * step(0.0, p.x);
    float cutoff = step(p.x, 0.18) * step(-0.02, p.x);
    return max(upper, lower) * cutoff;
}

void blend_layer(inout vec3 rgb, inout float alpha, vec4 color, float layer_alpha) {
    float a = clamp(layer_alpha * color.a, 0.0, 1.0);
    rgb = mix(rgb, color.rgb, a);
    alpha = max(alpha, a);
}

void fragment() {
    vec2 uv = UV - vec2(0.5);
	uv *= 0.7;
    vec3 rgb = vec3(0.0);
    float alpha = 0.0;

    vec2 card_size = vec2(0.17, 0.235);
    float back_card = card_mask(uv, vec2(-0.025, 0.045), -0.1, card_size);
    float front_card = card_mask(uv, vec2(0.085, -0.065), 0.08, card_size);
    float edge = card_edge(uv, vec2(-0.025, 0.045), -0.1, card_size);
    edge = max(edge, card_edge(uv, vec2(0.085, -0.065), 0.08, card_size));

    float wind = line_mask(uv, vec2(-0.11, -0.17), -0.2, 0.95);
    wind = max(wind, line_mask(uv, vec2(-0.13, 0.0), -0.17, 1.15));
    wind = max(wind, line_mask(uv, vec2(-0.09, 0.17), -0.14, 0.8));
    float chevron = chevron_mask(uv, vec2(0.1, 0.0), -0.16);

    float glow = smoothstep(0.58, 0.0, length(uv)) * 0.22 + wind * 0.28 + chevron * 0.24;
    blend_layer(rgb, alpha, glow_color, glow);
    blend_layer(rgb, alpha, card_color * vec4(0.8, 0.9, 1.05, 0.72), back_card);
    blend_layer(rgb, alpha, card_color, front_card);
    blend_layer(rgb, alpha, card_edge_color, edge);
    blend_layer(rgb, alpha, wind_color, wind);
    blend_layer(rgb, alpha, spark_color, chevron);

    COLOR *= vec4(rgb, alpha);
}
