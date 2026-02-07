shader_type canvas_item;

// --- 控制参数 ---
uniform vec4 base_color : source_color = vec4(0.03, 0.04, 0.06, 1.0); // 深底色
uniform vec4 accent_color : source_color = vec4(0.08, 0.12, 0.2, 1.0); // 中心渐变色
uniform vec4 grid_color : source_color = vec4(0.4, 0.5, 0.8, 0.1);    // 网格颜色基础值

uniform float star_speed : hint_range(0.0, 0.5) = 0.02;        // 星光飘动速度
uniform float grid_pulse_speed : hint_range(0.0, 5.0) = 1.5;   // 网格呼吸频率
uniform float star_density : hint_range(0.0, 1.0) = 0.97;      // 星光密度（越大越稀疏）

// 随机函数
float hash(vec2 p) {
	p = fract(p * vec2(123.34, 456.21));
	p += dot(p, p + 45.32);
	return fract(p.x * p.y);
}

void fragment() {
	vec2 uv = UV;
	vec2 centered_uv = (UV - 0.5) * 2.0;
	
	// 1. 动态深邃底色 (模拟星空深处)
	// 让中心渐变随时间有极其微小的位移，增加深邃感
	vec2 moving_center = centered_uv + vec2(sin(TIME * 0.2) * 0.05, cos(TIME * 0.15) * 0.05);
	float dist = length(moving_center);
	vec3 background = mix(accent_color.rgb, base_color.rgb, dist * 0.7);
	
	// 2. 连续变化的呼吸网格 (Grid)
	vec2 grid_uv = fract(uv * vec2(22.0, 13.0));
	float grid_lines = step(0.985, grid_uv.x) + step(0.985, grid_uv.y);
	
	// 计算呼吸不透明度：使用 sin 函数实现平滑起伏
	// 加入一点 UV 相关的偏移，让网格不同区域的起伏不同步，更自然
	float pulse = sin(TIME * grid_pulse_speed + (uv.x + uv.y) * 2.0) * 0.5 + 0.5;
	float final_grid_alpha = pulse * grid_color.a;
	
	// 边缘淡化网格
	grid_lines *= (1.0 - smoothstep(0.4, 1.4, dist));
	vec3 color = background + grid_color.rgb * grid_lines * final_grid_alpha;
	
	// 3. 飘动的星光 (Floating Stars)
	// 为星光增加随时间变化的位移偏移
	vec2 drift_offset = vec2(TIME * star_speed, TIME * star_speed * 0.6);
	vec2 star_uv_base = uv * 40.0;
	vec2 star_uv = star_uv_base + drift_offset; 
	
	vec2 ipos = floor(star_uv);
	float star_val = 0.0;
	
	if (hash(ipos) > star_density) {
		// 闪烁效果
		float twinkle = sin(TIME * 1.5 + hash(ipos) * 20.0) * 0.5 + 0.5;
		// 飘动的平滑圆点
		star_val = smoothstep(0.15, 0.0, length(fract(star_uv) - 0.5)) * twinkle;
	}
	
	// 让星光也有呼吸感并带有一点色彩
	color += star_val * (grid_color.rgb + 0.5) * 0.4;
	
	// 4. 边缘压暗 (增强沉浸感)
	float vignette = smoothstep(1.8, 0.4, length(centered_uv));
	color *= vignette;

	COLOR = vec4(color, 1.0);
}
