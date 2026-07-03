shader_type canvas_item;

uniform vec4 shield_color : source_color = vec4(0.42, 0.78, 0.95, 1.0);
uniform vec4 shield_edge : source_color = vec4(0.78, 0.95, 1.0, 1.0);
uniform vec4 card_color : source_color = vec4(0.92, 0.90, 0.84, 1.0);
uniform vec4 ash_color : source_color = vec4(0.55, 0.58, 0.62, 1.0);
uniform vec4 bg_color : source_color = vec4(0.0, 0.0, 0.0, 0.0);
uniform float animation_speed : hint_range(0.0, 4.0) = 1.1;

float rect_mask(vec2 uv, vec2 center, vec2 half_size, float blur) {
    vec2 delta = abs(uv - center) - half_size;
    float dist = length(max(delta, vec2(0.0))) + min(max(delta.x, delta.y), 0.0);
    float aa = max(fwidth(dist), 0.0015);
    return 1.0 - smoothstep(0.0, max(blur, aa), dist);
}

float shield_mask(vec2 uv) {
    vec2 p = uv - vec2(0.5, 0.56);
    float top = 1.0 - smoothstep(0.18, 0.24, abs(p.x) + p.y * 1.35);
    float body = rect_mask(uv, vec2(0.5, 0.62), vec2(0.22, 0.18), 0.02);
    float crest = rect_mask(uv, vec2(0.5, 0.40), vec2(0.08, 0.05), 0.015);
    return clamp(max(top * body, crest), 0.0, 1.0);
}

void fragment() {
    vec2 uv = UV;
    float time = TIME * animation_speed;
    float aa = max(fwidth(uv.x), fwidth(uv.y));

    vec2 stack_offset = vec2(0.028, -0.022);
    float card0 = rect_mask(uv, vec2(0.40, 0.72) + stack_offset * 0.0, vec2(0.11, 0.15), 0.012);
    float card1 = rect_mask(uv, vec2(0.46, 0.69) + stack_offset * 1.0, vec2(0.11, 0.15), 0.012);
    float card2 = rect_mask(uv, vec2(0.52, 0.66) + stack_offset * 2.0, vec2(0.11, 0.15), 0.012);
    float cards = clamp(card0 + card1 + card2, 0.0, 1.0);

    float shield = shield_mask(uv);
    float shield_edge_band = clamp(shield - shield_mask(uv + vec2(0.0, 0.012)) * 0.82, 0.0, 1.0);

    float ash = 0.0;
    for (int i = 0; i < 5; i++) {
        float fi = float(i);
        vec2 seed = vec2(0.18 + fi * 0.14, 0.18 + fi * 0.11);
        vec2 particle_uv = uv - seed;
        particle_uv.y -= fract(time * 0.35 + fi * 0.17) * 0.42;
        particle_uv.x += sin(time * 1.8 + fi * 1.7) * 0.02;
        float particle = exp(-dot(particle_uv, particle_uv) * 180.0);
        ash += particle * 0.55;
    }
    ash *= cards;

    vec3 rgb = bg_color.rgb;
    rgb = mix(rgb, card_color.rgb, cards * 0.85);
    rgb = mix(rgb, shield_color.rgb, shield * 0.92);
    rgb = mix(rgb, shield_edge.rgb, shield_edge_band * 0.75);
    rgb += ash_color.rgb * ash;

    float alpha = clamp(max(max(cards, shield), ash) * 0.95, 0.0, 1.0);
    alpha *= smoothstep(0.52, 0.44, length(uv - 0.5));

    COLOR *= vec4(rgb, alpha);
}
