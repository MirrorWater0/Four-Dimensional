shader_type canvas_item;

// --- 基础颜色与粗细 ---
uniform vec4 color : source_color = vec4(0.0, 0.0, 0.0, 1.0);
uniform float line_width : hint_range(0.01, 0.5) = 0.11;      

// --- 螺旋线形态参数 ---
uniform float spacing : hint_range(0.05, 0.5) = 0.28;         
uniform float turns : hint_range(0.5, 10.0) = 2.8;            
uniform bool clockwise = true;                                

// --- 末端变尖 (粗细过渡) 参数 ---
uniform float tail_taper_turns : hint_range(0.0, 2.0) = 0.8;  
uniform float head_taper_turns : hint_range(0.0, 2.0) = 0.0;  

// --- 动画与角度调整 ---
uniform float rotation_offset : hint_range(0.0, 6.283) = 0.0; 
uniform float spin_speed : hint_range(-10.0, 10.0) = 0.0;     


void fragment() {
    // 1. 坐标系映射与抗锯齿
    vec2 p = (UV - 0.5) * 2.0;
    if (clockwise) {
        p.x = -p.x;
    }
    float aa = fwidth(p.x) * 1.5;

    // 2. 整体旋转
    float current_rotation = rotation_offset + TIME * spin_speed;
    float c = cos(current_rotation);
    float s = sin(current_rotation);
    p = vec2(p.x * c - p.y * s, p.x * s + p.y * c);

    // 3. 极坐标转换
    float r = length(p);
    float a = atan(p.y, p.x); 
    float b = spacing / TAU;  

    float start_angle = 0.0;
    float end_angle = turns * TAU;
    
    float tail_taper_rad = tail_taper_turns * TAU;
    float head_taper_rad = head_taper_turns * TAU;

    // 4. 动态宽度 SDF 核心算法
    float n_base = round(r / spacing - a / TAU);
    float min_d = 1e20; 
    
    for (float i = -1.0; i <= 1.0; i += 1.0) {
        float n = n_base + i;
        float a_c = a + n * TAU; 
        
        float d_center;                 
        float local_width = line_width; 
        
        // --- 计算粗细渐变 ---
        float eval_angle = clamp(a_c, start_angle, end_angle);
        
        if (tail_taper_rad > 0.0 && eval_angle > end_angle - tail_taper_rad) {
            local_width *= smoothstep(0.0, 1.0, (end_angle - eval_angle) / tail_taper_rad);
        }
        if (head_taper_rad > 0.0 && eval_angle < start_angle + head_taper_rad) {
            local_width *= smoothstep(0.0, 1.0, (eval_angle - start_angle) / head_taper_rad);
        }

        // --- 计算到骨架的距离 ---
        if (a_c <= start_angle) {
            vec2 p_start = vec2(cos(start_angle), sin(start_angle)) * (b * start_angle);
            d_center = length(p - p_start);
        } 
        else if (a_c >= end_angle) {
            vec2 p_end = vec2(cos(end_angle), sin(end_angle)) * (b * end_angle);
            d_center = length(p - p_end);
        } 
        else {
            float r_c = b * a_c;
            
            // 🌟 修复中心横杠 Bug 的关键代码：
            // 使用当前半径和曲线半径中的最大值来计算法向修正，
            // 彻底避免靠近原点时由于修正系数归零导致的伪影射线。
            float max_r = max(r, r_c);
            max_r = max(max_r, 0.001); // 兜底防止除以0
            float correction = max_r / sqrt(b * b + max_r * max_r);
            
            d_center = abs(r - r_c) * correction;
        }
        
        min_d = min(min_d, d_center - local_width);
    }

    // 5. 抗锯齿输出
    float alpha = smoothstep(aa, -aa, min_d);
    COLOR *= vec4(color.rgb, color.a * alpha);
}
