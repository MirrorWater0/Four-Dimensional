shader_type canvas_item;
render_mode unshaded;

// Measure the silhouette in the control's local coordinates, independent of
// viewport scaling, rotation and the texture's cropped UV rectangle.
uniform vec2 rect_size = vec2(220.0, 192.0);
uniform float cut_size_px : hint_range(0.0, 128.0, 0.5) = 14.0;
uniform float anti_alias_strength : hint_range(0.5, 3.0, 0.05) = 1.25;
varying vec2 local_position;

void vertex() {
	local_position = VERTEX;
}

void fragment() {
	vec2 edge_distance = min(local_position, rect_size - local_position);
	float cut = clamp(cut_size_px, 0.0, min(rect_size.x, rect_size.y) * 0.5);
	float diagonal_distance = (edge_distance.x + edge_distance.y - cut) * 0.70710678;
	// Derivatives only soften the edge; they never determine the cut geometry.
	float aa = max(fwidth(diagonal_distance) * anti_alias_strength, 0.001);
	float mask = smoothstep(-aa * 0.5, aa * 0.5, diagonal_distance);

	COLOR.a *= mask;
}
