shader_type canvas_item;

uniform sampler2D avatar_tex;
uniform vec4 theme_color : source_color = vec4(0.2, 0.7, 1.0, 1.0);
uniform float u_rotation_time;
uniform float hover_progress : hint_range(0.0, 1.1) = 0.0;
uniform float global_thickness : hint_range(0.1, 3.0) = 1.0;

// --- 基础参数 ---
uniform float bg_ring_radius : hint_range(0.4, 1.0) = 0.74;
uniform float bg_ring_opacity : hint_range(0.0, 1.0) = 0.2;


// --- 【新增】圆弧长度控制参数 ---
// 0.1 是极短的点，0.9 是接近半圆的长弧
uniform float heavy_arc_len : hint_range(0.05, 0.95) = 0.35;


float sdTriangle(in vec2 p, in float r) {
    const float k = sqrt(3.0);
    p.x = abs(p.x) - r;
    p.y = p.y + r / k;
    if (p.x + k * p.y > 0.0) p = vec2(p.x - k * p.y, -k * p.x - p.y) / 2.0;
    p.x -= clamp(p.x, -2.0 * r, 0.0);
    return -length(p) * sign(p.y);
}

void fragment() {
	vec4 modulate = COLOR;
    float margin_scale = 2.7; // 调大余量，保证对称弧弹出不被切
    vec2 p = (UV - vec2(0.5)) * margin_scale;
    float dist = length(p);
    float angle = atan(p.y, p.x);
    float pixel_size = fwidth(dist);

    vec4 final_output = vec4(0.0);

    // --- 1. 头像层 (0.72) ---
    float avatar_r = 0.64;
    float avatar_mask = smoothstep(avatar_r, avatar_r - pixel_size, dist);
    vec2 avatar_uv = p / margin_scale + vec2(0.5);
    vec4 tex = texture(avatar_tex, avatar_uv);
    final_output = mix(final_output, tex, avatar_mask);

    // --- 2. 背景黑环 (底色) ---
    float bg_ring_w = 0.035 * global_thickness;
    float bg_ring_mask = smoothstep(bg_ring_w, bg_ring_w - pixel_size, abs(dist - bg_ring_radius));
    final_output = mix(final_output, vec4(0.0, 0.0, 0.0, bg_ring_opacity), bg_ring_mask);

    // --- 相对半径计算 ---
    float dash_r = bg_ring_radius - 0.025;
    float ball_r = dash_r - 0.045;

    // --- 3. 虚线圆周 (画在黑环上方) ---
    float dash_width = 0.007 * global_thickness;
    float dash_circle_mask = smoothstep(dash_width, dash_width - pixel_size, abs(dist - dash_r));
    float dash_pattern = step(0.5, fract(angle * 10.0 + u_rotation_time * 0.2));
    final_output = mix(final_output, vec4(theme_color.rgb, 0.8), dash_circle_mask * dash_pattern);

    // --- 4. 内轨道白点 ---
    float ball_count = 24.0;
    float ball_angle = u_rotation_time * 0.4;
    float ball_sector = round((angle - ball_angle) * ball_count / (2.0 * PI)) / ball_count * 2.0 * PI;
    vec2 ball_p = vec2(cos(ball_sector + ball_angle), sin(ball_sector + ball_angle)) * ball_r;
    float ball_size = mix(0.012, 0.016, hover_progress) * global_thickness;
    float balls_mask = smoothstep(ball_size, ball_size - pixel_size, length(p - ball_p));
    final_output = mix(final_output, vec4(1.0, 1.0, 1.0, 1.0), balls_mask);

    // --- 5. 三角形 (基于黑环轨道) ---
    float tri_dist = mix(bg_ring_radius, bg_ring_radius + 0.22, hover_progress);
    float tri_size = mix(0.028, 0.05,0.5 + hover_progress) ;
    float tri_angle = -u_rotation_time * 0.8;
    float tri_sec = round((angle - tri_angle) * 3.0 / (2.0 * PI)) / 3.0 * 2.0 * PI;
    vec2 tri_pos = p - vec2(cos(tri_sec + tri_angle), sin(tri_sec + tri_angle)) * tri_dist;
    float sa = sin(tri_sec + tri_angle + PI/2.0); float ca = cos(tri_sec + tri_angle + PI/2.0);
    tri_pos = vec2(tri_pos.x * ca - tri_pos.y * sa, tri_pos.x * sa + tri_pos.y * ca);
    float tri_mask = smoothstep(0.0, -pixel_size, sdTriangle(tri_pos, tri_size));
    final_output = mix(final_output, vec4(1.0), tri_mask);

    // --- 6. 【重点修改：对称且可调长短的粗圆弧】 ---
    float heavy_r = mix(bg_ring_radius + 0.08, bg_ring_radius + 0.35, hover_progress);
    // 使用 abs(cos) 实现 180 度对称
    float arc_val = abs(cos(angle - u_rotation_time * 0.8));
    // 阈值计算：heavy_arc_len 越大，阈值越小，圆弧越长
    // hover 时圆弧会额外变长一点点
    float threshold = (1.0 - heavy_arc_len) * mix(1.0, 0.7, hover_progress);
    float heavy_arc_mask = smoothstep(threshold, threshold + 0.01, arc_val);

    float heavy_width = 0.022 * global_thickness;
    float heavy_line = smoothstep(heavy_width, heavy_width - pixel_size, abs(dist - heavy_r));
    // 使用白色加深主题色混合，使其显眼
    final_output = mix(final_output, vec4(1.0), heavy_line * heavy_arc_mask);

    // --- 7. 最外圈对称细半圆环 (弹出) ---
    float outer_r = mix(bg_ring_radius + 0.18, bg_ring_radius + 0.52, hover_progress);
    float outer_width = 0.015 * global_thickness;
    float outer_line = smoothstep(outer_width, outer_width - pixel_size, abs(dist - outer_r));
    float symmetry_mask = step(0.4, abs(sin(angle * 1.0 + u_rotation_time * 0.6)));
    final_output = mix(final_output, vec4(1.0, 1.0, 1.0, 0.8), outer_line * symmetry_mask);

    // --- 8. 全局发光 ---
    float glow_range = 0.012 * global_thickness;
    float glow = exp(-pow(dist - bg_ring_radius, 2.0) / glow_range);
    final_output.rgb += theme_color.rgb * glow * mix(0.1, 0.4, hover_progress);

    COLOR = final_output*modulate;
}