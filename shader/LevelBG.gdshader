shader_type canvas_item;

// --- 基础参数 ---
uniform vec2 size = vec2(1000.0, 500.0); 
uniform vec4 base_color : source_color = vec4(0.03, 0.04, 0.06, 1.0);
uniform vec4 accent_color : source_color = vec4(0.08, 0.12, 0.2, 1.0);
uniform vec4 grid_color : source_color = vec4(0.5, 0.6, 1.0, 0.15); // 网格建议调低 Alpha

// --- 网格控制 ---
uniform float grid_spacing : hint_range(10.0, 200.0) = 60.0;
uniform float line_thickness : hint_range(0.1, 2.0) = 1.0; // 建议设为 0.8 - 1.0

// --- 星光控制 ---
uniform float star_speed : hint_range(0.0, 2.0) = 0.5;   
uniform float star_density : hint_range(0.0, 1.0) = 0.97; 
uniform float star_size : hint_range(0.5, 5.0) = 1.2;

// 随机函数
vec2 hash22(vec2 p) {
    p = vec2(dot(p, vec2(127.1, 311.7)), dot(p, vec2(269.5, 183.3)));
    return fract(sin(p) * 43758.5453123);
}

void fragment() {
    // 1. 坐标校正
    vec2 pixel_uv = UV * size;
    vec2 centered_uv = (UV - 0.5) * 2.0;
    
    // 2. 底色
    float d_center = length(centered_uv);
    vec3 background = mix(accent_color.rgb, base_color.rgb, d_center * 0.8);
    
    // 3. 【核心修复】极细动态抗锯齿网格
    // 使用三角函数生成连续网格，比 fract 更不容易产生像素撕裂
    vec2 grid_pos = pixel_uv / grid_spacing;
    vec2 grid_sin = abs(sin(grid_pos * 3.1415926));
    
    // 使用 fwidth 自动捕捉像素变化率，确保线条永远是精细的
    vec2 grid_diff = grid_sin / fwidth(grid_sin);
    float grid_line = 1.0 - smoothstep(0.0, line_thickness, min(grid_diff.x, grid_diff.y));
    
    // 动态效果：流动的亮度呼吸感
    float pulse = sin(TIME * 1.2 + (UV.x + UV.y) * 5.0) * 0.5 + 0.5;
    // 边缘淡出：防止线条撞到容器边缘显得突兀
    float fade = smoothstep(1.2, 0.2, d_center);
    
    vec3 final_grid = grid_color.rgb * grid_line * (grid_color.a * (0.3 + 0.7 * pulse)) * fade;
    vec3 color = background + final_grid;

    // 4. 星光逻辑
    float stars = 0.0;
    float cell_size = 50.0; 
    vec2 current_cell = floor(pixel_uv / cell_size);

    for (int y = -1; y <= 1; y++) {
        for (int x = -1; x <= 1; x++) {
            vec2 cell_id = current_cell + vec2(float(x), float(y));
            vec2 rand = hash22(cell_id);
            if (rand.x > star_density) {
                float phase = rand.y * 6.28;
                vec2 drift = vec2(sin(TIME * star_speed + phase), cos(TIME * star_speed * 1.1 + phase)) * cell_size * 0.2;
                vec2 star_center = (cell_id + 0.5 + (rand - 0.5) * 0.5) * cell_size + drift;
                float d = length(pixel_uv - star_center);
                stars += smoothstep(star_size + 1.0, star_size - 1.0, d);
            }
        }
    }
    color += stars * (grid_color.rgb + 0.5) * fade;

    // 5. 最终输出
    COLOR *= vec4(color * smoothstep(1.8, 0.6, d_center), 1.0);
}