shader_type canvas_item;
render_mode blend_add, unshaded;

// --- 原有参数 ---
uniform float rotation_speed: hint_range(0.5, 5.0, 0.1) = 3.0;
uniform float scale: hint_range(0.5, 3.0, 0.1) = 1.0;
uniform vec3 color: source_color = vec3(0.0, 1.0, 1.0);
uniform float thickness: hint_range(0.005, 0.05, 0.0001) = 0.004;
uniform float dist2 : hint_range(0.0, 1.5, 0.1) = 1.;
uniform float dist1 : hint_range(0.0, 1.5, 0.1) = 1.;

// --- 新增：距离控制参数 ---
uniform float outer_ring_dist : hint_range(0.0, 1.0, 0.01) = 0.43; // 控制最外圈圆弧半径
uniform float triangle_dist : hint_range(0.0, 1.0, 0.01) = 0.45;   // 控制三角形轨道半径

// --- 悬停动效控制 ---
uniform float hover: hint_range(0.0, 1.0) = 0.0;

mat2 Rot(float angle) {
	float c = cos(angle);
	float s = sin(angle);
	mat2 m;
	m[0] = vec2(c, -s);
	m[1] = vec2(s, c);
	return m;
}

float B(vec2 p, vec2 s) {
	return max(abs(p).x - s.x, abs(p).y - s. y);
}

float Tri(vec2 p, vec2 s, float a) {
	return max(-dot(p, vec2(cos(-a), sin(-a))),
		max(dot(p, vec2(cos(a), sin(a))),
		max(abs(p).x - s.x, abs(p).y - s.y)));
}

vec2 DF(vec2 p, float b) {
	float angle = atan(p.y, p.x);
	float r = length(p) * cos(mod(angle + 6.28 / (b * 8.0), 6.28 / ((b * 8.0) * 0.5))) + (b - 1.0) * 6.28 / (b * 8.0);
	return vec2(r * cos(angle), r * sin(angle));
}

float circleUI(vec2 p) {
	vec2 prevP = p;
	float speed = rotation_speed + (hover * 2.0);

    // 动态计算距离（基础距离 + 悬停增量）
    float current_outer = outer_ring_dist + (hover * 0.07);
    float current_tri = triangle_dist + (hover * 0.1);
    float d1 = dist1 + (hover * 0.1);
	float d2 = dist2 + (hover * 0.05);

	mat2 animRot = Rot(radians(TIME * speed) * 30.0);
	p *= animRot;
	p = DF(p, 32.0);
	p -= vec2(0.28);
	float d = B(p * Rot(radians(45.0)), vec2(thickness, 0.02));

	p = prevP;
	p *= animRot;
	float a = radians(130.0);
	d = max(dot(p, vec2(cos(a), sin(a))), d);
	a = radians(-130.0);
	d = max(dot(p, vec2(cos(a), sin(a))), d);

	float d2_val = abs(length(p) - d2 * 0.36) - thickness;
	d2_val = max(abs(p.x) - 0.2, d2_val);
	d = min(d, d2_val);

	p = prevP;
	animRot = Rot(radians(90.0) + radians(TIME * speed) * 38.0);
	p *= animRot;
	d2_val = abs(length(p) - d1 * 0.245) - thickness;
	d2_val = max(abs(p.x) - 0.1, d2_val);
	d = min(d, d2_val);

	p = prevP;
	d2_val = abs(length(p) - 0.18) - thickness * 0.5;
	d = min(d, d2_val);

	p = prevP;
	animRot = Rot(radians(145.0) + radians(TIME * speed) * 32.0);
	p *= animRot;
	d2_val = abs(length(p) - 0.18) - thickness * 1.2;
	a = radians(30.0);
	d2_val = max(dot(p, vec2(cos(a), sin(a))), d2_val);
	a = radians(-30.0);
	d2_val = max(dot(p, vec2(cos(a), sin(a))), d2_val);
	d = min(d, d2_val);

    // --- 三角形 1 距离控制 ---
	p = prevP;
	a = radians(TIME * speed) * 30.0;
	p.x += cos(a) * current_tri;
	p.y += sin(a) * current_tri;
	d2_val = abs(Tri(p * Rot(-a) * Rot(radians(90.0)), vec2(0.02), radians(45.0))) - thickness * 0.3;
	d = min(d, d2_val);

    // --- 三角形 2 距离控制 ---
	p = prevP;
	a = radians(-sin(TIME * speed * 0.5)) * 120.0;
	a += radians(-70.0);
	p.x += cos(a) * current_tri;
	p.y += sin(a) * current_tri;
	d2_val = abs(Tri(p * Rot(-a) * Rot(radians(90.0)), vec2(0.02), radians(45.0))) - thickness * 0.6;
	d = min(d, d2_val);

    // --- 最外圈圆弧距离控制 ---
	p = prevP;
	animRot = Rot(-radians(TIME * speed) * 27.0);
	p *= animRot;
	d2_val = abs(length(p) - current_outer) - thickness * 0.4;
	d2_val = max(abs(p.x) - 0.3, d2_val);
	d = min(d, d2_val);

	p = prevP;
	animRot = Rot(-radians(TIME * speed) * 12.0);
	p *= animRot;
	p = DF(p, 8.0);
	p -= vec2(0.103);
	d2_val = B(p * Rot(radians(45.0)), vec2(thickness * 0.6, 0.007));
	d = min(d, d2_val);

	p = prevP;
	animRot = Rot(radians(16.8) - radians(TIME * speed) * 12.0);
	p *= animRot;
	p = DF(p, 8.0);
	p -= vec2(0.098);
	d2_val = B(p * Rot(radians(45.0)), vec2(thickness * 0.5, 0.013));
	d = min(d, d2_val);

	p = prevP;
	animRot = Rot(radians(TIME * speed) * 30.0);
	p *= animRot;
	p = DF(p, 10.0);
	p -= vec2(0.28);
	d2_val = abs(B(p * Rot(radians(45.0)), vec2(0.02, 0.02))) - thickness * 0.5;

	p = prevP;
	p *= animRot;
	a = radians(50.0);
	d2_val = max(dot(p, vec2(cos(a), sin(a))), d2_val);
	a = radians(-50.0);
	d2_val = max(dot(p, vec2(cos(a), sin(a))), d2_val);
	d = min(d, d2_val);

	return d;
}

void fragment() {
	vec2 uv = (UV - 0.5) * 2.0;
	float current_scale = scale + (hover * 0.1);
	uv /= current_scale;

	float d = circleUI(uv);

	// --- 核心抗锯齿改进 ---
	// fwidth(d) 能自动计算出“一个像素”在当前坐标系下对应的距离值。
	// 这保证了无论 UI 缩放多大、分辨率是多少，边缘模糊永远只有 1 个像素宽。
	float aa = fwidth(d);

	// 如果觉得边缘太锐利，可以稍微增加倍数，例如 fwidth(d) * 1.5;
	float brightness = smoothstep(aa, -aa, d);

	// 最终颜色输出
	vec3 final_color = color * brightness;
	COLOR = vec4(final_color, brightness);
}