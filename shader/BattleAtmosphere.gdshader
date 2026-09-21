shader_type canvas_item;
render_mode unshaded;

uniform sampler2D screen_texture : hint_screen_texture, repeat_disable, filter_linear;
uniform float max_saturation : hint_range(0.0, 1.0) = 0.41;
uniform float atmosphere_time = 0.0;
uniform float focus_amount : hint_range(0.0, 1.0) = 0.0;
uniform float impact_amount : hint_range(0.0, 1.0) = 0.0;
uniform vec4 atmosphere_tint : source_color = vec4(0.20, 0.34, 0.46, 1.0);

vec3 rgb_to_hsv(vec3 color) {
	vec4 k = vec4(0.0, -0.3333333333, 0.6666666667, -1.0);
	vec4 p = mix(vec4(color.bg, k.wz), vec4(color.gb, k.xy), step(color.b, color.g));
	vec4 q = mix(vec4(p.xyw, color.r), vec4(color.r, p.yzx), step(p.x, color.r));
	float delta = q.x - min(q.w, q.y);
	return vec3(abs(q.z + (q.w - q.y) / (6.0 * delta + 0.0000000001)), delta / (q.x + 0.0000000001), q.x);
}

vec3 hsv_to_rgb(vec3 color) {
	vec3 p = abs(fract(color.xxx + vec3(0.0, 0.6666666667, 0.3333333333)) * 6.0 - 3.0);
	return color.z * mix(vec3(1.0), clamp(p - 1.0, 0.0, 1.0), color.y);
}

void fragment() {
	vec2 uv = SCREEN_UV;
	float combat_band = smoothstep(0.12, 0.26, uv.y) * (1.0 - smoothstep(0.52, 0.68, uv.y));
	float edge = smoothstep(0.26, 0.53, abs(uv.x - 0.5));
	float hand = smoothstep(0.64, 0.84, uv.y);
	vec2 warp = vec2(sin(uv.y * 15.0 + atmosphere_time), cos(uv.x * 12.0 - atmosphere_time));
	vec3 source = texture(screen_texture, clamp(uv + warp * impact_amount * 0.0016 * edge, vec2(0.0), vec2(1.0))).rgb;
	vec3 hsv = rgb_to_hsv(source);
	hsv.y = min(hsv.y, max_saturation);
	vec3 neutral = hsv_to_rgb(hsv);
	float luminance = dot(source, vec3(0.2126, 0.7152, 0.0722));
	neutral *= luminance / max(dot(neutral, vec3(0.2126, 0.7152, 0.0722)), 0.00001);
	// Keep the readable band stable even when a bright nebula drifts behind it.
	neutral *= 1.0 - combat_band * (0.30 + focus_amount * 0.12) - hand * 0.28;
	neutral = neutral / (vec3(1.0) + neutral * combat_band * 0.85);
	float mist = 0.5 + 0.25 * sin(uv.x * 6.0 + uv.y * 4.0 + atmosphere_time * 0.035)
		+ 0.25 * sin(uv.x * 3.0 - uv.y * 7.0 - atmosphere_time * 0.024);
	float horizon = exp(-pow((uv.y - 0.53) * 5.5, 2.0));
	vec3 floor_light = mix(vec3(0.009, 0.015, 0.025), atmosphere_tint.rgb * 0.105, mist * 0.55 + horizon * 0.35);
	neutral += floor_light * (1.0 - hand * 0.70);
	neutral += atmosphere_tint.rgb * impact_amount * edge * (1.0 - hand) * 0.065;
	float vignette = smoothstep(0.40, 0.78, length((uv - 0.5) * vec2(0.92, 1.0)));
	COLOR = vec4(clamp(neutral * (1.0 - vignette * 0.22), vec3(0.0), vec3(1.0)), 1.0);
}
