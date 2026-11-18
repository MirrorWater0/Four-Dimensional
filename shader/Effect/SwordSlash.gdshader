shader_type canvas_item;
render_mode unshaded, blend_mix;

uniform sampler2D cut_atlas : source_color, filter_linear, repeat_disable;
uniform sampler2D energy_atlas : source_color, filter_linear, repeat_disable;
uniform vec4 blade_color : source_color = vec4(0.94, 0.99, 1.0, 1.0);
uniform vec4 energy_color : source_color = vec4(1.0, 0.25, 0.29, 1.0);
uniform bool sword_energy = false;
uniform int frame_index : hint_range(0, 5) = 1;
uniform float effect_progress : hint_range(0.0, 1.0) = 0.27;
uniform float visible_amount : hint_range(0.0, 1.0) = 1.0;
uniform vec2 stroke_axis = vec2(0.7222361, -0.6916466);
uniform vec2 surface_span = vec2(1.0, 1.0);
uniform float length_scale = 1.0;
uniform float thickness : hint_range(0.3, 2.0) = 1.0;
uniform float travel_distance : hint_range(0.0, 0.5) = 0.1;
uniform float brightness : hint_range(0.0, 2.0) = 1.0;

void fragment() {
	vec4 tint = COLOR;
	vec2 across = vec2(-stroke_axis.y, stroke_axis.x);
	vec2 p = (UV - vec2(0.5)) * surface_span;
	vec2 local_uv = stroke_axis * dot(p, stroke_axis) / length_scale
		+ across * dot(p, across) / thickness + vec2(0.5);
	float alpha_fade = 1.0;
	vec2 grid;
	if (sword_energy) {
		// Keep the reference's broad, changing silhouette and short forward drift.
		float motion = effect_progress * (2.0 - effect_progress);
		local_uv.x -= motion * travel_distance * length_scale / (1.5 * thickness);
		alpha_fade = 1.0 - smoothstep(0.5, 1.0, effect_progress);
		grid = vec2(3.0, 2.0);
	} else {
		grid = vec2(2.0, 2.0);
	}

	float inside = step(0.0, local_uv.x) * step(local_uv.x, 1.0)
		* step(0.0, local_uv.y) * step(local_uv.y, 1.0);
	float frame = float(frame_index);
	vec2 cell = vec2(mod(frame, grid.x), floor(frame / grid.x));
	vec2 atlas_uv = (clamp(local_uv, vec2(0.001), vec2(0.999)) + cell) / grid;
	vec4 source;
	if (sword_energy) source = texture(energy_atlas, atlas_uv);
	else source = texture(cut_atlas, atlas_uv);

	vec3 color;
	if (sword_energy) {
		// The source flipbook encodes its color ramp in red, rather than final RGB.
		float core = smoothstep(0.503958, 0.556728, source.r);
		color = mix(energy_color.rgb, blade_color.rgb * 1.8, core);
	} else {
		// Preserve the white interior while recoloring the painted red edge.
		float white_core = min(source.g, source.b) / max(source.r, 0.001);
		color = mix(energy_color.rgb, blade_color.rgb, smoothstep(0.2, 0.9, white_core));
		color *= max(source.r, max(source.g, source.b));
	}
	float alpha = source.a * alpha_fade * visible_amount * inside * energy_color.a;
	COLOR = vec4(color * brightness, alpha) * tint;
}
