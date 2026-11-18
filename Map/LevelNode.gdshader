shader_type canvas_item;
render_mode blend_mix;

uniform vec4 ring_color : source_color = vec4(1.0);
uniform float ring_radius : hint_range(0.15, 0.46) = 0.37;
uniform float ring_thickness : hint_range(0.005, 0.08) = 0.035;
uniform float dot_radius : hint_range(0.02, 0.20) = 0.095;
uniform float smoothness_offset : hint_range(0.5, 2.0) = 1.0;

void fragment() {
	vec2 p = UV - vec2(0.5);
	float distance_to_center = length(p);
	float aa = max(fwidth(distance_to_center), 0.0005) * smoothness_offset;
	float half_thickness = ring_thickness * 0.5;

	float outer_ring = 1.0 - smoothstep(
		half_thickness - aa,
		half_thickness + aa,
		abs(distance_to_center - ring_radius)
	);
	float solid_dot = 1.0 - smoothstep(
		dot_radius - aa,
		dot_radius + aa,
		distance_to_center
	);
	float alpha = max(outer_ring, solid_dot) * ring_color.a;

	if (alpha <= 0.001) {
		discard;
	}

	// The center stays filled and shares the outer ring's tint in every state.
	COLOR *= vec4(ring_color.rgb, alpha);
}
