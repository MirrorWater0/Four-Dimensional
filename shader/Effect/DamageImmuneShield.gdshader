shader_type canvas_item;
render_mode unshaded;

uniform vec4 shield_color : source_color = vec4(0.22, 0.95, 0.65, 1.0);
uniform vec4 rim_color : source_color = vec4(0.78, 1.0, 0.94, 1.0);
uniform vec2 canvas_size = vec2(300.0, 400.0);
uniform float presence : hint_range(0.0, 1.0) = 1.0;
uniform float depth_layer : hint_range(-1.0, 1.0) = -1.0;
uniform float effect_time = 0.0;
uniform float rotation_speed : hint_range(-3.0, 3.0) = 0.85;
uniform float panel_arc : hint_range(0.4, 1.8) = 1.08;
uniform float panel_height : hint_range(0.1, 0.7) = 0.46;
uniform float impact_age = 0.55;
uniform vec2 impact_origin = vec2(0.92, 0.58);

float line(float distance_to_line, float width) {
	float aa = max(fwidth(distance_to_line), 0.65);
	return 1.0 - smoothstep(width, width + aa, abs(distance_to_line));
}

float hex_border(vec2 p) {
	vec2 cell = vec2(1.0, 1.7320508);
	vec2 a = mod(p, cell) - cell * 0.5;
	vec2 b = mod(p - cell * 0.5, cell) - cell * 0.5;
	vec2 nearest = dot(a, a) < dot(b, b) ? a : b;
	vec2 q = abs(nearest);
	return abs(max(dot(q, vec2(0.5, 0.8660254)), q.x) - 0.5);
}

void fragment() {
	vec4 tint = COLOR;
	float unfold = smoothstep(0.0, 1.0, presence);
	vec2 p = (UV - vec2(0.5, 0.58)) * canvas_size;
	vec2 radius = canvas_size * vec2(0.42, 0.085) * mix(0.88, 1.0, unfold);

	// Each X intersects the orbit twice: draw the rear branch below the sprite
	// and the front branch above it, just like CharacterShadow's extruded arcs.
	float orbit_x = p.x / radius.x;
	float side_mask = 1.0 - smoothstep(0.985, 1.0, abs(orbit_x));
	float x = clamp(orbit_x, -0.9999, 0.9999);
	float depth = sqrt(max(1.0 - x * x, 0.0)) * (depth_layer < 0.0 ? -1.0 : 1.0);
	float angle = atan(depth, x);
	float phase = 0.4 + effect_time * rotation_speed;
	float sector = TAU / 3.0;
	float panel_angle = mod(angle - phase + sector * 0.5, sector) - sector * 0.5;
	vec2 panel_position = vec2(panel_angle * radius.x, p.y - depth * radius.y);
	vec2 half_size = vec2(panel_arc * radius.x * 0.5, canvas_size.y * panel_height * 0.5);
	float chamfer = min(11.0, half_size.y * 0.16);
	vec2 q = abs(panel_position) - half_size;
	float panel_distance = max(max(q.x, q.y), (q.x + q.y + chamfer) * 0.7071068);
	float aa = max(fwidth(panel_distance), 0.65);
	float inside = 1.0 - smoothstep(-aa, aa, panel_distance);
	float rim = line(panel_distance, 1.0);
	float inset = line(panel_distance + 5.0, 0.45) * inside;
	float glow = exp(-abs(panel_distance) * 0.32);

	// The pattern stays attached to each moving panel instead of sliding over the art.
	float grid = line(hex_border(panel_position / 23.0) * 23.0, 0.4) * inside;
	float breath = 0.5 + 0.5 * sin(effect_time * 2.1);
	float scan_y = (fract(effect_time * 0.21) * 2.0 - 1.0) * half_size.y;
	float scan = exp(-abs(panel_position.y - scan_y) * 0.16) * inside;
	float depth_alpha = mix(0.48, 1.0, depth * 0.5 + 0.5);

	float hit_fade = 1.0 - smoothstep(0.0, 0.55, impact_age);
	float hit_flash = exp(-impact_age * 10.0) * hit_fade;
	vec2 hit_delta = (UV - impact_origin) * canvas_size;
	float hit_radius = impact_age * min(canvas_size.x, canvas_size.y) * 2.4;
	float wave = line(length(hit_delta) - hit_radius, 2.0) * hit_fade * inside;
	float hit_spot = exp(-length(hit_delta) * 0.025) * hit_flash * inside;

	float membrane = inside * (0.035 + breath * 0.012 + hit_flash * 0.07);
	float grid_alpha = grid * (0.065 + hit_flash * 0.15);
	float rim_alpha = rim * (0.57 + breath * 0.1 + hit_flash * 0.3);
	float detail_alpha = inset * 0.13 + scan * 0.055 + glow * 0.065;
	float impact_alpha = wave * 0.52 + hit_spot * 0.28;
	float alpha = clamp(membrane + grid_alpha + rim_alpha + detail_alpha + impact_alpha,
		0.0, 0.94) * unfold * side_mask * depth_alpha;
	float highlight = clamp(rim * 0.7 + wave * 0.8 + hit_spot * 0.5, 0.0, 1.0);
	vec3 color = mix(shield_color.rgb, rim_color.rgb, highlight);
	COLOR = vec4(color, alpha * shield_color.a) * tint;
}
