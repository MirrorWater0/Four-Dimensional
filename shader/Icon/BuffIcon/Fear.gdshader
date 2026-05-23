shader_type canvas_item;

uniform vec4 eye_color : source_color = vec4(0.96, 0.96, 0.98, 1.0);
uniform vec4 pupil_color : source_color = vec4(0.85, 0.18, 0.18, 1.0);
uniform vec4 slit_color : source_color = vec4(0.08, 0.02, 0.02, 1.0);
uniform float eye_width : hint_range(0.15, 0.48) = 0.38;
uniform float eye_height : hint_range(0.08, 0.32) = 0.22;
uniform float pupil_radius : hint_range(0.02, 0.18) = 0.09;
uniform float smoothness : hint_range(0.001, 0.08) = 0.012;

void fragment() {
	vec2 uv = UV - vec2(0.5);
    float aa = max(fwidth(length(uv)), 0.0015);
    float edge = max(smoothness, aa);

	float eye_mask = 1.0 - smoothstep(
		1.0 - edge,
		1.0,
		(uv.x * uv.x) / (eye_width * eye_width) + (uv.y * uv.y) / (eye_height * eye_height)
	);

	vec2 pupil_uv = uv / vec2(1.0, 0.95);
	float pupil = 1.0 - smoothstep(
		pupil_radius - edge,
		pupil_radius,
		length(pupil_uv)
	);

	float slit = 1.0 - smoothstep(0.012 - aa, 0.032 + aa, abs(uv.x) + abs(uv.y) * 0.18);
	slit *= pupil;

	vec4 color = vec4(0.0);
	color = mix(color, eye_color, eye_mask);
	color = mix(color, pupil_color, pupil);
	color = mix(color, slit_color, slit);
	COLOR *= color;
}
