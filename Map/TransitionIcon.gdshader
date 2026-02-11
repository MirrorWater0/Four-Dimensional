shader_type canvas_item;

uniform float icon_scale : hint_range(0.1, 1.2) = 0.8;
uniform float base_thickness : hint_range(0.005, 0.05) = 0.012;

// 1. 尖头圆弧函数：通过角度对厚度进行线性缩减，实现末端变尖
float sdSharpArc(vec2 p, float start_angle, float end_angle, float radius, float thickness) {
    float angle = atan(p.x, p.y);
    float d = abs(length(p) - radius);
    
    // 限制角度范围
    float angle_mask = step(start_angle, angle) * step(angle, end_angle);
    
    // 计算末端收缩：越靠近两端，厚度越小
    float center_angle = (start_angle + end_angle) * 0.5;
    float angle_dist = abs(angle - center_angle) / ((end_angle - start_angle) * 0.5);
    float tapered_thickness = thickness * (1.0 - pow(angle_dist, 4.0)); // 使用高次幂保持中间宽度，末端急速变细
    
    return d - tapered_thickness;
}

// 2. 尖刺函数（极细菱形实现）
float sdNeedle(vec2 p, vec2 b) {
    vec2 q = abs(p);
    float h = clamp((-2.0 * q.x + b.x) / b.x, -1.0, 1.0);
    float d = length(q - 0.5 * b * vec2(1.0 - h, 1.0 + h));
    return d * step(b.x, q.x + q.y) * sign(q.x + q.y - b.x);
}

void fragment() {
    vec2 uv = (UV - 0.5) / icon_scale;
    float aa = fwidth(uv.x);
    float final_mask = 0.0;

    // --- A. 顶部尖头大圆弧 ---
    // 范围大约从 -1.3 到 1.3 弧度
    float outer_arc = sdSharpArc(uv + vec2(0.0, 0.05), -1.3, 1.3, 0.4, base_thickness);
    final_mask = max(final_mask, smoothstep(aa, -aa, outer_arc));

    // --- B. 中间圆环 (带微弱的上下开口) ---
    // 为了美观，圆环也做了一点点尖锐处理（或者保持闭合，这里做了闭合处理但边缘锐利）
    float ring_d = abs(length(uv) - 0.18) - base_thickness;
    final_mask = max(final_mask, smoothstep(aa, -aa, ring_d));

    // --- C. 中央长尖刺 (穿透效果) ---
    // 宽度极窄，高度较长
    vec2 spike_uv = uv + vec2(0.0, -0.02);
    float spike = sdNeedle(spike_uv, vec2(0.03, 0.48));
    final_mask = max(final_mask, smoothstep(aa, -aa, spike));

    // --- D. 两侧垂直尖刺 (代替原来的平头腿) ---
    vec2 leg_uv = uv;
    leg_uv.x = abs(leg_uv.x) - 0.12;
    leg_uv.y -= 0.28;
    float legs = sdNeedle(leg_uv, vec2(0.015, 0.15));
    final_mask = max(final_mask, smoothstep(aa, -aa, legs));

    // 最终输出：纯白色
    COLOR = vec4(1.0, 1.0, 1.0, final_mask);
}
