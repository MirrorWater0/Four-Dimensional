shader_type canvas_item;

// --- 参数控制 ---
uniform vec4 color : source_color = vec4(0.75, 0.65, 0.2, 1.0); // 纯色颜色
uniform int spike_count : hint_range(3, 30) = 16;              // 尖刺数量
uniform float inner_radius : hint_range(0.1, 0.5) = 0.35;      // 内径（圆心到凹处）
uniform float outer_radius : hint_range(0.1, 0.5) = 0.45;      // 外径（圆心到凸处）
uniform float smoothing : hint_range(0.001, 0.05) = 0.005;     // 抗锯齿平滑度

void fragment() {
    // 1. 将 UV 映射到中心
    vec2 uv = UV - 0.5;
    
    // 2. 转换到极坐标
    float angle = atan(uv.y, uv.x); // 弧度: -PI 到 PI
    float dist = length(uv);        // 当前点到中心的距离
    
    // 3. 计算锯齿波 (计算当前角度下，边缘应该在哪个半径上)
    // tau 是 2*PI
    float tau = 6.28318530718;
    
    // f 会在 0 到 1 之间像锯齿一样往复
    float f = abs(fract(angle * float(spike_count) / tau - 0.5) * 2.0 - 1.0);
    
    // 在内径和外径之间根据锯齿波线性插值，得到当前角度的边界半径
    float radius_at_angle = mix(inner_radius, outer_radius, f);
    
    // 4. 绘制纯色遮罩
    // 如果点到中心的距离 < 当前角度的边界半径，则着色
    float mask = smoothstep(radius_at_angle, radius_at_angle - smoothing, dist);
    
    // 5. 输出纯色
    COLOR = vec4(color.rgb, mask * color.a);
}
