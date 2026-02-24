shader_type canvas_item;

// --- 颜色和线条基础设置 ---
uniform vec4 arrow_color : source_color = vec4(1.0, 1.0, 1.0, 1.0); // 箭头颜色
uniform float line_width : hint_range(0.01, 0.2) = 0.05;            // 空心尾巴的线条粗细

// --- 箭头形态参数 ---
uniform float head_length : hint_range(0.1, 1.0) = 0.4;             // 箭头三角形的长度
uniform float head_width : hint_range(0.1, 1.0) = 0.35;             // 箭头三角形的半宽
uniform float tail_length : hint_range(0.1, 1.0) = 0.45;            // 尾巴的长度
uniform float tail_width : hint_range(0.05, 0.5) = 0.15;            // 尾巴的半宽

void fragment() {
    // 1. 坐标系转换：UV映射到 (-1 ~ 1)
    vec2 uv = -(UV - 0.5) * 1.0;

    // 为了让整个箭头在视觉上完美居中，我们在 X 轴上做一点偏移
    float center_offset = (tail_length - head_length) / 2.0;
    uv.x += center_offset;

    // 【核心抗锯齿】：动态计算屏幕像素在当前 UV 坐标系下的物理宽度
    float aa = fwidth(uv.x) * 1.5;

    // === 2. 生成实心箭头头部 (三角形) ===
    // 计算斜边的法向量 (指向外侧)
    vec2 head_n = normalize(vec2(-head_width, head_length));
    // 计算当前像素到斜边的距离
    float d_edge = dot(vec2(uv.x + head_length, abs(uv.y)), head_n);
    // 使用 uv.x 截断右侧，形成三角形的底边 (x=0的位置)
    float d_solid_head = max(d_edge, uv.x);

    // === 3. 生成实心尾巴 (矩形) ===
    float outer_y = abs(uv.y) - tail_width;
    float outer_x_right = uv.x - tail_length;
    // 为了防止头部和尾巴拼接处出现 1 像素的缝隙伪影，让实心尾巴稍微往左延伸 0.05 嵌入头部中
    float outer_x_left = -(uv.x + 0.05); 
    float d_solid_tail = max(outer_y, max(outer_x_right, outer_x_left));

    // 将头部和尾巴合并成一个完整的“实心箭头”距离场 (min 代表图形的并集)
    float d_solid_arrow = min(d_solid_head, d_solid_tail);

    // === 4. 生成挖空遮罩 (内部小矩形) ===
    // 这一步决定了哪里会被掏空
    float inner_y = abs(uv.y) - (tail_width - line_width);
    float inner_x_right = uv.x - (tail_length - line_width);
    
    // 💡 提示：-uv.x 让挖空区域刚好停在三角形的底边，这样尾巴的上下两根线条会敞口完美融入实心三角形。
    // 如果你想让空心尾巴和三角形之间有一条垂直的线封口，把它改成 -(uv.x - line_width) 即可。
    float inner_x_left = -uv.x; 
    float d_hole = max(inner_y, max(inner_x_right, inner_x_left));

    // === 5. 核心魔法：从实心箭头中扣除挖空区域 ===
    // SDF 布尔相减：max(形状A, -形状B) 代表从 A 中挖掉 B
    float d_final = max(d_solid_arrow, -d_hole);

    // 6. 抗锯齿输出
    float alpha = smoothstep(aa, -aa, d_final);
    COLOR *= vec4(arrow_color.rgb, arrow_color.a * alpha);
}
