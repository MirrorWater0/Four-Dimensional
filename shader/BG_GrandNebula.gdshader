shader_type canvas_item;

uniform vec4 base_tint : source_color = vec4(0.035, 0.19, 0.62, 1.0);
uniform float nebula_strength : hint_range(0.0, 2.0) = 1.0;
uniform float star_strength : hint_range(0.0, 2.0) = 1.0;
uniform float drift_speed : hint_range(0.0, 2.0) = 0.45;

float hash21(vec2 p) {
	p = fract(p * vec2(123.34, 456.21));
	p += dot(p, p + 45.32);
	return fract(p.x * p.y);
}

float noise2(vec2 p) {
	vec2 i = floor(p);
	vec2 f = fract(p);
	vec2 u = f * f * f * (f * (f * 6.0 - 15.0) + 10.0);
	float a = hash21(i);
	float b = hash21(i + vec2(1.0, 0.0));
	float c = hash21(i + vec2(0.0, 1.0));
	float d = hash21(i + vec2(1.0, 1.0));
	return mix(mix(a, b, u.x), mix(c, d, u.x), u.y);
}

float fbm2(vec2 p) {
	float v = 0.0;
	float a = 0.5;
	mat2 m = mat2(vec2(1.62, 1.08), vec2(-1.08, 1.62));
	for (int i = 0; i < 5; i++) {
		v += a * noise2(p);
		p = m * p + vec2(11.7, 4.3);
		a *= 0.5;
	}
	return v;
}

vec2 rotate2(vec2 p, float a) {
	float c = cos(a);
	float s = sin(a);
	return mat2(vec2(c, s), vec2(-s, c)) * p;
}

vec3 star_color(float t) {
	vec3 blue = vec3(0.52, 0.78, 1.0);
	vec3 white = vec3(1.0, 0.96, 0.86);
	vec3 amber = vec3(1.0, 0.58, 0.28);
	vec3 cold = mix(blue, white, smoothstep(0.15, 0.65, t));
	return mix(cold, amber, smoothstep(0.72, 1.0, t) * 0.45);
}

vec3 nebula_color(float t) {
	vec3 cyan = vec3(0.07, 0.62, 0.86);
	vec3 violet = vec3(0.55, 0.22, 0.72);
	vec3 rose = vec3(0.95, 0.24, 0.46);
	vec3 gold = vec3(0.95, 0.58, 0.20);
	vec3 cold = mix(cyan, violet, smoothstep(0.10, 0.62, t));
	vec3 warm = mix(rose, gold, smoothstep(0.62, 1.0, t));
	return mix(cold, warm, smoothstep(0.48, 0.95, t) * 0.58);
}

float star_layer(vec2 uv, float scale, float threshold, float size) {
	vec2 cell = floor(uv * scale);
	vec2 local = fract(uv * scale) - 0.5;
	float seed = hash21(cell);
	float brightness = smoothstep(threshold, 1.0, seed);
	vec2 offset = vec2(hash21(cell + 17.2), hash21(cell + 41.9)) - 0.5;
	float d = length(local - offset * 0.65);
	return brightness * exp(-d * d * size);
}

void fragment() {
	vec2 resolution = 1.0 / SCREEN_PIXEL_SIZE;
	vec2 uv = (UV * 2.0 - 1.0);
	uv.x *= resolution.x / resolution.y;

	float t = TIME * drift_speed;
	vec2 sky = uv;
	vec2 galaxy = rotate2(sky, -0.43);
	galaxy.x += sin(t * 0.08) * 0.08;

	float core = exp(-dot(galaxy - vec2(-0.42, -0.10), galaxy - vec2(-0.42, -0.10)) * 2.4);
	float disc = exp(-abs(galaxy.y + sin(galaxy.x * 1.7 + t * 0.06) * 0.12) * 4.2);
	float outer_disc = exp(-abs(galaxy.y + sin(galaxy.x * 0.8 - t * 0.05) * 0.20) * 1.65);

	vec2 arm_uv = vec2(log(length(galaxy - vec2(-0.42, -0.1)) + 0.18), atan(galaxy.y + 0.1, galaxy.x + 0.42));
	float spiral = sin(arm_uv.y * 2.0 + arm_uv.x * 5.5 - t * 0.10);
	float spiral_fade = 1.0 - smoothstep(0.18, 1.75, length(galaxy - vec2(-0.42, -0.1)));
	float spiral_mask = smoothstep(0.12, 0.92, spiral) * spiral_fade;

	vec2 flow = galaxy * vec2(1.4, 2.4) + vec2(t * 0.05, -t * 0.025);
	vec2 warp = vec2(
		fbm2(flow * 0.62 + vec2(3.0, 8.0)),
		fbm2(flow * 0.62 + vec2(11.0, 2.0))
	) - 0.5;
	flow += warp * 0.82;
	float broad = fbm2(flow * 1.10);
	float wisps = fbm2(flow * 2.75 + broad * 1.15);
	float clouds = smoothstep(0.25, 1.05, broad * 0.70 + wisps * 0.52);
	float dust = fbm2(flow * 4.2 + vec2(9.0, 2.0));

	float band = max(disc * 0.75, outer_disc * 0.45);
	float nebula_mask = (band + core * 0.55 + spiral_mask * 0.75) * clouds;
	nebula_mask *= 1.0 - smoothstep(0.40, 0.92, dust) * 0.18;

	vec3 color = vec3(0.005, 0.008, 0.018) + base_tint.rgb * 0.025;
	vec3 ncol = nebula_color(fbm2(flow * 0.62 + vec2(4.0, 8.0)));
	color += ncol * nebula_mask * nebula_strength * 0.82;
	color += vec3(0.95, 0.78, 0.46) * core * 0.34;

	float far_stars = star_layer(sky + vec2(t * 0.004, -t * 0.002), 150.0, 0.955, 115.0);
	float near_stars = star_layer(sky + vec2(-t * 0.008, t * 0.003), 72.0, 0.925, 80.0);
	float star_seed = fbm2(sky * 8.0 + vec2(2.0, 7.0));
	vec3 stars = star_color(star_seed) * (far_stars * 0.95 + near_stars * 0.62) * star_strength;
	color += stars * (1.0 + band * 0.45);

	float rift = 1.0 - smoothstep(0.18, 0.62, abs(galaxy.y - sin(galaxy.x * 2.2 - 0.7) * 0.09));
	float rift_noise = smoothstep(0.25, 0.95, fbm2(galaxy * vec2(2.4, 5.5) + vec2(1.0, t * 0.03)));
	color *= 1.0 - rift * rift_noise * 0.16;

	float vignette = 1.0 - smoothstep(0.32, 1.75, length(uv * vec2(0.88, 1.05)));
	color *= 0.44 + vignette * 0.86;
	color = pow(max(color, vec3(0.0)), vec3(0.88));

	COLOR = vec4(color, 1.0);
}
