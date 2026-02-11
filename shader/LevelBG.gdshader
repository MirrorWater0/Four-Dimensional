shader_type canvas_item;

// --- 基础参数 ---
uniform vec2 size = vec2(1000.0, 500.0); 
uniform vec4 base_color : source_color = vec4(0.03, 0.04, 0.06, 1.0);
uniform vec4 accent_color : source_color = vec4(0.08, 0.12, 0.2, 1.0);
uniform vec4 grid_color : source_color = vec4(0.5, 0.6, 1.0, 0.15);

// --- 网格控制 ---
uniform float grid_spacing : hint_range(10.0, 200.0) = 60.0;
uniform float line_thickness : hint_range(0.1, 2.0) = 1.0; 

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
    // 1. 【核心修复】计算正确的宽高比，防止拉伸
    // 我们以高度为基准，计算宽度比例
    float aspect = size.x / size.y;
    vec2 aspect_uv = UV;
    aspect_uv.x *= aspect; 
    
    // 使用像素单位的坐标进行计算，但要基于修正后的比例
    vec2 pixel_uv = aspect_uv * size.y; 
    
    // 2. 底色 (使用原始 UV 处理径向渐变，确保覆盖全屏)
    vec2 centered_uv = (UV - 0.5) * 2.0;
    float d_center = length(centered_uv);
    vec3 background = mix(accent_color.rgb, base_color.rgb, d_center * 0.8);
    
    // 3. 【核心修复】更稳定的抗锯齿网格 (解决抖动)
    // 使用 fract 配合稳定的 smoothstep 抗锯齿，不再依赖 sin
    vec2 grid_uv = pixel_uv / grid_spacing;
    vec2 grid_dist = abs(fract(grid_uv - 0.5) - 0.5);
    // 使用 fwidth 获取屏幕空间的变化率，实现像素级精确抗锯齿
    vec2 unit_per_pixel = fwidth(grid_uv);
    vec2 grid_line_vec = smoothstep(unit_per_pixel * (line_thickness + 0.5), unit_per_pixel * (line_thickness - 0.5), grid_dist);
    float grid_line = max(grid_line_vec.x, grid_line_vec.y);
    
    // 动态效果：流动的亮度呼吸感
    float pulse = sin(TIME * 1.2 + (UV.x + UV.y) * 5.0) * 0.5 + 0.5;
    float fade = smoothstep(1.5, 0.2, d_center); // 边缘淡出
    
    vec3 final_grid = grid_color.rgb * grid_line * (grid_color.a * (0.3 + 0.7 * pulse)) * fade;
    vec3 color = background + final_grid;

    // 4. 【核心修复】星光逻辑 (无视 UV 拉伸的正圆)
    float stars = 0.0;
    float cell_size = 50.0; 
    vec2 current_cell = floor(pixel_uv / cell_size);

    for (int y = -1; y <= 1; y++) {
        for (int x = -1; x <= 1; x++) {
            vec2 cell_id = current_cell + vec2(float(x), float(y));
            vec2 rand = hash22(cell_id);
            
            if (rand.x > star_density) {
                float phase = rand.y * 6.28;
                // 让星星微弱漂移
                vec2 drift = vec2(sin(TIME * star_speed + phase), cos(TIME * star_speed * 1.1 + phase)) * cell_size * 0.2;
                vec2 star_pos = (cell_id + 0.5 + (rand - 0.5) * 0.5) * cell_size + drift;
                
                // 关键点：在修正后的 pixel_uv 空间计算距离，确保是正圆
                float d = length(pixel_uv - star_pos);
                // 增加一层抗锯齿
                stars += smoothstep(star_size + 0.5, star_size - 0.5, d);
            }
        }
    }
    color += stars * (grid_color.rgb + 0.5) * fade;

    // 5. 最终输出
    COLOR = vec4(color * smoothstep(1.8, 0.6, d_center), 1.0);
}