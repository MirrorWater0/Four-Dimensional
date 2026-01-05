shader_type canvas_item;
render_mode blend_add; 

uniform vec4 line_color : source_color = vec4(1.0, 1.0, 1.0, 1.0);
uniform vec4 glow_color : source_color = vec4(0.1, 0.4, 1.0, 1.0); // 降低基础饱和度
uniform float progress : hint_range(0.0, 1.0) = 0.5;
uniform float base_thickness : hint_range(0.0, 0.01) = 0.001; 
uniform float max_thickness : hint_range(0.0, 0.1) = 0.025;    
uniform float glow_intensity : hint_range(0.0, 100.0) = 30.0; 

void fragment() {
    vec2 p = UV - vec2(0.5);
    float dist = length(p);
    float angle = atan(p.y, p.x);
    float pixel_size = fwidth(dist);

    // 1. 更加极端的粗细变化系数
    // 使用 high power (pow 6.0) 让高亮区域变得非常窄且集中
    float t_mod = pow(sin(angle * 2.0 + TIME * 3.0) * 0.5 + 0.5, 6.0);
    t_mod += pow(cos(angle * 4.0 - TIME * 1.5) * 0.5 + 0.5, 8.0);
    t_mod = clamp(t_mod, 0.0, 1.0);
    
    // 2. 计算圆环
    float current_radius = progress * 0.5;
    float ring_dist = abs(dist - current_radius);
    
    // 3. 分层逻辑优化
    
    // A. 基础微弱细线 (平时几乎看不见)
    float base_line = smoothstep(base_thickness, base_thickness - pixel_size * 1.5, ring_dist);
    
    // B. 高亮爆燃区 (宽度随 t_mod 变化)
    float active_thickness = base_thickness + max_thickness * t_mod;
    float active_line = smoothstep(active_thickness, active_thickness - pixel_size * 2.0, ring_dist);
    
    // C. 辉光层 (只有在 t_mod 高的地方才大幅扩散)
    float glow_range = active_thickness * 6.0 * t_mod; 
    float glow_mask = smoothstep(glow_range, 0.0, ring_dist);

    // 4. 颜色与亮度合成 (重点在这里！)
    vec3 final_rgb = vec3(0.0);
    
    // 非发光区：仅保留极低亮度的基准线
    final_rgb += line_color.rgb * base_line * 0.5; 
    
    // 发光区：只有 t_mod 触发时才加入强光
    // 增加一个阈值判断，t_mod 小于一定程度时不增加额外亮度
    float intensity_factor = t_mod * glow_intensity;
    
    // 核心高亮：白芯 + 蓝晕
    final_rgb += glow_color.rgb * glow_mask * intensity_factor * 0.5;
    final_rgb += glow_color.rgb * active_line * intensity_factor;
    final_rgb += line_color.rgb * active_line * intensity_factor * 0.5;

    // 5. 消失逻辑
    float fade = clamp(1.0 - pow(progress, 4.0), 0.0, 1.0);
    
    // 透明度也受 t_mod 影响，不发光的地方更透明
    float alpha = (base_line * 0.2 + active_line + glow_mask * 0.3) * fade;

    COLOR = vec4(final_rgb, alpha);
}