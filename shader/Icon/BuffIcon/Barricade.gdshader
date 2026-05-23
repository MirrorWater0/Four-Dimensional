shader_type canvas_item;

uniform vec4 bg_color : source_color = vec4(0.0, 0.0, 0.0, 0.0);
uniform vec4 fill_color : source_color = vec4(0.36, 0.78, 0.86, 1.0);
uniform vec4 edge_color : source_color = vec4(0.70, 0.95, 0.98, 1.0);
uniform vec4 shadow_color : source_color = vec4(0.06, 0.20, 0.24, 0.55);
uniform float card_size : hint_range(0.40, 0.95) = 0.70;
uniform float edge_width : hint_range(0.0, 0.08) = 0.025;
uniform float offset_step : hint_range(0.02, 0.20) = 0.12;
uniform float corner_radius : hint_range(0.0, 0.10) = 0.035;

float rounded_rect_mask(vec2 uv, vec2 center, vec2 half_size, float radius) {
    vec2 q = abs(uv - center) - half_size + vec2(radius);
    float distance = length(max(q, vec2(0.0))) + min(max(q.x, q.y), 0.0) - radius;
    float aa = max(fwidth(distance), 0.001);
    return smoothstep(aa, -aa, distance);
}

float brick_mask(vec2 uv, vec2 center, vec2 half_size) {
    return rounded_rect_mask(uv, center, half_size, corner_radius);
}

void fragment() {
    vec2 uv = UV;

    vec2 brick_half = vec2(card_size * 0.36, card_size * 0.20);
    vec2 lower_center = vec2(0.36, 0.64);
    vec2 upper_center = lower_center + vec2(offset_step * 1.38, -offset_step * 1.55);
    vec2 shadow_offset = vec2(0.035, 0.045);

    float lower = brick_mask(uv, lower_center, brick_half);
    float upper = brick_mask(uv, upper_center, brick_half);
    float mask = max(lower, upper);

    float lower_shadow = brick_mask(uv, lower_center + shadow_offset, brick_half);
    float upper_shadow = brick_mask(uv, upper_center + shadow_offset, brick_half);
    float shadow = clamp(max(lower_shadow, upper_shadow) - mask, 0.0, 1.0);

    vec2 inner_half = max(brick_half - vec2(edge_width), vec2(0.001));
    float lower_inner = brick_mask(uv, lower_center, inner_half);
    float upper_inner = brick_mask(uv, upper_center, inner_half);
    float inner = max(lower_inner, upper_inner);
    float edge = clamp(mask - inner, 0.0, 1.0);

    float highlight = clamp(1.0 - uv.y, 0.0, 1.0) * 0.22;
    vec3 fill = fill_color.rgb + vec3(highlight);
    vec3 rgb = mix(fill, edge_color.rgb, edge);

    vec4 shadow_col = shadow_color * shadow;
    vec4 brick_col = vec4(rgb, mask * fill_color.a);
    vec4 composed = mix(bg_color, shadow_col, shadow_col.a);
    COLOR *= mix(composed, brick_col, brick_col.a);
}
