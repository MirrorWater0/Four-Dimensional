shader_type canvas_item;
render_mode blend_add, unshaded;

uniform float rotation_speed: hint_range(0.5, 5.0, 0.1) = 3.0;
uniform float scale: hint_range(0.5, 3.0, 0.1) = 1.0;
uniform vec3 color: source_color = vec3(0.0, 1.0, 1.0);
uniform float thickness: hint_range(0.005, 0.05, 0.0001) = 0.004;
uniform float dist2 : hint_range(0.0, 1.5, 0.1) = 1.;
uniform float dist1 : hint_range(0.0, 1.5, 0.1) = 1.;
// 2D rotation matrix
mat2 Rot(float angle) {
	float c = cos(angle);
	float s = sin(angle);
	mat2 m;
	m[0] = vec2(c, -s);
	m[1] = vec2(s, c);
	return m;
}

// Box shape
float B(vec2 p, vec2 s) {
	return max(abs(p).x - s.x, abs(p).y - s. y);
}

// Triangle shape
float Tri(vec2 p, vec2 s, float a) {
	return max(-dot(p, vec2(cos(-a), sin(-a))),
		max(dot(p, vec2(cos(a), sin(a))),
		max(abs(p).x - s.x, abs(p).y - s.y)));
}

// Distance field for flower/circle pattern
vec2 DF(vec2 p, float b) {
	float angle = atan(p.y, p.x);
	float r = length(p) * cos(mod(angle + 6.28 / (b * 8.0), 6.28 / ((b * 8.0) * 0.5))) + (b - 1.0) * 6.28 / (b * 8.0);
	return vec2(r * cos(angle), r * sin(angle));
}

// Main circular UI separator
float circleUI(vec2 p) {
	vec2 prevP = p;
	float speed = rotation_speed;
	mat2 animRot = Rot(radians(TIME * speed) * 30.0);
	p *= animRot;

	p = DF(p, 32.0);
	p -= vec2(0.28);

	float d = B(p * Rot(radians(45.0)), vec2(thickness, 0.02));

	p = prevP;
	p *= animRot;

	float a = radians(130.0);
	d = max(dot(p, vec2(cos(a), sin(a))), d);
	a = radians(-130.0);
	d = max(dot(p, vec2(cos(a), sin(a))), d);

	//p = prevP;
	//animRot = Rot(radians(TIME) * 20.0);
	//p *= animRot;

	//p = DF(p, 24.0);
	//p -= vec2(0.19);

	float d2 = B(p * Rot(radians(60.0)), vec2(thickness * 1.0, 0.00));

	//p = prevP;
	//p *= animRot;

	//a = radians(137.5);
	//d2 = max(dot(p, vec2(cos(a), sin(a))), d2);
	//a = radians(-137.5);
	//d2 = max(dot(p, vec2(cos(a), sin(a))), d2);

	//d = min(d, d2);
//
	//p = prevP;
	//animRot = Rot(-radians(TIME * speed) * 25.0);
	//p *= animRot;

	//p = DF(p, 16.0);
	//p -= vec2(0.16);
//
	//d2 = B(p * Rot(radians(45.0)), vec2(thickness, 0.01));
//
	//p = prevP;
	//p *= animRot;

	//a = radians(25.5);
	//d2 = max(dot(p, vec2(cos(a), sin(a))), d2);
	//a = radians(-25.5);
	//d2 = max(dot(p, vec2(cos(a), sin(a))), d2);

	//d = min(d, d2);

	//p = prevP;
	//animRot = Rot(radians(TIME * speed) * 35.0);
	//p *= animRot;

	//p = DF(p, 8.0);
	//p -= vec2(0.23);

	//d2 = B(p * Rot(radians(45.0)), vec2(0.02, 0.02));

	//p = prevP;
	//p *= animRot;
//
	//a = radians(40.0);
	//d2 = max(dot(p, vec2(cos(a), sin(a))), d2);
	//a = radians(-40.0);
	//d2 = max(dot(p, vec2(cos(a), sin(a))), d2);
//
	//d = min(d, d2);

	//p = prevP;
//
	//animRot = Rot(radians(TIME * speed) * 15.0);
	//p *= animRot;

	d2 = abs(length(p) - dist2 * 0.36) - thickness;
	d2 = max(abs(p.x) - 0.2, d2);
	d = min(d, d2);

	p = prevP;

	animRot = Rot(radians(90.0) + radians(TIME * speed) * 38.0);
	p *= animRot;

	d2 = abs(length(p) - dist1* 0.245) - thickness;
	d2 = max(abs(p.x) - 0.1, d2);
	d = min(d, d2);

	p = prevP;
	d2 = abs(length(p) - 0.18) - thickness * 0.5;
	d = min(d, d2);

	p = prevP;
	animRot = Rot(radians(145.0) + radians(TIME * speed) * 32.0);
	p *= animRot;
	d2 = abs(length(p) - 0.18) - thickness * 1.2;

	a = radians(30.0);
	d2 = max(dot(p, vec2(cos(a), sin(a))), d2);
	a = radians(-30.0);
	d2 = max(dot(p, vec2(cos(a), sin(a))), d2);

	d = min(d, d2);

	p = prevP;

	a = radians(TIME * speed) * 30.0;
	p.x += cos(a) * 0.45;
	p.y += sin(a) * 0.45;

	d2 = abs(Tri(p * Rot(-a) * Rot(radians(90.0)), vec2(0.02), radians(45.0))) - thickness * 0.3;
	d = min(d, d2);

	p = prevP;

	a = radians(-sin(TIME * speed * 0.5)) * 120.0;
	a += radians(-70.0);
	p.x += cos(a) * 0.45;
	p.y += sin(a) * 0.45;

	d2 = abs(Tri(p * Rot(-a) * Rot(radians(90.0)), vec2(0.02), radians(45.0))) - thickness * 0.6;
	d = min(d, d2);

	p = prevP;
	animRot = Rot(-radians(TIME * speed) * 27.0);
	p *= animRot;

	d2 = abs(length(p) - 0.43) - thickness * 0.3;
	d2 = max(abs(p.x) - 0.3, d2);
	d = min(d, d2);

	p = prevP;
	animRot = Rot(-radians(TIME * speed) * 12.0);
	p *= animRot;

	p = DF(p, 8.0);
	p -= vec2(0.103);

	d2 = B(p * Rot(radians(45.0)), vec2(thickness * 0.6, 0.007));
	d = min(d, d2);

	p = prevP;
	animRot = Rot(radians(16.8) - radians(TIME * speed) * 12.0);
	p *= animRot;

	p = DF(p, 8.0);
	p -= vec2(0.098);

	d2 = B(p * Rot(radians(45.0)), vec2(thickness * 0.5, 0.013));
	d = min(d, d2);

	p = prevP;
	animRot = Rot(radians(TIME * speed) * 30.0);
	p *= animRot;

	p = DF(p, 10.0);
	p -= vec2(0.28);

	d2 = abs(B(p * Rot(radians(45.0)), vec2(0.02, 0.02))) - thickness * 0.5;

	p = prevP;
	p *= animRot;

	a = radians(50.0);
	d2 = max(dot(p, vec2(cos(a), sin(a))), d2);
	a = radians(-50.0);
	d2 = max(dot(p, vec2(cos(a), sin(a))), d2);
	d = min(d, d2);

	return d;
}

void fragment() {
	vec2 uv = (UV - 0.5) * 2.0;
	uv /= scale;

	float d = circleUI(uv);

	// Smooth edge for anti-aliasing
	float aa = 0.001 / scale;
	float brightness = smoothstep(aa, -aa, d);

	vec3 final_color = color * brightness;

	COLOR = vec4(final_color, brightness);
}
