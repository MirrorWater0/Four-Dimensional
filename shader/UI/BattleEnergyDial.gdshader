shader_type canvas_item;
render_mode unshaded;

// Scene-authored linework, shared by editor and live battle.
uniform float animation_speed : hint_range(0.0, 2.0) = 1.0;
uniform float phase_offset = 0.0;
uniform vec4 line_color : source_color = vec4(1.0, 1.0, 1.0, 1.0);
uniform float core_radius : hint_range(0.28, 0.48) = 0.40;
uniform float orbit_radius : hint_range(0.65, 0.88) = 0.83;
uniform float orbit_tilt : hint_range(0.28, 0.62) = 0.43;
uniform float line_width : hint_range(0.001, 0.015) = 0.0035;
uniform float orbit_opacity : hint_range(0.0, 1.0) = 0.44;
uniform float rotation_speed : hint_range(0.0, 1.5) = 0.46;
uniform float precession_speed : hint_range(0.0, 0.25) = 0.035;
uniform float glow_strength : hint_range(0.0, 1.0) = 0.16;
uniform float energy_activity : hint_range(0.0, 1.0) = 1.0;
uniform float change_pulse : hint_range(0.0, 1.0) = 0.0;

vec2 rotate_point(vec2 p, float angle) {
	float c = cos(angle);
	float s = sin(angle);
	return vec2(c * p.x - s * p.y, s * p.x + c * p.y);
}

float stroke(float distance, float width, float aa) {
	return 1.0 - smoothstep(width, width + aa, abs(distance));
}

void fragment() {
	vec2 p = UV * 2.0 - 1.0;
	float aa = max(length(fwidth(p)) * 0.5, 0.002);
	float time = TIME * animation_speed + phase_offset;
	float activity = mix(0.35, 1.0, energy_activity);
	float r = length(p);
	float alpha = 0.0;
	float foreground_crossing = 0.0;
	// Only the rear half passes behind the central ring. The front half stays visible.
	float center_clearance = smoothstep(core_radius - 0.035, core_radius + 0.015, r);
	for (int orbit = 0; orbit < 3; orbit++) {
		float index = float(orbit);
		float axis = -0.54 + index * 1.08 + time * precession_speed;
		float tilt = orbit_tilt + (orbit == 1 ? 0.12 : (orbit == 2 ? -0.06 : 0.0));
		float radius = orbit_radius - index * 0.022 + change_pulse * 0.025;
		vec2 radii = vec2(radius, radius * tilt);
		vec2 q = rotate_point(p, -axis);
		vec2 ellipse = q / radii;
		float ellipse_length = max(length(ellipse), 0.0001);
		float distance = (ellipse_length - 1.0) / max(length(q / (radii * radii)) / ellipse_length, 0.001);
		float angle = atan(ellipse.y, ellipse.x);
		float depth = sin(angle);
		float front = smoothstep(-0.3, 0.6, depth);
		float orbit_visibility = mix(center_clearance, 1.0, smoothstep(-0.05, 0.05, depth));
		float phase = time * rotation_speed * (orbit == 1 ? -0.83 : 1.0 + index * 0.13) + index * 2.1;
		float angular_distance = mod(angle - phase + 3.1415927, 6.2831853) - 3.1415927;
		// A small travelling glint and its fine trail echo the start screen's orbits.
		float trail = exp(-angular_distance * angular_distance * 10.0);
		float line = stroke(distance, line_width, aa);
		float opacity = orbit_opacity * mix(0.34, 1.0, front) + trail * 0.25;
		float orbit_alpha = line * opacity * orbit_visibility;
		vec2 electron = radii * vec2(cos(phase), sin(phase));
		float dot_radius = mix(0.008, 0.014, 0.5 + 0.5 * sin(phase));
		float electron_distance = length(q - electron);
		float dot_mask = 1.0 - smoothstep(dot_radius, dot_radius + aa, electron_distance);
		float glow = exp(-electron_distance * electron_distance * 1600.0) * glow_strength;
		// The glint uses its own orbital depth, including the soft glow around it.
		float electron_visibility = mix(center_clearance, 1.0, smoothstep(-0.05, 0.05, sin(phase)));
		orbit_alpha = max(orbit_alpha, (dot_mask * 0.86 + glow) * electron_visibility);
		orbit_alpha *= activity;
		// A small break in the central ring at a front crossing makes the order legible.
		foreground_crossing = max(foreground_crossing,
			stroke(distance, line_width + 0.012, aa) * smoothstep(0.0, 0.1, depth));
		alpha = 1.0 - (1.0 - alpha) * (1.0 - clamp(orbit_alpha, 0.0, 1.0));
	}
	float ring = stroke(r - core_radius, line_width * 1.25, aa);
	float ring_alpha = ring * (0.72 + change_pulse * 0.22) * activity * (1.0 - foreground_crossing * 0.8);
	alpha = 1.0 - (1.0 - alpha) * (1.0 - ring_alpha);
	COLOR = vec4(line_color.rgb, clamp(alpha * line_color.a, 0.0, 1.0));
}
