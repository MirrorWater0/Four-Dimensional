shader_type canvas_item;
render_mode blend_mix, unshaded;

uniform float speed: hint_range(0.1, 3.0, 0.1) = 1.0;
uniform vec4 ring_color: source_color = vec4(0.0, 1.0, 1.0, 1.0);
uniform float inner_radius: hint_range(5.0, 50.0, 1.0) = 20.0;
uniform float outer_radius: hint_range(10.0, 100.0, 1.0) = 30.0;
uniform float glow_intensity: hint_range(0.0, 2.0, 0.1) = 0.5;
uniform float glow_radius: hint_range(0.0, 50.0, 1.0) = 15.0;
uniform bool disable = false;
// Smooth step function
float SMOOTH(float r) {
	return mix(1.0, 0.0, smoothstep(0.9, 1.0, r));
}

const float M_PI = 3.1415926535897932384626433832795;

// Moving ring function
float movingRing(vec2 uv, vec2 center, float r1, float r2) {
	vec2 d = uv - center;
	float r = sqrt(dot(d, d));
	d = normalize(d);
	float theta = -atan(d.y, d.x);
	theta = mod(-TIME * speed + 0.5 * (1.0 + theta / M_PI), 1.0);
	// Anti aliasing for ring's head
	theta -= max(theta - 1.0 + 0.01, 0.0) * 100.0;
	return theta * (SMOOTH(r / r2) - SMOOTH(r / r1));
}

void fragment() {
	vec2 uv = UV * vec2(1.0, 1.0);
	vec2 center = vec2(0.5, 0.5);

	float ring = movingRing(uv, center, inner_radius / 100.0, outer_radius / 100.0);

	// Calculate glow effect
	vec2 d = uv - center;
	float dist = length(d);
	float avg_radius = (inner_radius + outer_radius) / 200.0;
	float glow = exp(-pow(dist - avg_radius, 2.0) / (2.0 * pow(glow_radius / 100.0, 2.0)));
	glow *= glow_intensity;

	// Combine ring and glow
	float combined = ring + glow;

	// Transparent background with ring and glow
	vec4 color = vec4(ring_color.rgb * (0.1 + 0.9 * combined), ring_color.a * combined);

	COLOR = color;

	if(disable){
	COLOR *= vec4(0.7);
	}
}

//void light() {
//	// Called for every pixel for every light affecting the CanvasItem.
//	// Uncomment to replace the default light processing function with this one.
//}
