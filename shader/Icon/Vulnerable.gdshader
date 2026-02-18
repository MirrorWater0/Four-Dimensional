shader_type canvas_item;

uniform vec4 heart_color : source_color = vec4(0.75, 0.1, 0.1, 1.0);
uniform float cut_width : hint_range(0.0, 0.2) = 0.06;
uniform float cut_slope : hint_range(-2.0, 2.0) = 0.8;

void fragment() {
	// 1. 坐标初始化
	// 调整系数 (2.5) 确保心形完整显示在框内
	// 偏移量 (0.8) 将原点移到底尖
	vec2 uv = (UV - vec2(0.5, 0.8)) * 2.5; 
	uv.y *= -1.0; // 向上为正

	// 2. 极坐标计算
	// 使用 abs(uv.x) 保证左右完全镜像，解决“形没了”或者“只有一半”的问题
	float theta = atan(uv.y, abs(uv.x));
	float r = length(uv);

	// 3. 极坐标方程核心逻辑
	float heart_mask = 0.0;
	// 只在原点上方计算 (theta > 0)
	if (theta > 0.0 && theta < PI) {
		float t = tan(theta);
		float r_limit;
		
		// 奇点保护：当 theta 非常接近 PI/2 (90度) 时，tan 无穷大，r 趋近 1.0
		if (abs(theta - PI/2.0) < 0.01) {
			r_limit = 1.0;
		} else {
			// 方程：r = |tan|^ (1/tan)
			r_limit = pow(abs(t), 1.0 / t);
		}
		
		// 渲染形状：到原点距离 r 小于计算出的极限 r_limit 则填充颜色
		heart_mask = smoothstep(r_limit, r_limit - 0.03, r);
	}

	// 4. 破碎切口遮罩
	// (uv.y - 0.5) 是为了让切口中心稍微向上提一点
	float line_val = uv.x + (uv.y - 0.6) * cut_slope;
	float cut_mask = smoothstep(cut_width - 0.01, cut_width, abs(line_val));

	// 5. 输出
	vec3 color = mix(heart_color.rgb * 0.9, heart_color.rgb, UV.y);
	COLOR = vec4(color, heart_mask * cut_mask);
}