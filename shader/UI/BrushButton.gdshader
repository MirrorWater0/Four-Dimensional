shader_type canvas_item;

uniform vec4 line_color : source_color = vec4(1.0, 1.0, 1.0, 1.0);
uniform float thickness_px : hint_range(0.0, 30.0) = 2.0;
uniform float bevel_px : hint_range(0.0, 100.0) = 10.0;
uniform float cut_x : hint_range(0.0, 1.0) = 0.6; // 横向长度比例
uniform float cut_y : hint_range(0.0, 1.0) = 0.6; // 纵向长度比例

// 真正的 SDF 抗锯齿：根据像素变化率实现超平滑边缘
float sd_aa(float dist) {
    float af = fwidth(dist);
    return smoothstep(af, -af, dist);
}

void fragment() {
    // 自动获取像素坐标
    vec2 size = 1.0 / fwidth(UV);
    vec2 p = UV * size;
    
    float t = thickness_px;
    float b = bevel_px;
    
    // --- 1. 右上-横线 (Top-Right Horizontal) ---
    // 矩形范围: x从 (size.x * (1-cut_x)) 到 size.x, y从 0 到 t
    float tr_h_rect = max(max((size.x * (1.0 - cut_x)) - p.x, p.x - size.x), max(-p.y, p.y - t));
    // 斜切面: 切掉左端
    float tr_h_bevel = ((size.x * (1.0 - cut_x)) - p.x) + (t - p.y) * (b / t);
    float d_tr_h = max(tr_h_rect, tr_h_bevel);

    // --- 2. 右上-纵线 (Top-Right Vertical) ---
    // 矩形范围: x从 (size.x - t) 到 size.x, y从 0 到 (size.y * cut_y)
    float tr_v_rect = max(max((size.x - t) - p.x, p.x - size.x), max(-p.y, p.y - (size.y * cut_y)));
    // 斜切面: 切掉下端
    float tr_v_bevel = (p.y - (size.y * cut_y)) + (size.x - p.x) * (b / t);
    float d_tr_v = max(tr_v_rect, tr_v_bevel);

    // --- 3. 左下-横线 (Bottom-Left Horizontal) ---
    // 矩形范围: x从 0 到 (size.x * cut_x), y从 (size.y - t) 到 size.y
    float bl_h_rect = max(max(-p.x, p.x - (size.x * cut_x)), max((size.y - t) - p.y, p.y - size.y));
    // 斜切面: 切掉右端
    float bl_h_bevel = (p.x - (size.x * cut_x)) + (p.y - (size.y - t)) * (b / t);
    float d_bl_h = max(bl_h_rect, bl_h_bevel);

    // --- 4. 左下-纵线 (Bottom-Left Vertical) ---
    // 矩形范围: x从 0 到 t, y从 (size.y * (1-cut_y)) 到 size.y
    float bl_v_rect = max(max(-p.x, p.x - t), max((size.y * (1.0 - cut_y)) - p.y, p.y - size.y));
    // 斜切面: 切掉上端
    float bl_v_bevel = ((size.y * (1.0 - cut_y)) - p.y) + p.x * (b / t);
    float d_bl_v = max(bl_v_rect, bl_v_bevel);

    // --- 合并所有线段 ---
    // 在 SDF 中，min 代表两个形状的并集
    float final_dist = min(min(d_tr_h, d_tr_v), min(d_bl_h, d_bl_v));

    // 映射到 Alpha
    float alpha = sd_aa(final_dist);
    
    // 混合颜色
    COLOR = vec4(line_color.rgb, alpha * line_color.a);
}