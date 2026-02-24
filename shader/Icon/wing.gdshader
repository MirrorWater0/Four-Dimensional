shader_type canvas_item;

// --- 颜色与翻转 ---
uniform vec4 wing_color : source_color = vec4(1.0, 1.0, 1.0, 1.0);
uniform bool flip_vertical = false;

// --- 整体变换控制 ---
uniform float wing_size : hint_range(0.1, 3.0) = 1.0;
uniform vec2 pivot = vec2(0.5, 0.5);   // 旋转与缩放的中心点
uniform float rotation : hint_range(-6.28, 6.28) = 0.0; // 整体旋转角度 (弧度)
uniform float edge_smoothness : hint_range(0.0, 0.1) = 0.01;

// --- 翅膀细节 ---
uniform int blade_count : hint_range(3, 12) = 6;
uniform float wing_span : hint_range(0.1, 2.0) = 1.2;
uniform float spine_thickness : hint_range(0.01, 0.2) = 0.06;

// --- 辅助函数 ---

// 标准旋转函数
vec2 rotate(vec2 p, float angle) {
	float s = sin(angle);
	float c = cos(angle);
	return vec2(p.x * c - p.y * s, p.x * s + p.y * c);
}

// 修复后的分步三角形 SDF
float sd_triangle(vec2 p, vec2 p0, vec2 p1, vec2 p2) {
	vec2 e0 = p1 - p0, e1 = p2 - p1, e2 = p0 - p2;
	vec2 v0 = p - p0, v1 = p - p1, v2 = p - p2;
	vec2 pq0 = v0 - e0 * clamp(dot(v0, e0) / dot(e0, e0), 0.0, 1.0);
	vec2 pq1 = v1 - e1 * clamp(dot(v1, e1) / dot(e1, e1), 0.0, 1.0);
	vec2 pq2 = v2 - e2 * clamp(dot(v2, e2) / dot(e2, e2), 0.0, 1.0);
	float s = sign(e0.x * v0.y - e0.y * v0.x);
	vec2 d = min(min(vec2(dot(pq0, pq0), s * (e0.x * v0.y - e0.y * v0.x)),
					 vec2(dot(pq1, pq1), s * (e1.x * v1.y - e1.y * v1.x))),
					 vec2(dot(pq2, pq2), s * (e2.x * v2.y - e2.y * v2.x)));
	return -sqrt(d.x) * sign(d.y);
}

void fragment() {
	// 1. 基础变换：移动到中心点并缩放
	vec2 uv = (UV - pivot) / wing_size;

	// 2. 整体旋转应用
	// 我们在这里旋转整个 UV 空间
	uv = rotate(uv, rotation);

	// 3. 垂直翻转 (如果在旋转之后翻转，它会基于局部坐标翻转)
	if (flip_vertical) {
		uv.y = -uv.y;
	}

	float d = 1.0;

	// 4. 生成几何体
	for (int i = 0; i < blade_count; i++) {
		float t = 1.0 - (float(i) / float(blade_count - 1));
		float t_next = 1.0 - (float(i + 1) / float(blade_count - 1));

		// 脊柱坐标
		vec2 p_base = vec2(pow(t, 2.0) * 0.3, -t * 0.7);
		vec2 p_next = vec2(pow(t_next, 2.0) * 0.3, -t_next * 0.7);

		// 羽翼坐标 (相对于脊柱点旋转)
		float blade_len = mix(0.1, 0.7, t);
		float angle = mix(-2.8, -1.8, t);
		vec2 p_tip = p_base + rotate(vec2(blade_len, 0.0), angle);

		// 脊柱厚度坐标
		vec2 p_base_in = p_base - vec2(spine_thickness * t, 0.0);
		vec2 p_next_in = p_next - vec2(spine_thickness * t_next, 0.0);

		// 组合三角形
		float blade = sd_triangle(uv, p_base, p_next, p_tip);
		float s1 = sd_triangle(uv, p_base, p_next, p_base_in);
		float s2 = sd_triangle(uv, p_next, p_base_in, p_next_in);

		d = min(d, min(blade, min(s1, s2)));
	}

	// 5. 渲染
	float aa = fwidth(d) + edge_smoothness;
	float mask = 1.0 - smoothstep(0.0, aa, d);

	COLOR *= vec4(wing_color.rgb, mask * wing_color.a);
}