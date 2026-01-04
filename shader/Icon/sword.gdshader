shader_type canvas_item;

uniform vec4 sword_color : source_color = vec4(1.0, 0.0, 0.0, 1.0);
uniform vec4 blade_color : source_color = vec4(1.0, 0.2, 0.2, 1.0);
uniform vec4 handle_color : source_color = vec4(0.3, 0.1, 0.1, 1.0);
uniform float glow_intensity : hint_range(0.0, 2.0) = 0.5;
uniform float angle : hint_range(0.0, 1.0, 0.1) = 0.;
vec2 roate(vec2 v,float theta,vec2 p){
	return mat2(vec2(cos(theta),sin(theta)),
	            vec2(sin(theta),-cos(theta)))*(v-p);
}
// 三角形 SDF (用于刀尖)
float sdTriangle(vec2 p, vec2 p0, vec2 p1, vec2 p2) {
    vec2 e0 = p1 - p0, e1 = p2 - p1, e2 = p0 - p2;
    vec2 v0 = p - p0, v1 = p - p1, v2 = p - p2;
    vec2 pq0 = v0 - e0 * clamp(dot(v0, e0) / dot(e0, e0), 0.0, 1.0);
    vec2 pq1 = v1 - e1 * clamp(dot(v1, e1) / dot(e1, e1), 0.0, 1.0);
    vec2 pq2 = v2 - e2 * clamp(dot(v2, e2) / dot(e2, e2), 0.0, 1.0);
    float s = sign(e0.x * e2.y - e0.y * e2.x);
    vec2 d = min(min(vec2(dot(pq0, pq0), s * (v0.x * e0.y - v0.y * e0.x)),
                     vec2(dot(pq1, pq1), s * (v1.x * e1.y - v1.y * e1.x))),
                     vec2(dot(pq2, pq2), s * (v2.x * e2.y - v2.y * e2.x)));
    return -sqrt(d.x) * sign(d.y);
}

// 矩形 SDF (用于刀身、护手、剑柄)
float sdBox(vec2 p, vec2 b) {
    vec2 d = abs(p) - b;
    return length(max(d, 0.0)) + min(max(d.x, d.y), 0.0);
}

// 平滑混合
float smin(float a, float b, float k) {
    float h = clamp(0.5 + 0.5 * (b - a) / k, 0.0, 1.0);
    return mix(b, a, h) - k * h * (1.0 - h);
}

void fragment() {
    vec2 uv = UV - 0.5;
    uv *= 2.0;
	uv = roate(uv,angle*2.*PI,vec2(0.));
    float width = 0.15;

    // 1. 刀尖 (Triangle)
    float top = sdTriangle(-uv + vec2(0.0, 0.3),
                                  vec2(0.0, 1.2),
                                  vec2(-width, 0.9),
                                  vec2(width, 0.9));

    // 2. 刀身 (Box)
    // 宽度设为一致 (width)，高度增加到 0.45 确保与刀尖产生物理上的“重叠”
    float body = sdBox(uv + vec2(0.0, 0.16), vec2(width-0.005, 0.45));

    // --- 关键修改：将刀尖和刀身合并为一个整体 ---
    // 使用 min 而不是 smin，因为刀刃边缘应该是直的
    float blade_dist = min(top, body);

    // 3. 其他部件
    float guard_dist = sdBox(uv + vec2(0.0, -0.25), vec2(0.4, 0.05));
    float handle_dist = sdBox(uv + vec2(0.0, -0.5), vec2(0.06, 0.2));
    float pommel_dist = length(uv + vec2(0.0, -0.75)) - 0.08;

    // 4. 整体形状组合 (用于计算 Alpha 和 Glow)
    float sword_dist = smin(blade_dist, guard_dist, 0.03);
    sword_dist = smin(sword_dist, handle_dist, 0.03);
    sword_dist = smin(sword_dist, pommel_dist, 0.03);

    // 边缘抗锯齿
    float alpha = 1.0 - smoothstep(-0.001, 0.03, sword_dist);

    // 5. 颜色逻辑
    vec4 color;

    // 使用合并后的 blade_dist 进行判断，这样交界处逻辑就是连续的
    if (blade_dist < guard_dist && blade_dist < handle_dist && blade_dist < pommel_dist) {
        // 整个刀刃区域
        color = blade_color;

    } else if (guard_dist < handle_dist && guard_dist < pommel_dist) {
        // 护手
        color = sword_color;
    } else if (handle_dist < pommel_dist) {
        // 剑柄
        color = handle_color;
    } else {
        // 圆头
        color = sword_color;
    }

    // 6. 统一的发光效果 (基于整体 sword_dist)
    float glow = smoothstep(0.12, 0.0, sword_dist) * glow_intensity;
    color.rgb += sword_color.rgb * glow * 0.5;

    color.a *= alpha;

    COLOR = color;
}