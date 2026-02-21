shader_type canvas_item;

varying vec4 modulate;
uniform float progress : hint_range(0.0, 1.0) = 0.0;
// 中心消失进度
uniform float center_vanish : hint_range(0.0, 1.0) = 0.0;

uniform float line_density : hint_range(1.0, 200.0) = 80.0;
uniform float line_speed = 8.0;
uniform float aberration_amount : hint_range(0.0, 0.1) = 0.03;
uniform vec4 beam_color : source_color = vec4(0.0, 1.0, 1.0, 1.0);

void vertex(){
	modulate = COLOR;
}

float rand(float x) {
    return fract(sin(x) * 43758.5453123);
}

void fragment() {
    // --- 1. 坐标与中心收缩逻辑 ---
    vec2 center = vec2(0.5, 0.5);
    vec2 uv = UV;
    
    // 向中心收缩的扭曲效果：随着 center_vanish 增加，UV 向中心靠拢
    uv = mix(uv, center, center_vanish * center_vanish); 
    
    float p = progress;
    float cv = center_vanish;
    
    // --- 2. 核心 Alpha 遮罩逻辑 ---
    // 原有的垂直消融
    float mask_p = p * 1.2 - 0.1; 
    float current_height = 1.0 - uv.y;
    float vertical_mask = smoothstep(mask_p, mask_p + 0.1, current_height);
    
    // 【修改点：从圆形改为方形】
    // 计算当前像素到中心点的方形(切比雪夫)距离
    vec2 dist_vec = abs(uv - center);
    float dist_to_center = max(dist_vec.x, dist_vec.y);
    
    // 随着 cv 增加，可见边长从 0.8 缩小到 0
    float radius = 0.8 * (1.0 - cv);
    float center_mask = smoothstep(radius, radius - 0.1, dist_to_center);
    
    // 合并遮罩：两者取交集
    float final_alpha_mask = vertical_mask * center_mask;
    
    
    // --- 3. 动态色散与抖动 ---
    float total_p = max(p, cv); // 取两个进度中较大的一个来驱动抖动
    float shift = 0.0;
    if (total_p > 0.0) {
        float jitter = (rand(floor(uv.y * 100.0) + TIME * 20.0) - 0.5) * total_p * 0.02;
        shift = (aberration_amount * total_p) + jitter;
    }
    
    // 三通道偏移采样
    float r = texture(TEXTURE, uv + vec2(shift, 0.0)).r;
    float g = texture(TEXTURE, uv).g;
    float b = texture(TEXTURE, uv - vec2(shift, 0.0)).b;
    float base_alpha = texture(TEXTURE, uv).a;
    
    vec4 color = vec4(r, g, b, base_alpha);
    
    
    // --- 4. 竖线能量束 ---
    float line_rand = rand(floor(uv.x * line_density));
    float line_input = uv.x * line_density + (TIME * line_speed * (0.5 + line_rand));
    float lines = step(0.95, sin(line_input));
    
    // 线条随任意进度增强
    float line_effect = lines * color.a * total_p;
    
    
    // --- 5. 颜色合成 ---
    // 垂直消融亮边
    float edge_glow = smoothstep(0.15, 0.0, abs(current_height - (mask_p + 0.05))) * p;
    // 中心收缩亮边 (基于方形距离计算)
    float center_glow = smoothstep(0.1, 0.0, abs(dist_to_center - radius)) * cv;
    
    vec3 final_rgb = color.rgb;
    final_rgb = mix(final_rgb, beam_color.rgb * 4.0, line_effect);
    final_rgb += beam_color.rgb * (edge_glow + center_glow) * 2.0;

    // 最终 Alpha：原始透明度 * 综合遮罩
    COLOR = vec4(final_rgb, color.a * final_alpha_mask) * modulate;
}