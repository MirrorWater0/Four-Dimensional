shader_type canvas_item;

// Character target bracket, built as FOUR SEPARATE CORNER ASSEMBLIES rather than one closed
// outline. Each corner carries a different arrangement of arms, rivets, stubs and chamfer bars,
// so the frame reads as assembled hardware instead of a drawn rectangle.
//
// Everything is computed in LOCAL PIXELS. The rect is 200x400, so UV-space thicknesses used to
// come out twice as heavy vertically as horizontally; pixel space keeps every part consistent.
//
// `assemble` (0..1) drives the build-up: arms extend from their elbow, detached greebles snap in
// late, and each corner is staggered. Run it backwards to take the frame apart.
// `lock_amount` promotes "inspecting" to "targeted" and enables the travelling scan band.

uniform vec4 line_color : source_color = vec4(0.90, 0.97, 1.00, 1.0);
uniform vec4 accent_color : source_color = vec4(0.34, 0.86, 1.00, 1.0);
uniform vec4 fill_color : source_color = vec4(0.03, 0.16, 0.24, 0.08);
uniform vec4 scan_color : source_color = vec4(1.0, 0.62, 0.5, 1.0);

uniform vec2 frame_px = vec2(200.0, 400.0);
uniform float chamfer_px : hint_range(0.0, 60.0) = 24.0;
uniform float arm_px : hint_range(1.0, 8.0) = 3.0;
uniform float rail_gap_px : hint_range(2.0, 20.0) = 6.5;
uniform float glow_px : hint_range(0.0, 40.0) = 10.0;
uniform float scanline_px : hint_range(1.0, 16.0) = 5.0;
uniform float rim_inset_px : hint_range(2.0, 60.0) = 10.0;
uniform float rim_falloff_px : hint_range(2.0, 120.0) = 18.0;
uniform float fill_strength : hint_range(0.0, 3.0) = 1.0;

uniform float assemble : hint_range(0.0, 1.0) = 1.0;
uniform float lock_amount : hint_range(0.0, 1.0) = 0.0;
uniform float pulse : hint_range(0.0, 1.0) = 0.0;
uniform float scan_speed : hint_range(0.0, 2.0) = 0.55;

const float AA = 0.85;

float box(vec2 q, vec2 lo, vec2 hi) {
	vec2 d = min(q - lo, hi - q);
	return smoothstep(-AA, AA, min(d.x, d.y));
}

// Rail running along q.x, `t` thick, its outer face at q.y == y0.
float hbar(vec2 q, float x0, float x1, float y0, float t) {
	return box(q, vec2(x0, y0), vec2(x1, y0 + t));
}

// Rail running along q.y, `t` thick, its outer face at q.x == x0.
float vbar(vec2 q, float y0, float y1, float x0, float t) {
	return box(q, vec2(x0, y0), vec2(x0 + t, y1));
}

// Bar lying along the corner's diagonal cut.
float chamfer_bar(vec2 q, float at, float half_w, float reach) {
	float on_line = 1.0 - smoothstep(half_w - AA, half_w + AA, abs(q.x + q.y - at));
	float within = 1.0 - smoothstep(reach - AA, reach + AA, abs(q.x - q.y));
	return on_line * within;
}

// Per-corner build progress: corners land one after another.
float corner_progress(float t, int id) {
	float start = float(id) * 0.13;
	return clamp((t - start) / max(1.0 - 0.39, 0.0001), 0.0, 1.0);
}

void fragment() {
	vec4 modulate_color = COLOR;

	vec2 p = UV * frame_px;
	vec2 e = min(p, frame_px - p);
	float ed = min(e.x, e.y);

	float a = arm_px;
	float build = clamp(assemble, 0.0, 1.0);

	float main_m = 0.0;   // outer structural rail -> line_color
	float thin_m = 0.0;   // inboard secondary rail -> accent_color, the second layer
	float det_m = 0.0;    // rivets, stubs, greebles -> accent_color
	float cham_m = 0.0;   // chamfer bars -> hot mix

	float t2 = max(arm_px * 0.42, 1.0); // secondary rail weight
	float lane = rail_gap_px;           // channel between the two layers

	for (int id = 0; id < 4; id++) {
		// Corner-local coordinates: q always grows inward from that corner.
		vec2 q = id == 0 ? p
			: id == 1 ? vec2(frame_px.x - p.x, p.y)
			: id == 2 ? vec2(p.x, frame_px.y - p.y)
			: frame_px - p;

		float g = corner_progress(build, id);
		if (g <= 0.001) {
			continue;
		}
		float late = smoothstep(0.55, 1.0, g); // detached greebles snap in at the end
		float c = chamfer_px;

		float m = 0.0;   // outer rail
		float s = 0.0;   // inboard secondary rail
		float d = 0.0;   // greebles
		float ch = 0.0;  // chamfer bars

		if (id == 0) {
			// Top-left: one long lateral run. Secondary rail is shorter and starts further in,
			// so the two layers stagger instead of reading as one thick line.
			m = max(m, hbar(q, c - 6.0, c - 6.0 + 54.0 * g, 0.0, a));
			s = max(s, hbar(q, c + 4.0, c + 4.0 + 38.0 * g, lane, t2));
			m = max(m, vbar(q, c - 6.0, c - 6.0 + 21.0 * g, 0.0, a));
			s = max(s, vbar(q, c + 4.0, c + 4.0 + 12.0 * g, lane, t2));
			ch = max(ch, chamfer_bar(q, c, a * 0.62, c * 0.95));
			d = max(d, box(q, vec2(9.0, 9.0), vec2(14.0, 14.0)));
			d = max(d, hbar(q, 84.0, 98.0, 0.0, a) * late);
		} else if (id == 1) {
			// Top-right: dominant vertical spar, secondary rail overruns it, rivets in the channel.
			m = max(m, vbar(q, c - 6.0, c - 6.0 + 74.0 * g, 0.0, a));
			s = max(s, vbar(q, c + 2.0, c + 2.0 + 86.0 * g, lane, t2));
			m = max(m, hbar(q, c - 6.0, c - 6.0 + 19.0 * g, 0.0, a));
			ch = max(ch, chamfer_bar(q, c, a * 0.62, c * 0.95));
			ch = max(ch, chamfer_bar(q, c + 6.5, a * 0.34, c * 0.55));
			d = max(d, box(q, vec2(a + 1.2, 34.0), vec2(lane - 0.8, 39.0)));
			d = max(d, box(q, vec2(a + 1.2, 45.0), vec2(lane - 0.8, 50.0)));
			d = max(d, hbar(q, 0.0, 9.0, 106.0, a) * late);
		} else if (id == 2) {
			// Bottom-left: stepped bracket, both rails doubled, wide shoe, detached slot.
			m = max(m, vbar(q, c - 6.0, c - 6.0 + 46.0 * g, 0.0, a));
			s = max(s, vbar(q, c + 3.0, c + 3.0 + 28.0 * g, lane, t2));
			m = max(m, hbar(q, c - 6.0, c - 6.0 + 31.0 * g, 0.0, a));
			s = max(s, hbar(q, c + 3.0, c + 3.0 + 44.0 * g, lane, t2));
			ch = max(ch, chamfer_bar(q, c, a * 0.62, c * 0.95));
			d = max(d, box(q, vec2(8.0, 7.0), vec2(21.0, 12.0)));
			d = max(d, hbar(q, 62.0, 74.0, 0.0, a * 0.7) * late);
		} else {
			// Bottom-right: heavy block corner. Thick primary, long thin secondary, rivet stack.
			m = max(m, hbar(q, c - 4.0, c - 4.0 + 36.0 * g, 0.0, a + 2.0));
			s = max(s, hbar(q, c + 2.0, c + 2.0 + 54.0 * g, a + 2.0 + lane * 0.6, t2));
			m = max(m, vbar(q, c - 4.0, c - 4.0 + 29.0 * g, 0.0, a + 2.0));
			s = max(s, vbar(q, c + 2.0, c + 2.0 + 24.0 * g, a + 2.0 + lane * 0.6, t2));
			ch = max(ch, chamfer_bar(q, c, a * 0.75, c * 0.95));
			ch = max(ch, chamfer_bar(q, c + 7.5, a * 0.34, c * 0.5));
			d = max(d, box(q, vec2(9.0, 9.0), vec2(16.0, 14.0)));
			d = max(d, box(q, vec2(9.0, 17.0), vec2(14.0, 21.0)));
			d = max(d, box(q, vec2(9.0, 24.0), vec2(12.5, 27.0)));
		}

		float fade = smoothstep(0.0, 0.3, g);
		main_m = max(main_m, m * fade);
		thin_m = max(thin_m, s * fade);
		det_m = max(det_m, d * fade);
		cham_m = max(cham_m, ch * fade);
	}

	// --- inner rim glow + scanlines ------------------------------------------
	// Hugs the bracket band; a full-area wash would read as a coloured box over the artwork.
	float scan_lines = 0.82 + 0.18 * step(0.5, fract(p.y / scanline_px));
	float rim = smoothstep(0.0, 3.0, ed)
		* (1.0 - smoothstep(rim_inset_px, rim_inset_px + rim_falloff_px, ed))
		* smoothstep(-AA, AA, (e.x + e.y) - chamfer_px);
	float fill = rim * scan_lines * fill_strength * build;

	float glow = (1.0 - smoothstep(0.0, glow_px, ed))
		* smoothstep(-AA, AA, (e.x + e.y) - chamfer_px) * build;

	// --- lock state ------------------------------------------------------------
	float beat = mix(1.0, 1.0 + pulse * 0.7, lock_amount);
	float scan = 0.0;
	if (lock_amount > 0.001 && scan_speed > 0.001) {
		float travel = fract(UV.y + TIME * scan_speed);
		scan = exp(-travel * travel * 24.0) + exp(-(1.0 - travel) * (1.0 - travel) * 24.0);
		scan *= rim * lock_amount * 0.5 * build;
	}

	vec3 hot = mix(line_color.rgb, accent_color.rgb, 0.65);

	vec3 rgb = vec3(0.0);
	rgb += fill_color.rgb * fill * beat;
	// Main rails deliberately skip `beat`: letting the pulse drive them blew the corners to white.
	rgb += line_color.rgb * main_m * 0.95;
	rgb += accent_color.rgb * thin_m * 0.8 * beat;
	rgb += accent_color.rgb * det_m * 0.95 * beat;
	rgb += hot * cham_m * beat;
	rgb += accent_color.rgb * glow * (0.06 + lock_amount * 0.24) * beat;
	rgb += scan_color.rgb * scan;

	float alpha = fill * fill_color.a * beat;
	alpha = max(alpha, main_m * line_color.a);
	alpha = max(alpha, thin_m * accent_color.a * 0.85 * beat);
	alpha = max(alpha, det_m * accent_color.a * beat);
	alpha = max(alpha, cham_m * accent_color.a * beat);
	alpha = max(alpha, glow * (0.08 + lock_amount * 0.2) * beat);
	alpha = max(alpha, scan * scan_color.a);
	alpha = clamp(alpha, 0.0, 1.0);

	COLOR = vec4(rgb, alpha) * modulate_color;
}
