shader_type canvas_item;

uniform vec4 back_card_color : source_color = vec4(0.10, 0.14, 0.16, 0.76);
uniform vec4 mid_card_color : source_color = vec4(0.25, 0.34, 0.35, 0.70);
uniform vec4 front_card_color : source_color = vec4(0.58, 0.68, 0.66, 0.72);
uniform vec4 edge_color : source_color = vec4(0.84, 0.92, 0.88, 0.72);
uniform vec4 shadow_color : source_color = vec4(0.02, 0.03, 0.04, 0.52);
uniform float hover_amount : hint_range(0.0, 1.0) = 0.0;
uniform float disabled_amount : hint_range(0.0, 1.0) = 0.0;
uniform float pressed_amount : hint_range(0.0, 1.0) = 0.0;
uniform float receive_amount : hint_range(0.0, 1.0) = 0.0;
uniform float stack_tilt : hint_range(-0.5, 0.5) = -0.10;
uniform float fan_amount : hint_range(0.0, 0.35) = 0.12;
uniform float card_radius : hint_range(0.01, 0.12) = 0.045;
uniform float edge_width : hint_range(0.005, 0.06) = 0.018;

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

void blend_layer(inout vec3 rgb, inout float alpha, vec4 color, float layer_alpha) {
    float a = clamp(layer_alpha * color.a, 0.0, 1.0);
    rgb = mix(rgb, color.rgb, a);
    alpha = max(alpha, a);
}

void fragment() {
    vec2 uv = UV - vec2(0.5);
    float receive_wave = sin(receive_amount * 3.1415926);
    float spread = fan_amount + hover_amount * 0.035 + receive_wave * 0.05;
    vec2 card_size = vec2(0.17, 0.235) * (1.0 + hover_amount * 0.035 + receive_wave * 0.08);

    vec2 receive_pull = vec2(0.0, -0.035 * receive_wave);
    vec2 back_pos = vec2(-0.10 - hover_amount * 0.015, 0.080) + receive_pull;
    vec2 mid_pos = vec2(0.0, 0.015) + receive_pull * 0.6;
    vec2 front_pos = vec2(0.10 + hover_amount * 0.015, -0.070) + receive_pull * 0.2;

    float back_angle = stack_tilt - spread;
    float mid_angle = stack_tilt * 0.45;
    float front_angle = stack_tilt + spread;

    float back = card_mask(uv, back_pos, back_angle, card_size);
    float mid = card_mask(uv, mid_pos, mid_angle, card_size);
    float front = card_mask(uv, front_pos, front_angle, card_size);

    vec2 shadow_offset = vec2(0.035, 0.045);
    float shadow = card_mask(uv - shadow_offset, front_pos, front_angle, card_size);
    shadow = max(shadow, card_mask(uv - shadow_offset, mid_pos, mid_angle, card_size));

    vec3 rgb = vec3(0.0);
    float alpha = 0.0;
    blend_layer(rgb, alpha, shadow_color, shadow * (0.78 + hover_amount * 0.08));
    blend_layer(rgb, alpha, back_card_color, back);
    blend_layer(rgb, alpha, mid_card_color, mid);
    blend_layer(rgb, alpha, front_card_color, front);

    float edge = card_edge_mask(uv, back_pos, back_angle, card_size);
    edge = max(edge, card_edge_mask(uv, mid_pos, mid_angle, card_size));
    edge = max(edge, card_edge_mask(uv, front_pos, front_angle, card_size));
    blend_layer(rgb, alpha, edge_color, edge * (0.72 + hover_amount * 0.28));

    float face_glint = smoothstep(0.58, 0.96, uv.y + uv.x * 0.35 + 0.28) * front;
    blend_layer(rgb, alpha, edge_color, face_glint * hover_amount * 0.16);

    float receive_ring_dist = abs(length(uv) - mix(0.16, 0.43, receive_amount));
    float receive_ring = smoothstep(0.030, 0.0, receive_ring_dist) * (1.0 - receive_amount);
    float receive_spark = smoothstep(0.92, 1.0, sin((uv.x - uv.y) * 28.0 + TIME * 16.0));
    blend_layer(rgb, alpha, edge_color, receive_ring * 0.78);
    rgb += edge_color.rgb * receive_spark * receive_wave * 0.16;

    rgb *= 1.0 - pressed_amount * 0.16;
    float gray = dot(rgb, vec3(0.299, 0.587, 0.114));
    rgb = mix(rgb, vec3(gray) * 0.78, disabled_amount * 0.82);
    alpha *= 1.0 - disabled_amount * 0.28;

    COLOR *= vec4(rgb, alpha);
}
