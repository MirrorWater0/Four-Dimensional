shader_type canvas_item;

uniform vec4 shape_color : source_color = vec4(0.0, 0.8, 1.0, 1.0);
uniform float hover : hint_range(0.0, 1.0) = 0.0;
uniform float line_thickness : hint_range(0.0, 0.05) = 0.01;

// --- 速度与角度常量 ---
const float ROT_SPEED_INNER = 0.6;
const float ROT_SPEED_OUTER = -0.9;
const float HOVER_ROT_OFFSET = 0.7; // Hover时旋转的冲劲

mat2 rotate(float a) {
    float s = sin(a), c = cos(a);
    return mat2(vec2(c, -s), vec2(s, c));
}

float sdBoxOutline(vec2 p, vec2 b, float th) {
    vec2 d = abs(p) - b;
    float dist = length(max(d, 0.0)) + min(max(d.x, d.y), 0.0);
    return abs(dist) - th;
}

float sdSegment(vec2 p, vec2 a, vec2 b) {
    vec2 pa = p - a, ba = b - a;
    float h = clamp(dot(pa, ba) / dot(ba, ba), 0.0, 1.0);
    return length(pa - ba * h);
}

void fragment() {
    // 1. 速度感逻辑：fast_t 让位移在前半段极快，产生“弹射”感
    float t = hover;
    float fast_t = 1.0 - pow(1.0 - t, 3.0); // 类似 EaseOutQuart，产生速度感
    
    vec2 uv = (UV - 0.5);
    float aa = fwidth(uv.x) * 1.5;
    float current_thick = line_thickness * mix(1.0, 1.2, t);

    float final_mask = 0.0;

    // ------------------------------------------------
    // 【主体 A】：内层核心 (适度扩张)
    // ------------------------------------------------
    // 旋转带一点快慢偏移
    float angle_in = TIME * ROT_SPEED_INNER + fast_t * HOVER_ROT_OFFSET;
    vec2 uv_in = uv * rotate(angle_in);
    
    // 尺寸从 0.12 扩张到 0.18 (不再夸张)
    float core_size = mix(0.12, 0.18, fast_t);
    float d_core = sdBoxOutline(uv_in, vec2(core_size), current_thick);
    
    // 内部的小细节 (中心点)
    float d_dot = length(uv) - current_thick * 1.5;
    final_mask = max(final_mask, smoothstep(aa, 0.0, d_core));
    final_mask = max(final_mask, smoothstep(aa, 0.0, d_dot));

    // ------------------------------------------------
    // 【主体 B】：外层圆环框架
    // ------------------------------------------------
    float angle_out = TIME * ROT_SPEED_OUTER - fast_t * HOVER_ROT_OFFSET;
    vec2 uv_out = uv * rotate(angle_out);
    
    float orbit_rad = mix(0.18, 0.25, fast_t);
    float d_orbit = abs(length(uv)) - orbit_rad;
    
    // 环切断效果：在旋转坐标系下切除，增加动态
    float sector = step(0.4, sin(atan(uv_out.y, uv_out.x) * 4.0));
    final_mask = max(final_mask, smoothstep(aa, 0.0, abs(d_orbit) - current_thick * 0.7) * sector);

    // ------------------------------------------------
    // 【零件 C】：四角 L 型支架 (重做的速度感零件)
    // ------------------------------------------------
    vec2 uv_corner = abs(uv);
    
    // 初始位置 0.18，弹出到 0.35
    float spread = mix(0.18, 0.35, fast_t);
    uv_corner -= vec2(spread);
    
    // 关键：弹出时伴随旋转，且旋转速度随 fast_t 变化
    uv_corner *= rotate(mix(PI * 0.5, 0.0, fast_t));
    
    // 绘制上一版精良的 L 型零件
    float arm = 0.04;
    float d_bracket = sdSegment(uv_corner, vec2(-arm, 0.0), vec2(arm, 0.0));
    d_bracket = min(d_bracket, sdSegment(uv_corner, vec2(arm, 0.0), vec2(arm, arm)));
    
    final_mask = max(final_mask, smoothstep(aa, 0.0, d_bracket - current_thick * 0.8));

    // ------------------------------------------------
    // 视觉装饰：扫描线与亮度
    // ------------------------------------------------
    vec3 color = shape_color.rgb;
    
    // Hover时亮度提升，并在边缘产生轻微“过载”感
    color *= mix(1.0, 1.6, fast_t);
    
    // 增加一个极细的对角线扫描光点
    float scan = sin((uv.x + uv.y) * 10.0 - TIME * 4.0) * 0.1 * fast_t;
    color += scan;
    
    // 简单的呼吸感
    color *= (0.95 + 0.05 * sin(TIME * 3.0));

    COLOR = vec4(color, final_mask * shape_color.a);
}
