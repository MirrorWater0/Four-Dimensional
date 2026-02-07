shader_type canvas_item;

uniform vec4 symbol_color : source_color = vec4(1.0, 1.0, 1.0, 1.0);
uniform float thickness : hint_range(0.005, 0.1) = 0.02;
uniform float main_length : hint_range(0.1, 2.5) = 1.4;
uniform float main_width : hint_range(0.1, 2.0) = 1.1;
uniform float small_length : hint_range(0.1, 2.0) = 0.5;
uniform float small_width : hint_range(0.1, 2.0) = 0.7;
uniform float petal_overall_size : hint_range(0.05, 0.5) = 0.5;
uniform float petal_pinch : hint_range(0.2, 1.0) = 0.6;
uniform float ring_radius : hint_range(0.3, 0.9) = 0.72;
uniform float glow_intensity : hint_range(0.0, 2.0) = 0.4;
uniform float tip_softness : hint_range(0.0, 1.5) = 0.5; // 新增：控制尖端虚化的程度

float smin(float a, float b, float k) {
	float h = clamp(0.5 + 0.5 * (b - a) / k, 0.0, 1.0);
	return mix(b, a, h) - k * h * (1.0 - h);
}

// 核心函数：获取SDF以及局部的“虚化系数”
vec2 get_sdf_with_fade(vec2 uv) {
	float d = 10.0;
	float fade = 1.0;
	
	// 1. Ring
	float ring = abs(length(uv) - ring_radius) - (thickness * 0.5);
	d = ring;

	// 2. Central Star
	vec2 p_center = abs(uv);
	float center_star = pow(p_center.x + 1e-4, 0.45) + pow(p_center.y + 1e-4, 0.45) - 0.8;
	float center_hole = (p_center.x + p_center.y) - 0.18;
	d = smin(d, max(center_star, -center_hole), 0.05);

	// 3. Petals
	for (int i = 0; i < 12; i++) {
		float angle = float(i) * (6.283185 / 12.0);
		float c = cos(-angle);
		float s = sin(-angle);
		vec2 p_rot = vec2(uv.x * c - uv.y * s, uv.x * s + uv.y * c);
		
		float dist_from_center = p_rot.x - ring_radius; 
		p_rot.x -= ring_radius;
		
		bool is_main = (i % 2 == 0);
		float cur_len = is_main ? main_length : small_length;
		vec2 scale = is_main ? vec2(cur_len, main_width * 0.5) : vec2(small_length, small_width * 0.5);
		
		vec2 p_scaled = p_rot / scale;
		vec2 ap = abs(p_scaled);
		
		// 稍微圆角化处理：通过添加一个微小的 0.01 偏移
		float petal = pow(ap.x + 1e-3, petal_pinch) + pow(ap.y + 1e-3, petal_pinch) - petal_overall_size;
		petal *= min(scale.x, scale.y);
		
		// 计算尖端虚化：越靠近花瓣末端，fade值越小
		// tip_pos 是当前点在花瓣长度上的百分比 (0 到 1)
		float tip_pos = clamp(dist_from_center / (cur_len * petal_overall_size * 1.5), 0.0, 1.0);
		float current_fade = smoothstep(1.0, 1.0 - (0.2 * tip_softness), tip_pos);
		
		if (petal < d) {
			d = smin(d, petal, 0.02);
			fade = min(fade, current_fade); // 取最窄处的虚化
		} else {
			d = smin(d, petal, 0.02);
		}
	}
	return vec2(d, fade);
}

void fragment() {
	vec2 pixel_size = fwidth(UV * 2.5);
	float unit = length(pixel_size);
	
	// --- 旋转网格采样 (RGSSAA) ---
	vec2 offsets[4];
	offsets[0] = vec2( 0.125,  0.375);
	offsets[1] = vec2( 0.375, -0.125);
	offsets[2] = vec2(-0.125, -0.375);
	offsets[3] = vec2(-0.375,  0.125);

	float total_mask = 0.0;
	float avg_fade = 0.0;
	vec2 base_uv = (UV - 0.5) * 2.5;
	
	for(int i = 0; i < 4; i++) {
		vec2 sample_uv = base_uv + offsets[i] * pixel_size;
		vec2 res = get_sdf_with_fade(sample_uv);
		
		// 动态平滑：尖端区域(res.y小)增加模糊宽度(unit大)
		float soft_unit = unit * (1.0 + (1.0 - res.y) * 2.0 * tip_softness);
		float m = clamp(0.5 - res.x / soft_unit, 0.0, 1.0);
		
		total_mask += m;
		avg_fade += res.y;
	}
	
	float mask = total_mask * 0.25;
	avg_fade *= 0.25;

	// 应用淡化，让尖端在视觉上“消失”得更平滑
	mask *= avg_fade;

	// --- 渲染细节 ---
	vec2 final_res = get_sdf_with_fade(base_uv);
	float glow = exp(-max(0.0, final_res.x) * (15.0 - tip_softness * 5.0)) * glow_intensity;
	
	// 混合颜色
	vec3 color = symbol_color.rgb + (mask * 0.2);
	// 重点：alpha不仅由mask决定，还受fade控制，确保尖端是半透明的
	float alpha = clamp(mask + glow * avg_fade, 0.0, 1.0);

	COLOR = vec4(color, alpha * COLOR.a) * COLOR;
}