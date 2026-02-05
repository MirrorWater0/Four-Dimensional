shader_type canvas_item;

uniform vec4 glow_color : source_color = vec4(0.0, 0.3, 1.0, 1.0); // 你想要的深蓝色
uniform float glow_intensity : hint_range(0.0, 20.0) = 5.0;       // 发光强度
uniform float glow_radius : hint_range(0.0, 20.0) = 10.0;        // 扩散范围
uniform float color_boost : hint_range(1.0, 10.0) = 3.0;         // 颜色浓郁度
uniform float exposure_bias : hint_range(0.0, 1.0) = 0.5;        // 控制中心发白程度（越低越蓝）

void fragment() {
    vec4 tex_color = texture(TEXTURE, UV);
    vec2 pixel_size = TEXTURE_PIXEL_SIZE;
    
    vec3 sampled_color = vec3(0.0);
    float alpha_mask = 0.0;
    
    // 多级采样，获取更柔和的边缘
    for(float i = -1.0; i <= 1.0; i += 0.4) {
        for(float j = -1.0; j <= 1.0; j += 0.4) {
            vec2 offset = vec2(i, j) * pixel_size * glow_radius;
            vec4 neighbor = texture(TEXTURE, UV + offset);
            
            // 核心改进：采样时强行混入我们想要的 glow_color
            sampled_color += mix(neighbor.rgb, glow_color.rgb, 0.6) * neighbor.a;
            alpha_mask += neighbor.a;
        }
    }
    
    sampled_color /= 25.0;
    alpha_mask /= 25.0;
    
    // 增加色彩鲜艳度：让蓝色更蓝
    sampled_color = pow(sampled_color, vec3(0.8)); // 降低伽马值，让色彩更浓
    
    vec4 final_color;
    if (tex_color.a > 0.0) {
        // 物体本体逻辑
        // 混合原图颜色和目标蓝色，防止中心纯白
        vec3 body_color = mix(tex_color.rgb, glow_color.rgb, exposure_bias);
        final_color = vec4(body_color * glow_intensity, tex_color.a);
    } else {
        // 外部光晕逻辑
        final_color = vec4(sampled_color * color_boost * glow_intensity, alpha_mask);
    }
    
    COLOR = final_color;
}
