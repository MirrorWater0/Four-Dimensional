shader_type canvas_item;

uniform vec4 glow_color : source_color = vec4(0.3, 0.6, 1.0, 1.0); // 发光颜色
uniform float brightness : hint_range(1.0, 20.0) = 5.0;            // 核心亮度
uniform float pulse_speed : hint_range(0.0, 10.0) = 2.0;         // 呼吸闪烁速度

void fragment() {
    vec4 tex = texture(TEXTURE, UV);
    
    // 增加呼吸感：让亮度随时间微弱变化
    float pulse = 1.0 + sin(TIME * pulse_speed) * 0.2;
    
    // 核心逻辑：保留原图色彩，但在白色部分进行超亮度叠加
    // 这样会让原本蓝色的地方更蓝，原本白色的中心直接“炸”开
    vec3 final_rgb = tex.rgb * glow_color.rgb * brightness * pulse;
    
    COLOR = vec4(final_rgb, tex.a);
}