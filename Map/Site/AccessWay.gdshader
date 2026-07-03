shader_type canvas_item;

uniform vec4 route_color : source_color = vec4(0.22, 0.58, 0.95, 1.0);
uniform vec4 glow_color : source_color = vec4(0.72, 0.94, 1.0, 1.0);
uniform float line_width : hint_range(0.02, 0.5) = 0.12;
uniform float glow_width : hint_range(0.05, 0.8) = 0.42;
uniform float flow_speed : hint_range(0.0, 4.0) = 0.65;
uniform float segment_density : hint_range(1.0, 24.0) = 9.0;

void fragment() {
	vec2 uv = UV;
	float source_alpha = COLOR.a;
	float fill_strength = smoothstep(0.45, 0.95, source_alpha);
	float route_strength = smoothstep(0.02, 0.28, source_alpha);

	float center_dist = abs(uv.y - 0.5);
	float px = max(fwidth(center_dist), 0.001);
	float lane = smoothstep(line_width + px, line_width - px, center_dist);
	float glow = smoothstep(glow_width, 0.0, center_dist);

	float end_fade = smoothstep(0.0, 0.08, uv.x) * smoothstep(1.0, 0.92, uv.x);
	float wave = sin((uv.x * segment_density - TIME * flow_speed) * 6.2831853) * 0.5 + 0.5;
	float pulse = smoothstep(0.78, 1.0, wave) * fill_strength;
	float core = lane * (0.28 * route_strength + 0.82 * fill_strength);

	vec3 color = route_color.rgb * glow * (0.16 + 0.28 * route_strength);
	color += glow_color.rgb * core;
	color += glow_color.rgb * pulse * lane * 0.9;

	float alpha = (glow * 0.22 * route_strength + core + pulse * lane * 0.48) * end_fade;
	COLOR = vec4(color, clamp(alpha, 0.0, 1.0) * route_color.a);
}
