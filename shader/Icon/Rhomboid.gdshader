shader_type canvas_item;

// 辅助函数：二维向量的“点积”变体
float ndot(vec2 a, vec2 b) { 
    return a.x * b.x - a.y * b.y; 
}

// 棱形 SDF
// p: 采样点坐标
// b: vec2(宽度的一半, 高度的一半)
float sdRhombus(in vec2 p, in vec2 b) {
    p = abs(p);
    float h = clamp(ndot(b - 2.0 * p, b) / dot(b, b), -1.0, 1.0);
    float d = length(p - 0.5 * b * vec2(1.0 - h, 1.0 + h));
    return d * sign(p.x * b.y + p.y * b.x - b.x * b.y);
}

void fragment() {
    vec2 uv = UV - 0.5;
    uv *= 2.0;
    float Rhombus = smoothstep(0.02,0.,sdRhombus(uv,vec2(0.5,0.7)));
	COLOR = Rhombus*vec4(0.6,1.,1.,1.);
}
