shader_type canvas_item;

uniform vec4 line_color : source_color = vec4(0.90, 0.97, 1.00, 1.0);
uniform vec4 accent_color : source_color = vec4(0.34, 0.86, 1.00, 1.0);
uniform vec4 fill_color : source_color = vec4(0.03, 0.16, 0.24, 0.18);

uniform float border_size : hint_range(0.004, 0.08) = 0.018;
uniform float inner_line_offset : hint_range(0.01, 0.18) = 0.06;
uniform float inner_line_size : hint_range(0.001, 0.03) = 0.004;
uniform float corner_cut : hint_range(0.02, 0.24) = 0.085;
uniform float corner_length : hint_range(0.03, 0.28) = 0.14;
uniform float glow_size : hint_range(0.01, 0.12) = 0.045;
uniform float pulse_speed : hint_range(0.0, 6.0) = 1.5;
uniform float sweep_speed : hint_range(0.0, 3.0) = 0.3;
uniform float sweep_width : hint_range(0.02, 0.25) = 0.09;
uniform float ring_flow_speed : hint_range(0.0, 4.0) = 0.55;
uniform float ring_flow_width : hint_range(0.04, 0.45) = 0.18;
uniform float ring_trail_strength : hint_range(0.0, 1.5) = 0.7;
uniform float ring_glow_strength : hint_range(0.0, 2.5) = 0.95;
uniform float ring_aspect : hint_range(0.2, 1.5) = 0.56;

const float TAU_VALUE = 6.28318530718;

float line_mask(float value, float center, float half_width) {
	float aa = max(fwidth(value), 0.0007);
	return 1.0 - smoothstep(half_width - aa, half_width + aa, abs(value - center));
}

float wrapped_distance(float a, float b) {
	float d = abs(a - b);
	return min(d, 1.0 - d);
}

void fragment() {
	vec4 modulate_color = COLOR;
	vec2 uv = UV;
	vec2 edge_uv = min(uv, 1.0 - uv);
	float edge = min(edge_uv.x, edge_uv.y);

	float aa_diag = max(fwidth(edge_uv.x + edge_uv.y), 0.0007);
	float corner_keep = smoothstep(corner_cut - aa_diag, corner_cut + aa_diag, edge_uv.x + edge_uv.y);

	float outer_frame = (1.0 - smoothstep(border_size, border_size + max(fwidth(edge), 0.0007), edge)) * corner_keep;
	float inner_frame = line_mask(edge, border_size + inner_line_offset, inner_line_size) * corner_keep;

	float brace_vertical = line_mask(edge_uv.x, border_size * 0.7, inner_line_size * 1.3);
	brace_vertical *= 1.0 - smoothstep(corner_length, corner_length + max(fwidth(edge_uv.y), 0.0007), edge_uv.y);
	float brace_horizontal = line_mask(edge_uv.y, border_size * 0.7, inner_line_size * 1.3);
	brace_horizontal *= 1.0 - smoothstep(corner_length, corner_length + max(fwidth(edge_uv.x), 0.0007), edge_uv.x);
	float corner_brace = max(brace_vertical, brace_horizontal) * corner_keep;

	float diagonal_cap = line_mask(edge_uv.x + edge_uv.y, corner_cut + border_size * 0.32, inner_line_size * 1.7);
	diagonal_cap *= 1.0 - smoothstep(corner_length * 1.05, corner_length * 1.5, max(edge_uv.x, edge_uv.y));

	float panel_mask = smoothstep(border_size + inner_line_offset * 0.45, border_size + inner_line_offset * 0.95, edge) * corner_keep;
	float center_fade = pow(1.0 - abs(uv.y * 2.0 - 1.0), 1.6);
	float fill = panel_mask * (0.45 + center_fade * 0.55);

	float pulse = 0.88 + 0.12 * sin(TIME * pulse_speed);
	float sweep_phase = fract(TIME * sweep_speed);
	float sweep_line = uv.y - (1.05 - sweep_phase + (uv.x - 0.5) * 0.55);
	float sweep = exp(-pow(sweep_line / max(sweep_width, 0.001), 2.0));
	sweep *= outer_frame + inner_frame * 0.7 + corner_brace * 1.2;

	float glow_outer = 1.0 - smoothstep(0.0, glow_size, edge);
	glow_outer *= corner_keep;

	float scan = 0.92 + 0.08 * sin(uv.y * 240.0 + TIME * 2.1 + uv.x * 8.0);

	vec2 orbit_uv = uv - vec2(0.5);
	orbit_uv.y *= ring_aspect;
	float orbit_angle = atan(orbit_uv.y, orbit_uv.x);
	float orbit_phase = fract(orbit_angle / TAU_VALUE + 1.0);
	float orbit_head = fract(TIME * ring_flow_speed);
	float orbit_distance = wrapped_distance(orbit_phase, orbit_head);
	float orbit_core = exp(-pow(orbit_distance / max(ring_flow_width * 0.22, 0.001), 2.0));
	float orbit_tail_phase = fract(orbit_head - orbit_phase + 1.0);
	float orbit_tail = smoothstep(ring_flow_width, 0.0, orbit_tail_phase) * (1.0 - orbit_core * 0.4);
	float orbit_lane = max(outer_frame, inner_frame * 0.5 + corner_brace * 0.8 + diagonal_cap * 0.45);
	float orbit_glow_lane = max(glow_outer, orbit_lane);
	float orbit_flicker = 0.84 + 0.16 * sin(TIME * 6.0 + orbit_phase * TAU_VALUE * 4.0);
	float ring_flow = orbit_lane * (orbit_core + orbit_tail * ring_trail_strength) * orbit_flicker;
	float ring_glow = orbit_glow_lane * (orbit_core * ring_glow_strength + orbit_tail * ring_trail_strength * 0.45);

	vec3 rgb = vec3(0.0);
	rgb += fill_color.rgb * fill * scan;
	rgb += line_color.rgb * outer_frame * pulse;
	rgb += mix(line_color.rgb, accent_color.rgb, 0.5) * inner_frame * 0.9;
	rgb += accent_color.rgb * corner_brace * (0.8 + pulse * 0.25);
	rgb += mix(line_color.rgb, accent_color.rgb, 0.75) * diagonal_cap * 0.85;
	rgb += accent_color.rgb * sweep * 0.8;
	rgb += accent_color.rgb * glow_outer * 0.2;
	rgb += mix(accent_color.rgb, vec3(1.0), 0.38) * ring_flow * 1.15;
	rgb += accent_color.rgb * ring_glow * 0.42;

	float alpha = 0.0;
	alpha += fill * fill_color.a;
	alpha = max(alpha, outer_frame * line_color.a);
	alpha = max(alpha, inner_frame * line_color.a * 0.85);
	alpha = max(alpha, corner_brace * accent_color.a);
	alpha = max(alpha, diagonal_cap * accent_color.a * 0.9);
	alpha = max(alpha, glow_outer * 0.22);
	alpha = max(alpha, ring_flow * accent_color.a * 0.95);
	alpha = max(alpha, ring_glow * 0.2);
	alpha = clamp(alpha, 0.0, 1.0);

	COLOR = vec4(rgb, alpha) * modulate_color;
}
