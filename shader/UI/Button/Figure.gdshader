shader_type canvas_item;

// 可以在 Godot 右侧检查器中直接调整的参数
uniform vec4 shape_color : source_color = vec4(0.0, 0.8, 1.0, 1.0); // 形状颜色 (默认赛博蓝)
uniform float speed1 = 1.0;            // 内侧正方形旋转速度
uniform float speed2 = -1.5;           // 外侧正方形旋转速度 (负数代表反向)
uniform float square_size1 = 0.2;      // 内侧正方形大小
uniform float square_size2 = 0.28;     // 外侧正方形大小
uniform float ring_radius = 0.12;      // 中心圆环半径
uniform float line_thickness = 0.01;   // 线条粗细

// 2D 旋转矩阵函数
mat2 rotate(float angle) {
    float s = sin(angle);
    float c = cos(angle);
    return mat2(vec2(c, -s), vec2(s, c));
}

// 绘制空心正方形 (SDF 距离场算法)
float box_outline(vec2 p, vec2 size, float thickness) {
    vec2 d = abs(p) - size;
    float dist = length(max(d, 0.0)) + min(max(d.x, d.y), 0.0);
    return abs(dist) - thickness;
}

// 绘制空心圆环 (SDF 距离场算法)
float circle_outline(vec2 p, float radius, float thickness) {
    return abs(length(p) - radius) - thickness;
}

void fragment() {
    // 将 UV 坐标的中心点从左上角 (0,0) 移动到中心 (0.5, 0.5)
    // 这样坐标范围就变成了 -0.5 到 0.5
    vec2 uv = UV - 0.5;

    // 根据时间计算两个正方形的旋转 UV
    vec2 uv_sq1 = uv * rotate(TIME * speed1);
    vec2 uv_sq2 = uv * rotate(TIME * speed2);

    // 获取三个形状的距离场数值
    float d_sq1 = box_outline(uv_sq1, vec2(square_size1), line_thickness);
    float d_sq2 = box_outline(uv_sq2, vec2(square_size2), line_thickness);
    float d_ring = circle_outline(uv, ring_radius, line_thickness);

    // 使用 smoothstep 实现平滑抗锯齿边缘
    float blur = 0.005; // 边缘模糊度，越小越锐利
    float mask1 = smoothstep(blur, 0.0, d_sq1);
    float mask2 = smoothstep(blur, 0.0, d_sq2);
    float mask3 = smoothstep(blur, 0.0, d_ring);

    // 将三个形状的遮罩合并起来
    float final_mask = max(max(mask1, mask2), mask3);

    // 输出最终颜色，利用遮罩控制透明度
    COLOR = shape_color;
    COLOR.a *= final_mask;
}
