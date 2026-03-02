shader_type canvas_item;
uniform sampler2D noise;

varying vec4 modulate;
void vertex(){
	modulate = COLOR;
}
float circle(vec2 uv,float r,vec2 p){
	return 1. - smoothstep(r,r+0.008,length(uv-p));
}

float rect(vec2 st,vec2 size,vec2 p){
	st -= p;                     // 中心点归零
    st = abs(st) - size * 0.5;        // 计算轴向距离分量
    float sdf = length(max(st, 0.0))       // 外部距离
         + min(max(st.x, st.y), 0.0); // 内部距离修正
	return smoothstep(0.001,0.,sdf);
}
vec2 roate(vec2 v,float theta,vec2 p){
	return mat2(vec2(sin(theta),cos(theta)),
	            vec2(-cos(theta),sin(theta)))*(v-p);
}
float ring(vec2 uv,float time){
	vec2 p =vec2(0.0);
	vec2 uv1 = 0.6*roate(uv,time,vec2(0.5));
	float col = clamp(circle(uv1,0.26,p)-circle(uv1,0.2,p)-rect(uv1,vec2(0.15,0.7),p)-rect(uv1,vec2(0.43,0.2),p),0.,1.);
	vec2 uv2 = 1.*roate(uv,time/2.,vec2(0.5));
	col += clamp(circle(uv2,0.3,p)-circle(uv2,0.2,p)-rect(uv2,vec2(0.15,0.7),p)-rect(uv2,vec2(0.43,0.2),p),0.,1.);
	return col;
}

void fragment() {
	COLOR = vec4(1.);
	vec2 noise_uv = UV;
	noise_uv = abs(sin(noise_uv+TIME));
	vec4 noise_tex = texture(noise,noise_uv);
	float noiseF = pow(noise_tex.r,10.);
	
	
	vec2 uv = UV;
	float ring1 = ring(uv,TIME);
	COLOR.rgb = vec3(0.5,0.6,0.7+sin(TIME)/8.) + vec3(0.1);
	COLOR.a = ring1; 
	//vec4 cyc = vec4(0.9,0.8,0.8,1.)*circle(uv,0.08,vec2(0.5));
	//COLOR += cyc;
	COLOR *= modulate;
}

//void light() {
	// Called for every pixel for every light affecting the CanvasItem.
	// Uncomment to replace the default light processing function with this one.
//}
