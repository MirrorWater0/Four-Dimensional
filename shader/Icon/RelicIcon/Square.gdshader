shader_type canvas_item;

// --- 参数控制 ---
uniform vec4 line_color : source_color = vec4(1.0, 1.0, 1.0, 1.0); // 线条颜色
uniform float size : hint_range(0.1, 1.0) = 0.6;                  // 正方形大小
uniform float thickness : hint_range(0.005, 0.2) = 0.02;         // 线条粗细
uniform float smoothing : hint_range(0.001, 0.1) = 0.01;         // 抗锯齿平滑度
uniform float roundness : hint_range(0.0, 0.5) = 0.0;            // 圆角程度 (可选)

// 计算矩形的 SDF 函数
float sdBox(vec2 p, vec2 b, float r) {
    vec2 q = abs(p) - b + r;
    return length(max(q, 0.0)) + min(max(q.x, q.y), 0.0) - r;
}

void fragment() {
    // 将 UV 坐标映射到 -0.5 到 0.5，并居中
    vec2 uv = UV - 0.5;
    
    // 1. 计算到正方形边缘的距离
    // size * 0.5 是半径，roundness 控制圆角
    float dist = sdBox(uv, vec2(size * 0.5), roundness);
    
    // 2. 取绝对值实现“空心”化
    // abs(dist) 会得到点到边界的距离，减去厚度得到线框范围
    float hollow_dist = abs(dist) - thickness;
    
    // 3. 使用 smoothstep 渲染出平滑线条
    float alpha = smoothstep(smoothing, 0.0, hollow_dist);
    
    // 输出最终颜色
    COLOR *= vec4(line_color.rgb, line_color.a * alpha);
}
