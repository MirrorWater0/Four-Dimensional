shader_type canvas_item;
render_mode blend_add;

uniform vec4 main_color : source_color = vec4(0.2, 0.6, 1.0, 1.0);
uniform float progress : hint_range(0.0, 1.0) = 0.0;
uniform float glow_intensity : hint_range(0.0, 50.0) = 25.0;
uniform float ray_count : hint_range(0.0, 100.0) = 15.0; // 减少数量，增加单根粗度

float hash(vec2 p) {
    return fract(sin(dot(p, vec2(12.9898, 78.233))) * 43758.5453);
}

void fragment() {
    vec2 uv = UV - vec2(0.5);
    float dist = length(uv);
    float angle = atan(uv.y, uv.x);
    float pixel_size = fwidth(dist);

    // 边缘羽化，防止方框切边
    float boundary_mask = smoothstep(0.5, 0.35, dist); 

    vec3 final_rgb = vec3(0.0);

    // --- 1. 扩大的中心核心光 (Large Center Flare) ---
    // 这里的 0.35 是半径，足以覆盖大多数图标
    // 使用 pow(..., 2.0) 让中心光圈更厚实，不是薄薄一层
    float core_radius = 0.17 * (1.0 - pow(progress, 2.0));
    float core_flare = smoothstep(core_radius, core_radius * 0.5, dist);
    
    // 增加一个极亮的中心点，确保图标中心被完全覆盖
    float center_hotspot = smoothstep(0.1 * (1.0 - progress), 0.0, dist);
    
    final_rgb += main_color.rgb * (core_flare + center_hotspot) * glow_intensity * (1.0 - progress);

    // --- 2. 粗化射线 (Bold Rays) ---
    float seed = floor(angle * ray_count);
    float ray_random_len = 0.5 + hash(vec2(seed, 0.5)) * 0.5;
    
    // 核心改进：降低 pow 指数。从 80.0 降到 12.0，射线会变粗很多
    float ray_shape = pow(max(0.0, 1.0 - abs(sin(angle * ray_count))), 12.0);
    
    // 射线长度随进度刺出
    float ray_limit = progress * 1.5 * ray_random_len;
    float ray_mask = smoothstep(ray_limit, ray_limit - 0.4, dist) * ray_shape;
    
    // 只有在爆发前缘才显示，并增加宽度感
    ray_mask *= smoothstep(progress - 0.4, progress, dist);
    
    final_rgb += main_color.rgb * ray_mask * glow_intensity * 1.2;

    // --- 3. 溅射碎星 (Sparks) ---
    // 调大星光尺寸，防止缩小看不见
    float spark_grid = 20.0;
    vec2 grid_id = floor(vec2(angle * spark_grid, dist * 8.0 - progress * 8.0));
    float spark_noise = hash(grid_id);
    
    float sparks = 0.0;
    if (spark_noise > 0.94) {
        vec2 f_uv = fract(vec2(angle * spark_grid, dist * 8.0 - progress * 8.0)) - 0.5;
        // 增大星光基础大小
        float s_size = 0.2 * (1.0 - dist); 
        sparks = smoothstep(s_size, 0.0, length(f_uv));
        // 十字光条也加粗
        sparks += smoothstep(s_size * 0.2, 0.0, abs(f_uv.x) * abs(f_uv.y * 8.0));
        sparks += smoothstep(s_size * 0.2, 0.0, abs(f_uv.y) * abs(f_uv.x * 8.0));
        
        sparks *= smoothstep(progress, progress - 0.2, dist) * smoothstep(progress - 0.4, progress - 0.2, dist);
    }
    
    final_rgb += vec3(1.0) * sparks * glow_intensity;

    // --- 4. 消失逻辑 ---
    // 先慢后快，保持在屏幕上的存在感
    float fade = 1.0 - pow(progress, 4.0);
    
    COLOR = vec4(final_rgb, (core_flare + ray_mask + sparks) * fade * boundary_mask);
}
