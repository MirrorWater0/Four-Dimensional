shader_type canvas_item;

// --- 颜色与强度 ---
uniform vec4 main_color : source_color = vec4(1.0, 0.2, 0.1, 1.0);
uniform float line_thickness : hint_range(0.0, 0.05) = 0.01;
uniform float glow_height : hint_range(0.01, 1.0) = 0.4;
uniform float glow_intensity : hint_range(0.0, 10.0) = 4.0;

// --- 形状参数 ---
uniform float base_top_y : hint_range(0.0, 1.0) = 0.4;
uniform float base_bottom_y : hint_range(0.0, 1.0) = 0.8;
uniform float base_top_width : hint_range(0.0, 1.0) = 0.4;
uniform float base_bottom_width : hint_range(0.0, 1.0) = 0.8;

// --- 核心同步参数 ---
uniform float skew_strength : hint_range(-1.0, 1.0) = 0.25;
uniform float margin : hint_range(0.0, 0.5) = 0.15; 

// 线段距离函数（绘制红框）
float df_line(vec2 p, vec2 a, vec2 b) {
    vec2 pa = p - a, ba = b - a;
    float h = clamp(dot(pa,ba)/dot(ba,ba), 0.0, 1.0);
    return length(pa - ba*h);
}

// 核心函数：计算每一面墙的光效，严格限制在边的 X 范围内
float get_wall_glow(vec2 uv, vec2 pA, vec2 pB, float h) {
    float min_x = min(pA.x, pB.x);
    float max_x = max(pA.x, pB.x);
    
    // 严格 X 轴判定：如果像素不在该线段的横向投影内，直接不发光
    // 这样就解决了侧墙光效“飞”出去的问题
    if (uv.x < min_x - 0.001 || uv.x > max_x + 0.001) return 0.0;
    
    // 计算当前 X 坐标下对应的底边 Y 坐标
    float base_y;
    if (abs(pB.x - pA.x) < 0.0001) {
        base_y = mix(pA.y, pB.y, clamp((uv.y - pA.y)/(pB.y - pA.y), 0.0, 1.0));
    } else {
        float t = (uv.x - pA.x) / (pB.x - pA.x);
        base_y = mix(pA.y, pB.y, t);
    }
    
    // 判定：像素必须在底边上方，且在高度范围内
    if (uv.y <= base_y + 0.001 && uv.y >= base_y - h) {
        float dist_ratio = (base_y - uv.y) / h;
        return pow(1.0 - dist_ratio, 2.5);
    }
    return 0.0;
}

void fragment() {
    // 1. 坐标缩放与居中，margin 确保上方发光不被节点边缘切断
    vec2 uv = (UV - 0.5) * (1.0 + margin * 2.0) + 0.5;
    
    // 2. 顶点计算（实现蓝色草图的倾斜透视）
    float shift = (base_bottom_y - base_top_y) * skew_strength;
    
    vec2 p1 = vec2(0.5 - base_top_width * 0.5, base_top_y);               // 左上
	vec2 p2 = vec2(0.5 + base_top_width * 0.5, base_top_y);               // 右上
	vec2 p3 = vec2(0.5 + base_bottom_width * 0.5 + shift, base_bottom_y); // 右下
	vec2 p4 = vec2(0.5 - base_bottom_width * 0.5 + shift, base_bottom_y); // 左下

    // 3. 绘制底座实线框
    float d = min(min(df_line(uv, p1, p2), df_line(uv, p4, p3)), 
                  min(df_line(uv, p1, p4), df_line(uv, p2, p3)));
    float line_mask = smoothstep(line_thickness, line_thickness * 0.5, d);

    // 4. 计算四面墙光效并融合
    // 每一面墙现在都自带严格的裁剪，不再需要全局 clip_mask
    float glow = 0.0;
    glow = max(glow, get_wall_glow(uv, p1, p2, glow_height)); // 后墙
    glow = max(glow, get_wall_glow(uv, p1, p4, glow_height)); // 左墙
    glow = max(glow, get_wall_glow(uv, p2, p3, glow_height)); // 右墙
    glow = max(glow, get_wall_glow(uv, p4, p3, glow_height)); // 前墙

    // 5. 最终合成
    // canvas_mask 保证只渲染 UV 范围内的像素
    float canvas_mask = step(0.0, uv.x) * step(uv.x, 1.0) * step(0.0, uv.y) * step(uv.y, 1.0);
    
    vec3 color = main_color.rgb;
    // 叠加线条和光墙
    float alpha = clamp(line_mask + glow * glow_intensity, 0.0, 1.0) * main_color.a * canvas_mask;
    
    COLOR = vec4(color, alpha);
}