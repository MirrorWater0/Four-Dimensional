shader_type canvas_item;

// --- 参数控制 ---
uniform vec4 bg_color : source_color = vec4(0.08, 0.13, 0.22, 1.0);
uniform vec4 line_color : source_color = vec4(0.7, 0.75, 0.8, 1.0);
uniform float border_thickness : hint_range(0.0, 0.1) = 0.005;
uniform float corner_size : hint_range(0.0, 0.5) = 0.2;

// 辅助函数：绘制实线
float line(float value, float target, float thickness, float softness) {
	return smoothstep(target - thickness - softness, target - thickness, value) -
	       smoothstep(target + thickness, target + thickness + softness, value);
}

void fragment() {
	// 1. 坐标标准化：将 UV 映射到 0.0 到 0.5 之间，实现四个角对称
	vec2 uv = abs(UV - 0.5);
	float x = uv.x;
	float y = uv.y;

	// 基础背景色
	vec3 final_color = bg_color.rgb;
	float alpha = bg_color.a;

	// --- 2. 绘制双层主边框 ---
	// 外框位置 0.45，内框位置 0.42
	float frame = line(x, 0.46, border_thickness, 0.002) * step(y, 0.46);
	frame += line(y, 0.46, border_thickness, 0.002) * step(x, 0.46);

	frame += line(x, 0.44, border_thickness, 0.002) * step(y, 0.44);
	frame += line(y, 0.44, border_thickness, 0.002) * step(x, 0.44);

	// --- 3. 绘制角落装饰 (L型结构) ---
	// 定义角落激活区域
	if (x > 0.5 - corner_size && y > 0.5 - corner_size) {
		// 角落里的小方块或短线
		float corner_pattern = 0.0;

		// 外部装饰 L 线
		corner_pattern += line(x, 0.485, border_thickness, 0.002) * step(y, 0.485) * step(0.42, y);
		corner_pattern += line(y, 0.485, border_thickness, 0.002) * step(x, 0.485) * step(0.42, x);

		// 内部装饰小 L
		float inner_l_pos = 0.38;
		corner_pattern += line(x, inner_l_pos, border_thickness, 0.002) * step(y, inner_l_pos) * step(inner_l_pos - 0.05, y);
		corner_pattern += line(y, inner_l_pos, border_thickness, 0.002) * step(x, inner_l_pos) * step(inner_l_pos - 0.05, x);

		// 角落的连接点（模拟图中的短点）
		corner_pattern += step(0.47, x) * step(0.47, y) * (1.0 - step(0.49, x)) * (1.0 - step(0.49, y));

		frame += corner_pattern;
	}

	// 限制边框不要超出中心
	frame = clamp(frame, 0.0, 1.0);

	// 混合颜色
	final_color = mix(final_color, line_color.rgb, frame);

	COLOR = vec4(final_color, alpha);
}