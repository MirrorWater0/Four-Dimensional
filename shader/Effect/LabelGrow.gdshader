shader_type canvas_item;
render_mode unshaded;

uniform vec4 glow_color : source_color = vec4(1.0, 0.5, 0.0, 1.0);
uniform float intensity : hint_range(0.0, 10.0) = 0.0;
uniform float spread : hint_range(0.0, 10.0) = 3.0;

void fragment() {
    // 1. 采样核心文本
    vec4 tex = texture(TEXTURE, UV);

    // 2. 采样周边 Alpha (用于生成外发光)
    float a = 0.0;
    vec2 ps = TEXTURE_PIXEL_SIZE * spread;
    a += texture(TEXTURE, UV + vec2(ps.x, 0.0)).a;
    a += texture(TEXTURE, UV + vec2(-ps.x, 0.0)).a;
    a += texture(TEXTURE, UV + vec2(0.0, ps.y)).a;
    a += texture(TEXTURE, UV + vec2(0.0, -ps.y)).a;
    float glow_mask = clamp(a / 4.0, 0.0, 1.0);

    // --- 核心修正：防白块逻辑 ---
    // 如果原始文字没像素，且发光遮罩也没像素，直接“丢弃”该像素点
    // 这样 CanvasGroup 的空白区域就绝对不会变白
    if (tex.a + glow_mask <= 0.0001) {
        discard;
    }

    // 3. 计算最终颜色
    // 文字部分保持原色，边缘部分混合发光色
    vec3 final_color = mix(tex.rgb, glow_color.rgb, glow_mask * intensity * (1.0 - tex.a));

    // 如果是文字本身，也让它带点发光色
    final_color += glow_color.rgb * tex.a * intensity * 0.5;

    // 最终透明度：文字透明度 + 发光透明度
    float final_alpha = clamp(tex.a + (glow_mask * intensity), 0.0, 1.0);

    COLOR = vec4(final_color, final_alpha);
}