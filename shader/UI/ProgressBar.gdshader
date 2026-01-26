shader_type canvas_item;

uniform vec4 base_color : source_color = vec4(0.2, 0.5, 0.9, 1.0); // 基础底色
uniform float glass_reflection : hint_range(0.0, 1.0) = 0.5;      // 整体反光强度
uniform float glossiness : hint_range(0.0, 1.0) = 0.3;            // 顶部高亮强度

void fragment() {
    vec2 uv = UV;
    vec4 final_col = base_color;

    // 1. 顶部边缘极细高光 (Rim Light)
    // 在 y 非常靠近 0 的地方产生一条白线
    float rim = smoothstep(0.06, 0.0, uv.y);
    final_col.rgb += rim * 0.5 * glossiness;

    // 2. 上半部分的“玻璃罩”感 (Top Gloss)
    // 给上半部分（0.0 到 0.5）增加一个较淡的覆盖层
    float top_gloss = smoothstep(0.5, 0.0, uv.y);
    final_col.rgb += top_gloss * 0.15 * glass_reflection;

    // 3. 中间的“折射横梁” (Refraction Beam)
    // 这是玻璃质感的灵魂，通常在 y = 0.2 到 0.3 之间有一条亮带
    float beam = smoothstep(0.2, 0.25, uv.y) * smoothstep(0.35, 0.3, uv.y);
    final_col.rgb += beam * 0.2 * glossiness;

    // 4. 底部微弱的暗部渐变 (Bottom Shadow)
    // 增加体积感
    float shadow = smoothstep(0.6, 1.0, uv.y);
    final_col.rgb -= shadow * 0.1;

    // 5. 动态扫光 (Moving Sheen) - 模拟环境光滑过
    // 如果不需要动态效果，可以把这一段删掉
    float sheen_pos = fract(TIME * 0.5); // 循环时间
    float sheen = smoothstep(sheen_pos - 0.1, sheen_pos, uv.x + uv.y * 0.5) * 
                  smoothstep(sheen_pos + 0.1, sheen_pos, uv.x + uv.y * 0.5);
    final_col.rgb += sheen * 0.15 * glass_reflection;

    COLOR = final_col;
}