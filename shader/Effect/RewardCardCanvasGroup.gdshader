shader_type canvas_item;

uniform sampler2D screen_texture : hint_screen_texture, repeat_disable, filter_linear;
varying vec4 modulate;
varying vec2 card_position;
uniform float progress : hint_range(0.0, 1.0) = 0.0;
uniform float center_vanish : hint_range(0.0, 1.0) = 0.0;
uniform bool appearing = true;

uniform vec2 card_size = vec2(240.0, 370.0);
uniform int phantom_count : hint_range(2, 8, 1) = 5;
uniform float phantom_rotation : hint_range(0.0, 3.0) = 1.9;
uniform float phantom_opacity : hint_range(0.0, 1.0) = 0.24;
uniform float phantom_outline_width : hint_range(0.5, 5.0) = 1.5;
uniform vec4 phantom_color : source_color = vec4(0.58, 0.78, 1.0, 1.0);
uniform float phantom_blur_px : hint_range(0.0, 12.0) = 4.5;
uniform float phantom_trail_length : hint_range(0.0, 0.35) = 0.18;
uniform float card_focus_blur_px : hint_range(0.0, 12.0) = 4.0;

uniform float line_density : hint_range(1.0, 200.0) = 80.0;
uniform float line_speed = 8.0;
uniform float line_strength : hint_range(0.0, 1.0) = 1.0;
uniform float aberration_amount : hint_range(0.0, 0.1) = 0.03;
uniform vec4 beam_color : source_color = vec4(0.5, 0.7, 1.0, 1.0);

void vertex() {
    modulate = COLOR;
    card_position = VERTEX;
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

float rectangle_distance(vec2 point, vec2 half_size) {
    vec2 distance_to_edge = abs(point) - half_size;
    return length(max(distance_to_edge, vec2(0.0)))
        + min(max(distance_to_edge.x, distance_to_edge.y), 0.0);
}

float soft_minimum(float a, float b) {
    float blend = max(0.04 - abs(a - b), 0.0) / 0.04;
    return min(a, b) - blend * blend * 0.01;
}

vec4 sample_forming_card(vec2 screen_uv, vec2 blur_offset) {
    // Blur premultiplied samples before restoring straight alpha to avoid dark fringes.
    vec4 color = textureLod(screen_texture, screen_uv, 0.0) * 0.25;
    color += textureLod(screen_texture, screen_uv + vec2(blur_offset.x, 0.0), 0.0) * 0.125;
    color += textureLod(screen_texture, screen_uv - vec2(blur_offset.x, 0.0), 0.0) * 0.125;
    color += textureLod(screen_texture, screen_uv + vec2(0.0, blur_offset.y), 0.0) * 0.125;
    color += textureLod(screen_texture, screen_uv - vec2(0.0, blur_offset.y), 0.0) * 0.125;
    color += textureLod(screen_texture, screen_uv + blur_offset, 0.0) * 0.0625;
    color += textureLod(screen_texture, screen_uv - blur_offset, 0.0) * 0.0625;
    color += textureLod(screen_texture, screen_uv + vec2(blur_offset.x, -blur_offset.y), 0.0) * 0.0625;
    color += textureLod(screen_texture, screen_uv + vec2(-blur_offset.x, blur_offset.y), 0.0) * 0.0625;
    if (color.a > 0.0001) {
        color.rgb /= color.a;
    }
    return color;
}

vec3 phantom_shape(vec2 point, vec2 half_size, float time, float layer, float blur) {
    // Monotonic cubic settling: a fast spin followed by a soft landing, without reversals.
    float spread = pow(1.0 - time, 3.0);
    float direction = mod(layer, 2.0) * 2.0 - 1.0;
    float angle = direction * (0.55 + layer * 0.33) * phantom_rotation * spread;
    float c = cos(angle);
    float s = sin(angle);
    vec2 offset = vec2(cos(layer * 2.4), sin(layer * 2.4)) * 12.0 * spread;

    // Reserve space for soft edges inside the existing 32 px CanvasGroup margin.
    vec2 axis_weight = sqrt(vec2(c * c, s * s) + vec2(0.0001));
    vec2 rotated_extent = vec2(
        axis_weight.x * half_size.x + axis_weight.y * half_size.y,
        axis_weight.y * half_size.x + axis_weight.x * half_size.y
    );
    vec2 available_extent = half_size + vec2(10.0) - abs(offset);
    vec2 fit_ratio = available_extent / rotated_extent;
    float fit_scale = soft_minimum(fit_ratio.x, fit_ratio.y);
    float scale = soft_minimum(mix(0.56, 1.0, 1.0 - spread), fit_scale);
    vec2 relative_point = point - offset;
    vec2 rotated_point = vec2(
        c * relative_point.x + s * relative_point.y,
        -s * relative_point.x + c * relative_point.y
    );
    float distance_to_edge = rectangle_distance(rotated_point, half_size * scale);
    float feather = max(fwidth(distance_to_edge), 0.6) + blur;
    float fill = 1.0 - smoothstep(-feather, feather, distance_to_edge);
    float outline_distance = distance_to_edge / (phantom_outline_width + feather * 0.7);
    float outline = exp(-0.5 * outline_distance * outline_distance);
    float glow = exp(-abs(distance_to_edge) / (2.0 + feather));
    return vec3(fill, outline, glow);
}

vec4 form_card(vec4 card, float time) {
    vec2 half_size = max(card_size * 0.5, vec2(1.0));
    vec2 point = card_position - half_size;
    vec3 phantom_rgb = vec3(0.0);
    float phantom_alpha = 0.0;
    float visibility = smoothstep(0.0, 0.045, time)
        * (1.0 - smoothstep(0.42, 0.94, time));
    float motion = pow(1.0 - time, 2.0);

    for (int i = 0; i < 8; i++) {
        if (i >= phantom_count) {
            break;
        }

        float layer = float(i);
        vec3 shape = vec3(0.0);
        float total_weight = 0.0;
        // Integrate six past poses along the rotation arc, with softer distant echoes.
        for (int j = 0; j < 6; j++) {
            float trail = float(j) / 5.0;
            float past_time = max(time - trail * phantom_trail_length, 0.0);
            float weight = exp(-trail * 2.0);
            float blur = phantom_blur_px * motion * (0.5 + trail);
            shape += phantom_shape(point, half_size, past_time, layer, blur)
                * weight * smoothstep(0.0, 0.025, past_time);
            total_weight += weight;
        }
        shape /= total_weight;
        float alpha = clamp((shape.x * phantom_opacity + shape.y * 0.48 + shape.z * 0.1)
            * visibility * phantom_color.a, 0.0, 1.0);
        vec3 tint = phantom_color.rgb * (0.75 + layer * 0.06)
            + vec3(shape.y * 0.35 + shape.z * 0.1);

        // Accumulate premultiplied color so transparent fills overlap cleanly.
        phantom_rgb = tint * alpha + phantom_rgb * (1.0 - alpha);
        phantom_alpha = alpha + phantom_alpha * (1.0 - alpha);
    }

    float card_alpha = card.a * smoothstep(0.18, 0.82, time);
    float alpha = card_alpha + phantom_alpha * (1.0 - card_alpha);
    vec3 rgb = card.rgb * card_alpha + phantom_rgb * (1.0 - card_alpha);
    return vec4(rgb / max(alpha, 0.0001), alpha);
}

void fragment() {
    float p = clamp(progress, 0.0, 1.0);
    if (appearing && p > 0.0 && center_vanish <= 0.0) {
        // Recover linear time from the incoming cubic-out tween for blur and fading.
        float time = 1.0 - pow(p, 1.0 / 3.0);
        float focus_blur = card_focus_blur_px * (1.0 - smoothstep(0.18, 0.82, time));
        vec4 card = sample_forming_card(SCREEN_UV, SCREEN_PIXEL_SIZE * focus_blur);
        COLOR = form_card(card, time) * modulate;
    } else {
        vec2 center = vec2(0.5, 0.5);
        vec2 uv = UV;
        uv = mix(uv, center, center_vanish * center_vanish);

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
}
