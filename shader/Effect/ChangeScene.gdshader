shader_type canvas_item;

// 基础参数
uniform float progress : hint_range(0.0, 1.0) = 0.0;
uniform vec2 cell_count = vec2(10.0, 6.0);
uniform float shatter_strength : hint_range(0.0, 1.0) = 0.5;

// 顶点独立随机移动参数
uniform float vertex_move_speed : hint_range(0.0, 5.0) = 1.0;   // 移动快慢
uniform float vertex_move_amount : hint_range(0.0, 0.5) = 0.2; // 移动幅度（越大线条变形越猛）

// 视觉增强
uniform vec4 edge_color : source_color = vec4(1.0, 1.0, 1.0, 1.0);
uniform float edge_thickness : hint_range(0.0, 0.1) = 0.02; 
uniform float edge_softness : hint_range(0.0, 0.1) = 0.01;

uniform sampler2D SCREEN_TEXTURE : hint_screen_texture, filter_linear_mipmap;

// 随机函数
vec2 hash22(vec2 p) {
    p = vec2(dot(p, vec2(127.1, 311.7)), dot(p, vec2(269.5, 183.3)));
    return fract(sin(p) * 43758.5453123);
}

// 获取每个胞元独立的随机移动位置
vec2 get_cell_point(vec2 id) {
    vec2 base_pos = hash22(id); // 原始随机位置
    
    // 为每个点生成独立的随机速度和方向
    vec2 move_rand = hash22(id + vec2(1.23, 4.56)); 
    float time_offset = TIME * vertex_move_speed + (move_rand.x * 6.28);
    
    // 产生平滑的圆周晃动
    vec2 offset = vec2(
        cos(time_offset * move_rand.y), 
        sin(time_offset * (1.0 - move_rand.y))
    ) * vertex_move_amount;
    
    return base_pos + offset;
}

void fragment() {
    float p = progress;
    vec2 uv = SCREEN_UV;
    
    // 1. Voronoi 采样空间
    vec2 v_uv = uv * cell_count;
    vec2 i_uv = floor(v_uv);
    vec2 f_uv = fract(v_uv);
    
    vec2 cell_id;
    vec2 closest_rel_point;
    float min_dist = 8.0;

    // 第一遍：寻找最近的动态特征点
    for (int j = -1; j <= 1; j++) {
        for (int i = -1; i <= 1; i++) {
            vec2 g = vec2(float(i), float(j));
            vec2 p_pos = get_cell_point(i_uv + g);
            vec2 r = g + p_pos - f_uv;
            float d = dot(r, r);
            if (d < min_dist) {
                min_dist = d;
                cell_id = i_uv + g;
                closest_rel_point = g + p_pos;
            }
        }
    }

    // 第二遍：计算动态边缘距离 (确保顶点平滑移动的关键)
    float edge_dist = 8.0;
    for (int j = -2; j <= 2; j++) {
        for (int i = -2; i <= 2; i++) {
            vec2 g = vec2(float(i), float(j));
            vec2 p_pos = get_cell_point(i_uv + g);
            vec2 r = g + p_pos;
            if (length(closest_rel_point - r) > 0.0001) {
                // 计算当前像素到动态生成的平分线的投影距离
                float d = dot(0.5 * (closest_rel_point + r) - f_uv, normalize(r - closest_rel_point));
                edge_dist = min(edge_dist, d);
            }
        }
    }

    // 2. 碎片位移 (基于当前动态位置生成的随机方向)
    vec2 move_seed = hash22(cell_id);
    vec2 explode_dir = normalize(move_seed - 0.5);
    vec2 center_dir = normalize(uv - 0.5);
    
    // 线性位移
    vec2 move_vec = (explode_dir + center_dir * 0.2) * p * shatter_strength;
    vec2 sampled_uv = uv - move_vec;
    
    // 3. 采样与着色
    vec3 col = texture(SCREEN_TEXTURE, sampled_uv).rgb;
    
    // 动态边缘线条
    float edge_mask = smoothstep(0.0, edge_softness, edge_dist);
    float glow = smoothstep(edge_thickness + edge_softness, 0.0, edge_dist);
    
    // 混合
    vec3 final_color = mix(col, edge_color.rgb, glow * edge_color.a);
    
    // Alpha 控制 (解决锯齿虚线的关键还是这里的 edge_mask)
    float alpha = edge_mask;
    alpha *= smoothstep(1.0, 0.8, p); // 进度淡出

    if (sampled_uv.x < 0.0 || sampled_uv.x > 1.0 || sampled_uv.y < 0.0 || sampled_uv.y > 1.0) alpha = 0.0;

    COLOR = vec4(final_color, alpha);
}