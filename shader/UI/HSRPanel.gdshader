shader_type canvas_item;

// Honkai: Star Rail style panel: subtle vertical gradient, hairline border,
// and a signature diagonal cut at the top-right corner with a golden edge.
// Enhanced with a faint blueprint grid, a slow holographic sheen sweep,
// a double golden hairline on the cut, and a soft inner vignette for depth.

uniform vec4 bg_top : source_color = vec4(0.070, 0.096, 0.160, 0.94);
uniform vec4 bg_bottom : source_color = vec4(0.034, 0.050, 0.092, 0.94);
uniform vec4 border_color : source_color = vec4(0.62, 0.70, 0.86, 0.16);
uniform vec4 cut_color : source_color = vec4(0.90, 0.76, 0.50, 0.90);
uniform vec2 rect_size = vec2(600.0, 400.0);
uniform float cut_size : hint_range(0.0, 120.0) = 24.0;
uniform float border_width : hint_range(0.0, 4.0) = 1.0;
uniform float sheen : hint_range(0.0, 0.2) = 0.04;

// --- Beautification uniforms (subtle defaults, safe to leave untouched) ---
uniform vec4 grid_color : source_color = vec4(0.42, 0.56, 0.82, 1.0);
uniform float grid_spacing : hint_range(8.0, 128.0) = 34.0;
uniform float grid_alpha : hint_range(0.0, 0.2) = 0.035;
uniform vec4 sweep_color : source_color = vec4(0.62, 0.78, 1.0, 1.0);
uniform float sweep_alpha : hint_range(0.0, 0.3) = 0.05;
uniform float sweep_speed : hint_range(0.0, 1.0) = 0.10;
uniform float vignette_strength : hint_range(0.0, 0.6) = 0.16;
uniform float corner_accent : hint_range(0.0, 1.0) = 1.0;

varying vec2 px;

void vertex() {
	px = VERTEX;
}

void fragment() {
	vec2 p = px;
	float max_dim_y = max(rect_size.y, 1.0);
	vec2 n = p / max(rect_size, vec2(1.0)); // normalized 0..1 within the panel

	// Signed distance to the plain rect (negative inside).
	vec2 half_size = rect_size * 0.5;
	vec2 q = abs(p - half_size) - half_size;
	float d_rect = max(q.x, q.y);

	// Diagonal cut plane at the top-right corner.
	float cut = clamp(cut_size, 0.0, min(rect_size.x, rect_size.y) * 0.5);
	float d_cut = (p.x - p.y - (rect_size.x - cut)) * 0.70710678;

	float d = max(d_rect, d_cut);

	float aa = 1.25;
	float inside = 1.0 - smoothstep(-aa, 0.0, d);

	// Vertical gradient background with a faint top sheen.
	vec4 bg = mix(bg_top, bg_bottom, clamp(p.y / max_dim_y, 0.0, 1.0));
	bg.rgb += (1.0 - clamp(p.y / max_dim_y, 0.0, 1.0)) * sheen * vec3(0.9, 0.95, 1.0);

	// Faint blueprint grid — thin lines on a fixed pixel lattice.
	if (grid_alpha > 0.0005) {
		vec2 g = abs(fract(p / grid_spacing) - 0.5) / fwidth(p / grid_spacing);
		float line = 1.0 - min(min(g.x, g.y), 1.0);
		// Brighter dots where lines cross.
		float node = (1.0 - min(g.x, 1.0)) * (1.0 - min(g.y, 1.0));
		float grid = clamp(line + node * 0.6, 0.0, 1.0);
		bg.rgb = mix(bg.rgb, grid_color.rgb, grid * grid_alpha);
	}

	// Slow holographic sheen sweeping diagonally across the panel.
	if (sweep_alpha > 0.0005) {
		float phase = (n.x + n.y) * 0.5 - fract(TIME * sweep_speed) * 1.6 + 0.3;
		float band = exp(-phase * phase * 34.0);
		bg.rgb += sweep_color.rgb * band * sweep_alpha;
	}

	// Soft inner vignette: edges of the panel sit slightly deeper.
	if (vignette_strength > 0.0005) {
		float edge = clamp(-d / (min(rect_size.x, rect_size.y) * 0.5 + 0.001), 0.0, 1.0);
		float vig = 1.0 - smoothstep(0.0, 0.55, edge);
		bg.rgb *= 1.0 - vig * vignette_strength;
	}

	// Hairline border band inside the shape.
	float bw = max(border_width, 0.001);
	float border_mask = smoothstep(-bw - aa, -bw, d) * inside;

	// The cut edge glows gold instead of the plain border color.
	float near_cut = 1.0 - smoothstep(0.0, bw + aa * 0.5, abs(d_cut));

	// A second, inner golden hairline parallel to the cut — signature HSR detail.
	float inner_offset = max(cut * 0.34, 6.0);
	float body_mask = smoothstep(0.0, bw + 3.0, -d_rect); // fade the inner line away from the outer border
	float near_inner_cut = (1.0 - smoothstep(0.0, bw + aa * 0.5, abs(d_cut + inner_offset)))
		* body_mask * corner_accent;

	vec4 edge_col = vec4(mix(border_color.rgb, cut_color.rgb, near_cut),
	                     mix(border_color.a, cut_color.a, near_cut));

	float alpha = inside * mix(bg.a, max(bg.a, edge_col.a), border_mask);
	vec3 rgb = mix(bg.rgb, edge_col.rgb, border_mask);

	// Lay the inner cut line on top of everything.
	rgb = mix(rgb, cut_color.rgb, near_inner_cut * cut_color.a);
	alpha = max(alpha, near_inner_cut * cut_color.a * inside);

	COLOR = vec4(rgb, alpha);
}
