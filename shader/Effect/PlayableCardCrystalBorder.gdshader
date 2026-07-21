shader_type canvas_item;
render_mode blend_add;

uniform float ease = 0.005;
uniform float modulo_width :hint_range(0.0, 0.1, 0.01) = 0.02;
uniform float width :hint_range(0.0, 1.0, 0.1) = 0.0;
uniform float ripple_speed = 0.28;
uniform vec2 card_size = vec2(240.0, 370.0);
uniform vec2 effect_margin = vec2(32.0, 32.0);
uniform float corner_radius_px = 8.0;
uniform float glow_radius_px = 46.0;
uniform sampler2D noise_tex : repeat_enable, filter_linear_mipmap;
uniform vec4 core_color : source_color = vec4(0.72, 0.96, 1.0, 1.0);
uniform vec4 accent_color : source_color = vec4(0.08, 0.42, 1.0, 1.0);
uniform vec4 spark_color : source_color = vec4(0.58, 0.92, 1.0, 1.0);
uniform float pulse_speed = 2.25;
uniform float flame_height_px = 34.0;
uniform float flame_scale = 7.0;
uniform float flame_speed = 0.85;
uniform float flame_turbulence = 0.42;

float rounded_box_sdf(vec2 p, vec2 half_size, float radius) {
	vec2 q = abs(p) - half_size + radius;
	return min(max(q.x, q.y), 0.0) + length(max(q, 0.0)) - radius;
}

float edge_flow_coord(vec2 p, vec2 half_size) {
	vec2 safe_half = max(half_size, vec2(1.0));
	vec2 q = abs(p) / safe_half;
	float vertical_side = step(q.x, q.y);
	float top_bottom_coord = p.x / max(safe_half.x, 1.0);
	float left_right_coord = -p.y / max(safe_half.y, 1.0);
	float side_coord = mix(left_right_coord, top_bottom_coord, vertical_side);
	float y_positive = step(0.0, p.y);
	float x_positive = step(0.0, p.x);
	float y_side_id = 0.5 * (1.0 - y_positive);
	float x_side_id = mix(0.75, 0.25, x_positive);
	float side_id = mix(x_side_id, y_side_id, vertical_side);
	return fract(side_id + side_coord * 0.125 + 0.5);
}

float noise_sample(vec2 uv) {
	return texture(noise_tex, uv).r;
}

float texture_fbm(vec2 uv) {
	float value = noise_sample(uv) * 0.52;
	value += noise_sample(uv * 2.03 + vec2(0.31, 0.17)) * 0.26;
	value += noise_sample(uv * 4.11 + vec2(0.67, 0.43)) * 0.15;
	value += noise_sample(uv * 8.07 + vec2(0.11, 0.79)) * 0.07;
	return value;
}

void fragment() {
	vec2 effect_size = card_size + effect_margin * 2.0;
	vec2 card_px = UV * effect_size - effect_margin;
	vec2 center = card_size * 0.5;
	vec2 p = card_px - center;
	vec2 half_size = max(card_size * 0.5 - vec2(corner_radius_px), vec2(1.0));

	float sdf = rounded_box_sdf(p, half_size, corner_radius_px);
	float outside_dist = max(sdf, 0.0);
	float edge_dist = abs(sdf);
	float outside_mask = smoothstep(-10.0, 2.0, sdf);

	float visible = smoothstep(0.001, 0.18, width);
	float pulse = 0.86 + 0.14 * sin(TIME * pulse_speed);
	float hot_rim = 1.0 - smoothstep(0.0, 2.2, edge_dist);
	float soft_rim = 1.0 - smoothstep(0.0, 8.0, edge_dist);
	float inner_rim = (1.0 - smoothstep(0.0, 6.0, -sdf)) * step(sdf, 0.0);

	float flow_coord = edge_flow_coord(p, half_size);
	vec2 flame_uv = vec2(flow_coord * flame_scale, outside_dist / max(flame_height_px, 1.0));
	vec2 flame_warp = vec2(
		texture_fbm(flame_uv * 0.72 + vec2(TIME * 0.10, -TIME * flame_speed * 0.34)),
		texture_fbm(flame_uv * 0.91 + vec2(-TIME * 0.08, TIME * flame_speed * 0.22))
	) - vec2(0.5);
	float flame_field = texture_fbm(
		flame_uv + flame_warp * flame_turbulence + vec2(TIME * 0.06, -TIME * flame_speed)
	);
	float flame_lift = texture_fbm(vec2(flow_coord * flame_scale * 1.7, -TIME * flame_speed * 1.35));
	float tongue_height = flame_height_px * mix(0.34, 1.18, flame_field);
	tongue_height *= mix(0.78, 1.28, flame_lift);
	float flame_body = 1.0 - smoothstep(tongue_height * 0.20, tongue_height, outside_dist);
	flame_body *= outside_mask;
	float flame_tip = smoothstep(0.42, 0.86, flame_field) * (1.0 - smoothstep(tongue_height * 0.45, tongue_height * 1.08, outside_dist));
	flame_tip *= outside_mask;
	float outer_glow = pow(1.0 - smoothstep(0.0, glow_radius_px, outside_dist), 1.55) * outside_mask;
	float wide_halo = pow(1.0 - smoothstep(0.0, glow_radius_px * 1.65, outside_dist), 2.2) * outside_mask;

	float modulo_safe = max(modulo_width, 0.001);
	float ripple_phase = mod(flow_coord - TIME * ripple_speed + flame_field * 0.18, modulo_safe);
	float front_ease = smoothstep(0.0, max(ease, 0.0001), ripple_phase);
	float ease_amount = front_ease * modulo_safe;
	float brightness = flame_body - ripple_phase + ease_amount;
	float ripple = smoothstep(1.0 - width, width + (1.0 - width), brightness);

	float ember_noise = texture_fbm(vec2(flow_coord * 24.0, outside_dist * 0.045 - TIME * 1.6));
	float ember_gate = texture_fbm(vec2(flow_coord * 51.0 + TIME * 0.7, outside_dist * 0.12));
	float spark = smoothstep(0.86, 0.98, ember_noise) * smoothstep(0.78, 0.96, ember_gate);
	spark *= (flame_body + soft_rim * 0.45) * (1.0 - smoothstep(0.0, flame_height_px * 1.15, outside_dist));

	float energy = (
		wide_halo * 0.16 +
		outer_glow * 0.34 +
		flame_body * 0.56 +
		flame_tip * 0.72 +
		soft_rim * 0.20 +
		hot_rim * 0.82 +
		inner_rim * 0.16 +
		ripple * 0.42 +
		spark * 1.25
	) * visible * pulse;

	vec3 ember_color = mix(accent_color.rgb * 0.45, accent_color.rgb, clamp(flame_body + flame_tip, 0.0, 1.0));
	vec3 energy_color = mix(ember_color, core_color.rgb, clamp(hot_rim + flame_tip * 0.6 + ripple * 0.25, 0.0, 1.0));
	energy_color = mix(energy_color, spark_color.rgb, clamp(spark * 1.25, 0.0, 1.0));
	COLOR.rgb = energy_color;
	COLOR.a = clamp(energy, 0.0, 1.0);
}
