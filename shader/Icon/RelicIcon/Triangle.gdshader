shader_type canvas_item;

// --- 参数控制 ---
uniform vec4 line_color : source_color = vec4(1.0, 1.0, 1.0, 1.0); // 线条颜色
uniform float size : hint_range(0.1, 1.0) = 0.5;                  // 三角形大小
uniform float thickness : hint_range(0.01, 0.2) = 0.02;          // 线条粗细
uniform float smoothing : hint_range(0.001, 0.1) = 0.01;         // 抗锯齿平滑度

// 计算正三角形的 SDF 函数
float sdEquilateralTriangle(in vec2 p, in float r) {
    const float k = sqrt(3.0);
    p.x = abs(p.x) - r;
    p.y = p.y + r / k;
    if (p.x + k * p.y > 0.0) p = vec2(p.x - k * p.y, -k * p.x - p.y) / 2.0;
    p.x -= clamp(p.x, -2.0 * r, 0.0);
    return -length(p) * sign(p.y);
}

void fragment() {
    // 将 UV 坐标映射到 -0.5 到 0.5 之间，并居中
    vec2 uv = UV - 0.5;
    
    // 如果纹理不是正方形，可以传入屏幕比例进行修正
    // uv.y *= -1.0; // 如果三角形颠倒了，取消注释这一行

    // 1. 获取到三角形边缘的距离
    float dist = sdEquilateralTriangle(uv, size * 0.5);
    
    // 2. 取绝对值实现“空心”效果
    // abs(dist) 会得到离边界的距离，减去厚度来控制空心范围
    float hollow_dist = abs(dist) - thickness;
    
    // 3. 使用 smoothstep 进行抗锯齿渲染
    float alpha = smoothstep(smoothing, 0.0, hollow_dist);
    
    // 输出颜色
    COLOR *= vec4(line_color.rgb, line_color.a * alpha);
}
