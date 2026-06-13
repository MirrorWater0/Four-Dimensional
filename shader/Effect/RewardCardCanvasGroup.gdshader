shader_type canvas_item;

uniform sampler2D screen_texture : hint_screen_texture, repeat_disable, filter_nearest;
varying vec4 modulate;
uniform float progress : hint_range(0.0, 1.0) = 0.0;
uniform float center_vanish : hint_range(0.0, 1.0) = 0.0;

uniform float line_density : hint_range(1.0, 200.0) = 80.0;
uniform float line_speed = 8.0;
uniform float line_strength : hint_range(0.0, 1.0) = 1.0;
uniform float aberration_amount : hint_range(0.0, 0.1) = 0.03;
uniform vec4 beam_color : source_color = vec4(0.5, 0.7, 1.0, 1.0);

void vertex() {
    modulate = COLOR;
}

float rand(float x) {
    return fract(sin(x) * 43758.5453123);
}

vec4 sample_card(vec2 screen_uv) {
    vec4 color = textureLod(screen_texture, screen_uv, 0.0);
    if (color.a > 0.0001) {
        color.rgb /= color.a;
    }
    return color;
}

void fragment() {
    vec2 center = vec2(0.5, 0.5);
    vec2 uv = UV;
    uv = mix(uv, center, center_vanish * center_vanish);

    float p = progress;
    float cv = center_vanish;

    float mask_p = p * 1.2 - 0.1;
    float current_height = 1.0 - uv.y;
    float vertical_mask = smoothstep(mask_p, mask_p + 0.1, current_height);

    vec2 dist_vec = abs(uv - center);
    float dist_to_center = max(dist_vec.x, dist_vec.y);
    float radius = 0.8 * (1.0 - cv);
    float center_mask = smoothstep(radius, radius - 0.1, dist_to_center);
    float final_alpha_mask = vertical_mask * center_mask;

    float total_p = max(p, cv);
    float shift = 0.0;
    if (total_p > 0.0) {
        float jitter = (rand(floor(uv.y * 100.0) + TIME * 20.0) - 0.5) * total_p * 0.02;
        shift = (aberration_amount * total_p) + jitter;
    }

    float r = sample_card(SCREEN_UV + vec2(shift, 0.0)).r;
    float g = sample_card(SCREEN_UV).g;
    float b = sample_card(SCREEN_UV - vec2(shift, 0.0)).b;
    float base_alpha = sample_card(SCREEN_UV).a;
    vec4 color = vec4(r, g, b, base_alpha);

    float line_rand = rand(floor(uv.x * line_density));
    float line_input = uv.x * line_density + (TIME * line_speed * (0.5 + line_rand));
    float lines = step(0.95, sin(line_input));
    float line_effect = lines * color.a * p * line_strength;

    float edge_glow = smoothstep(0.15, 0.0, abs(current_height - (mask_p + 0.05))) * p;
    float center_glow = smoothstep(0.1, 0.0, abs(dist_to_center - radius)) * cv;

    vec3 final_rgb = color.rgb;
    final_rgb = mix(final_rgb, beam_color.rgb * 4.0, line_effect);
    final_rgb += beam_color.rgb * (edge_glow + center_glow) * 2.0;

    COLOR = vec4(final_rgb, color.a * final_alpha_mask) * modulate;
}
