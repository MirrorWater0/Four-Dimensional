shader_type canvas_item;

uniform vec4 heart_color : source_color = vec4(0.75, 0.1, 0.1, 1.0);
uniform float cut_width : hint_range(0.0, 0.3) = 0.06;
uniform float cut_slope : hint_range(-2.0, 2.0) = 0.8;

void fragment() {
    // 1. 坐标初始化 (2.2 倍缩放，确保不贴边)
    vec2 uv = (UV - vec2(0.5, 0.75)) * 2.; 
    uv.y *= -1.1; 
	uv.y += 0.1;
    // 2. 极坐标转换 (abs(uv.x) 保证镜像对称)
    float theta = atan(uv.y, abs(uv.x));
    float r = length(uv);

    float heart_mask = 0.0;

    // 3. 极坐标方程核心逻辑 (优化版)
    if (theta > 0.0 && theta < PI) {
        float t = tan(theta);
        // 使用 log 代替 pow，并加入一个小偏移量防止分母为 0
        // r = exp(ln(tan) / tan)，当 tan 很大时，ln(tan)/tan 趋近 0，r 趋近 exp(0)=1
        float t_safe = max(abs(t), 0.0001);
        float r_limit = exp(log(t_safe) / t_safe);
        
        // --- 核心修复：动态抗锯齿 ---
        // fwidth 会根据像素密度自动计算平滑范围，解决凹陷处的锯齿
        float dist = r - r_limit;
        float edge_softness = fwidth(dist) * 1.5; 
        heart_mask = smoothstep(edge_softness, -edge_softness, dist);
    }

    // 4. 破碎切口遮罩 (优化平滑参数)
    float line_val = uv.x + (uv.y - 0.5) * cut_slope;
    // 确保 smoothstep 的第一个参数不比第二个大太多
    float cut_aa = fwidth(line_val) * 1.5;
    float cut_mask = smoothstep(cut_width - cut_aa, cut_width + cut_aa, abs(line_val));

    // 5. 颜色处理与输出
    // 增加细微的明暗变化
    vec3 color = heart_color.rgb;
    color *= (1.0 - length(uv) * 0.1); 
    
    // 使用最终混合结果赋值给 COLOR
    COLOR *= vec4(color, heart_mask * cut_mask);
}