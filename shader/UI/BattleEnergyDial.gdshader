shader_type canvas_item;
render_mode unshaded;

void fragment() {
	vec2 p = UV * 2.0 - 1.0;
	float r = length(p);
	float aa = max(fwidth(r), 0.006);
	float disk = 1.0 - smoothstep(0.77 - aa, 0.77 + aa, r);
	float rim = 1.0 - smoothstep(0.008, 0.008 + aa, abs(r - 0.80));
	float inner = 1.0 - smoothstep(0.003, 0.003 + aa, abs(r - 0.69));
	float angle = atan(p.y, p.x);
	float segment = smoothstep(0.13, 0.21, abs(sin(angle * 3.0)));
	float outer = (1.0 - smoothstep(0.014, 0.014 + aa, abs(r - 0.93))) * segment;
	float halo = exp(-r * r * 4.0) * 0.18;
	vec3 color = vec3(0.045, 0.065, 0.082);
	color = mix(color, vec3(0.56, 0.74, 0.73), max(rim * 0.72, outer * 0.82));
	color += vec3(0.12, 0.25, 0.26) * halo + inner * vec3(0.10, 0.19, 0.20);
	COLOR = vec4(color, max(disk * 0.96, max(rim, max(outer, inner * 0.4))));
}
