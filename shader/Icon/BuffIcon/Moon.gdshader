shader_type canvas_item;

// 月亮的颜色
uniform vec4 moon_color : source_color = vec4(1.0, 0.9, 0.5, 1.0);
// 月亮的弯曲程度（偏移量），建议范围 0.1 到 0.3
uniform float offset : hint_range(-0.5, 0.5) = 0.2;
// 月亮的大小
uniform float radius : hint_range(0.1, 0.5) = 0.4;
// 抗锯齿强度，值越小边缘越锐利
uniform float smoothness : hint_range(0.001, 0.1) = 0.01;

void fragment() {
    // 将 UV 坐标中心化到 (-0.5, -0.5) 到 (0.5, 0.5)
    vec2 uv = UV - 0.5;
    
    // 计算第一个圆（月亮主体）
    float d1 = length(uv);
    float circle1 = 1.0 - smoothstep(radius - smoothness, radius, d1);
    
    // 计算第二个圆（用来切掉主体，形成月牙）
    // 通过偏移 offset 来控制月亮的胖瘦
    vec2 offset_uv = uv - vec2(offset, 0.0);
    float d2 = length(offset_uv);
    float circle2 = 1.0 - smoothstep(radius - smoothness, radius, d2);
    
    // 月亮形状 = 圆1 减去 圆2
    float moon_mask = clamp(circle1 - circle2, 0.0, 1.0);
    
    // 输出最终颜色
    COLOR = vec4(moon_color.rgb, moon_mask * moon_color.a);
}
