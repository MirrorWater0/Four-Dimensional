shader_type canvas_item;


void vertex() {

	// Called for every vertex the material is visible on.
}
float circleSDF(vec2 uv,float r){
	return length(uv) - r;
}

void fragment() {
	vec4 modulate = COLOR;
	// Called for every pixel the material is visible on.
	vec2 uv =UV -0.5;
	float circle = smoothstep(0.02,0.,circleSDF(uv,0.43))-smoothstep(0.02,0.,circleSDF(uv,0.40));
	COLOR = vec4(vec3(1.),1.)*circle*modulate;
}
