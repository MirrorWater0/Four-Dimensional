shader_type canvas_item;
render_mode blend_mix;

uniform vec4 smoke_tint : source_color = vec4(0.9, 0.92, 0.95, 1.0);
uniform float alpha_gain : hint_range(1.0, 6.0) = 3.2;
uniform float alpha_power : hint_range(0.2, 1.2) = 0.62;
uniform float brightness : hint_range(0.8, 2.5) = 1.35;
uniform float edge_cut : hint_range(0.0, 0.3) = 0.015;
uniform float core_boost : hint_range(0.0, 1.5) = 0.55;

void fragment() {
	vec4 tex = texture(TEXTURE, UV);
	float lum = dot(tex.rgb, vec3(0.299, 0.587, 0.114));
	float density = max(tex.a, lum);
	density = max(density - edge_cut, 0.0) / max(1.0 - edge_cut, 0.001);
	density = mix(density, sqrt(density), core_boost);
	float alpha = pow(density, alpha_power) * alpha_gain;
	alpha = clamp(alpha, 0.0, 1.0);
	vec3 rgb = mix(tex.rgb, smoke_tint.rgb, 0.42) * brightness;
	COLOR = vec4(rgb, alpha) * COLOR;
}
