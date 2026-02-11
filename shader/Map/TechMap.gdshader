shader_type canvas_item;

// 颜色定义
uniform vec4 water_color : source_color = vec4(0.05, 0.08, 0.1, 1.0);
uniform vec4 land_color : source_color = vec4(0.1, 0.2, 0.15, 1.0);
uniform vec4 line_color : source_color = vec4(0.4, 0.8, 0.7, 0.3);
uniform vec4 red_zone_color : source_color = vec4(0.3, 0.05, 0.05, 1.0);

// 控制参数
uniform float brightness : hint_range(0.0, 2.0) = 1.0;
uniform float contrast : hint_range(0.0, 2.0) = 1.1;
uniform float contour_frequency : hint_range(1.0, 100.0) = 40.0;
uniform float line_threshold : hint_range(0.0, 1.0) = 0.95;

// 噪点函数
float hash(vec2 p) {
    return fract(sin(dot(p, vec2(127.1, 311.7))) * 43758.5453123);
}

void fragment() {
    // 1. 获取原图颜色并转为灰度
    vec4 tex = texture(TEXTURE, UV);
    float gray = dot(tex.rgb, vec3(0.299, 0.587, 0.114));
    
    // 2. 增强对比度，区分陆地和海洋
    float mask = smoothstep(0.4, 0.6, gray);
    
    // 3. 基础配色方案（鸣潮深冷色调）
    vec3 base_style = mix(water_color.rgb, land_color.rgb, mask);
    
    // 4. 生成等高线效果
    // 利用灰阶的周期函数产生线条
    float contour = sin(gray * contour_frequency);
    contour = smoothstep(line_threshold, 1.0, contour);
    vec3 with_lines = mix(base_style, line_color.rgb, contour * 0.5);
    
    // 5. 模拟红区（基于原图亮度的一个特定区间，或者你可以根据坐标传入变量）
    // 这里简单演示：在亮度中等偏下的陆地部分加入红色浸染
    float red_mask = smoothstep(0.5, 0.3, gray) * mask; 
    vec3 final_color = mix(with_lines, red_zone_color.rgb, red_mask * 0.6);
    
    // 6. 添加科技感网格
    vec2 grid_uv = UV * 50.0;
    float grid = (step(0.98, fract(grid_uv.x)) + step(0.98, fract(grid_uv.y))) * 0.1;
    final_color += grid * line_color.rgb;
    
    // 7. 扫描线效果
    float scanline = sin(UV.y * 400.0 + TIME * 2.0) * 0.02;
    final_color += scanline;

    // 8. 边缘发光 (简单的锐化叠加)
    float edge = fwidth(gray);
    final_color += edge * line_color.rgb * 2.0;

    // 亮度对比度调整
    final_color = (final_color - 0.5) * contrast + 0.5 + (brightness - 1.0);

    COLOR = vec4(final_color, tex.a);
}
