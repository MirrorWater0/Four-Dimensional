shader_type canvas_item;

// 抓取下方图标内容
uniform sampler2D screen_texture : hint_screen_texture, filter_linear_mipmap;

// --- 变换参数 ---
uniform float blur_amount : hint_range(0.0, 0.2) = 0.05; // 位移强度
uniform vec2 blur_dir = vec2(1.0, 0.0);                 // 位移方向
uniform float shrink_amount : hint_range(0.0, 1.0) = 0.2; // 缩小强度 (越大残影越小)
uniform vec2 center_pos = vec2(0.5, 0.5);               // 缩放中心 (屏幕空间 UV)

// --- 渲染参数 ---
uniform int samples = 12;
uniform float alpha_fade : hint_range(0.1, 2.0) = 1.2;  // 残影淡出速度

void fragment() {
	vec4 combined_col = vec4(0.0);
	float total_w = 0.0;

	for (int i = 0; i < samples; i++) {
		// t 从 0 (本体) 到 1 (最后面的残影)
		float t = float(i) / float(samples - 1);

		// 1. 计算缩放：t 越大，采样坐标离中心越远，图像看起来就越小
		float current_scale = 1.0 + (t * shrink_amount);
		vec2 scaled_uv = (SCREEN_UV - center_pos) * current_scale + center_pos;

		// 2. 计算位移：沿方向的反向移动采样点
		vec2 sample_uv = scaled_uv - (blur_dir * blur_amount * t);

		// 采样颜色
		vec4 s = texture(screen_texture, sample_uv);

		// 3. 计算权重 (Alpha 衰减)
		float w = pow(1.0 - t, alpha_fade);

		combined_col += s * w;
		total_w += w;
	}

	COLOR = combined_col / total_w;
}
