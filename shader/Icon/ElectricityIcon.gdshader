shader_type canvas_item;

// --- 参数控制 ---
uniform vec4 icon_color : source_color = vec4(1.0, 0.7, 0.0, 1.0); // 纯色：黄色/橙色
uniform float ring_thickness : hint_range(0.01, 0.1) = 0.03;      // 圆环厚度
uniform float ring_radius : hint_range(0.1, 0.5) = 0.45;         // 圆环半径
uniform float bolt_scale : hint_range(0.1, 1.0) = 0.6;           // 闪电缩放大小
uniform float smoothness : hint_range(0.0, 0.05) = 0.01;         // 边缘平滑度（抗锯齿）

// 计算点是否在三角形内（用于构建闪电）
float sdTriangle(in vec2 p, in vec2 p0, in vec2 p1, in vec2 p2) {
    vec2 e0 = p1-p0, e1 = p2-p1, e2 = p0-p2;
    vec2 v0 = p -p0, v1 = p -p1, v2 = p -p2;
    vec2 pq0 = v0 - e0*clamp( dot(v0,e0)/dot(e0,e0), 0.0, 1.0 );
    vec2 pq1 = v1 - e1*clamp( dot(v1,e1)/dot(e1,e1), 0.0, 1.0 );
    vec2 pq2 = v2 - e2*clamp( dot(v2,e2)/dot(e2,e2), 0.0, 1.0 );
    float s = sign( e0.x*e2.y - e0.y*e2.x );
    vec2 d = min(min(vec2(dot(pq0,pq0), s*(v0.x*e0.y-v0.y*e0.x)),
                     vec2(dot(pq1,pq1), s*(v1.x*e1.y-v1.y*e1.x))),
                     vec2(dot(pq2,pq2), s*(v2.x*e2.y-v2.y*e2.x)));
    return -sqrt(d.x)*sign(d.y);
}

void fragment() {
    // 将坐标中心移到 (0,0)，范围变为 -0.5 到 0.5
    vec2 uv = UV - 0.5;
    
    // --- 1. 绘制圆环 ---
    float dist = length(uv);
    // 使用 abs 计算到圆周的距离，实现圆环效果
    float ring_dist = abs(dist - ring_radius);
    float ring_mask = smoothstep(ring_thickness, ring_thickness - smoothness, ring_dist);
    
    // --- 2. 绘制闪电 (基于缩放后的坐标) ---
    vec2 bv = uv / bolt_scale; 
    // 定义闪电的顶点（基于中心点微调）
    float bolt = 1e20;
    // 上半部分三角形
    bolt = min(bolt, sdTriangle(bv, vec2(0.15, -0.45), vec2(-0.3, 0.05), vec2(0.05, 0.05)));
    // 下半部分三角形
    bolt = min(bolt, sdTriangle(bv, vec2(-0.05, -0.05), vec2(0.3, -0.05), vec2(-0.15, 0.45)));
    
    float bolt_mask = smoothstep(smoothness, 0.0, bolt);
    
    // --- 3. 合并 ---
    float final_mask = max(ring_mask, bolt_mask);
    
    // 输出最终颜色
    COLOR = vec4(icon_color.rgb, final_mask * icon_color.a);
}
