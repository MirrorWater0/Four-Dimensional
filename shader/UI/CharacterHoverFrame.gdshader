shader_type canvas_item;

uniform vec4 line_color : source_color = vec4(0.90, 0.97, 1.00, 1.0);
uniform vec4 accent_color : source_color = vec4(0.34, 0.86, 1.00, 1.0);
uniform vec4 fill_color : source_color = vec4(0.03, 0.16, 0.24, 0.08);

uniform float border_size : hint_range(0.004, 0.08) = 0.018;
uniform float inner_line_offset : hint_range(0.01, 0.18) = 0.06;
uniform float inner_line_size : hint_range(0.001, 0.03) = 0.004;
uniform float corner_cut : hint_range(0.02, 0.24) = 0.085;
uniform float corner_length : hint_range(0.03, 0.28) = 0.14;
uniform float glow_size : hint_range(0.01, 0.12) = 0.032;

float line_mask(float value, float center, float half_width) {
	float aa = max(fwidth(value), 0.0007);
	return 1.0 - smoothstep(half_width - aa, half_width + aa, abs(value - center));
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

	float glow_outer = 1.0 - smoothstep(0.0, glow_size, edge);
	glow_outer *= corner_keep;

	vec3 rgb = vec3(0.0);
	rgb += fill_color.rgb * fill;
	rgb += line_color.rgb * outer_frame;
	rgb += mix(line_color.rgb, accent_color.rgb, 0.5) * inner_frame * 0.9;
	rgb += accent_color.rgb * corner_brace * 1.05;
	rgb += mix(line_color.rgb, accent_color.rgb, 0.75) * diagonal_cap * 0.85;
	rgb += accent_color.rgb * glow_outer * 0.08;

	float alpha = 0.0;
	alpha += fill * fill_color.a;
	alpha = max(alpha, outer_frame * line_color.a);
	alpha = max(alpha, inner_frame * line_color.a * 0.85);
	alpha = max(alpha, corner_brace * accent_color.a);
	alpha = max(alpha, diagonal_cap * accent_color.a * 0.9);
	alpha = max(alpha, glow_outer * 0.1);
	alpha = clamp(alpha, 0.0, 1.0);

	COLOR = vec4(rgb, alpha) * modulate_color;
}
