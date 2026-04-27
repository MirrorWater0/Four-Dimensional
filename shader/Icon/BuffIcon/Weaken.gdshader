shader_type canvas_item;

uniform vec4 sword_color : source_color = vec4(0.85, 1.0, 0.85, 1.0);
uniform float break_y : hint_range(-0.5, 0.5) = -0.05;  // 切口高度
uniform vec2 break_offset = vec2(0.08, -0.05);          // 断口偏移
uniform float cut_angle : hint_range(-1.0, 1.0) = 0.35; // 斜切角度
uniform float gap : hint_range(0.0, 0.05) = 0.005;      // 裂缝宽度

// 矩形 SDF
float sdBox(vec2 p, vec2 b) {
    vec2 d = abs(p) - b;
    return length(max(d, 0.0)) + min(max(d.x, d.y), 0.0);
}

// 获取完美比例的完整剑身
float get_sword(vec2 p) {
    // 1. 剑柄与护手 (精确对齐，无缝衔接)
    float hilt = sdBox(p - vec2(0.0, 0.26), vec2(0.03, 0.08));
    float guard = sdBox(p - vec2(0.0, 0.155), vec2(0.12, 0.025));
    
    // 2. 剑刃矩形：【完美修复】
    // 中心上移到 -0.08，半高设为 0.21
    // 这样顶部边缘在 -0.29 (足以包住 -0.25的剑尖)
    // 底部边缘在 0.13 (刚好接住护手，绝不越界！)
    float blade_box = sdBox(p - vec2(0.0, -0.08), vec2(0.06, 0.21));
    
    // 3. 剑尖斜切面
    float tip_cut = (abs(p.x) * 1.6667 - p.y - 0.25) * 0.5145;
    
    // 组合
    float blade = max(blade_box, tip_cut);
    return min(min(hilt, guard), blade);
}

void fragment() {
    vec2 uv = UV - 0.5;
	uv.y += 0.1;
	uv*= 0.7;
    float a = -0.785398; // 旋转 45 度
    float s = sin(a), c = cos(a);
    vec2 p = vec2(uv.x * c - uv.y * s, uv.x * s + uv.y * c);

    // --- 纯数学斜面切割 (0 锯齿) ---
    vec2 cut_normal = normalize(vec2(cut_angle, 1.0));
    float cut_dist = break_y * cut_normal.y;

    // 上半截
    vec2 p_top = p - break_offset;
    float d_top = get_sword(p_top);
    float plane_top = dot(p_top, cut_normal) - cut_dist + gap;
    d_top = max(d_top, plane_top);

    // 下半截
    vec2 p_bottom = p;
    float d_bottom = get_sword(p_bottom);
    float plane_bottom = -(dot(p_bottom, cut_normal) - cut_dist) + gap;
    d_bottom = max(d_bottom, plane_bottom);

    // 合并上下段
    float d = min(d_top, d_bottom);

    // 抗锯齿边缘
    float alpha = smoothstep(0.015, -0.01, d);
    
    vec3 col = sword_color.rgb * (1.1 - uv.y * 0.3);
    COLOR *= vec4(col, alpha);
}
