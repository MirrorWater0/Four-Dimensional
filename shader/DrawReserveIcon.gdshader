shader_type canvas_item;

uniform vec4 card_color : source_color = vec4(0.54, 0.86, 0.96, 0.92);
uniform vec4 back_card_color : source_color = vec4(0.12, 0.46, 0.62, 0.36);
uniform vec4 line_color : source_color = vec4(0.82, 0.98, 1.0, 0.95);
uniform vec4 arrow_color : source_color = vec4(0.70, 0.95, 1.0, 0.92);
uniform float glow = 0.65;
uniform float back_card_rotation_degrees : hint_range(-45.0, 45.0, 0.1) = 20.5;
uniform float front_card_rotation_degrees : hint_range(-45.0, 45.0, 0.1) = 5.7;

float aa(float d) {
	float w = max(fwidth(d), 0.002);
	return smoothstep(w, -w, d);
}

float stroke(float d, float width) {
	float w = max(fwidth(d), 0.002);
	return smoothstep(width + w, width - w, abs(d));
}

float sd_box(vec2 p, vec2 b) {
	vec2 d = abs(p) - b;
	return length(max(d, 0.0)) + min(max(d.x, d.y), 0.0);
}

float sd_segment(vec2 p, vec2 a, vec2 b) {
	vec2 pa = p - a;
	vec2 ba = b - a;
	float h = clamp(dot(pa, ba) / dot(ba, ba), 0.0, 1.0);
	return length(pa - ba * h);
}

float cross2(vec2 a, vec2 b) {
	return a.x * b.y - a.y * b.x;
}

float sd_polygon(vec2 p, vec2 p0, vec2 p1, vec2 p2, vec2 p3, vec2 p4, vec2 p5, vec2 p6) {
	vec2 points[7] = { p0, p1, p2, p3, p4, p5, p6 };
	float d = 1e20;
	float winding = 0.0;
	for (int i = 0; i < 7; i++) {
		vec2 a = points[i];
		vec2 b = points[(i + 1) % 7];
		vec2 e = b - a;
		vec2 w = p - a;
		vec2 q = w - e * clamp(dot(w, e) / dot(e, e), 0.0, 1.0);
		d = min(d, dot(q, q));
		if ((a.y <= p.y && b.y > p.y && cross2(e, p - a) > 0.0) || (a.y > p.y && b.y <= p.y && cross2(e, p - a) < 0.0)) {
			winding += (b.y > a.y) ? 1.0 : -1.0;
		}
	}
	return sqrt(d) * (abs(winding) > 0.5 ? -1.0 : 1.0);
}

vec2 rotate2(vec2 p, float angle) {
	float c = cos(angle);
	float s = sin(angle);
	return vec2(c * p.x - s * p.y, s * p.x + c * p.y);
}

float block_arrow(vec2 p) {
	vec2 q = rotate2(p - vec2(0.02, 0.00), -0.44);
	return sd_polygon(
		q,
		vec2(-0.30, -0.055),
		vec2(0.10, -0.055),
		vec2(0.10, -0.145),
		vec2(0.33, 0.0),
		vec2(0.10, 0.145),
		vec2(0.10, 0.055),
		vec2(-0.30, 0.055)
	);
}

void fragment() {
	vec2 p = UV * 2.0 - 1.0;
	p.x *= 1.02;
	p *= 0.75;

	vec4 col = vec4(0.0);
	float pulse = 0.88 + 0.12 * sin(TIME * 1.6);

	vec2 back = rotate2(p + vec2(0.11, 0.02), radians(back_card_rotation_degrees));
	float d_back = sd_box(back, vec2(0.34, 0.50));
	float a_back = aa(d_back);
	float back_edge = stroke(d_back, 0.016);
	col.rgb = mix(col.rgb, back_card_color.rgb, a_back * back_card_color.a);
	col.a = max(col.a, a_back * back_card_color.a);
	col.rgb = mix(col.rgb, line_color.rgb * 0.42, back_edge * 0.35);

	vec2 front = rotate2(p + vec2(-0.05, -0.01), radians(front_card_rotation_degrees));
	float d_card = sd_box(front, vec2(0.35, 0.52));
	float a_card = aa(d_card);
	float edge = stroke(d_card, 0.018);
	float inner = sd_box(front, vec2(0.27, 0.40));
	float inner_edge = stroke(inner, 0.012);

	vec3 fill = card_color.rgb * (0.48 + 0.08 * pulse);
	col.rgb = mix(col.rgb, fill, a_card * card_color.a);
	col.a = max(col.a, a_card * card_color.a);
	col.rgb = mix(col.rgb, line_color.rgb, edge * 0.72);
	col.rgb = mix(col.rgb, line_color.rgb * 0.56, inner_edge * 0.32);

	float d_arrow = block_arrow(p);
	float a_arrow = aa(d_arrow);
	float arrow_edge = stroke(d_arrow, 0.014);
	col.rgb = mix(col.rgb, arrow_color.rgb, a_arrow);
	col.a = max(col.a, a_arrow * arrow_color.a);
	col.rgb = mix(col.rgb, line_color.rgb, arrow_edge * 0.55);

	float glow_mask = exp(-abs(d_card) * 12.0) * a_card * glow * pulse;
	col.rgb += line_color.rgb * glow_mask * 0.12;

	COLOR = col;
}
