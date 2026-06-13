shader_type canvas_item;

uniform sampler2D screen_texture : hint_screen_texture, repeat_disable, filter_nearest;
uniform float exhaust_progress : hint_range(0.0, 1.0) = 0.0;
uniform float edge_width : hint_range(0.01, 0.25) = 0.08;
uniform float noise_scale : hint_range(0.5, 3.0) = 1.25;
uniform float ember_amount : hint_range(0.0, 2.0) = 0.55;
uniform float aberration_amount : hint_range(0.0, 0.08) = 0.006;
uniform sampler2D noise_tex : repeat_enable;
uniform vec2 noise_offset = vec2(0.0, 0.0);
uniform vec4 ember_color : source_color = vec4(0.5, 0.6, 1.0, 1.0);
uniform vec4 ash_color : source_color = vec4(0.10, 0.08, 0.11, 1.0);

varying vec4 modulate;

void vertex() {
    modulate = COLOR;
}

vec4 sample_card(vec2 screen_uv) {
    vec4 color = textureLod(screen_texture, screen_uv, 0.0);
    if (color.a > 0.0001) {
        color.rgb /= color.a;
    }
    return color;
}

void fragment() {
    vec2 uv = UV;
    float p = exhaust_progress;
    vec4 original = sample_card(SCREEN_UV) * modulate;

    vec2 noise_uv = uv + noise_offset;
    vec2 low_uv = noise_uv + vec2(0.13, 0.37);
    vec2 mid_uv = noise_uv + vec2(-0.41, 0.19);
    vec2 high_uv = noise_uv + vec2(0.73, -0.58);
    float low_noise = texture(noise_tex, low_uv).r;
    float mid_noise = texture(noise_tex, mid_uv).r;
    float high_noise = texture(noise_tex, high_uv).r;
    float burn_field = low_noise * 0.74 + mid_noise * 0.21 + high_noise * 0.05;
    burn_field = clamp((burn_field - 0.5) * noise_scale + 0.5, 0.0, 1.0);
    burn_field = smoothstep(0.12, 0.88, burn_field);

    float threshold = p * 1.18 - 0.10;
    float visible_mask = smoothstep(threshold - edge_width, threshold + edge_width, burn_field);
    float edge = 1.0 - smoothstep(0.0, edge_width * 1.8, abs(burn_field - threshold));
    edge *= smoothstep(0.03, 0.16, p);

    float channel_shift = aberration_amount * p * edge;
    vec2 curl = vec2(mid_noise - 0.5, high_noise - 0.5) * 0.018 * p;
    vec2 sample_uv = SCREEN_UV + curl;

    float r = sample_card(sample_uv + vec2(channel_shift, 0.0)).r;
    float g = sample_card(sample_uv).g;
    float b = sample_card(sample_uv - vec2(channel_shift, 0.0)).b;
    float a = sample_card(sample_uv).a;
    vec4 card = vec4(r, g, b, a);

    float spark_noise = texture(noise_tex, uv + noise_offset * 5.0 + vec2(TIME * 0.09, -TIME * 0.14)).r;
    float sparse_sparks = step(0.992, spark_noise);
    sparse_sparks *= edge * p * ember_amount;
    float heat = edge * (1.15 + sparse_sparks);
    float char_band = smoothstep(threshold + edge_width * 0.7, threshold + edge_width * 4.0, burn_field);
    char_band *= 1.0 - smoothstep(threshold + edge_width * 4.0, threshold + edge_width * 9.0, burn_field);
    char_band *= visible_mask;

    vec3 rgb = mix(card.rgb, ash_color.rgb, char_band * 0.52 + p * 0.08);
    rgb += ember_color.rgb * heat * 1.55;
    rgb = mix(rgb, ember_color.rgb * 2.4, sparse_sparks * 0.65);

    float fade_tail = 1.0 - smoothstep(0.76, 1.0, p);
    vec4 burned_color = vec4(rgb, card.a * visible_mask * fade_tail) * modulate;
    COLOR = mix(original, burned_color, smoothstep(0.001, 0.04, p));
}
