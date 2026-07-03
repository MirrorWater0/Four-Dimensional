shader_type canvas_item;

// Port of julianlumia's Shadertoy "Unstable Universe" (wtlfz8 / lcfGzr).
// CC BY-NC-SA 3.0 — https://www.shadertoy.com/view/wtlfz8
// Adapted for Godot battle background: single-pass Buffer A core + light vignette.

uniform vec4 color_tint : source_color = vec4(0.5, 0.5, 0.5, 1.0);
uniform float time_scale : hint_range(0.0, 3.0) = 1.0;
uniform float view_z : hint_range(2.0, 12.0) = 6.5;
uniform float fov : hint_range(0.2, 2.0) = 0.8;
uniform float camera_drift : hint_range(0.0, 1.0) = 0.35;
uniform float vignette_strength : hint_range(0.0, 2.0) = 1.2;
uniform float exposure : hint_range(0.2, 2.0) = 0.85;

#define MAX_STEPS 40
#define MAX_DIST 50.0
#define SURF_DIST 0.001
#define AO_STEPS 12
#define REFLECT_STEPS 24
#define VOLUME_STEPS 20

vec2 condmin(vec2 d1, vec2 d2) {
	return vec2(min(d1.x, d2.x), mix(d1.y, d2.y, step(d2.x, d1.x)));
}

float smin(float a, float b, float k) {
	float h = clamp(0.5 + 0.5 * (b - a) / k, 0.0, 1.0);
	return mix(b, a, h) - k * h * (1.0 - h);
}

float sdOctahedron(vec3 p, float s) {
	p = abs(p);
	return (p.x + p.y + p.z - s) * 0.57735027;
}

mat2 orbit_mat(float the) {
	return mat2(vec2(-cos(the), sin(the)), vec2(-sin(the), -cos(the)));
}

mat2 mouse_rot(float a) {
	float s = sin(a);
	float c = cos(a);
	return mat2(vec2(c, -s), vec2(s, c));
}

float sdSphere(vec3 p, float s) {
	return length(p) - s;
}

float sdBox(vec3 p, vec3 s) {
	p = abs(p) - s;
	return length(max(p, vec3(0.0))) + min(max(p.x, max(p.y, p.z)), 0.0);
}

float hash1(float p) {
	vec3 p3 = fract(vec3(p) * 0.1031);
	p3 += dot(p3, p3.yzx + 19.19);
	return fract((p3.x + p3.y) * p3.z);
}

vec2 get_dist(vec3 p, float i_time, inout vec3 sphere_pos, inout float g2, inout float g3, inout float g4, inout float g8) {
	vec2 d = vec2(p.y + 1.0, 7.0);
	vec3 p4 = p - vec3(0.0, 2.0, 0.5);
	float the = i_time * 0.5;

	p4.xy = orbit_mat(the) * p4.xy;
	p4.yz = orbit_mat(the) * p4.yz;

	vec2 box10 = vec2(sdOctahedron(p4, 2.0), 5.0);
	vec2 octabox = vec2(sdBox(p4, vec3(1.5)), 5.0);
	vec2 box25 = vec2(sdBox(p + vec3(0.0, 3.0, 0.0), vec3(1.0, 2.5, 1.0)), 3.0);
	float displacement = sin(1.5 * p.x + i_time * 1.4) * sin(1.2 * p.y + i_time) * cos(1.5 * p.z + i_time);
	box25.x += displacement;

	sphere_pos = vec3(-10.0, 8.0, 6.0);
	the = i_time * 0.25;
	sphere_pos.xz = orbit_mat(the) * sphere_pos.xz;

	float box9 = sdSphere(p - sphere_pos, 1.2);
	vec3 p2 = p;
	float two = dot(sin(vec3(p2.z, p2.z, p2.y) + i_time * 0.2) * 5.0, cos(vec3(p2.z, p2.x, p2.y) * 6.0 + i_time));
	d.x = mix(d.x, two, sin(p.x * 0.5 + i_time) * 0.06);
	d.x *= 0.8;

	p2.xz = orbit_mat(the) * p2.xz;
	vec2 box = vec2(sdSphere(p2 + vec3(3.0, -1.2, cos(i_time) * 2.0), 1.0), 3.0);
	vec2 box2 = vec2(sdSphere(p2 + vec3(-3.0, -0.5, sin(i_time) * 3.0), 0.75), 3.0);
	box.x = min(box.x, box2.x);
	box.x = min(box.x, box9);

	if (box.x < d.x) {
		d = condmin(d, box);
	}

	box10.x = mix(octabox.x, box10.x, 0.5);
	d.x = smin(d.x, box10.x, 2.0);
	g2 += 0.04 / (0.05 + pow(abs(box.x), 6.0));
	g4 += 1.0 / (0.4 + pow(abs(box10.x), 10.0));
	g8 += 2.0 / (0.04 + pow(abs(box9), 3.0));
	g3 += 1.0 / (1.0 + box9 * box9);
	d.x = smin(d.x, box25.x, 1.0);
	d = condmin(d, box10);
	return d;
}

vec2 ray_march(vec3 ro, vec3 rd, float i_time, inout vec3 sphere_pos, inout float g2, inout float g3, inout float g4, inout float g8) {
	vec2 h;
	vec2 t = vec2(0.0);
	for (int i = 0; i < MAX_STEPS; i++) {
		h = get_dist(ro + t.x * rd, i_time, sphere_pos, g2, g3, g4, g8);
		if (h.x < SURF_DIST || abs(t.x) > MAX_DIST) {
			break;
		}
		t.x += h.x;
		t.y = h.y;
	}
	if (t.x > MAX_DIST) {
		t.x = 100.0;
	}
	t.x += h.x;
	return t;
}

float trace_ref(vec3 o, vec3 r, float i_time, inout vec3 sphere_pos, inout float g2, inout float g3, inout float g4, inout float g8, inout float march_count) {
	float t = 0.0;
	march_count = 0.0;
	for (int i = 0; i < REFLECT_STEPS; i++) {
		vec3 p = o + r * t;
		float d = get_dist(p, i_time, sphere_pos, g2, g3, g4, g8).x;
		if (d < 0.001 || t > 100.0) {
			break;
		}
		t += d * 0.12;
		march_count += 1.0 / d;
	}
	return t;
}

mat3 set_camera(vec3 ro, vec3 ta, float cr) {
	vec3 cw = normalize(ta - ro);
	vec3 cp = vec3(sin(cr), cos(cr), 0.0);
	vec3 cu = normalize(cross(cw, cp));
	vec3 cv = cross(cu, cw);
	return mat3(cu, cv, cw);
}

vec3 get_normal(vec3 p, float i_time, inout vec3 sphere_pos, inout float g2, inout float g3, inout float g4, inout float g8) {
	vec2 e = vec2(0.00035, -0.00035);
	return normalize(
		e.xyy * get_dist(p + e.xyy, i_time, sphere_pos, g2, g3, g4, g8).x +
		e.yxy * get_dist(p + e.yxy, i_time, sphere_pos, g2, g3, g4, g8).x +
		e.yyx * get_dist(p + e.yyx, i_time, sphere_pos, g2, g3, g4, g8).x +
		e.xxx * get_dist(p + e.xxx, i_time, sphere_pos, g2, g3, g4, g8).x
	);
}

vec3 random_sphere_dir(vec2 rnd) {
	float s = rnd.x * 3.14159265 * 2.0;
	float t = rnd.y * 2.0 - 1.0;
	return vec3(sin(s), cos(s), t) / sqrt(1.0 + t * t);
}

vec3 random_hemisphere_dir(vec3 dir, float i) {
	vec3 v = random_sphere_dir(vec2(hash1(i + 1.0), hash1(i + 2.0)));
	return v * sign(dot(v, dir));
}

float ambient_occlusion(vec3 p, vec3 n, float max_dist, float falloff, float i_time, inout vec3 sphere_pos, inout float g2, inout float g3, inout float g4, inout float g8) {
	const float nb_ite_inv = 1.0 / float(AO_STEPS);
	const float rad = 1.0 - 1.0 * nb_ite_inv;
	float ao = 0.0;

	for (int i = 0; i < AO_STEPS; i++) {
		float l = hash1(float(i)) * max_dist;
		vec3 rd = normalize(n + random_hemisphere_dir(n, l) * rad) * l;
		ao += (l - max(get_dist(p + rd, i_time, sphere_pos, g2, g3, g4, g8).x, 0.0)) / max_dist * falloff;
	}

	return clamp(1.0 - ao * nb_ite_inv, 0.0, 1.0);
}

float shadow_trace(vec3 r0, vec3 rd, float max_dist, float i_time, inout vec3 sphere_pos, inout float g2, inout float g3, inout float g4, inout float g8) {
	float d = 0.001;
	float sh = 1.0;
	for (int i = 0; i < 32; i++) {
		if (d >= max_dist) {
			break;
		}
		float t = get_dist(r0 + d * rd, i_time, sphere_pos, g2, g3, g4, g8).x;
		if (t < 0.05) {
			return 0.0;
		}
		d += t;
		sh = min(sh, 50.0 * (t / d));
	}
	return sh;
}

float get_light(vec3 p, float i_time, inout vec3 sphere_pos, inout float g2, inout float g3, inout float g4, inout float g8) {
	vec3 light_pos = sphere_pos;
	vec3 l = normalize(light_pos - p);
	vec3 n = get_normal(p, i_time, sphere_pos, g2, g3, g4, g8);
	float dif = clamp(dot(n, l) * 0.5 + 0.5, 0.0, 1.0);
	ray_march(p + n * SURF_DIST, l, i_time, sphere_pos, g2, g3, g4, g8);
	return dif;
}

void fragment() {
	vec2 i_resolution = 1.0 / SCREEN_PIXEL_SIZE;
	vec2 frag_coord = UV * i_resolution;
	float i_time = TIME * time_scale;

	vec3 sphere_pos = vec3(0.0);
	float g2 = 0.0;
	float g3 = 0.0;
	float g4 = 0.0;
	float g8 = 0.0;
	float march_count = 0.0;

	vec2 uv = (2.0 * frag_coord - i_resolution) / i_resolution.y;
	vec2 m = vec2(0.6, 0.65) + vec2(sin(i_time * 0.17), cos(i_time * 0.13)) * camera_drift;

	vec3 eye = vec3(0.0, 0.0, view_z);
	float orbit = -(i_time * 0.35);
	eye.xz = orbit_mat(orbit) * eye.xz;

	vec3 col = vec3(0.0);
	vec3 look_at = vec3(0.0);
	mat3 camera = set_camera(eye, look_at, 0.0);
	vec3 dir = camera * normalize(vec3(uv, fov));

	vec3 shifted_ray_origin = eye;
	vec3 shifted_ray = dir;
	shifted_ray.yz = mouse_rot(-m.y) * shifted_ray.yz;
	shifted_ray.xz = mouse_rot(-m.x) * shifted_ray.xz;

	vec2 d = ray_march(shifted_ray_origin, shifted_ray, i_time, sphere_pos, g2, g3, g4, g8);
	float t = d.x;

	if (t < MAX_DIST) {
		vec3 hit_pos = shifted_ray_origin + shifted_ray * t;
		vec3 sn = get_normal(hit_pos, i_time, sphere_pos, g2, g3, g4, g8);
		vec3 refl = reflect(shifted_ray, sn);

		if (d.y == 3.0) {
			trace_ref(hit_pos + refl * 0.9, refl, i_time, sphere_pos, g2, g3, g4, g8, march_count);
		}
		if (d.y == 5.0) {
			trace_ref(hit_pos + refl * 0.9, refl, i_time, sphere_pos, g2, g3, g4, g8, march_count);
			col += vec3(1.0, 3.0, 2.0);
		}
		if (d.y == 7.0) {
			trace_ref(hit_pos + refl * 0.9, refl, i_time, sphere_pos, g2, g3, g4, g8, march_count);
			col += vec3(2.0, 3.0, 2.0);
		}

		vec3 p = hit_pos;
		vec3 n = get_normal(p, i_time, sphere_pos, g2, g3, g4, g8);
		float ao = ambient_occlusion(p, n, 5.5, 5.0, i_time, sphere_pos, g2, g3, g4, g8);

		vec3 lp = sphere_pos;
		vec3 vd = normalize(shifted_ray);
		float step_size = length(p - shifted_ray_origin) / float(VOLUME_STEPS);
		vec3 vo = shifted_ray_origin + step_size * vd;
		float accum = 0.0;

		for (int i = 0; i < VOLUME_STEPS; i++) {
			vec3 ld = normalize(lp - vo);
			float shad = shadow_trace(vo, ld, 5.0, i_time, sphere_pos, g2, g3, g4, g8);
			float dist_sq = dot(vo, vo);
			accum += (0.001 / dist_sq) * shad;
			vo += step_size * vd;
		}

		col *= ao;
		vec3 white = vec3(1.0);
		col += g2 * 0.05 * vec3(0.2, 0.5, 0.6) * 5.0;
		col += g4 * 0.03 * vec3(1.0, 0.6, 0.6) * 0.9;
		col += g8 * 5.0 * vec3(1.0, 0.4, 0.0);
		col *= march_count * vec3(0.4) * 0.0007;
		float dif = get_light(p, i_time, sphere_pos, g2, g3, g4, g8);
		col *= vec3(dif) + 2.0;
		vec3 sky = vec3(5.9, 3.0, 10.0);
		col *= mix(sky, col, 1.0 / (t * t / 3.0 * 0.1 + 1.5));
		col += g3 * white;
		col *= accum * white * 3.0;
	}

	col = smoothstep(vec3(0.0), vec3(1.0), col);
	col = pow(col, vec3(0.4545));

	vec2 uvn = (frag_coord - 0.5 * i_resolution) / i_resolution;
	col *= 1.0 - dot(uvn, uvn) * vignette_strength;
	col *= exposure * (color_tint.rgb + 0.5);

	COLOR = vec4(col, 1.0);
}
