shader_type canvas_item;

uniform vec4 color : source_color = vec4(0.5, 0.7, 1.0, 1.0);
uniform float speed = 1.0;
uniform int teeth = 8;
uniform float tooth_length = 0.1; 
uniform float radius_percent = 0.45; 
uniform float hole_percent = 0.2;

void fragment() {
    vec2 uv = UV - 0.5;
    float angle = atan(uv.y, uv.x);
    float dist = length(uv);
    
    // 旋转
    angle += TIME * speed;
    
    // 计算当前角度对应的齿轮外边缘半径 r
    float t = cos(angle * float(teeth));
    float r = radius_percent + smoothstep(0.4, 0.6, t) * tooth_length;
    
    // === 🌟 核心修复：基于“距离场方程”的完美自适应抗锯齿 ===
    
    // 1. 齿轮外圈：
    // 计算当前像素距离边缘的真实数学差值 (d_outer)
    float d_outer = dist - r; 
    // 让 GPU 动态算出这个差值在当前像素上的变化率！
    // 无论边缘是平缓（齿顶）还是极其陡峭（齿侧），fwidth 都能精准捕捉并给出最合适的羽化宽度。
    float aa_outer = fwidth(d_outer) * 0.707; 
    float gear = smoothstep(aa_outer, -aa_outer, d_outer);
    
    // 2. 齿轮内孔：
    float d_hole = hole_percent - dist;
    float aa_hole = fwidth(d_hole) * 0.707;
    float hole = smoothstep(aa_hole, -aa_hole, d_hole);
    
    // 组合外圈和内孔
    float shape = gear * hole;
    
    // 3. 装饰内环 (也使用同样的方程求导法保证完美平滑)
    float ring_r = radius_percent * 0.7;
    float ring_w = 0.02;
    float d_ring_out = dist - (ring_r + ring_w);
    float d_ring_in  = ring_r - dist;
    
    float ring = smoothstep(fwidth(d_ring_out)*0.707, -fwidth(d_ring_out)*0.707, d_ring_out) * 
                 smoothstep(fwidth(d_ring_in)*0.707, -fwidth(d_ring_in)*0.707, d_ring_in);
    
    // 输出最终颜色
    COLOR *= vec4(color.rgb, color.a * max(shape, ring));
}
