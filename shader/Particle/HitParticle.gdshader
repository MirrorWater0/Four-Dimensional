shader_type canvas_item;

// 颜色设置
uniform vec4 glow_color : source_color = vec4(1.); 
// 宽度控制（X轴方向）
uniform float width : hint_range(0.01, 1.0) = 0.5;  
// 高度控制（Y轴方向）
uniform float height : hint_range(0.1, 2.0) = 0.3; 
// 边缘硬度（数值越小越锋利）
uniform float feather : hint_range(0.0, 0.04) = 0.01; 

void fragment() {
	vec4 modulate = COLOR;
    // 1. 将 UV 居中 (范围从 0~1 变为 -0.5~0.5)
    vec2 uv = UV - 0.5;
    uv *= mat2(vec2(cos(PI/2.),sin(PI/2.)),vec2(-sin(PI/2.),cos(PI/2.)));
    // 2. 核心算法：为了让 Y 轴两端变尖
    // 我们在 Y 轴上施加 abs()，并通过一个偏移量让两个圆弧在 Y 轴交汇
    // 调整 0.5 这个常数可以改变顶部的“尖锐度”
    float shape = length(vec2(uv.x / width, abs(uv.y / height) + 0.5)) - 1.0;
    
    // 3. 生成遮罩
    float mask = smoothstep(feather, 0.0, shape);
    
    // 4. 最终输出
    COLOR = vec4(glow_color.rgb, mask * glow_color.a)*COLOR;
}
