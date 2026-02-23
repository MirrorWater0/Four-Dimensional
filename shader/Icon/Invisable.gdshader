shader_type canvas_item;

// --- 颜色设置 ---
uniform vec4 ring_color : source_color = vec4(1.0, 0.2, 0.2, 1.0); // 圆环颜色 (默认红色)
uniform vec4 bar_color : source_color = vec4(1.0, 0.2, 0.2, 1.0);  // 条形颜色 (默认红色)

// --- 尺寸与宽度参数 ---
uniform float ring_radius : hint_range(0.0, 0.5) = 0.35; // 圆环的半径
uniform float ring_width : hint_range(0.01, 0.2) = 0.05; // 圆环的粗细

uniform float bar_width : hint_range(0.01, 0.2) = 0.05;  // 条形的粗细
uniform float bar_length : hint_range(0.0, 0.6) = 0.4;   // 条形的长度 (从中心向两侧延伸)
uniform float bar_angle : hint_range(-90.0, 90.0) = 45.0;// 条形的旋转角度 (默认45度斜杠)

// --- SDF (符号距离场) ---
// 1. 圆环的距离场
float sdf_ring(vec2 p, float r, float w) {
    return abs(length(p) - r) - w;
}

// 2. 圆角条形 (胶囊) 的距离场
float sdf_capsule(vec2 p, vec2 a, vec2 b, float r) {
    vec2 pa = p - a, ba = b - a;
    float h = clamp(dot(pa, ba) / dot(ba, ba), 0.0, 1.0);
    return length(pa - ba * h) - r;
}

void fragment() {
    // 将 UV 居中，原点 (0,0) 放在中心
    vec2 uv = UV - 0.5;

    // 【完美抗锯齿核心】：动态计算当前屏幕上1个像素的物理跨度
    // 乘以 1.5 让边缘羽化正好覆盖 1~2 个像素，永远不带锯齿
    float aa = fwidth(uv.x) * 1.5;

    // === 第一层：绘制圆环 ===
    float d_ring = sdf_ring(uv, ring_radius, ring_width);
    float ring_mask = smoothstep(aa, -aa, d_ring);
    
    // 初始化最终颜色为圆环颜色
    vec4 final_color = ring_color;
    final_color.a *= ring_mask; // 应用圆环的透明度遮罩

    // === 第二层：绘制条形 ===
    // 将角度转换为弧度，并计算条形的起点和终点
    float angle_rad = radians(bar_angle);
    vec2 dir = vec2(cos(angle_rad), sin(angle_rad));
    vec2 bar_start = -dir * bar_length;
    vec2 bar_end = dir * bar_length;

    // 计算条形的距离场并生成遮罩
    float d_bar = sdf_capsule(uv, bar_start, bar_end, bar_width);
    float bar_mask = smoothstep(aa, -aa, d_bar);

    // === 混合层：条形直接盖在圆环上方 ===
    // 利用标准的 Alpha 混合算法将条形覆盖上去
    // 如果两者颜色一样（比如都是红色），这里会无缝融合成一个整体图形
    final_color.rgb = mix(final_color.rgb, bar_color.rgb, bar_mask * bar_color.a);
    final_color.a = final_color.a + bar_color.a * bar_mask * (1.0 - final_color.a);

    COLOR = final_color;
}