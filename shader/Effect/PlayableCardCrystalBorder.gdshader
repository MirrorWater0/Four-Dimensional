shader_type canvas_item;
render_mode blend_add;

uniform float highlight_enabled : hint_range(0.0, 5.0) = 0.0;
uniform float highlight_strength : hint_range(0.0, 4.0) = 1.0;
uniform float effect_opacity : hint_range(0.0, 1.0) = 0.48;
uniform vec4 crystal_color : source_color = vec4(0.42, 0.95, 1.0, 0.72);
uniform vec4 inner_color : source_color = vec4(0.90, 1.0, 1.0, 0.32);
uniform float rim_power : hint_range(0.0, 4.0) = 1.15;
uniform float glow_power : hint_range(0.0, 4.0) = 1.8;
uniform float inner_glow_power : hint_range(0.0, 2.0) = 0.18;
uniform float effect_margin_pixels : hint_range(0.0, 64.0) = 0.0;
uniform float corner_radius : hint_range(0.0, 0.5) = 0.085;
uniform float border_width : hint_range(0.001, 0.2) = 0.03;
uniform float glow_width : hint_range(0.001, 0.3) = 0.055;
uniform float shimmer_speed : hint_range(0.0, 4.0) = 0.45;
uniform float facet_flash_contrast : hint_range(0.5, 8.0) = 3.2;
uniform vec2 cell_count = vec2(5.0, 8.0);
uniform float cell_min_brightness : hint_range(0.0, 1.0) = 0.38;
uniform float cell_edge_thickness : hint_range(0.0, 0.18) = 0.035;
uniform float cell_edge_softness : hint_range(0.001, 0.12) = 0.018;

float hash11(float value) {
	return fract(sin(value * 127.1) * 43758.5453123);
}

vec2 hash22(vec2 p) {
	p = vec2(dot(p, vec2(127.1, 311.7)), dot(p, vec2(269.5, 183.3)));
	return fract(sin(p) * 43758.5453123);
}

vec3 mirror_cell_light(vec2 card_uv) {
	vec2 v_uv = card_uv * max(cell_count, vec2(1.0));
	vec2 i_uv = floor(v_uv);
	vec2 f_uv = fract(v_uv);
	vec2 cell_id = i_uv;
	vec2 closest_rel_point = vec2(0.0);
	float min_dist = 8.0;

	for (int j = -1; j <= 1; j++) {
		for (int i = -1; i <= 1; i++) {
			vec2 g = vec2(float(i), float(j));
			vec2 p_pos = hash22(i_uv + g);
			vec2 r = g + p_pos - f_uv;
			float d = dot(r, r);
			if (d < min_dist) {
				min_dist = d;
				cell_id = i_uv + g;
				closest_rel_point = g + p_pos;
			}
		}
	}

	float edge_dist = 8.0;
	for (int j = -2; j <= 2; j++) {
		for (int i = -2; i <= 2; i++) {
			vec2 g = vec2(float(i), float(j));
			vec2 p_pos = hash22(i_uv + g);
			vec2 r = g + p_pos;
			if (length(closest_rel_point - r) > 0.0001) {
				float d = dot(0.5 * (closest_rel_point + r) - f_uv, normalize(r - closest_rel_point));
				edge_dist = min(edge_dist, d);
			}
		}
	}

	float phase = hash11(dot(cell_id, vec2(13.17, 41.83))) * 6.2831853;
	float speed = mix(0.72, 1.38, hash11(dot(cell_id, vec2(7.31, 19.57))));
	float wave = 0.5 + 0.5 * sin(TIME * shimmer_speed * speed + phase);
	float animated_light = pow(wave, facet_flash_contrast * 0.35);
	float cell_light = mix(cell_min_brightness, 1.0, animated_light);
	cell_light *= mix(0.78, 1.18, hash11(dot(cell_id, vec2(29.11, 5.73))));

	float cell_edge = 1.0 - smoothstep(
		cell_edge_thickness,
		cell_edge_thickness + cell_edge_softness,
		edge_dist
	);
	return vec3(cell_light, cell_edge, min_dist);
}

float rounded_rect_sdf(vec2 p, vec2 half_size, float radius) {
	vec2 q = abs(p) - half_size + vec2(radius);
	return length(max(q, vec2(0.0))) + min(max(q.x, q.y), 0.0) - radius;
}

void fragment() {
	vec2 card_size = vec2(240.0, 370.0);
	vec2 effect_size = card_size + vec2(effect_margin_pixels * 2.0);
	vec2 card_uv = (UV * effect_size - vec2(effect_margin_pixels)) / card_size;
	vec2 aspect = card_size / card_size.y;
	vec2 p = (UV * effect_size - effect_size * 0.5) / card_size.y;
	vec2 half_size = 0.5 * aspect - vec2(0.018);
	float radius = corner_radius;
	float sdf = rounded_rect_sdf(p, half_size, radius);
	float edge_distance = abs(sdf);
	float border = 1.0 - smoothstep(border_width, border_width + 0.004, edge_distance);
	float outer_distance = max(sdf - border_width, 0.0);
	float outer_glow = pow(1.0 - smoothstep(0.0, glow_width, outer_distance), 2.6) * step(border_width, sdf);
	float edge_halo = pow(1.0 - smoothstep(0.0, glow_width * 0.42, edge_distance), 3.0);
	float inner_distance = max(-sdf - border_width, 0.0);
	float inner_glow = pow(1.0 - smoothstep(0.0, glow_width * 0.12, inner_distance), 3.0) * step(border_width, -sdf);

	vec3 cell_info = mirror_cell_light(card_uv);
	float facet_light = cell_info.x;
	float facet_edge = cell_info.y;
	float crystal =
		border * rim_power * (0.16 + facet_light * 1.12 + facet_edge * 0.42)
		+ outer_glow * glow_power * (0.16 + facet_light * 0.54 + facet_edge * 0.22)
		+ edge_halo * glow_power * (0.08 + facet_light * 0.20 + facet_edge * 0.14)
		+ inner_glow * inner_glow_power * (facet_light + facet_edge * 0.5);
	crystal *= highlight_enabled * highlight_strength * effect_opacity;

	float color_mix = clamp(facet_light * 0.45 + facet_edge * 0.3, 0.0, 1.0);
	vec3 color = mix(crystal_color.rgb, inner_color.rgb, color_mix);
	float alpha = clamp(crystal, 0.0, 1.0) * mix(crystal_color.a, inner_color.a, color_mix);
	COLOR = vec4(color, alpha);
}
