shader_type canvas_item;

// --- 基础参数 ---
uniform float progress : hint_range(0.0, 1.0) = 0.0; // 转场进度 0->1
uniform sampler2D screen_texture : hint_screen_texture, filter_nearest;

// --- Y2K 风格控制 ---
uniform float pixel_size : hint_range(1.0, 100.0) = 20.0;     // 像素块大小
uniform float chromatic_aberration : hint_range(0.0, 0.1) = 0.02; // 色散强度
uniform float glitch_intensity : hint_range(0.0, 1.0) = 0.5;   // 故障抖动感
uniform vec4 flash_color : source_color = vec4(0.0, 1.0, 0.8, 1.0); // 1999经典的青/粉色调

// --- 噪声函数 (用于模拟数字抖动) ---
float random(vec2 uv) {
    return fract(sin(dot(uv.xy, vec2(12.9898, 78.233))) * 43758.5453123);
}

void fragment() {
    // 1. 动态像素化计算
    // 随着进度增加，像素感变强，随后彻底消失
    float current_pixel_res = mix(1000.0, pixel_size, sin(progress * PI));
    vec2 grid_uv = floor(SCREEN_UV * current_pixel_res) / current_pixel_res;
    
    // 2. 模拟 90s 数字故障 (Glitch)
    float shift = random(vec2(grid_uv.y, progress)) * glitch_intensity * progress * 0.05;
    vec2 glitch_uv = grid_uv + vec2(shift, 0.0);

    // 3. 菱形遮罩算法 (经典的 90s 游戏/视频转场形状)
    float diamond = abs(SCREEN_UV.x - 0.5) + abs(SCREEN_UV.y - 0.5);
    // 进度影响遮罩边缘
    float mask = step(progress * 1.5, diamond + random(grid_uv) * 0.1);

    // 4. RGB 色散偏移 (Chromatic Aberration)
    float ca_offset = chromatic_aberration * progress;
    float r = texture(screen_texture, glitch_uv + vec2(ca_offset, 0.0)).r;
    float g = texture(screen_texture, glitch_uv).g;
    float b = texture(screen_texture, glitch_uv - vec2(ca_offset, 0.0)).b;
    
    vec4 scene_color = vec4(r, g, b, 1.0);
    
    // 5. 扫描线与色调混合
    float scanline = sin(SCREEN_UV.y * 200.0) * 0.1 * progress;
    scene_color.rgb += scanline;
    
    // 6. 最终合成：结合遮罩和 1999 特色闪光
    vec4 final_color = mix(scene_color, flash_color, progress * 0.3);
    
    COLOR = vec4(final_color.rgb, mask);
}