shader_type canvas_item;

uniform vec4 color_left : source_color = vec4(0.0, 0.5, 0.8, 1.0);
uniform vec4 color_right : source_color = vec4(0.0, 0.8, 1.0, 1.0);
uniform vec4 color_rim : source_color = vec4(1.0, 1.0, 1.0, 1.0);
uniform float tilt_angle : hint_range(-1.5, 1.5) = -0.5;
uniform float rim_width : hint_range(0.0, 0.1) = 0.02;
uniform float shape_width : hint_range(0.1, 1.0) = 0.4;
uniform float shape_length : hint_range(0.1, 1.0) = 0.7;

void fragment() {
    // 1. 坐标变换与旋转（1.5倍缩放防止切边）
    vec2 uv = (UV - 0.5) * 0.6;
    float s = sin(tilt_angle);
    float c = cos(tilt_angle);
    uv = mat2(vec2(c, -s), vec2(s, c)) * uv;

    // 2. 构造“前大后小”的硬朗几何体 (SDF)
    // 利用绝对值实现对称
    vec2 p = vec2(abs(uv.x), uv.y);
    
    // 我们用两条相交的直线定义边缘：
    // line_top: 顶部的锐角边缘 (前大)
    // line_bottom: 尾部的收缩边缘 (后小)
    float line_top = p.x * 1.5 + p.y * 0.8 - 0.2; 
    float line_bottom = p.x * 2.5 - p.y * 0.8 - 0.2;
    
    // d 是当前点到形状边缘的距离：d < 0 表示在形状内，d > 0 表示在形状外
    float d = max(line_top, line_bottom);

    // 3. 极高质量抗锯齿计算
    // 利用 fwidth 获取像素变化率，实现无论缩放多少都只有 1 像素平滑
    float fw = fwidth(d);
    float mask = smoothstep(fw, -fw, d);
    
    // 4. 计算边框 (Rim) 和 脊线 (Spine)
    // 边框逻辑：在距离边缘 rim_width 的范围内显示
    float rim_mask = smoothstep(fw, -fw, d) * smoothstep(-fw, fw, d + rim_width);
    
    // 脊线逻辑：在中心线左右一个小范围内显示
    float spine_fw = fwidth(uv.x);
    float spine_mask = smoothstep(0.00 + spine_fw, 0.00 - spine_fw, abs(uv.x)) * mask;

    // 5. 颜色合成
    // 左右切面使用纯色，中心线处进行 1 像素平滑防止中心线锯齿
    float side_split = smoothstep(-spine_fw, spine_fw, uv.x);
    vec3 base_color = mix(color_left.rgb, color_right.rgb, side_split);
    
    // 叠加顺序：基础填充 -> 脊线 -> 边框
    vec3 final_rgb = base_color;
    final_rgb = mix(final_rgb, color_rim.rgb, spine_mask);
    final_rgb = mix(final_rgb, color_rim.rgb, rim_mask);

    // 6. 输出结果
    // 使用 mask 作为 Alpha 彻底消除外边缘锯齿
    COLOR = vec4(final_rgb, mask * color_left.a);
}
