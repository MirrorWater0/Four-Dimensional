shader_type canvas_item;

// Modified from @XorDev's volumetric starfield.
#define iTime TIME
#define iResolution (1.0 / SCREEN_PIXEL_SIZE)

#define iterations 17
#define formuparam 0.53

#define volsteps 6
#define stepsize 0.1

#define zoom 1.8
#define tile 0.85
#define speed 0.006

#define brightness 0.0010
#define darkmatter 0.300
#define distfading 0.730
#define saturation 0.850

uniform vec4 moduate : source_color = vec4(0.03529412, 0.3254902, 1.0, 1.0);
uniform float star_color_shift : hint_range(0.0, 1.0) = 0.35;

float hash3(vec3 p) {
	return fract(sin(dot(p, vec3(127.1, 311.7, 74.7))) * 43758.5453123);
}

float noise3(vec3 p) {
	vec3 i = floor(p);
	vec3 f = fract(p);
	vec3 u = f * f * (3.0 - 2.0 * f);

	float n000 = hash3(i + vec3(0.0, 0.0, 0.0));
	float n100 = hash3(i + vec3(1.0, 0.0, 0.0));
	float n010 = hash3(i + vec3(0.0, 1.0, 0.0));
	float n110 = hash3(i + vec3(1.0, 1.0, 0.0));
	float n001 = hash3(i + vec3(0.0, 0.0, 1.0));
	float n101 = hash3(i + vec3(1.0, 0.0, 1.0));
	float n011 = hash3(i + vec3(0.0, 1.0, 1.0));
	float n111 = hash3(i + vec3(1.0, 1.0, 1.0));

	float nx00 = mix(n000, n100, u.x);
	float nx10 = mix(n010, n110, u.x);
	float nx01 = mix(n001, n101, u.x);
	float nx11 = mix(n011, n111, u.x);
	float nxy0 = mix(nx00, nx10, u.y);
	float nxy1 = mix(nx01, nx11, u.y);
	return mix(nxy0, nxy1, u.z);
}

float fbm3(vec3 p) {
	float value = 0.0;
	float amplitude = 0.5;
	for (int i = 0; i < 4; i++) {
		value += amplitude * noise3(p);
		p = p * 2.03 + vec3(7.1, 3.7, 5.3);
		amplitude *= 0.5;
	}
	return value;
}

vec3 star_palette(float t) {
	vec3 cold = vec3(0.52, 0.84, 1.0);
	vec3 white = vec3(1.0, 1.0, 0.92);
	vec3 warm = vec3(1.0, 0.66, 0.38);
	vec3 tint = mix(cold, white, smoothstep(0.18, 0.58, t));
	return mix(tint, warm, smoothstep(0.66, 1.0, t) * 0.55);
}

void fragment() {
	vec2 uv = UV;
	uv.y *= iResolution.y / iResolution.x;
	vec3 dir = vec3(uv * zoom, 1.0);
	float time = iTime * speed + 0.25;

	mat2 rot1 = mat2(vec2(cos(0.5), sin(0.5)), vec2(-sin(0.5), cos(0.5)));
	mat2 rot2 = mat2(vec2(cos(0.8), sin(0.8)), vec2(-sin(0.8), cos(0.8)));
	dir.xz *= rot1;
	dir.xy *= rot2;

	vec3 from = vec3(1.0, 0.5, 0.5);
	from += vec3(time * 2.0, time, -2.0);
	from.xz *= rot1;
	from.xy *= rot2;

	float s = 0.1;
	float fade = 1.0;
	vec3 stars = vec3(0.0);

	for (int r = 0; r < volsteps; r++) {
		vec3 space_pos = from + s * dir * 0.5;
		vec3 p = abs(vec3(tile) - mod(space_pos, vec3(tile * 2.0)));
		float pa = 0.0;
		float a = 0.0;

		for (int i = 0; i < iterations; i++) {
			p = abs(p) / dot(p, p) - formuparam;
			a += abs(length(p) - pa);
			pa = length(p);
		}

		float dm = max(0.0, darkmatter - a * a * 0.001);
		a *= a * a;
		if (r > 3) {
			fade *= 1.0 - dm;
		}

		float star_tint_seed = hash3(space_pos * 4.7 + vec3(13.0, 2.0, 8.0));
		vec3 star_tint = mix(vec3(1.0), star_palette(star_tint_seed), star_color_shift);
		stars += fade * star_tint;
		stars += vec3(s, s * s, s * s * s * s) * a * brightness * fade * star_tint;

		fade *= distfading;
		s += stepsize;
	}

	stars = mix(vec3(length(stars)), stars, saturation);
	vec3 color = stars * 0.0085 * moduate.rgb;
	color = pow(max(color, vec3(0.0)), vec3(0.9));
	COLOR = vec4(color, moduate.a);
}
