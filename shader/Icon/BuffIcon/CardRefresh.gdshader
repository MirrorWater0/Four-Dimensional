shader_type canvas_item;

uniform vec4 back_card_color : source_color = vec4(0.18, 0.45, 0.62, 0.64);
uniform vec4 front_card_color : source_color = vec4(0.24, 0.76, 0.86, 0.95);
uniform vec4 card_edge_color : source_color = vec4(0.88, 1.0, 0.95, 1.0);
uniform vec4 arrow_color : source_color = vec4(1.0, 0.84, 0.24, 1.0);
uniform vec4 glow_color : source_color = vec4(0.22, 0.88, 1.0, 0.45);
uniform float card_radius : hint_range(0.01, 0.12) = 0.045;
uniform float edge_width : hint_range(0.005, 0.06) = 0.018;
uniform float arrow_radius : hint_range(0.18, 0.48) = 0.36;
uniform float arrow_width : hint_range(0.015, 0.08) = 0.036;

float rounded_box_sdf(vec2 p, vec2 half_size, float radius) {
    vec2 q = abs(p) - half_size + vec2(radius);
    return length(max(q, vec2(0.0))) + min(max(q.x, q.y), 0.0) - radius;
}

float mask_from_sdf(float d) {
    float aa = max(fwidth(d) * 1.5, 0.001);
    return smoothstep(aa, -aa, d);
}

vec2 rotate_point(vec2 p, float angle) {
    float s = sin(angle);
    float c = cos(angle);
    return vec2(c * p.x - s * p.y, s * p.x + c * p.y);
}

float card_mask(vec2 uv, vec2 center, float angle, vec2 half_size) {
    vec2 p = rotate_point(uv - center, -angle);
    return mask_from_sdf(rounded_box_sdf(p, half_size, card_radius));
}

float card_edge_mask(vec2 uv, vec2 center, float angle, vec2 half_size) {
    vec2 p = rotate_point(uv - center, -angle);
    float outer = mask_from_sdf(rounded_box_sdf(p, half_size, card_radius));
    float inner = mask_from_sdf(
        rounded_box_sdf(p, half_size - vec2(edge_width), max(card_radius - edge_width, 0.001))
    );
    return clamp(outer - inner, 0.0, 1.0);
}

float arrow_head_mask(vec2 uv, vec2 center, float angle) {
    vec2 p = rotate_point(uv - center, -angle);
    float body = max(abs(p.y) - (0.095 - p.x * 0.55), max(-p.x - 0.015, p.x - 0.12));
    return mask_from_sdf(body);
}

void blend_layer(inout vec3 rgb, inout float alpha, vec4 color, float layer_alpha) {
    float a = clamp(layer_alpha * color.a, 0.0, 1.0);
    rgb = mix(rgb, color.rgb, a);
    alpha = max(alpha, a);
}

void fragment() {
    vec2 uv = UV - vec2(0.5);

    vec3 rgb = vec3(0.0);
    float alpha = 0.0;

    vec2 card_size = vec2(0.17, 0.235);
    float back_a = card_mask(uv, vec2(-0.085, 0.075), -0.18, card_size);
    float mid_a = card_mask(uv, vec2(0.0, 0.015), -0.08, card_size);
    float front_a = card_mask(uv, vec2(0.085, -0.065), 0.04, card_size);

    blend_layer(rgb, alpha, back_card_color, back_a);
    blend_layer(rgb, alpha, back_card_color * vec4(1.18, 1.18, 1.18, 1.0), mid_a);
    blend_layer(rgb, alpha, front_card_color, front_a);

    float edge = card_edge_mask(uv, vec2(-0.085, 0.075), -0.18, card_size);
    edge = max(edge, card_edge_mask(uv, vec2(0.0, 0.015), -0.08, card_size));
    edge = max(edge, card_edge_mask(uv, vec2(0.085, -0.065), 0.04, card_size));
    blend_layer(rgb, alpha, card_edge_color, edge);

    float r = length(uv);
    float angle = atan(uv.y, uv.x);
    float ring = smoothstep(arrow_width, 0.0, abs(r - arrow_radius));
    float arc = ring * step(-2.55, angle) * step(angle, 1.42);
    float head = arrow_head_mask(uv, vec2(0.06, 0.35), 1.23);
    float arrow = max(arc, head);

    float glow = smoothstep(arrow_width * 3.6, 0.0, abs(r - arrow_radius)) * 0.38;
    glow += (back_a + mid_a + front_a) * 0.1;
    blend_layer(rgb, alpha, glow_color, glow);
    blend_layer(rgb, alpha, arrow_color, arrow);

    COLOR *= vec4(rgb, alpha);
}
