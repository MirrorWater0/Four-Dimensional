shader_type canvas_item;
render_mode unshaded;

uniform vec4 void_color : source_color = vec4(0.018, 0.019, 0.022, 1.0);
uniform vec4 plane_color : source_color = vec4(0.21, 0.215, 0.225, 1.0);
uniform vec4 edge_color : source_color = vec4(0.86, 0.87, 0.89, 1.0);
uniform vec2 space_center = vec2(0.5, 0.43);
uniform vec2 field_size = vec2(2560.0, 1440.0);
uniform vec2 field_origin = vec2(-257.0, -225.0);
uniform vec2 composition_size = vec2(1920.0, 1080.0);
uniform float plane_strength : hint_range(0.0, 1.0) = 0.82;
uniform float edge_strength : hint_range(0.0, 1.0) = 0.46;
uniform float motion_speed : hint_range(0.0, 1.0) = 0.12;
uniform float perspective : hint_range(1.0, 3.0) = 1.95;
uniform vec3 initial_rotation = vec3(-0.19, 0.32, 0.10);
uniform float panel_inset : hint_range(0.1, 0.4) = 0.18;
uniform float space_time = 0.0;
uniform float focus_amount = 0.0;
uniform float impact_amount = 0.0;
uniform float outer_line_width : hint_range(0.35, 2.5) = 1.0;

vec3 rotate_space(vec3 p, vec3 angles) {
	vec3 c = cos(angles);
	vec3 s = sin(angles);
	p.yz = mat2(vec2(c.x, s.x), vec2(-s.x, c.x)) * p.yz;
	p.xz = mat2(vec2(c.y, s.y), vec2(-s.y, c.y)) * p.xz;
	p.xy = mat2(vec2(c.z, s.z), vec2(-s.z, c.z)) * p.xy;
	return p;
}

float hairline(float distance_to_line, float width) {
	// Keep line width in wall space, then widen the transition by the actual
	// pixel footprint. This avoids shimmer when a wall rotates through the view.
	float aa = max(fwidth(distance_to_line) * 1.5, 0.00035);
	return 1.0 - smoothstep(width, width + aa, abs(distance_to_line));
}

float soft_line(float distance_to_line, float width, float strength) {
	float aa = max(fwidth(distance_to_line) * 1.8, 0.0012);
	return (1.0 - smoothstep(width, width + aa, abs(distance_to_line))) * strength;
}

void fragment() {
	// Every view ray hits one of six INNER walls. Rotating the volume changes
	// the perspective itself instead of spinning a flat image behind the teams.
	vec2 uv = (UV * field_size + field_origin) / composition_size;
	float aspect = composition_size.x / composition_size.y;
	vec2 p = (uv - space_center) * vec2(aspect, 1.0);
	float t = space_time * motion_speed;
	vec3 angles = initial_rotation + vec3(t * 0.31, t, t * 0.19);
	vec3 ray = rotate_space(normalize(vec3(p * perspective, 1.0)), angles);
	vec3 wall_times = 1.0 / max(abs(ray), vec3(0.00001));
	float distance_to_wall = min(wall_times.x, min(wall_times.y, wall_times.z));
	vec3 hit = ray * distance_to_wall;
	vec3 normal;
	vec2 wall;
	float face_shade;
	if (wall_times.x < wall_times.y && wall_times.x < wall_times.z) {
		normal = vec3(sign(ray.x), 0.0, 0.0);
		wall = hit.zy;
		face_shade = ray.x > 0.0 ? 0.42 : 0.68;
	} else if (wall_times.y < wall_times.z) {
		normal = vec3(0.0, sign(ray.y), 0.0);
		wall = hit.xz;
		face_shade = ray.y > 0.0 ? 0.34 : 0.78;
	} else {
		normal = vec3(0.0, 0.0, sign(ray.z));
		wall = hit.xy;
		face_shade = ray.z > 0.0 ? 0.51 : 0.61;
	}

	// Broad matte faces, each with one recessed panel, avoid a tiled grid.
	float border_distance = 1.0 - max(abs(wall.x), abs(wall.y));
	float panel_distance = border_distance - panel_inset;
	float panel_aa = max(fwidth(panel_distance) * 1.5, 0.0007);
	float panel = smoothstep(-panel_aa, panel_aa, panel_distance);
	float wall_light = 0.86 + dot(hit, normalize(vec3(-0.4, -0.7, -0.3))) * 0.16;
	vec3 color = void_color.rgb + plane_color.rgb * plane_strength
		* face_shade * wall_light * mix(0.77, 0.42, panel);
	float fold = exp(-border_distance * 22.0);
	color += plane_color.rgb * fold * plane_strength * 0.18;
	float edge = hairline(border_distance, 0.012 * outer_line_width);
	float seam = hairline(panel_distance, 0.009);
	// Large inset panel joints make the six faces read as constructed walls.
	float panel_joint_x = soft_line(abs(abs(wall.x) - 0.58), 0.010, 0.70);
	float panel_joint_y = soft_line(abs(abs(wall.y) - 0.58), 0.010, 0.70);
	float corner_marks = hairline(panel_distance - 0.045, 0.008)
		* smoothstep(0.48, 0.67, min(abs(wall.x), abs(wall.y)));
	// A short light trace travels along the physical edges of the cube.
	vec3 tangent = cross(normal, normalize(vec3(0.37, 0.61, 0.73)));
	float trace = pow(0.5 + 0.5 * cos(dot(hit, tangent) * 7.0 - space_time * 0.65), 14.0);
	float structure = edge * (0.42 + trace * 0.72) + seam * 0.18
		+ (panel_joint_x + panel_joint_y) * 0.08 + corner_marks * 0.10;
	float combat_band = smoothstep(0.22, 0.34, uv.y) * (1.0 - smoothstep(0.60, 0.72, uv.y));
	float team_mask = smoothstep(0.08, 0.27, abs(uv.x - 0.5)) * combat_band;
	color += edge_color.rgb * structure * edge_strength * 0.72 * (1.0 - team_mask * 0.46)
		* (1.0 - focus_amount * 0.32 + impact_amount * 0.18);
	float hand = smoothstep(0.65, 0.91, uv.y);
	float vignette = smoothstep(0.48, 1.24, length(p));
	color *= (1.0 - hand * 0.72) * (1.0 - vignette * 0.22);
	COLOR *= vec4(color, 1.0);
}
