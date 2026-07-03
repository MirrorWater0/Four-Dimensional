shader_type canvas_item;
render_mode blend_add;

uniform float ease = 0.005;
uniform float modulo_width :hint_range(0.0, 0.1, 0.01) = 0.02;
uniform float width :hint_range(0.0, 1.0, 0.1) = 0.0;
uniform float ripple_speed = 0.03;
uniform vec2 card_size = vec2(240.0, 370.0);
uniform vec2 effect_margin = vec2(32.0, 32.0);
uniform float corner_radius_px = 8.0;
uniform float glow_radius_px = 30.0;

float rounded_box_sdf(vec2 p, vec2 half_size, float radius) {
	vec2 q = abs(p) - half_size + radius;
	return min(max(q.x, q.y), 0.0) + length(max(q, 0.0)) - radius;
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
	float outside_mask = smoothstep(-6.0, 0.0, sdf);

	float visible = smoothstep(0.001, 0.075, width);
	float outer_glow = (1.0 - smoothstep(0.0, glow_radius_px, outside_dist)) * outside_mask;
	float rim = 1.0 - smoothstep(0.0, 4.0, edge_dist);
	float sdf_alpha = pow(clamp(outer_glow, 0.0, 1.0), 0.18);

	float ripple_phase = mod(TIME * ripple_speed + sdf_alpha, modulo_width);
	float front_ease = smoothstep(0.0, ease, ripple_phase);
	float ease_amount = front_ease * modulo_width;
	float brightness = sdf_alpha - ripple_phase + ease_amount;
	float ripple = smoothstep(1.0 - width, width + (1.0 - width), brightness);

	COLOR.a = clamp((outer_glow * 0.28 + ripple * 0.72 + rim * 0.55) * visible, 0.0, 1.0);
	COLOR.rgb = vec3(1.0);
}
