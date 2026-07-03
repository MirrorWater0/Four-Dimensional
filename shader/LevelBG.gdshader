shader_type canvas_item;

// --- 基础参数 ---
uniform vec2 size = vec2(1000.0, 500.0); 
uniform vec4 base_color : source_color = vec4(0.03, 0.04, 0.06, 1.0);
uniform vec4 accent_color : source_color = vec4(0.08, 0.12, 0.2, 1.0);
uniform vec4 grid_color : source_color = vec4(0.5, 0.6, 1.0, 0.15);

// --- 兼容旧场景参数：现在用于星云尺度和暗尘强度 ---
uniform float grid_spacing : hint_range(10.0, 200.0) = 60.0;
uniform float line_thickness : hint_range(0.1, 2.0) = 1.0; 

// --- 星光控制 ---
uniform float star_speed : hint_range(0.0, 2.0) = 0.5;   
uniform float star_density : hint_range(0.0, 1.0) = 0.97; 
uniform float star_size : hint_range(0.5, 5.0) = 1.2;
uniform sampler2D noise_tex : repeat_enable, filter_linear_mipmap;

vec2 hash22(vec2 p) {
    p = vec2(dot(p, vec2(127.1, 311.7)), dot(p, vec2(269.5, 183.3)));
    return fract(sin(p) * 43758.5453123);
}

float noise_sample(vec2 uv) {
    return texture(noise_tex, uv).r;
}

float texture_fbm(vec2 uv) {
    float value = noise_sample(uv) * 0.56;
    value += noise_sample(uv * 2.03 + vec2(0.31, 0.17)) * 0.27;
    value += noise_sample(uv * 4.11 + vec2(0.67, 0.43)) * 0.12;
    value += noise_sample(uv * 8.07 + vec2(0.11, 0.79)) * 0.05;
    return value;
}

float star_layer(vec2 pixel_uv, float cell_size, float threshold, float radius_scale, float speed) {
    float stars = 0.0;
    vec2 current_cell = floor(pixel_uv / cell_size);

    for (int y = -1; y <= 1; y++) {
        for (int x = -1; x <= 1; x++) {
            vec2 cell_id = current_cell + vec2(float(x), float(y));
            vec2 rand = hash22(cell_id);

            if (rand.x > threshold) {
                float phase = rand.y * 6.28;
                float twinkle = 0.55 + 0.45 * sin(TIME * speed + phase);
                vec2 star_pos = (cell_id + 0.5 + (rand - 0.5) * 0.72) * cell_size;
                float d = length(pixel_uv - star_pos);
                float r = (0.55 + rand.x * 1.35) * star_size * radius_scale;
                float core = smoothstep(r + 0.8, 0.0, d);
                float glow = 0.015 / max(d / max(r, 0.1), 0.35);
                stars += (core + glow * 0.12) * twinkle;
            }
        }
    }

    return stars;
}

void fragment() {
    float aspect = size.x / size.y;
    vec2 space_uv = vec2(UV.x * aspect, UV.y);
    vec2 centered = vec2((UV.x - 0.5) * aspect, UV.y - 0.5);
    vec2 pixel_uv = space_uv * size.y;
    float d_center = length(centered * 1.55);

    // Stellaris 星图底色：中心略亮，边缘深空暗角。
    float vignette = smoothstep(1.28, 0.22, d_center);
    vec3 color = mix(base_color.rgb * 0.42, accent_color.rgb * 0.62, vignette);

    // 大尺度星云和暗尘，替代原来的科技网格。
    float nebula_scale = mix(0.55, 1.45, clamp(grid_spacing / 200.0, 0.0, 1.0));
    float n1 = texture_fbm(space_uv * nebula_scale * 0.22 + vec2(-0.08, TIME * 0.004));
    float n2 = texture_fbm(space_uv * nebula_scale * 0.48 + vec2(0.64, -TIME * 0.006));
    float nebula = smoothstep(0.48, 0.88, n1 + n2 * 0.38);
    float dust = smoothstep(0.52, 0.84, texture_fbm(space_uv * 0.72 + vec2(0.27, 0.41)));
    vec3 nebula_color = mix(accent_color.rgb, grid_color.rgb, 0.38);
    color += nebula_color * nebula * grid_color.a * 0.62 * vignette;
    color *= 1.0 - dust * line_thickness * 0.075;

    // 稀疏多层星场：小星点为主，少量亮星。
    float stars =
        star_layer(pixel_uv, 42.0, clamp(star_density + 0.035, 0.0, 0.995), 0.42, star_speed * 2.2)
        + star_layer(pixel_uv + vec2(117.0, 53.0), 92.0, clamp(star_density + 0.015, 0.0, 0.995), 0.78, star_speed * 1.35)
        + star_layer(pixel_uv + vec2(331.0, 211.0), 180.0, clamp(star_density - 0.02, 0.0, 0.985), 1.25, star_speed);
    vec3 star_tint = mix(vec3(0.72, 0.86, 1.0), vec3(1.0, 0.93, 0.78), noise_sample(space_uv * 0.9 + vec2(0.2)));
    color += stars * star_tint * (0.35 + grid_color.a * 0.9);

    // 很淡的银河尘带，给星图纵深但不抢节点。
    float band = exp(-pow(abs(centered.y + sin(centered.x * 2.2) * 0.07), 2.0) * 18.0);
    band *= smoothstep(0.25, 1.0, texture_fbm(space_uv * 0.28 + vec2(0.9)));
    color += nebula_color * band * 0.08;

    COLOR = vec4(color, 1.0);
}