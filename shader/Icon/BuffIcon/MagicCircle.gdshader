shader_type canvas_item;

// --- 控制参数 ---
uniform vec4 magic_color : source_color = vec4(0.3, 0.6, 1.0, 1.0); // 魔法阵颜色
uniform float line_thickness : hint_range(0.005, 0.05) = 0.01;      // 线条粗细
uniform float glow_intensity : hint_range(0.0, 0.2) = 0.05;         // 外层发光强度
uniform float size : hint_range(0.1, 0.9) = 0.8;                   // 整体大小控制

// 绘制圆环的 SDF
float sdCircle(vec2 p, float r) {
    return length(p) - r;
}

// 绘制正三角形的 SDF
float sdTriangle(in vec2 p, in float r) {
    const float k = sqrt(3.0);
    p.x = abs(p.x) - r;
    p.y = p.y + r / k;
    if (p.x + k * p.y > 0.0) p = vec2(p.x - k * p.y, -k * p.x - p.y) / 2.0;
    p.x -= clamp(p.x, -2.0 * r, 0.0);
    return -length(p) * sign(p.y);
}

// 将距离转换为线条
float draw_line(float dist, float thickness, float smoothing) {
    return smoothstep(thickness + smoothing, thickness, abs(dist));
}

void fragment() {
    // 归一化 UV 到 -0.5 ~ 0.5
    vec2 uv = UV - 0.5;
    float s = size * 0.5;
    float smoothing = 0.025; // 抗锯齿平滑度

    // 1. 外圆环
    float ring1 = draw_line(sdCircle(uv, s), line_thickness, smoothing);
    
    // 2. 内圆环
    float ring2 = draw_line(sdCircle(uv, s * 0.95), line_thickness * 1.7, smoothing);
    
    // 3. 正三角形 (向上)
    float tri1 = draw_line(sdTriangle(uv, s * 0.75), line_thickness * 0.8, smoothing);
    
    // 4. 倒三角形 (向下)
    vec2 uv_flipped = vec2(uv.x, -uv.y); // 垂直翻转坐标
    float tri2 = draw_line(sdTriangle(uv_flipped, s * 0.75), line_thickness * 0.8, smoothing);
    
    // 5. 中心小圆点
    float center_dot = smoothstep(line_thickness * 1.5, 0.0, length(uv) - 0.01);

    // 合并所有形状 (取最大值)
    float final_shape = max(max(ring1, ring2), max(tri1, tri2));
    final_shape = max(final_shape, center_dot);

    // 添加简单的外发光效果
    // 计算所有形状到边缘的最近距离 (简化版)
    float d = min(min(abs(sdCircle(uv, s)), abs(sdTriangle(uv, s * 0.75))), abs(sdTriangle(uv_flipped, s * 0.75)));
    float glow = exp(-d * 20.0) * glow_intensity;

    // 最终颜色
    vec3 final_rgb = magic_color.rgb;
    float final_alpha = clamp(final_shape + glow, 0.0, 1.0) * magic_color.a;

    COLOR = vec4(final_rgb, final_alpha);
}
