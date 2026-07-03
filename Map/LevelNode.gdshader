shader_type canvas_item;
render_mode blend_mix;

// 颜色控制
uniform vec4 ring_color : source_color = vec4(1.0, 1.0, 1.0, 1.0);
uniform vec4 inner_color : source_color = vec4(1.0, 1.0, 1.0, 1.0);

// 形状控制（参数名保留以兼容现有 C# 逻辑）
uniform float ring_size : hint_range(0.1, 1.0) = 0.8;          // 外层六边形半径
uniform float ring_thickness : hint_range(0.01, 2.0) = 0.05;   // 线框粗细
uniform float inner_square_size : hint_range(0.0, 1.0) = 0.4;  // 内部圆环半径
uniform bool show_inner = true;                               // 已完成：点亮内层圆环
uniform float polygon_sides : hint_range(3.0, 6.0) = 6.0; // 外框边数
uniform bool show_chest_icon = false;

// 抗锯齿平滑度
uniform float smoothness_offset : hint_range(0.0, 2.0) = 1.0;

float stroke(float value, float width, float px) {
	return smoothstep(width + px, width - px, abs(value));
}

float sdRegularPolygon(vec2 p, float r, float sides) {
	sides = clamp(floor(sides + 0.5), 3.0, 6.0);

	float rotation = PI * 0.5;
	if (sides < 3.5) {
		rotation = PI * 0.5;
	} else if (sides < 4.5) {
		rotation = 0.0;
	}

	float apothem = r * cos(PI / sides);
	float result = -1000.0;
	for (int i = 0; i < 6; i++) {
		if (float(i) >= sides) {
			continue;
		}

		float angle = rotation + float(i) * TAU / sides;
		vec2 normal = vec2(cos(angle), sin(angle));
		result = max(result, dot(p, normal) - apothem);
	}

	return result;
}

float sdBox(vec2 p, vec2 half_size) {
	vec2 d = abs(p) - half_size;
	return length(max(d, vec2(0.0))) + min(max(d.x, d.y), 0.0);
}

float sdSegment(vec2 p, vec2 a, vec2 b) {
	vec2 pa = p - a;
	vec2 ba = b - a;
	float h = clamp(dot(pa, ba) / dot(ba, ba), 0.0, 1.0);
	return length(pa - ba * h);
}

float lineAlpha(float dist, float width, float px) {
	return 1.0 - smoothstep(width - px, width + px, dist);
}

float drawChestIcon(vec2 p, float px) {
	vec2 cp = p * 0.78;
	float width = 0.018;
	float icon = 0.0;

	icon = max(icon, lineAlpha(sdSegment(cp, vec2(-0.17, 0.015), vec2(0.17, 0.015)), width, px));
	icon = max(icon, lineAlpha(sdSegment(cp, vec2(-0.15, -0.080), vec2(0.15, -0.080)), width, px));
	icon = max(icon, lineAlpha(sdSegment(cp, vec2(-0.17, 0.015), vec2(-0.15, -0.080)), width, px));
	icon = max(icon, lineAlpha(sdSegment(cp, vec2(0.17, 0.015), vec2(0.15, -0.080)), width, px));

	icon = max(icon, lineAlpha(sdSegment(cp, vec2(-0.16, 0.015), vec2(-0.16, 0.165)), width, px));
	icon = max(icon, lineAlpha(sdSegment(cp, vec2(0.16, 0.015), vec2(0.16, 0.165)), width, px));
	icon = max(icon, lineAlpha(sdSegment(cp, vec2(-0.16, 0.165), vec2(0.16, 0.165)), width, px));
	icon = max(icon, lineAlpha(sdSegment(cp, vec2(-0.16, 0.075), vec2(0.16, 0.075)), width, px));
	icon = max(icon, lineAlpha(sdSegment(cp, vec2(0.0, 0.015), vec2(0.0, 0.165)), width * 0.9, px));

	float lock = sdBox(cp - vec2(0.0, 0.072), vec2(0.034, 0.028));
	icon = max(icon, lineAlpha(abs(lock), width * 0.9, px));

	return clamp(icon, 0.0, 1.0);
}

void fragment() {
	vec2 p = UV - vec2(0.5);
	vec3 rgb;
	float alpha;

	if (show_chest_icon) {
		float chest_px = max(fwidth(p.x), 0.002) * smoothness_offset;
		float chest_line = drawChestIcon(p, chest_px);
		if (chest_line <= 0.001) {
			discard;
		}

		rgb = mix(ring_color.rgb, vec3(1.0), show_inner ? 0.58 : 0.22);
		alpha = chest_line * (show_inner ? 1.0 : 0.96) * ring_color.a;
	} else {
		float dist = length(p);

		float hex_radius = mix(0.31, 0.38, ring_size);
		float inner_radius = mix(0.12, 0.20, inner_square_size);
		float line_width = mix(0.003, 0.036, clamp(ring_thickness / 1.2, 0.0, 1.0));

		float hex_dist = sdRegularPolygon(p, hex_radius, polygon_sides);
		float hex_px = max(fwidth(hex_dist), 0.002) * smoothness_offset;
		float circle_px = max(fwidth(dist - inner_radius), 0.002) * smoothness_offset;

		float hex_line = line_width;
		float circle_line = line_width * 0.78;

		float outer_hex = stroke(hex_dist, hex_line, hex_px * 0.65);
		float inner_ring = stroke(dist - inner_radius, circle_line, circle_px * 0.55);

		vec3 star_color = ring_color.rgb;
		vec3 inner_ring_color = mix(star_color, inner_color.rgb, show_inner ? 0.18 : 0.0);
		float inner_strength = show_inner ? 0.82 : 0.13;
		float line_alpha = outer_hex * 0.86 + inner_ring * inner_strength;
		float soft_halo = smoothstep(hex_line * 2.2, 0.0, abs(hex_dist)) * 0.05;

		rgb = star_color * (outer_hex * 0.98 + soft_halo);
		rgb += inner_ring_color * inner_ring * inner_strength;
		alpha = clamp(line_alpha + soft_halo, 0.0, 0.86) * ring_color.a;

		if (alpha <= 0.001) {
			discard;
		}
	}

	COLOR = vec4(rgb, alpha) * COLOR;
}
