shader_type canvas_item;

varying vec4 modulate;
uniform float progress : hint_range(0.0, 1.0) = 0.0;
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
    vec2 uv = UV;
    float p = progress;
    
    // --- 1. 核心修复：Alpha 遮罩逻辑 ---
    // 我们让进度在 -0.1 到 1.1 之间映射，确保 p=0 时完全不触碰 0-1 的范围
    float mask_p = p * 1.2 - 0.1; 
    
    // current_height 从 0(底) 到 1(顶)
    float current_height = 1.0 - uv.y;
    
    // 计算消融遮罩：当 p=0 时，mask_p 为 -0.1，smoothstep 结果恒为 1.0
    float alpha_mask = smoothstep(mask_p, mask_p + 0.1, current_height);
    
    
    // --- 2. 动态色散与抖动 ---
    float shift = 0.0;
    if (p > 0.0) {
        // 只有在传送进行中才加入随机跳变
        float jitter = (rand(floor(uv.y * 100.0) + TIME * 20.0) - 0.5) * p * 0.02;
        shift = (aberration_amount * p) + jitter;
    }
    
    // 三通道偏移采样
    float r = texture(TEXTURE, uv + vec2(shift, 0.0)).r;
    float g = texture(TEXTURE, uv).g;
    float b = texture(TEXTURE, uv - vec2(shift, 0.0)).b;
    float base_alpha = texture(TEXTURE, uv).a;
    
    vec4 color = vec4(r, g, b, base_alpha);
    
    
    // --- 3. 竖线能量束 ---
    // 生成基于 X 轴的随机细竖线
    float line_rand = rand(floor(uv.x * line_density));
    float line_input = uv.x * line_density + (TIME * line_speed * (0.5 + line_rand));
    float lines = step(0.95, sin(line_input));
    
    // 线条只在有物体的地方显示，且随进度增强
    float line_effect = lines * color.a * p;
    
    
    // --- 4. 颜色合成 ---
    // 在传送边缘增加亮边效果
    float edge_glow = smoothstep(0.15, 0.0, abs(current_height - (mask_p + 0.05))) * p;
    
    vec3 final_rgb = color.rgb;
    // 叠加竖线颜色和边缘光
    final_rgb = mix(final_rgb, beam_color.rgb * 4.0, line_effect);
    final_rgb += beam_color.rgb * edge_glow * 2.0;

    // 最终 Alpha：原始透明度 * 消融遮罩
    COLOR = vec4(final_rgb, color.a * alpha_mask)*modulate;
}