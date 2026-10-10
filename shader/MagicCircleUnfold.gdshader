shader_type canvas_item;
render_mode unshaded, blend_add;

uniform vec4 primary_color : source_color = vec4(0.32, 0.78, 1.0, 1.0);
uniform vec4 accent_color : source_color = vec4(0.72, 0.48, 1.0, 1.0);
uniform float effect_time = 0.0;
uniform float unfold_seconds = 1.05;
uniform float hold_seconds = 1.25;
uniform float fade_seconds = 0.65;
uniform float brightness : hint_range(0.0, 3.0) = 1.0;
uniform float rotation_speed = 0.12;



float hash(float value) {
	return fract(sin(value * 127.1 + 311.7) * 43758.5453);
}

mat2 rotate(float angle) {
	return mat2(vec2(cos(angle), sin(angle)), vec2(-sin(angle), cos(angle)));
}

float stroke(float distance, float width) {
	float aa = max(fwidth(distance), 0.001);
	float core = 1.0 - smoothstep(width, width + aa, distance);
	float halo = exp(-distance * 105.0) * 0.30;
	return core + halo;
}

float segment_distance(vec2 p, vec2 a, vec2 b) {
	vec2 direction = b - a;
	float t = clamp(dot(p - a, direction) / max(dot(direction, direction), 0.00001), 0.0, 1.0);
	return length(p - a - direction * t);
}

float ring(vec2 p, float radius, float width) {
	return stroke(abs(length(p) - radius), width);
}

// Each sector contains one geometric glyph, with alternate branches and diamonds.
float rune(vec2 p, float index) {
	float d = segment_distance(p, vec2(0.0, -0.028), vec2(0.0, 0.029));
	d = min(d, segment_distance(p, vec2(-0.017, -0.015), vec2(0.0, -0.002)));
	d = min(d, segment_distance(p, vec2(0.0, -0.002), vec2(0.019, -0.022)));
	if (mod(index, 3.0) < 1.0) {
		d = min(d, segment_distance(p, vec2(-0.017, 0.011), vec2(0.018, 0.011)));
	} else if (mod(index, 3.0) < 2.0) {
		d = min(d, segment_distance(p, vec2(0.0, 0.002), vec2(-0.018, 0.020)));
		d = min(d, segment_distance(p, vec2(-0.018, 0.020), vec2(0.0, 0.029)));
	} else {
		d = min(d, segment_distance(p, vec2(0.0, 0.003), vec2(0.018, 0.016)));
		d = min(d, segment_distance(p, vec2(0.018, 0.016), vec2(0.0, 0.029)));
	}
	return stroke(d, 0.0015);
}

void fragment() {
	vec2 p = UV * 2.0 - 1.0;
	float time = max(effect_time, 0.0);
	float progress = clamp(time / max(unfold_seconds, 0.05), 0.0, 1.0);
	float appear = smoothstep(0.0, 0.10, time);
	float fade = 1.0 - smoothstep(unfold_seconds + hold_seconds,
		unfold_seconds + hold_seconds + max(fade_seconds, 0.05), time);
	float expansion = 1.0 - pow(1.0 - progress, 3.0);
	float size = mix(0.08, 1.0, expansion);
	vec2 q = p / max(size, 0.08);
	float radius = length(q);
	float angle = mod(atan(q.y, q.x) + TAU, TAU);
	float spin = time * rotation_speed;
	float reveal = smoothstep(0.06, 0.48, progress);
	float sweep = 1.0 - smoothstep(progress * TAU - 0.12, progress * TAU + 0.06, angle);
	sweep = mix(sweep, 1.0, smoothstep(0.94, 1.0, progress));

	float outer = ring(q, 0.755, 0.0020) + ring(q, 0.723, 0.0011) * 0.65;
	float inner = ring(q, 0.572, 0.0017) + ring(q, 0.554, 0.0008) * 0.6;
	float ticks_angle = angle + spin * 0.5;
	float ticks = stroke(abs(sin(ticks_angle * 48.0)) * max(radius, 0.01) / 48.0, 0.0010);
	ticks *= smoothstep(0.689, 0.695, radius) * (1.0 - smoothstep(0.715, 0.721, radius));
	float rings = (outer + inner + ticks * 0.68) * sweep * reveal;

	float glyph_angle = mod(angle - spin + TAU, TAU);
	float sector = floor(glyph_angle / TAU * 24.0);
	float local_angle = (fract(glyph_angle / TAU * 24.0) - 0.5) * TAU / 24.0;
	vec2 glyph_p = vec2(sin(local_angle) * radius, cos(local_angle) * radius - 0.636);
	float glyphs = rune(glyph_p, sector);
	glyphs *= smoothstep(0.59, 0.60, radius) * (1.0 - smoothstep(0.675, 0.685, radius));
	float glyph_on = smoothstep(0.36 + sector / 24.0 * 0.39, 0.43 + sector / 24.0 * 0.39, progress);
	glyphs *= glyph_on;

	vec2 star_p = rotate(-spin * 0.7) * q;
	float star_distance = 10.0;
	float hex_distance = 10.0;
	for (int i = 0; i < 6; i++) {
		float a = float(i) * TAU / 6.0 - TAU / 4.0;
		vec2 start = vec2(cos(a), sin(a)) * 0.49;
		vec2 end = vec2(cos(a + TAU / 3.0), sin(a + TAU / 3.0)) * 0.49;
		star_distance = min(star_distance, segment_distance(star_p, start, end));
		vec2 next = vec2(cos(a + TAU / 6.0), sin(a + TAU / 6.0)) * 0.49;
		hex_distance = min(hex_distance, segment_distance(star_p, start, next));
	}
	float geometry_on = smoothstep(0.35, 0.82, progress);
	float geometry = (stroke(star_distance, 0.0018) + stroke(hex_distance, 0.0008) * 0.45) * geometry_on;
	float core = ring(q, 0.215, 0.0015) + ring(q, 0.185, 0.0009) * 0.65;
	vec2 diamond = rotate(spin) * q;
	core += stroke(abs(abs(diamond.x) + abs(diamond.y) - 0.105) * 0.7071, 0.0017);
	core *= smoothstep(0.0, 0.36, progress);

	// Six satellite seals and connecting rays give the silhouette a clear hierarchy.
	float seals = 0.0;
	for (int i = 0; i < 6; i++) {
		float a = float(i) * TAU / 6.0 - TAU / 4.0 + spin * 0.7;
		vec2 direction = vec2(cos(a), sin(a));
		vec2 seal_p = q - direction * 0.825;
		float local = ring(seal_p, 0.035, 0.0015);
		local += stroke(abs(seal_p.x) + abs(seal_p.y), 0.004) * 0.7;
		local += stroke(segment_distance(q, direction * 0.76, direction * 0.788), 0.0011);
		seals += local * smoothstep(0.52 + float(i) * 0.035, 0.70 + float(i) * 0.035, progress);
	}

	float flash = exp(-pow((time - unfold_seconds) / 0.105, 2.0));
	float shock_radius = 0.76 + max(time - unfold_seconds, 0.0) * 0.36;
	float shock = ring(p, shock_radius, 0.0020) * exp(-max(time - unfold_seconds, 0.0) * 5.0);
	shock *= step(unfold_seconds, time);
	float glow = exp(-length(p) * 8.0) * (0.08 + flash * 0.8);
	glow += exp(-pow((length(p) - 0.69 * size) * 22.0, 2.0)) * 0.045 * reveal;
	float sparkles = 0.0;
	for (int i = 0; i < 20; i++) {
		float seed = float(i) + 1.0;
		float a = hash(seed) * TAU + spin * (0.3 + hash(seed + 9.0));
		float travel = fract(time * (0.16 + hash(seed + 5.0) * 0.1) + hash(seed + 2.0));
		float r = (0.30 + travel * 0.64) * size;
		vec2 spark_p = p - vec2(cos(a), sin(a)) * r;
		float envelope = sin(travel * 3.141593) * smoothstep(0.3, 0.8, progress);
		float spot = exp(-length(spark_p) * 420.0);
		float cross = exp(-abs(spark_p.x) * 650.0 - abs(spark_p.y) * 100.0)
			+ exp(-abs(spark_p.x) * 100.0 - abs(spark_p.y) * 650.0);
		sparkles += (spot + cross * 0.28) * envelope;
	}

	float pulse = 0.94 + 0.06 * sin(time * 3.0);
	vec3 light = primary_color.rgb * (rings + core + shock * 0.75 + glow + sparkles) * primary_color.a;
	light += accent_color.rgb * (geometry * 0.72 + glyphs * 0.92 + seals) * accent_color.a;
	light += vec3(0.68, 0.88, 1.0) * flash * (rings + geometry + core) * 0.6;
	float edge = 1.0 - smoothstep(0.94, 1.0, length(p));
	COLOR = vec4(light * brightness * pulse, appear * fade * edge);
}
