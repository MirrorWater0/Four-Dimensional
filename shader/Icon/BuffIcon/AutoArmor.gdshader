shader_type canvas_item;

uniform vec4 block_color : source_color = vec4(0.6, 0.8, 1.0, 1.0); // 方块颜色
uniform vec4 bg_color : source_color = vec4(0.0, 0.0, 0.0, 0.0);    // 背景颜色（默认透明）
uniform float scale : hint_range(0.5, 1.0) = 0.8;                   // 图案整体缩放
uniform float gap : hint_range(0.0, 0.5) = 0.05;                    // 四个方块之间的间距
uniform float margin : hint_range(0.0, 0.5) = 0.02;                 // 整体四周的留白

float rect_mask(float value, float start, float end, float aa) {
    return smoothstep(start - aa, start + aa, value) * (1.0 - smoothstep(end - aa, end + aa, value));
}

void fragment() {
    vec2 uv = (UV - vec2(0.5)) / max(scale, 0.001) + vec2(0.5);
    float aa = max(max(fwidth(uv.x), fwidth(uv.y)), 0.0015);
    
    float bounds_x = rect_mask(uv.x, 0.0, 1.0, aa);
    float bounds_y = rect_mask(uv.y, 0.0, 1.0, aa);
    float in_bounds = bounds_x * bounds_y;

    float left_x = rect_mask(uv.x, margin, 0.5 - gap * 0.5, aa);
    float right_x = rect_mask(uv.x, 0.5 + gap * 0.5, 1.0 - margin, aa);
    float top_y = rect_mask(uv.y, margin, 0.5 - gap * 0.5, aa);
    float bottom_y = rect_mask(uv.y, 0.5 + gap * 0.5, 1.0 - margin, aa);

    float in_x = max(left_x, right_x);
    float in_y = max(top_y, bottom_y);
    float mask = in_bounds * in_x * in_y;

    COLOR *= mix(bg_color, block_color, mask);
}
