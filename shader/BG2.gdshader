shader_type canvas_item;

// --- 背景与色彩 ---
uniform vec4 color_top : source_color = vec4(0.02, 0.05, 0.1, 1.0);
uniform vec4 color_mid : source_color = vec4(0.03, 0.02, 0.06, 1.0);
uniform vec4 color_bot : source_color = vec4(0.01, 0.01, 0.02, 1.0);
uniform vec4 star_color : source_color = vec4(0.6, 0.85, 1.0, 1.0);
uniform vec4 line_color : source_color = vec4(0.35, 0.55, 1.0, 0.35);

// --- 基础参数 ---
uniform float spacing = 160.0;     
uniform float density = 0.28;
uniform float star_base_size = 2.0;

// --- 运动与抗锯齿控制 ---
uniform float line_weight = 0.7;    // 核心线宽
uniform float line_softness = 1.3;  // 关键：连线软化度（值越大移动越平滑）
uniform float drift_speed = 0.45;   // 缓动速度
uniform float drift_dist = 0.22;    // 飘动幅度

// --- 1. 工具函数 (必须放在 fragment 之前) ---

vec2 hash22(vec2 p) {
    p = vec2(dot(p, vec2(127.1, 311.7)), dot(p, vec2(269.5, 183.3)));
    return fract(sin(p) * 43758.5453);
}

// 获取星体在特定单元格内的“动态位置”
vec2 get_star_pos(vec2 id) {
    vec2 rand = hash22(id);
    float t = TIME * drift_speed + rand.x * 6.2831;
    // 使用不均匀的sin/cos周期，让运动看起来更随机
    vec2 drift = vec2(sin(t), cos(t * 0.85 + rand.y)) * drift_dist;
    return 0.5 + (rand - 0.5) * 0.7 + drift;
}

// 抗锯齿画线函数：使用指数衰减代替线性切断，消除移动时的像素跳变
float draw_smooth_line(vec2 p, vec2 a, vec2 b, float weight, float softness) {
    vec2 pa = p - a, ba = b - a;
    float h = clamp(dot(pa, ba) / dot(ba, ba), 0.0, 1.0);
    float d = length(pa - ba * h);
    
    // 核心公式：使用 exp(-d^2) 产生平滑的能量场，在动态下极度稳定
    float radius = weight * softness;
    return exp(-pow(d / radius, 2.0)) * smoothstep(radius * 3.0, 0.0, d);
}

// --- 2. 主渲染逻辑 ---

void fragment() {
    // 平滑三色背景渐变
    float g_val = UV.y;
    vec4 bg = mix(color_top, color_mid, smoothstep(0.0, 0.7, g_val));
    bg = mix(bg, color_bot, smoothstep(0.4, 1.0, g_val));

    vec2 uv = FRAGCOORD.xy;
    vec2 g_uv = uv / spacing;
    vec2 i_uv = floor(g_uv);
    vec2 f_uv = fract(g_uv);

    float stars_mask = 0.0;
    float lines_mask = 0.0;

    // 遍历邻域 (3x3 或 5x5 以支持长连线)
    for (int y = -2; y <= 2; y++) {
        for (int x = -2; x <= 2; x++) {
            vec2 neighbor = vec2(float(x), float(y));
            vec2 cell_id = i_uv + neighbor;
            vec2 rand = hash22(cell_id);

            if (rand.x > density) continue;

            // 获取当前点在当前帧的准确位置
            vec2 pos = neighbor + get_star_pos(cell_id);
            
            // --- 绘制星体 (采用平滑发光感) ---
            float dist_s = length(f_uv - pos) * (spacing / star_base_size);
            stars_mask += exp(-dist_s * dist_s * 1.5);

            // --- 绘制连线 ---
            for (int ny = -1; ny <= 1; ny++) {
                for (int nx = -1; nx <= 1; nx++) {
                    if (nx == 0 && ny == 0) continue;
                    
                    vec2 n_cell_id = cell_id + vec2(float(nx), float(ny));
                    vec2 n_rand = hash22(n_cell_id);
                    
                    // 连线条件：满足密度且有一定的随机缘分
                    if (n_rand.x < density && rand.y > 0.72 && n_rand.y < 0.48) {
                        // 获取目标点在当前帧的准确位置
                        vec2 n_pos = neighbor + vec2(float(nx), float(ny)) + get_star_pos(n_cell_id);
                        
                        // 在像素空间计算平滑线段
                        float l_val = draw_smooth_line(f_uv * spacing, pos * spacing, n_pos * spacing, line_weight, line_softness);
                        
                        // 使用 max 混合确保亮度均匀，不产生叠加过载
                        lines_mask = max(lines_mask, l_val);
                    }
                }
            }
        }
    }

    // 4. 颜色合成与亮度补偿
    vec4 stars = clamp(stars_mask, 0.0, 1.0) * star_color;
    vec4 lines = clamp(lines_mask, 0.0, 1.0) * line_color;
    
    // 最终输出：背景 + 线条 + 星点
    // 加一点淡淡的 Gamma 修正让暗部发光更通透
    vec4 result = bg + lines + stars;
    COLOR *= vec4(pow(result.rgb, vec3(0.96)), result.a);
}