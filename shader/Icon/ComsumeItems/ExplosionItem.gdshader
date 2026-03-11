shader_type canvas_item;

uniform vec4 main_color : source_color = vec4(0.0, 0.8, 1.0, 1.0);
uniform vec4 accent_color : source_color = vec4(0.5, 0.9, 1.0, 1.0);
uniform float speed : hint_range(0.0, 10.0) = 2.0;
uniform float pulse_speed : hint_range(0.0, 10.0) = 3.0;
uniform float line_thickness : hint_range(0.01, 0.1) = 0.02;

// 简单的旋转函数
vec2 rotate(vec2 uv, float angle) {
    float s = sin(angle);
    float c = cos(angle);
    return vec2(uv.x * c - uv.y * s, uv.x * s + uv.y * c);
}

void fragment() {
    // 将 UV 坐标居中 (范围从 -0.5 到 0.5)
    vec2 center_uv = UV - 0.5;
	center_uv *= 1.3;
    float dist = length(center_uv);
    
    // 1. 基础圆环 (Outer Ring)
    float ring_outer = smoothstep(0.46, 0.45, dist);
    float ring_inner = smoothstep(0.43, 0.44, dist);
    float main_ring = ring_outer * ring_inner;
    
    // 2. 旋转刻度 (Rotating Dashes)
    vec2 rotated_uv = rotate(center_uv, TIME * speed);
    float angle = atan(rotated_uv.y, rotated_uv.x);
    float dashes = step(0.7, sin(angle * 8.0)); // 8个刻度
    float dash_ring = smoothstep(0.41, 0.40, dist) * smoothstep(0.36, 0.37, dist) * dashes;
    
    // 3. 核心脉动圆 (Pulsing Core)
    float pulse = (sin(TIME * pulse_speed) * 0.5 + 0.5) * 0.05;
    float core_dist = 0.15 + pulse;
    float core = smoothstep(core_dist, core_dist - 0.02, dist);
    
    // 4. 扫描线效果 (Scanlines)
    float scanline = sin(UV.y * 100.0 + TIME * 5.0) * 0.1 + 0.9;
    
    // 5. 组合颜色
    vec3 final_rgb = main_color.rgb;
    float final_alpha = 0.0;
    
    // 叠加各部分
    final_alpha += main_ring;
    final_alpha += dash_ring * 0.8;
    final_alpha += core * 1.2;
    
    // 边缘发光感
    float glow = smoothstep(0.5, 0.0, dist) * 0.3;
    final_alpha += glow;
    
    // 应用颜色和扫描线
    vec4 final_color = vec4(final_rgb * scanline, final_alpha);
    
    // 增加亮度强调
    if (dash_ring > 0.1 || core > 0.1) {
        final_color.rgb = mix(final_color.rgb, accent_color.rgb, 0.4);
    }

    COLOR = final_color;
}