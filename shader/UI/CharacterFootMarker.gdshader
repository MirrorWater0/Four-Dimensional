shader_type canvas_item;

uniform vec4 fill_color : source_color = vec4(0.86, 0.2, 0.2, 0.92);
uniform vec4 frame_color : source_color = vec4(0.94, 0.88, 0.72, 1.0);
uniform float hover_amount : hint_range(0.0, 1.0) = 0.0;

uniform float body_top : hint_range(0.1, 0.9) = 0.34;
uniform float body_bottom : hint_range(0.1, 0.9) = 0.68;
uniform float end_cap_width : hint_range(0.03, 0.2) = 0.09;
uniform float rail_thickness : hint_range(0.01, 0.06) = 0.02;
uniform float step_height : hint_range(0.01, 0.12) = 0.042;
uniform float core_band_height : hint_range(0.08, 0.5) = 0.36;
uniform float cap_glow_width : hint_range(0.01, 0.18) = 0.055;
uniform float shadow_offset : hint_range(0.0, 0.15) = 0.05;

float stroke(float dist, float half_width) {
	float aa = max(fwidth(dist), 0.0015);
	return 1.0 - smoothstep(half_width - aa, half_width + aa, abs(dist));
}

float rect_mask(vec2 uv, vec2 min_corner, vec2 max_corner) {
	return step(min_corner.x, uv.x) * step(uv.x, max_corner.x) * step(min_corner.y, uv.y) * step(uv.y, max_corner.y);
}

void fragment() {
	vec2 uv = UV;
	float top = body_top;
	float bottom = body_bottom;
	float step_y = top - step_height;
	float left = end_cap_width;
	float right = 1.0 - end_cap_width;
	float mid_y = (top + bottom) * 0.5;
	float body_height = max(bottom - top, 0.001);

	float inside_body = rect_mask(uv, vec2(left, top), vec2(right, bottom));
	float inside_cap_left = rect_mask(uv, vec2(0.0, step_y), vec2(left, bottom));
	float inside_cap_right = rect_mask(uv, vec2(right, step_y), vec2(1.0, bottom));
	float shell = max(inside_body, max(inside_cap_left, inside_cap_right));

	float bottom_rail = stroke(uv.y - bottom, rail_thickness * 0.5);
	float top_rail_center = stroke(uv.y - top, rail_thickness * 0.5)
		* step(left + 0.018, uv.x) * step(uv.x, right - 0.018);
	float top_step_left = stroke(uv.y - step_y, rail_thickness * 0.5)
		* step(0.0, uv.x) * step(uv.x, left + 0.018);
	float top_step_right = stroke(uv.y - step_y, rail_thickness * 0.5)
		* step(right - 0.018, uv.x) * step(uv.x, 1.0);
	float vert_left = stroke(uv.x - left, rail_thickness * 0.5)
		* step(step_y, uv.y) * step(uv.y, bottom);
	float vert_right = stroke(uv.x - right, rail_thickness * 0.5)
		* step(step_y, uv.y) * step(uv.y, bottom);
	float inner_left = stroke(uv.x - left, rail_thickness * 0.34)
		* step(top, uv.y) * step(uv.y, bottom);
	float inner_right = stroke(uv.x - right, rail_thickness * 0.34)
		* step(top, uv.y) * step(uv.y, bottom);

	float frame = bottom_rail;
	frame = max(frame, top_rail_center);
	frame = max(frame, top_step_left);
	frame = max(frame, top_step_right);
	frame = max(frame, vert_left);
	frame = max(frame, vert_right);
	frame = max(frame, inner_left);
	frame = max(frame, inner_right);

	vec2 shadow_uv = uv - vec2(0.0, shadow_offset);
	float shadow_body = rect_mask(shadow_uv, vec2(left, top), vec2(right, bottom));
	float shadow_cap_left = rect_mask(shadow_uv, vec2(0.0, step_y), vec2(left, bottom));
	float shadow_cap_right = rect_mask(shadow_uv, vec2(right, step_y), vec2(1.0, bottom));
	float shadow_shell = max(shadow_body, max(shadow_cap_left, shadow_cap_right));

	float plate_shine = clamp(1.0 - abs(uv.y - mid_y) / (body_height * 0.82), 0.0, 1.0);
	float top_fade = clamp((uv.y - step_y) / max(bottom - step_y, 0.001), 0.0, 1.0);
	vec3 plate_rgb = mix(vec3(0.05, 0.07, 0.10), fill_color.rgb, 0.22 + hover_amount * 0.06);
	plate_rgb *= mix(0.74, 0.92, plate_shine);
	plate_rgb *= mix(0.92, 1.0, top_fade);

	float core_half_height = body_height * core_band_height * 0.5;
	float core_top = mid_y - core_half_height;
	float core_bottom = mid_y + core_half_height;
	float core = rect_mask(uv, vec2(left + 0.02, core_top), vec2(right - 0.02, core_bottom));
	float core_vertical = clamp((uv.y - core_top) / max(core_bottom - core_top, 0.001), 0.0, 1.0);
	vec3 core_rgb = mix(fill_color.rgb * 1.28, fill_color.rgb * 0.62, core_vertical);
	core_rgb *= mix(0.96, 1.16, hover_amount);

	float core_highlight = smoothstep(core_top, mid_y, uv.y) * (1.0 - smoothstep(mid_y, core_bottom, uv.y));
	float cap_glow_left = smoothstep(cap_glow_width, 0.0, abs(uv.x - left * 0.48)) * inside_cap_left;
	float cap_glow_right = smoothstep(cap_glow_width, 0.0, abs(uv.x - (1.0 - left * 0.48))) * inside_cap_right;
	float cap_glow = max(cap_glow_left, cap_glow_right);

	vec3 frame_rgb = mix(frame_color.rgb * 0.82, frame_color.rgb * 1.08, hover_amount);
	vec3 shadow_rgb = mix(vec3(0.02, 0.03, 0.05), fill_color.rgb, 0.12);

	vec3 rgb = shadow_rgb * shadow_shell * 0.22;
	rgb = mix(rgb, plate_rgb, shell * 0.56);
	rgb += core_rgb * core * 0.92;
	rgb += fill_color.rgb * core_highlight * core * (0.18 + hover_amount * 0.18);
	rgb += frame_rgb * frame * 0.92;
	rgb += frame_color.rgb * cap_glow * (0.2 + hover_amount * 0.16);

	float alpha = shadow_shell * 0.18;
	alpha = max(alpha, shell * mix(0.56, 0.72, hover_amount));
	alpha = max(alpha, core * mix(0.86, 1.0, hover_amount));
	alpha = max(alpha, frame * frame_color.a * 0.92);
	COLOR = vec4(rgb, alpha) * COLOR;
}
