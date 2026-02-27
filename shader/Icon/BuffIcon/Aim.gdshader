shader_type canvas_item;

// --- 颜色和线条基础设置 ---
uniform vec4 target_color : source_color = vec4(0.0, 0.0, 0.0, 1.0);
uniform float line_width : hint_range(0.0, 0.1) = 0.04;

// --- 中心圆环 ---
uniform float inner_ring_radius : hint_range(0.0, 0.5) = 0.18;

// --- 外侧四段圆弧 ---
uniform float outer_arc_radius : hint_range(0.0, 1.0) = 0.65;
uniform float outer_arc_length : hint_range(0.0, 1.57) = 0.35;

// --- 四个角的三角形 ---
uniform float tri_inner_radius : hint_range(0.0, 1.0) = 0.45;
uniform float tri_outer_radius : hint_range(0.0, 1.5) = 0.95;
uniform float tri_width : hint_range(0.0, 2.0) = 0.8;

void fragment() {
    // 1. 坐标系转换：UV映射到 (-1 ~ 1)，中心点为 (0,0)
    vec2 p = (UV - 0.5) * 2.0;
    float r = length(p);

    // 【核心抗锯齿】: fwidth 动态计算当前屏幕上一个像素在 p 坐标系下的宽度。
    // 乘以 1.5 使得边缘过渡更加柔和，无论怎么缩放都能完美适配。
    float aa = fwidth(p.x) * 1.5; 

    // === 以下采用 SDF (有向距离场) 算法，精确计算每个像素到各个图形边缘的最短距离 ===
    // 距离 < 0 表示在图形内部，距离 > 0 表示在外部。

    // --- 形状 A: 中心圆环 ---
    // 距离 = 偏离目标半径的绝对值 - 线宽
    float d_ring = abs(r - inner_ring_radius) - line_width;

    // --- 形状 B: 十字准星 ---
    float d_h = abs(p.y) - line_width;
    float d_v = abs(p.x) - line_width;
    float d_cross = min(d_h, d_v); // 组合横竖线
    
    // 挖空中心区域：如果处于中心圆环内部，强制让距离变为正值（剔除掉）
    float d_center_hole = (inner_ring_radius - line_width) - r;
    d_cross = max(d_cross, d_center_hole);

    // --- 形状 C: 外侧断开的圆弧 ---
    float d_outer_ring = abs(r - outer_arc_radius) - line_width;
    vec2 p_abs = abs(p); // 将坐标折叠到第一象限以简化计算
    float angle = atan(p_abs.y, p_abs.x);
    
    // 计算角度距离，并转化为空间距离 (* r)
    float d_angle1 = angle - outer_arc_length;
    float d_angle2 = (PI / 2.0 - outer_arc_length) - angle;
    float d_angle = min(d_angle1, d_angle2) * r;
    
    float d_arcs = max(d_outer_ring, d_angle); // 取外圆环和角度遮罩的交集

    // --- 形状 D: 四个角的三角形 ---
    // 旋转坐标系，将对角线直接转到 X 轴上
    float c = 0.707106781; // cos(45度)
    float s = -0.707106781; // sin(-45度)
    vec2 p_diag = vec2(p_abs.x * c - p_abs.y * s, p_abs.x * s + p_abs.y * c);

    float d_tri_left = tri_inner_radius - p_diag.x;  // 内侧尖端界限
    float d_tri_right = p_diag.x - tri_outer_radius; // 外侧底边界限
    
    // 计算两侧斜边距离
    float d_tri_y = abs(p_diag.y) - tri_width * (p_diag.x - tri_inner_radius);
    // SDF 数学修正：为了保证斜边抗锯齿均匀，乘以校正系数将垂直 Y 轴的距离修正为真实的法向距离
    d_tri_y *= 1.0 / sqrt(1.0 + tri_width * tri_width);

    // 取三角形三条边的交集
    float d_tri = max(max(d_tri_left, d_tri_right), d_tri_y);

    // === 3. 合并所有形状 ===
    // Min(距离) 代表形状的合并（并集）
    float d_final = min(d_ring, d_cross);
    d_final = min(d_final, d_arcs);
    d_final = min(d_final, d_tri);

    // === 4. 输出最终颜色 ===
    // smoothstep 从正距离(外部 aa 像素) 平滑过渡到 负距离(内部 -aa 像素)
    float alpha = smoothstep(aa, -aa, d_final);
    
    COLOR *= vec4(target_color.rgb, target_color.a * alpha);
}