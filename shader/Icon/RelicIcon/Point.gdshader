shader_type canvas_item;



void fragment() {
	vec2 uv = UV -0.5;
	float a = smoothstep(0.,0.04,0.3 - length(uv));
	COLOR *= vec4(0.1,0.9,0.9,a);
}

//void light() {
//	// Called for every pixel for every light affecting the CanvasItem.
//	// Uncomment to replace the default light processing function with this one.
//}
