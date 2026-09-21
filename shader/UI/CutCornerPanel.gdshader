shader_type canvas_item;
render_mode unshaded;

uniform vec4 fill_color : source_color = vec4(1.0);
uniform float cut_size_px : hint_range(0.0, 128.0, 0.5) = 18.0;
uniform float anti_alias_strength : hint_range(0.5, 3.0, 0.05) = 1.25;

void fragment() {
	float uv_pixel_x = max(fwidth(UV.x), 0.00001);
	float uv_pixel_y = max(fwidth(UV.y), 0.00001);
	float height_to_width = uv_pixel_x / uv_pixel_y;
	float cut_height = cut_size_px * uv_pixel_y;
	float cut_edge = UV.x + max(cut_height - UV.y, 0.0) * height_to_width - 1.0;
	float anti_alias_width = max(fwidth(cut_edge) * anti_alias_strength, 0.0001);
	float cut_mask = 1.0 - smoothstep(0.0, anti_alias_width, cut_edge);
	COLOR *= vec4(fill_color.rgb, fill_color.a * cut_mask);
}
