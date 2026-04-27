shader_type canvas_item;

// --- 神圣色彩配置 ---
uniform vec4 holy_color : source_color = vec4(1.0, 0.85, 0.3, 1.0); // 辉光金
uniform vec4 core_color : source_color = vec4(1.0, 1.0, 1.0, 1.0);  // 纯洁白
uniform float animation_speed : hint_range(0.0, 5.0) = 1.0;

// 2D 旋转矩阵函数
vec2 rotate(vec2 uv, float angle) {
    float s = sin(angle);
    float c = cos(angle);
    return vec2(uv.x * c - uv.y * s, uv.x * s + uv.y * c);
}

void fragment() {
    // 1. 坐标中心化 (范围 -0.5 到 0.5)
    vec2 uv = UV - 0.5;
    uv *= 0.8;
    // 时间变量
    float t = TIME * animation_speed;

    // ==========================================
    // 元素 1：无限十字星芒 (Holy Cross Rays)
    // ==========================================
    // 利用反比例函数 (1/x) 创造极具张力的光芒，代替死板的矩形
    float ray_glow = 0.0015 / (abs(uv.x * uv.y) + 0.0005);
    // 限制光芒不要超出方块边界，使其在边缘平滑消失
    ray_glow *= smoothstep(0.45, 0.1, length(uv)); 

    // ==========================================
    // 元素 2：核心神圣晶体 (Sacred Crystal Core)
    // ==========================================
    // 让晶体随时间轻微“呼吸”胀缩
    float breathe = sin(t * 2.0) * 0.015;
    // 菱形 SDF 公式：abs(x) + abs(y)
    float d_crystal = (abs(uv.x) * 1.5 + abs(uv.y)) - (0.12 + breathe);
    
    // 晶体实体 (平滑边缘)
    float crystal_solid = smoothstep(0.01, 0.0, d_crystal);
    // 晶体外发光 (使用 exp 函数模拟光线的自然物理衰减)
    float crystal_glow = exp(-max(d_crystal, 0.0) * 25.0);

    // ==========================================
    // 元素 3：旋转的刻文光环 (Rotating Runic Halo)
    // ==========================================
    // 让光环坐标系缓慢逆时针旋转
    vec2 uv_halo = rotate(uv, -t * 0.5);
    // 圆环 SDF
    float d_ring = abs(length(uv_halo) - 0.28) - 0.008;
    
    // 利用极坐标 (atan) 把圆环切断，产生 8 段“刻文”感
    float angle = atan(uv_halo.y, uv_halo.x);
    float ring_dash = smoothstep(-0.2, 0.2, sin(angle * 8.0)); 
    
    // 光环实体与发光
    float ring_solid = smoothstep(0.01, 0.0, d_ring) * ring_dash;
    float ring_glow = exp(-max(d_ring, 0.0) * 40.0) * ring_dash * 0.8;

    // ==========================================
    // 混合与着色 (Blending)
    // ==========================================
    vec3 final_rgb = vec3(0.0);

    // 1. 铺设底层辉光 (全部使用金色)
    final_rgb += holy_color.rgb * (ray_glow + crystal_glow * 0.8 + ring_glow);

    // 2. 覆盖高亮实体 (混合金色和纯白色)
    // 越靠近中心越白，边缘偏金
    vec3 solid_fill = mix(holy_color.rgb, core_color.rgb, 0.8);
    // 将实体覆盖到辉光之上 (使用 clamp 确保不超界)
    float solid_mask = clamp(crystal_solid + ring_solid, 0.0, 1.0);
    final_rgb = mix(final_rgb, solid_fill, solid_mask);

    // 3. 计算最终透明度 Alpha
    // 只要有发光或实体的地方就有透明度
    float final_alpha = clamp(ray_glow + crystal_solid + crystal_glow + ring_solid + ring_glow, 0.0, 1.0);
    // 圆滑地裁剪掉四个角的溢出
    final_alpha *= smoothstep(0.5, 0.35, length(uv));

    COLOR *= vec4(final_rgb, final_alpha);
}
