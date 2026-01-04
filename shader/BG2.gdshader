shader_type canvas_item;
render_mode blend_mix, unshaded;

uniform float speed: hint_range(0.1, 3.0, 0.1) = 1.0;
uniform vec3 bg_color: source_color = vec3(0.01, 0.16, 0.42);
uniform vec3 rect_color: source_color = vec3(0.01, 0.26, 0.57);
uniform float rect_count: hint_range(10.0, 100.0, 1.0) = 60.0;
uniform float edge_blur: hint_range(0.001, 0.01, 0.0001) = 0.003;

// Random function (used for rectangles)
float random(vec2 co) {
	return fract(sin(dot(co.xy, vec2(12.9898, 78.233))) * 43758.5453);
}

// Background without noise
vec3 bg(vec2 uv) {
	float velocity = TIME * speed / 1.6;
	
	// Reduced intensity for more uniform color
	float intensity = sin(uv.x * 3.0 + velocity * 2.0) * 0.2 + 1.0;
	
	// Subtle gradient - reduced variation
	float gradient = smoothstep(-1.0, 1.0, uv.y) * 0.3 + 0.7;
	
	// Very subtle wave effect
	float wave = sin(uv.x * 3.0 + velocity * 2.0) * 0.03;
	
	// Combine with reduced variation
	float brightness = gradient * intensity + wave;
	
	// Apply color with brightness
	vec3 col = bg_color * brightness;
	return col;
}

// Rectangle shape
float rectangle(vec2 uv, vec2 pos, float width, float height, float blur) {
	pos = (vec2(width, height) + 0.01) / 2.0 - abs(uv - pos);
	pos = smoothstep(0.0, blur, pos);
	return pos.x * pos.y;
}

// 2D rotation matrix
mat2 rotate2d(float angle) {
	mat2 m;
	float c = cos(angle);
	float s = sin(angle);
	m[0] = vec2(c, -s);
	m[1] = vec2(s, c);
	return m;
}

void fragment() {
	vec2 uv = (UV - 0.5) * 2.0;
	
	// Background
	vec3 color = bg(uv) * (2.0 - abs(uv.y * 2.0));
	
	// Rectangles
	const float minSize = 0.03;
	const float maxSize = 0.08 - minSize;
	const float yDistribution = 0.5;
	
	float velX = -TIME * speed / 8.0;
	float velY = TIME * speed / 10.0;
	
	for (float i = 0.0; i < 100.0; i++) {
		if (i >= rect_count) break;
		
		float index = i / rect_count;
		float rnd = random(vec2(index));
		vec3 pos = vec3(0.0, 0.0, 0.0);
		pos.x = fract(velX * rnd + index) * 4.0 - 2.0;
		pos.y = sin(index * rnd * 1000.0 + velY) * yDistribution;
		pos.z = maxSize * rnd + minSize;
		
		vec2 uvRot = uv - pos.xy + pos.z / 2.0;
		uvRot = rotate2d(i + TIME * speed / 2.0) * uvRot;
		uvRot += pos.xy + pos.z / 2.0;
		
		float rect = rectangle(uvRot, pos.xy, pos.z, pos.z, (maxSize + minSize - pos.z) / 2.0 * edge_blur);
		color += rect_color * rect * pos.z / maxSize;
	}
	
	COLOR = vec4(color, 1.0);
}