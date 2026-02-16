 shader_type canvas_item;
render_mode blend_add;
uniform vec4 ball_color : source_color = vec4(0.0, 0.6, 1.0, 1.0); // 基础蓝色
uniform float core_size : hint_range(0.0, 0.5) = 0.05;            // 亮白色核心的大小
uniform float glow_softness : hint_range(1.0, 20.0) = 8.0;       // 边缘扩散的柔和度
uniform float glow_intensity : hint_range(0.0, 10.0) = 2.5;     // 整体亮度强度
uniform float pulse_speed : hint_range(0.0, 10.0) = 2.0;        // 呼吸速度
uniform float pulse_amount : hint_range(0.0, 1.0) = 0.2;        // 呼吸幅度

void fragment() {
    // 1. 建立中心坐标系
    vec2 center_uv = UV - vec2(0.5);
    float dist = length(center_uv);

    // 2. 呼吸动画逻辑
    float pulse = 1.0 + sin(TIME * pulse_speed) * pulse_amount;
    float animated_dist = dist / pulse;

    // 3. 计算核心亮度 (最亮的部分)
    float core = smoothstep(core_size, 0.0, animated_dist);

    // 4. 计算边缘晕染 (Glow)
    // 使用 exp(-x) 模拟真实的物理光照衰减，比简单的线性渐变更自然
    float glow = exp(-animated_dist * glow_softness) * glow_intensity;

    // 5. 颜色合成
    // 核心部分强制趋向白色，边缘部分使用自定义颜色
    vec3 final_color = ball_color.rgb * glow;
    final_color += vec3(core); // 加上白色核心

    // 6. 透明度处理
    // 让光球在远处完全透明，不产生矩形框边界
    float alpha = clamp(glow + core, 0.0, 1.0);

    // 如果想要更强烈的发光感，可以开启 HDR (在 Godot 环境中调高原始颜色强度)
    COLOR = vec4(final_color, alpha * ball_color.a);
}