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
	vec4 modulate = COLOR;
	// 1. 坐标标准化：将 UV 映射到 0.0 到 0.5 之间，实现四个角对称
	vec2 uv = abs(UV - 0.5);
	float x = uv.x;
	float y = uv.y;

	// --- 2. 线条位置定义 ---
	float inner_frame_pos = 0.44;  // 最内层完整方框的位置
	float outer_frame_pos = 0.46;  // 第二层方框的位置
	float decoration_pos = 0.485; // 最外层角落装饰线的位置

	// --- 3. 绘制线条 Mask ---
	float frame_mask = 0.0;

	// 内层主边框 (0.44)
	frame_mask += line(x, inner_frame_pos, border_thickness, 0.002) * step(y, inner_frame_pos);
	frame_mask += line(y, inner_frame_pos, border_thickness, 0.002) * step(x, inner_frame_pos);

	// 外层主边框 (0.46)
	frame_mask += line(x, outer_frame_pos, border_thickness, 0.002) * step(y, outer_frame_pos);
	frame_mask += line(y, outer_frame_pos, border_thickness, 0.002) * step(x, outer_frame_pos);

	// 角落装饰
	if (x > 0.5 - corner_size && y > 0.5 - corner_size) {
		// 最外层 L 线
		frame_mask += line(x, decoration_pos, border_thickness, 0.002) * step(y, decoration_pos) * step(outer_frame_pos - 0.04, y);
		frame_mask += line(y, decoration_pos, border_thickness, 0.002) * step(x, decoration_pos) * step(outer_frame_pos - 0.04, x);

		// 内部装饰 L (更靠近中心)
		float inner_l_pos = 0.38;
		frame_mask += line(x, inner_l_pos, border_thickness, 0.002) * step(y, inner_l_pos) * step(inner_l_pos - 0.05, y);
		frame_mask += line(y, inner_l_pos, border_thickness, 0.002) * step(x, inner_l_pos) * step(inner_l_pos - 0.05, x);

		// 角落的小方点
		frame_mask += step(0.47, x) * step(0.47, y) * (1.0 - step(0.49, x)) * (1.0 - step(0.49, y));
	}

	frame_mask = clamp(frame_mask, 0.0, 1.0);

	// --- 4. 背景裁剪逻辑 ---

	// 关键修改：背景只存在于比最内层边框（inner_frame_pos）更靠内的区域
	// 减去 border_thickness 是为了让背景刚好贴合在内线的内侧
	float bg_limit = inner_frame_pos - border_thickness;
	float bg_mask = step(x, bg_limit) * step(y, bg_limit);

	// --- 5. 颜色与透明度合成 ---

	// 分别计算背景和线条的 Alpha
	float final_bg_alpha = bg_mask * bg_color.a;
	float final_line_alpha = frame_mask * line_color.a;

	// 颜色混合：如果有线条，则显示线条颜色，否则显示背景色
	// 注意：这里的 mix 逻辑在没有背景的区域（bg_mask=0）依然能通过 final_line_alpha 渲染线条
	vec3 final_rgb = mix(bg_color.rgb, line_color.rgb, final_line_alpha);

	// 最终透明度：背景区域或线条区域
	float final_alpha = max(final_bg_alpha, final_line_alpha);

	COLOR = vec4(final_rgb, final_alpha)*modulate;
}