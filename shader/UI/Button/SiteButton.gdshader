shader_type canvas_item;

uniform vec4 main_color : source_color = vec4(1.0, 1.0, 1.0, 1.0);
uniform float smoothness : hint_range(0.0, 0.02) = 0.005;
uniform float rotation_speed : hint_range(-10.0, 10.0) = 1.0;

// --- 环形参数 ---
uniform float center_dot : hint_range(0.0, 0.1) = 0.03;
uniform float inner_ring_radius : hint_range(0.0, 0.2) = 0.08;
uniform float inner_ring_width : hint_range(0.0, 0.05) = 0.01;
uniform float broken_ring_radius : hint_range(0.1, 0.4) = 0.14;
uniform float broken_ring_width : hint_range(0.0, 0.05) = 0.015;
uniform float broken_ring_gap : hint_range(0.0, 2.0) = 0.8;

// --- 刀锋三角形参数 (现在非常直观) ---
// 想象一个指向右侧的三角形：
// p1 是靠近中心的点, p2 是最远的尖端, p3 是侧边的拐点
uniform vec2 p1 = vec2(0., 0.); // 靠近中心的起点
uniform vec2 p2 = vec2(0.2, 0.);  // 最远的尖端
uniform vec2 p3 = vec2(0.4, 0.2);  // 侧边的撑开点

// 旋转函数
vec2 rotate(vec2 v, float a) {
    float s = sin(a); float c = cos(a);
    return vec2(v.x * c - v.y * s, v.x * s + v.y * c);
}

// 标准三角形 SDF
float sdTriangle(vec2 p, vec2 a, vec2 b, vec2 c) {
    vec2 e0 = b-a, e1 = c-b, e2 = a-c;
    vec2 v0 = p-a, v1 = p-b, v2 = p-c;
    vec2 pq0 = v0 - e0*clamp(dot(v0,e0)/dot(e0,e0), 0.0, 1.0);
    vec2 pq1 = v1 - e1*clamp(dot(v1,e1)/dot(e1,e1), 0.0, 1.0);
    vec2 pq2 = v2 - e2*clamp(dot(v2,e2)/dot(e2,e2), 0.0, 1.0);
    float s = sign(e0.x*e2.y - e0.y*e2.x);
    vec2 d = min(min(vec2(dot(pq0,pq0), s*(v0.x*e0.y-v0.y*e0.x)),
                     vec2(dot(pq1,pq1), s*(v1.x*e1.y-v1.y*e1.x))),
                     vec2(dot(pq2,pq2), s*(v2.x*e2.y-v2.y*e2.x)));
    return -sqrt(d.x)*sign(d.y);
}

void fragment() {
    vec2 uv = UV - 0.5;
    vec4 modulate = COLOR;
    // 1. 全局旋转
    uv = rotate(uv, TIME * rotation_speed);
    
    float dist = length(uv);
    float angle = atan(uv.y, uv.x);
    float mask = 0.0;

    // --- 绘制圆环部分 ---
    // 中心圆点
    mask = max(mask, smoothstep(center_dot, center_dot - smoothness, dist));
    // 内部闭合圆环
    mask = max(mask, smoothstep(inner_ring_width, inner_ring_width - smoothness, abs(dist - inner_ring_radius)));
    // 外部断开圆环
    float ring_shape = smoothstep(broken_ring_width, broken_ring_width - smoothness, abs(dist - broken_ring_radius));
    float ring_mask = smoothstep(-1.0 + broken_ring_gap, -0.9 + broken_ring_gap, cos(angle * 4.0));
    mask = max(mask, ring_shape * ring_mask);

    // --- 绘制 4 个对称三角形 ---
    // 我们手动循环 4 次，每次旋转 90 度，这样你在面板调 p1,p2,p3 就是直观的 XY 偏移
    for (int i = 0; i < 4; i++) {
        vec2 rotated_uv = rotate(uv,1.1 + float(i) * PI / 2.0);
        float tri = sdTriangle(rotated_uv, p1, p2, p3);
        float blade = smoothstep(smoothness, 0.0, tri);
        float offset = 0.3;
        // 这里的 mask 确保三角形不会插到最中心去，你可以注释掉看效果
        float radial_cutoff = smoothstep(inner_ring_radius+0.03, inner_ring_radius+0.03 + smoothness, dist);
        mask = max(mask, blade * radial_cutoff);
    }

    COLOR = vec4(main_color.rgb, mask * main_color.a);
}