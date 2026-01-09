shader_type canvas_item;
render_mode blend_mix, unshaded;

uniform float speed: hint_range(0.1, 3.0, 0.1) = 1.0;
uniform vec3 bg_color: source_color = vec3(0.01, 0.16, 0.42);
uniform vec3 rect_color: source_color = vec3(0.01, 0.26, 0.57);
uniform float rect_count: hint_range(10.0, 100.0, 1.0) = 60.0;
uniform float edge_blur: hint_range(0.001, 0.01, 0.0001) = 0.003;

// --- 修复1: 更加稳定的 Hash 函数 (Gold Noise 变种) ---
// 不再使用 sin，消除了因显卡精度差异导致的闪烁
float hash12(vec2 p) {
	vec3 p3  = fract(vec3(p.xyx) * .1031);
	p3 += dot(p3, p3.yzx + 33.33);
	return fract((p3.x + p3.y) * p3.z);
}

// 保持背景逻辑不变
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
	// 防止 Time 过大
	float safe_time = mod(TIME, 3600.0);
	
	vec2 uv = (UV - 0.5) * 2.0;
	vec3 color = bg(uv, safe_time) * (2.0 - abs(uv.y * 2.0));
	
	const float minSize = 0.03;
	const float maxSize = 0.08 - minSize;
	const float yDistribution = 0.5;
	
	float velX = -safe_time * speed / 8.0;
	float velY = safe_time * speed / 10.0;
	
	// 固定循环次数
	for (float i = 0.0; i < 100.0; i++) {
		// --- 修复2: 增加 Epsilon 偏移，防止 rect_count 在整数临界点浮动导致第N个方块闪烁 ---
		float visible = step(i, rect_count - 0.5); 
		
		// 如果不可见，直接跳过计算以节省性能（虽然 GPU 不一定真跳，但逻辑上安全）
		// 注意：在某些极其老旧的驱动上，continue 也有问题，但大部分现代情况没问题
		// 如果还闪，把 continue 去掉，用 visible * ... 放在最后
		if (visible < 0.5) continue;

		// --- 修复3: 彻底解耦随机种子 ---
		// 不要用 (i / rect_count) 做种子！
		// 直接用 i 做种子。这样即使 rect_count 变化，第 i 个方块的属性永远固定。
		float rnd = hash12(vec2(i + 13.0, 42.0)); // 使用新的 Hash 函数
		
		// 为了布局逻辑，我们需要一个 0-1 的分布值，这里手动计算，不影响随机种子
		float layout_fraction = i / 100.0; // 使用固定分母 100，保证位置分布均匀且稳定
		
		vec3 pos = vec3(0.0);
		
		// 重新设计位置逻辑，使其更稳定
		// 使用 layout_fraction 保证在画面上铺开，rnd 增加随机性
		pos.x = fract(velX * rnd + layout_fraction * 2.345) * 4.0 - 2.0; 
		pos.y = sin(layout_fraction * 15.0 + velY * rnd) * yDistribution;
		pos.z = maxSize * rnd + minSize;
		
		// 旋转修正
		vec2 uvRot = uv - pos.xy;
		uvRot = rotate2d(i + safe_time * speed / 2.0) * uvRot;
		uvRot += pos.xy;
		
		float current_blur = pos.z * edge_blur;
		float rect = rectangle(uvRot, pos.xy, pos.z, pos.z, current_blur);
		
		color += rect_color * rect * (pos.z / maxSize);
	}
	
	COLOR = vec4(color, 1.0);
}