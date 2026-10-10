shader_type canvas_item;
render_mode unshaded;

uniform vec4 fill_color : source_color = vec4(0.62, 0.63, 0.66, 1.0);
uniform vec4 wall_color : source_color = vec4(0.62, 0.63, 0.66, 1.0);
uniform float hover_amount : hint_range(0.0, 1.0) = 0.0;
uniform vec2 ring_center = vec2(0.5, 0.8956044);
uniform vec2 ring_radius = vec2(0.325, 0.05714286);
uniform vec2 projection_size = vec2(400.0, 364.0);
uniform float ring_thickness : hint_range(0.1, 8.0) = 1.0;
uniform float ring_glow_width : hint_range(0.0, 24.0) = 4.0;
uniform float ring_phase : hint_range(0.0, 6.283185) = 0.0;
uniform float ring_arc_length : hint_range(0.0, 6.283185) = 2.25;
uniform float depth_layer : hint_range(-1.0, 1.0) = -1.0;
uniform float near_ring_alpha : hint_range(0.0, 1.0) = 1.0;
uniform float far_ring_alpha : hint_range(0.0, 1.0) = 0.36;
uniform float highlight_phase : hint_range(0.0, 6.283185) = 0.8;
uniform float highlight_arc_length : hint_range(0.0, 6.283185) = 0.20;
uniform float highlight_alpha : hint_range(0.0, 1.0) = 0.46;
uniform vec2 outer_ring_scale = vec2(1.18, 1.375);
uniform float outer_ring_phase : hint_range(0.0, 6.283185) = 3.1415927;
uniform float outer_ring_arc_length : hint_range(0.0, 6.283185) = 2.25;
uniform float outer_ring_alpha : hint_range(0.0, 1.0) = 0.28;
uniform float projection_glow_alpha : hint_range(0.0, 1.0) = 0.20;
uniform float wall_height : hint_range(0.05, 1.0) = 0.80;
uniform float wall_edge_softness : hint_range(0.001, 0.2) = 0.012;
uniform float wall_fade_power : hint_range(0.1, 4.0) = 1.4;
uniform float wall_alpha : hint_range(0.0, 1.0) = 0.52;
uniform float wall_glow : hint_range(0.0, 1.0) = 0.42;

float arc_mask(float angle, float phase, float arc_length) {
	float relative = mod(angle - phase + 6.283185, 6.283185);
	float end = max(arc_length, 0.001);
	return smoothstep(0.0, 0.035, relative)
		* (1.0 - smoothstep(max(end - 0.035, 0.0), end, relative));
}

float ring_mask(vec2 position, vec2 radius, float phase, float arc_length, float width) {
	float ellipse_length = max(length(position), 0.001);
	vec2 gradient = position / ellipse_length / max(radius * projection_size, vec2(0.001));
	float distance = abs(ellipse_length - 1.0) / max(length(gradient), 0.001);
	float aa = max(fwidth(distance), 0.5);
	return (1.0 - smoothstep(max(width * 0.5 - aa, 0.0), width * 0.5 + aa, distance))
		* arc_mask(atan(position.y, position.x), phase, arc_length);
}

float depth_alpha(vec2 position, float near_alpha, float far_alpha) {
	// UV Y grows downward: the front/lower half of the ellipse is nearer.
	float near_amount = clamp(position.y * 0.5 + 0.5, 0.0, 1.0);
	return mix(far_alpha, near_alpha, near_amount);
}

float ellipse_depth_mask(vec2 position) {
	// The rear half is drawn behind the character and the front half above it.
	float front = smoothstep(-0.02, 0.02, position.y);
	return depth_layer < 0.0 ? 1.0 - front : front;
}

float wall_mask(vec2 uv, vec2 radius, float phase, float arc_length) {
	// Extrude each ellipse arc straight upward: X stays constant at every height.
	float x = (uv.x - ring_center.x) / radius.x;
	float curve = sqrt(max(1.0 - x * x, 0.0));
	float side_mask = 1.0 - smoothstep(radius.x - wall_edge_softness, radius.x,
		abs(uv.x - ring_center.x));
	float front_angle = atan(curve, clamp(x, -1.0, 1.0));
	float back_angle = atan(-curve, clamp(x, -1.0, 1.0));
	float front_base = ring_center.y + radius.y * curve;
	float back_base = ring_center.y - radius.y * curve;
	float front_height = (front_base - uv.y) / max(wall_height, 0.001);
	float back_height = (back_base - uv.y) / max(wall_height, 0.001);
	float front = pow(1.0 - clamp(front_height, 0.0, 1.0), wall_fade_power)
		* smoothstep(-0.008, 0.008, front_base - uv.y)
		* arc_mask(front_angle, phase, arc_length);
	float back = pow(1.0 - clamp(back_height, 0.0, 1.0), wall_fade_power)
		* smoothstep(-0.008, 0.008, back_base - uv.y)
		* arc_mask(back_angle, phase, arc_length);
	return (depth_layer < 0.0 ? back : front) * side_mask;
}

void fragment() {
	vec2 radius = max(ring_radius, vec2(0.001));
	vec2 outer_radius = radius * max(outer_ring_scale, vec2(0.001));
	vec2 position = (UV - ring_center) / radius;
	vec2 outer_position = (UV - ring_center) / outer_radius;
	// Reuse BattleAtmosphere's two broken arcs and soft foot projection.
	float ring = ring_mask(position, radius, ring_phase, ring_arc_length, ring_thickness)
		* ellipse_depth_mask(position);
	float outer = ring_mask(outer_position, outer_radius, ring_phase + outer_ring_phase,
		outer_ring_arc_length, ring_thickness * 0.72) * ellipse_depth_mask(outer_position);
	float ring_depth = depth_alpha(position, near_ring_alpha, far_ring_alpha);
	float outer_depth = depth_alpha(outer_position, near_ring_alpha, far_ring_alpha);
	float halo = ring_mask(position, radius, ring_phase, ring_arc_length,
		ring_thickness + ring_glow_width) * 0.12 * ellipse_depth_mask(position);
	float highlight = max(
		ring_mask(position, radius, ring_phase + highlight_phase, highlight_arc_length,
			ring_thickness * 1.15) * ellipse_depth_mask(position),
		ring_mask(outer_position, outer_radius, ring_phase + outer_ring_phase + highlight_phase,
			highlight_arc_length, ring_thickness * 0.9) * ellipse_depth_mask(outer_position));
	float ground = pow(1.0 - smoothstep(0.0, 1.0,
		length((UV - ring_center) / (radius * vec2(1.35, 1.5)))), 2.0);
	ground *= depth_layer < 0.0 ? 1.0 : 0.0;
	float base_alpha = max(ring * fill_color.a * ring_depth,
		max(outer * outer_ring_alpha * outer_depth, halo * fill_color.a * ring_depth));
	base_alpha = max(base_alpha, ground * projection_glow_alpha);
	float wall = max(wall_mask(UV, radius, ring_phase, ring_arc_length),
		wall_mask(UV, outer_radius, ring_phase + outer_ring_phase, outer_ring_arc_length))
		* hover_amount;
	float light_alpha = clamp(wall * wall_alpha * wall_color.a, 0.0, 1.0);
	float alpha = base_alpha + light_alpha * (1.0 - base_alpha);
	vec3 base_rgb = fill_color.rgb * (1.0 + ring * 0.16);
	base_rgb = mix(base_rgb, fill_color.rgb * 1.45, highlight * highlight_alpha);
	vec3 light_rgb = wall_color.rgb * (1.0 + wall_glow * wall);
	vec3 rgb = (base_rgb * base_alpha + light_rgb * light_alpha * (1.0 - base_alpha))
		/ max(alpha, 0.0001);
	COLOR = vec4(rgb, alpha) * COLOR;
}
