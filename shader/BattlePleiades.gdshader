shader_type canvas_item;

// Direct port of Shadertoy NcfXDf — "Pleiades Star Cluster (M45)".
// https://www.shadertoy.com/view/NcfXDf
// Copyright by original author. Mechanical Godot adaptation only.

#define iTime TIME

uniform vec4 i_mouse = vec4(0.0);

// Camera / time
uniform int camera_mode : hint_range(0, 4) = 4;
// 0 = galactic plane arc (original)
// 1 = slow yaw orbit, always faces cluster
// 2 = gentle drift, minimal parallax
// 3 = fixed viewpoint
// 4 = free path — camera and look-at drift independently, cluster not screen-locked
uniform float time_scale : hint_range(0.0, 3.0) = 1.0;
uniform float time_offset = -4.0;
uniform float orbit_radius : hint_range(4.0, 30.0) = 14.0;
uniform float orbit_speed : hint_range(0.0, 2.0) = 0.48;
uniform float camera_zoom : hint_range(0.2, 2.0) = 0.75;
uniform float camera_height : hint_range(-1.0, 1.0) = 0.2;
uniform float look_lead : hint_range(0.0, 1.0) = 0.35;
uniform float drift_amount : hint_range(0.0, 1.0) = 0.18;
uniform float wander_amount : hint_range(0.0, 1.0) = 0.42;
uniform float wander_speed : hint_range(0.1, 2.0) = 0.65;
uniform vec3 cluster_center = vec3(0.0, 0.8, 0.0);

// Cluster star spacing — scales offsets from cluster_center over time
uniform float cluster_spread_min : hint_range(0.5, 2.5) = 1.25;
uniform float cluster_spread_max : hint_range(0.5, 2.5) = 1.75;
uniform float cluster_spread_speed : hint_range(0.0, 2.0) = 0.35;

// Tone
uniform float exposure : hint_range(0.2, 4.0) = 2.2;

// Sister stars
uniform float blue_factor : hint_range(0.5, 3.0) = 1.5;
uniform float core_gain : hint_range(0.0, 1.0) = 0.22;
uniform float halo_gain : hint_range(0.0, 0.4) = 0.06;
uniform float spike_gain : hint_range(0.0, 0.08) = 0.012;

// Reflection nebula
uniform float nebula_strength : hint_range(0.0, 3.0) = 0.85;
uniform float dust_cloud_radius : hint_range(4.0, 60.0) = 28.0;

const int NUM_STARS = 20;

const vec3 STAR_POS[20] = vec3[20](
	vec3(1.15, -0.05, -0.4),
	vec3(4.58, -0.62, 2.1),
	vec3(-4.40, -0.05, -1.9),
	vec3(-2.42, 2.32, -2.6),
	vec3(-1.41, -1.58, 5.2),
	vec3(-3.76, 3.21, -2.5),
	vec3(4.62, 0.22, 1.9),
	vec3(-4.56, 1.60, -0.8),
	vec3(-2.28, 4.01, -4.0),
	vec3(-1.97, 3.78, -1.5),
	vec3(-3.82, 6.65, 2.3),
	vec3(2.85, -6.42, -6.2),
	vec3(0.74, 0.02, 2.7),
	vec3(-0.02, 3.71, 1.3),
	vec3(3.16, 2.12, -4.2),
	vec3(-3.34, 1.08, 0.5),
	vec3(3.75, -2.31, 3.8),
	vec3(-5.70, -0.95, -2.9),
	vec3(0.60, 1.53, -0.1),
	vec3(4.38, -3.68, 3.1)
);

const float STAR_BRIGHT[20] = float[20](
	1.000, 0.500, 0.465, 0.398, 0.299, 0.268, 0.129, 0.093, 0.070, 0.037,
	0.077, 0.094, 0.043, 0.026, 0.023, 0.047, 0.047, 0.033, 0.035, 0.055
);

const vec3 STAR_COLOR[20] = vec3[20](
	vec3(0.72, 0.82, 1.00), vec3(0.78, 0.86, 1.00), vec3(0.68, 0.80, 1.00), vec3(0.72, 0.82, 1.00),
	vec3(0.68, 0.80, 1.00), vec3(0.66, 0.78, 1.00), vec3(0.78, 0.86, 1.00), vec3(0.72, 0.82, 1.00),
	vec3(0.78, 0.86, 1.00), vec3(0.85, 0.90, 1.00), vec3(0.78, 0.86, 1.00), vec3(0.78, 0.86, 1.00),
	vec3(0.82, 0.88, 1.00), vec3(0.82, 0.88, 1.00), vec3(0.85, 0.90, 1.00), vec3(0.78, 0.86, 1.00),
	vec3(0.82, 0.88, 1.00), vec3(0.82, 0.88, 1.00), vec3(0.80, 0.85, 1.00), vec3(0.82, 0.88, 1.00)
);

const vec3 WORLD_UP = vec3(0.0, 1.0, 0.0);

const vec3 GAL_NORTH = normalize(vec3(0.617, 0.679, 0.399));
const vec3 GAL_CENTER = normalize(vec3(-0.433, -0.130, 0.892));



#define SPIKE_LENGTH 400.0
#define NEBULA_ILLUMINATORS 4

struct Ray {
	vec3 o;
	vec3 d;
};

Ray setupCamera(vec2 uv, vec3 pos, vec3 lookAt, float zoom, vec3 upHint) {
	vec3 fwd = normalize(lookAt - pos);
	vec3 upRef = (abs(dot(fwd, upHint)) > 0.999) ? vec3(0.0, 0.0, 1.0) : upHint;
	vec3 right = normalize(cross(fwd, upRef));
	vec3 up = normalize(cross(right, fwd));
	vec3 img = pos + fwd * zoom + right * uv.x + up * uv.y;
	Ray r;
	r.o = pos;
	r.d = normalize(img - pos);
	return r;
}

float clusterSpread(float time) {
	float wave = 0.5 + 0.5 * sin(time * cluster_spread_speed);
	return mix(cluster_spread_min, cluster_spread_max, wave);
}

vec3 clusterStarPos(vec3 base_pos, float spread) {
	return cluster_center + (base_pos - cluster_center) * spread;
}

vec3 renderStar(Ray r, vec3 ray_right, vec3 ray_up, vec3 p, float brightness, vec3 color) {
	vec3 op = p - r.o;
	float t = dot(op, r.d);
	if (t < 0.0) {
		return vec3(0.0);
	}

	vec3 closest = r.o + r.d * t - p;
	float d2 = dot(closest, closest);

	float size = 0.12 + 0.55 * sqrt(brightness);
	float s2 = size * size;

	if (d2 > s2 * 900.0) {
		return vec3(0.0);
	}

	float core = core_gain * brightness / (d2 / s2 + 0.012);
	float halo = halo_gain * brightness / (d2 / (s2 * 20.0) + 0.08);

	float sx = dot(closest, ray_right);
	float sy = dot(closest, ray_up);

	float spikes = 0.0;
	if (brightness > 0.05) {
		float spike1 = max(0.0, 1.0 - abs(sx * sy * SPIKE_LENGTH / s2));
		vec2 rot45 = vec2(sx + sy, sx - sy) * 0.7071;
		float spike2 = max(0.0, 1.0 - abs(rot45.x * rot45.y * SPIKE_LENGTH * 0.6 / s2));
		spikes = (spike1 + spike2 * 0.4) * spike_gain * brightness;
		spikes *= smoothstep(size * 25.0, size * 2.0, sqrt(d2));
	}

	return color * (core + halo + spikes);
}

float hash3d(vec3 p) {
	p = fract(p * vec3(0.1031, 0.1030, 0.0973));
	p += dot(p, p.yxz + 33.33);
	return fract((p.x + p.y) * p.z);
}

float noise3d(vec3 p) {
	vec3 i = floor(p);
	vec3 f = fract(p);
	f = f * f * (3.0 - 2.0 * f);
	return mix(
		mix(mix(hash3d(i), hash3d(i + vec3(1.0, 0.0, 0.0)), f.x),
			mix(hash3d(i + vec3(0.0, 1.0, 0.0)), hash3d(i + vec3(1.0, 1.0, 0.0)), f.x), f.y),
		mix(mix(hash3d(i + vec3(0.0, 0.0, 1.0)), hash3d(i + vec3(1.0, 0.0, 1.0)), f.x),
			mix(hash3d(i + vec3(0.0, 1.0, 1.0)), hash3d(i + vec3(1.0, 1.0, 1.0)), f.x), f.y),
		f.z);
}

float dustDensity(vec3 p) {
	float d = 0.01;
	d += noise3d(p * 0.35) * 0.50;
	d += noise3d(p * 0.70 + 3.7) * 0.33;
	d += noise3d(p * 1.50 + 7.1) * 0.22;

	float r = length(p - cluster_center) / dust_cloud_radius;
	d *= smoothstep(1.0, 0.3, r);

	return max(d - 0.25, 0.05);
}

vec3 renderDustNebula(Ray r, float spread) {
	vec3 oc = r.o - cluster_center;
	float b = dot(oc, r.d);
	float c = dot(oc, oc) - dust_cloud_radius * dust_cloud_radius;
	float h = b * b - c;
	if (h < 0.0) {
		return vec3(0.0);
	}

	float root = sqrt(h);
	float tNear = max(-b - root, 0.0);
	float tFar = -b + root;
	if (tFar < 0.0) {
		return vec3(0.0);
	}

	float path_len = max(tFar - tNear, 0.0);
	float tMid = tNear + path_len * 0.42;
	vec3 pos = r.o + r.d * tMid;
	float center_falloff = smoothstep(dust_cloud_radius, dust_cloud_radius * 0.12, length(pos - cluster_center));
	float dust = dustDensity(pos) * center_falloff;
	if (dust <= 0.001) {
		return vec3(0.0);
	}

	vec3 light = vec3(0.0);
	for (int s = 0; s < NEBULA_ILLUMINATORS; s++) {
		vec3 star_pos = clusterStarPos(STAR_POS[s], spread);
		vec3 toStar = star_pos - pos;
		float dist2 = dot(toStar, toStar);
		light += STAR_COLOR[s] * STAR_BRIGHT[s] / (dist2 + 0.75);
	}

	float thickness = min(path_len, dust_cloud_radius) * 0.18;
	vec3 scattered = light * vec3(0.50, 0.72, 1.00) * dust * thickness;
	return scattered * nebula_strength;
}

Ray buildCameraRay(vec2 uv, float time, vec2 resolution) {
	vec3 camPos;
	vec3 lookAt = cluster_center;

	if (i_mouse.x > 0.0) {
		vec2 m = i_mouse.xy / resolution.xy;
		float yaw = m.x * TAU;
		float pitch = (m.y - 0.5) * PI * 0.98;
		vec3 orbitDir = vec3(
			sin(yaw) * cos(pitch),
			sin(pitch),
			cos(yaw) * cos(pitch));
		camPos = cluster_center + orbitDir * orbit_radius;
	} else if (camera_mode == 0) {
		vec3 galX = normalize(GAL_CENTER - GAL_NORTH * dot(GAL_CENTER, GAL_NORTH));
		vec3 galY = normalize(cross(GAL_NORTH, galX));
		float a = time * 0.35;
		vec3 planeDir = normalize(cos(a) * galX + sin(a) * galY);
		camPos = cluster_center
			+ planeDir * orbit_radius
			+ GAL_NORTH * orbit_radius * camera_height;
		lookAt = cluster_center + planeDir * orbit_radius * look_lead;
	} else if (camera_mode == 1) {
		float a = time * 0.35;
		camPos = cluster_center + vec3(
			sin(a) * orbit_radius,
			camera_height * orbit_radius * 0.35,
			cos(a) * orbit_radius);
	} else if (camera_mode == 2) {
		float a = time * 0.22;
		float drift = drift_amount * orbit_radius;
		camPos = cluster_center + vec3(
			sin(a) * drift,
			sin(a * 0.7 + 1.2) * drift * 0.45 + camera_height * orbit_radius * 0.15,
			cos(a * 0.85) * drift * 0.65 + orbit_radius * 0.88);
	} else if (camera_mode == 4) {
		float camT = time * 0.35;
		float lookT = time * 0.35 * wander_speed;
		float r = orbit_radius;
		float wander = wander_amount * r;

		camPos = cluster_center + vec3(
			sin(camT) * r,
			sin(camT * 0.58 + 0.9) * r * (0.28 + camera_height * 0.35),
			cos(camT * 0.82) * r * 0.92);

		lookAt = cluster_center + vec3(
			cos(lookT * 0.71 + 1.4) * wander,
			sin(lookT * 0.43 + 0.6) * wander * (0.25 + camera_height * 0.2),
			sin(lookT * 0.89 + 2.0) * wander * 0.85);
	} else {
		camPos = cluster_center + vec3(
			0.0,
			camera_height * orbit_radius * 0.2,
			orbit_radius);
	}

	return setupCamera(uv, camPos, lookAt, camera_zoom, WORLD_UP);
}

void fragment() {
	vec2 i_resolution = 1.0 / SCREEN_PIXEL_SIZE;
	vec2 fragCoord = UV * i_resolution;
	vec2 uv = fragCoord.xy / i_resolution.xy - 0.5;
	uv.y *= i_resolution.y / i_resolution.x;

	float time = (iTime * time_scale + time_offset) * orbit_speed;
	Ray r = buildCameraRay(uv, time, i_resolution);
	float spread = clusterSpread(time);

	vec3 col = vec3(0.0);

	col += renderDustNebula(r, spread);

	vec3 up_hint = abs(dot(r.d, WORLD_UP)) > 0.999 ? vec3(0.0, 0.0, 1.0) : WORLD_UP;
	vec3 ray_right = normalize(cross(r.d, up_hint));
	vec3 ray_up = normalize(cross(ray_right, r.d));
	for (int i = 0; i < NUM_STARS; i++) {
		vec3 star_pos = clusterStarPos(STAR_POS[i], spread);
		col += renderStar(r, ray_right, ray_up, star_pos, STAR_BRIGHT[i], STAR_COLOR[i]);
	}

	col *= exposure;
	col = col / (col + 1.0);

	COLOR = vec4(col, 1.0);
}
