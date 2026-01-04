shader_type canvas_item;

uniform vec4 EXcolor : source_color = vec4(1.);

float circleSDF(vec2 uv,float r){
	return length(uv) - r;
}
void fragment() {
	vec2 uv = UV - 0.5;
	float r1 = 0.4;
	float r2 = 0.3;
	float circle1 = smoothstep(0.01,0.,circleSDF(uv,r1)) - smoothstep(0.01,0.,circleSDF(uv,r1-0.03));
	float circle2 = smoothstep(0.01 ,0.,circleSDF(uv,r2)) - smoothstep(0.01,0.,circleSDF(uv,r2-0.02));
	COLOR = circle1*EXcolor + circle2*vec4(0.9);
}

//void light() {
//	// Called for every pixel for every light affecting the CanvasItem.
//	// Uncomment to replace the default light processing function with this one.
//}
