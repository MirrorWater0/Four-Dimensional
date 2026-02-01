shader_type canvas_item;
render_mode blend_mix, unshaded;

uniform float speed: hint_range(0.1, 3.0, 0.1) = 1.0;
uniform vec3 bg_color: source_color = vec3(0.01, 0.16, 0.42);
uniform vec3 rect_color: source_color = vec3(0.01, 0.26, 0.57);
uniform float rect_count: hint_range(10.0, 100.0, 1.0) = 60.0;
uniform float edge_blur: hint_range(0.001, 0.01, 0.0001) = 0.003;

// --- 新增控制参数 ---
uniform float hue_variation: hint_range(-0.2, 0.3, 0.01) = 0.1; // 色相偏移幅度
uniform float appearance: hint_range(0.0, 1.0, 0.01) = 1.0;    // 0=全无, 1=完全出现

// 辅助函数：HSV 转 RGB
vec3 hsv2rgb(vec3 c) {
    vec4 K = vec4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
    vec3 p = abs(fract(c.xxx + K.xyz) * 6.0 - K.www);
    return c.z * mix(K.xxx, clamp(p - K.xxx, 0.0, 1.0), c.y);
}

// 辅助函数：RGB 转 HSV
vec3 rgb2hsv(vec3 c) {
    vec4 K = vec4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
    vec4 p = mix(vec4(c.bg, K.wz), vec4(c.gb, K.xy), step(c.b, c.g));
    vec4 q = mix(vec4(p.xyw, c.r), vec4(c.r, p.yzx), step(p.x, c.r));
    float d = q.x - min(q.w, q.y);
    float e = 1.0e-10;
    return vec3(abs(q.z + (q.w - q.y) / (6.0 * d + e)), d / (q.x + e), q.x);
}

float hash12(vec2 p) {
	vec3 p3  = fract(vec3(p.xyx) * .1031);
	p3 += dot(p3, p3.yzx + 33.33);
	return fract((p3.x + p3.y) * p3.z);
}

vec3 bg(vec2 uv, float t) {
	float velocity = t * speed / 1.6;
	float intensity = sin(uv.x * 3.0 + velocity * 2.0) * 0.2 + 1.0;
	float gradient = smoothstep(-1.0, 1.0, uv.y) * 0.3 + 0.7;
	float wave = sin(uv.x * 3.0 + velocity * 2.0) * 0.03;
	float brightness = gradient * intensity + wave;
	return bg_color * brightness;
}

float rectangle(vec2 uv, vec2 pos, float width, float height, float blur) {
	vec2 extent = vec2(width, height) / 2.0;
	vec2 dist = abs(uv - pos);
	vec2 s = smoothstep(extent, extent - blur, dist);
	return s.x * s.y;
}

mat2 rotate2d(float angle) {
	float c = cos(angle);
	float s = sin(angle);
	return mat2(vec2(c, -s), vec2(s, c));
}

void fragment() {
	vec4 modulate = COLOR;
	float safe_time = mod(TIME, 3600.0);
	vec2 uv = (UV - 0.5) * 2.0;
	vec3 color = bg(uv, safe_time) * (2.0 - abs(uv.y * 2.0));

	const float minSize = 0.03;
	const float maxSize = 0.08 - minSize;
	const float yDistribution = 0.5;

	float velX = -safe_time * speed / 8.0;
	float velY = safe_time * speed / 10.0;

	// 预转换基础颜色的 HSV 方便偏移
	vec3 base_hsv = rgb2hsv(rect_color);

	for (float i = 0.0; i < 100.0; i++) {
		float visible = step(i, rect_count - 0.5);
		if (visible < 0.5) continue;

		float rnd = hash12(vec2(i + 13.0, 42.0));
		float layout_fraction = i / 100.0;

		// --- 新增：入场过渡逻辑 ---
		// 每个方块根据其 rnd 随机值，在 appearance 进程中 staggered (交错) 出现
		// appearance 从 0-1 变化时，方块会经历从缩放到淡入的过程
		float individual_appearance = clamp(appearance * 1.5 - rnd * 0.5, 0.0, 1.0);
		if (individual_appearance <= 0.0) continue;

		vec3 pos = vec3(0.0);
		pos.x = fract(velX * rnd + layout_fraction * 2.345) * 4.0 - 2.0;
		pos.y = sin(layout_fraction * 15.0 + velY * rnd) * yDistribution;

		// 方块大小受入场动画影响 (从 0 缩放到目标大小)
		float target_size = maxSize * rnd + minSize;
		pos.z = target_size * individual_appearance;

		// --- 新增：色相偏移逻辑 ---
		// 在基础色相上加上一个小范围的随机偏移
		vec3 shifted_hsv = base_hsv;
		shifted_hsv.x = fract(shifted_hsv.x + (rnd - 0.5) * hue_variation);
		vec3 individual_rect_color = hsv2rgb(shifted_hsv);

		// 旋转修正
		vec2 uvRot = uv - pos.xy;
		uvRot = rotate2d(i + safe_time * speed / 2.0) * uvRot;
		uvRot += pos.xy;

		float current_blur = pos.z * edge_blur;
		float rect = rectangle(uvRot, pos.xy, pos.z, pos.z, current_blur);

		// 最终叠加颜色，乘上 individual_appearance 增加亮度的淡入感
		color += individual_rect_color * rect * (pos.z / maxSize) * individual_appearance;
	}

	COLOR = vec4(color, 1.0)*modulate;
}