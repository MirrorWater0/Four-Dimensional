shader_type canvas_item;
render_mode unshaded;

uniform sampler2D screen_texture : hint_screen_texture, repeat_disable, filter_linear;
uniform vec2 panel_size = vec2(520.0, 78.0);
uniform float prominence : hint_range(0.0, 1.0) = 1.0;
uniform float hover_amount : hint_range(0.0, 1.0) = 0.0;
uniform float press_amount : hint_range(0.0, 1.0) = 0.0;
uniform float disabled_amount : hint_range(0.0, 1.0) = 0.0;
uniform vec2 pointer = vec2(0.5);

float panel_distance(vec2 p, vec2 half_size, float cut) {
    vec2 q = abs(p) - half_size + cut;
    return length(max(q, vec2(0.0))) + min(max(q.x, q.y), 0.0) - cut;
}

void fragment() {
    vec4 tint = COLOR;
    vec2 size = max(panel_size, vec2(1.0));
    vec2 p = (UV - 0.5) * size;
    float cut = mix(5.0, 8.0, prominence);
    float distance_to_edge = panel_distance(p, size * 0.5 - 0.7, cut);
    float aa = max(fwidth(distance_to_edge), 0.65);
    float mask = 1.0 - smoothstep(-aa, aa, distance_to_edge);
    float rim = 1.0 - smoothstep(0.0, 0.35 + aa, -distance_to_edge);
    float bevel = 1.0 - smoothstep(0.5, 3.5, -distance_to_edge);
    float inner_distance = panel_distance(p, size * 0.5 - vec2(2.2, 2.5), max(cut - 1.2, 0.0));
    float inner_rim = 1.0 - smoothstep(0.0, 0.25 + aa, abs(inner_distance));

    // Refraction is confined to the thick lip; the center transmits the button below.
    vec2 normal = normalize(vec2(p.x / size.x, p.y / size.y) + vec2(0.0001));
    vec2 offset = normal * SCREEN_PIXEL_SIZE * bevel * (2.8 - press_amount);
    vec3 transmitted = texture(screen_texture, SCREEN_UV + offset).rgb;
    transmitted.r = texture(screen_texture, SCREEN_UV + offset * 1.35).r;
    transmitted.b = texture(screen_texture, SCREEN_UV + offset * 0.65).b;

    float strength = 1.0 - disabled_amount * 0.76;
    float active = hover_amount * (1.0 - disabled_amount);
    float curve = 1.0 - pow((UV.x - 0.5) * 2.0, 2.0);
    // A stationary reflected softbox has a defined curved edge, not a diffuse veil.
    float reflection_edge = 0.18 + curve * 0.06;
    float reflection_aa = max(fwidth(UV.y), 0.002);
    float cap = 1.0 - smoothstep(reflection_edge - reflection_aa, reflection_edge + reflection_aa, UV.y);
    cap *= mix(1.0, 0.50, clamp(UV.y / reflection_edge, 0.0, 1.0));
    float crown = exp(-UV.y * size.y * 0.9) * (0.55 + curve * 0.45);
    float lower_lip = smoothstep(0.72, 0.98, UV.y);
    float pointer_light = exp(-length((UV - pointer) * vec2(size.x / size.y, 1.0)) * 2.0) * active;

    vec3 pearl = vec3(0.89, 0.91, 0.92);
    vec3 glass_color = vec3(0.39, 0.41, 0.43);
    float body_alpha = (0.004 + cap * (0.095 + prominence * 0.065)
        + crown * 0.12 + pointer_light * 0.009) * strength;
    body_alpha *= 1.0 - press_amount * 0.25;
    vec3 body_color = mix(glass_color, pearl, 0.93);
    vec3 premultiplied = body_color * body_alpha;
    float alpha = body_alpha;

    float refraction_alpha = bevel * (0.12 + prominence * 0.04) * strength;
    premultiplied = transmitted * refraction_alpha + premultiplied * (1.0 - refraction_alpha);
    alpha = refraction_alpha + alpha * (1.0 - refraction_alpha);

    // Bright exterior, dark inset and a luminous lower lip separate the glass layers.
    float groove = exp(-pow((-distance_to_edge - 1.8) / 0.55, 2.0));
    float groove_alpha = groove * 0.15 * strength;
    premultiplied *= 1.0 - groove_alpha;
    alpha = groove_alpha + alpha * (1.0 - groove_alpha);
    float corner_light = exp(-length((UV - vec2(0.045, 0.05)) * vec2(size.x / size.y, 1.0)) * 3.5);
    corner_light += exp(-length((UV - vec2(0.97, 0.96)) * vec2(size.x / size.y, 1.0)) * 4.0);
    float rim_light = 0.12 + prominence * 0.08 + crown * 0.65 + lower_lip * 0.18 + corner_light * 0.28;
    float rim_alpha = rim * min(0.98, rim_light + active * 0.13) * strength;
    premultiplied = pearl * rim_alpha + premultiplied * (1.0 - rim_alpha);
    alpha = rim_alpha + alpha * (1.0 - rim_alpha);
    float lip_alpha = inner_rim * (lower_lip * 0.12 + crown * 0.04) * strength;
    premultiplied = pearl * lip_alpha + premultiplied * (1.0 - lip_alpha);
    alpha = lip_alpha + alpha * (1.0 - lip_alpha);
    COLOR = vec4(premultiplied / max(alpha, 0.0001), alpha * mask) * tint;
}
