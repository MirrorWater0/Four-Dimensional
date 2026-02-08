shader_type canvas_item;

uniform vec4 line_color : source_color = vec4(1.0, 1.0, 1.0, 1.0);
uniform float thickness_px : hint_range(0.0, 15.0) = 2.0;    // 线条粗细 (像素单位)
uniform float cut_x_px : hint_range(0.0, 200.0) = 40.0;     // 横向切口长度 (像素单位)
uniform float cut_y_px : hint_range(0.0, 200.0) = 30.0;     // 纵向切口长度 (像素单位)
uniform float bevel_px : hint_range(1.0, 40.0) = 5.0;      // 斜角的大小 (像素单位)

// 核心抗锯齿函数：在边缘处提供 1.5 像素的平滑过渡
float alpha_step(float edge, float value) {
    // 根据屏幕空间导数自动计算 1 像素在当前缩放下的宽度
    float px_unit = fwidth(value); 
    // 使用 1.5 倍像素宽度进行平滑，消除阶梯感
    return smoothstep(edge - px_unit * 1.5, edge, value);
}

void fragment() {

    // 获取当前节点在屏幕上的实际像素尺寸
    vec2 size = 1.0 / SCREEN_PIXEL_SIZE;
    vec2 pixel_uv = UV * size; // 将 UV 转换为像素坐标
    
    float final_mask = 0.0;
    float t = thickness_px;
    float b = bevel_px;

    // ----- 右上 L 型 -----
    // 顶部横线：在 (cut_x_px + 斜角偏移) 处开始
    float top_h = alpha_step(pixel_uv.y, t) * alpha_step(0.0, pixel_uv.y);
    float top_v = alpha_step(cut_x_px + (t - pixel_uv.y) * (b/t), pixel_uv.x);
    float top_line = top_h * top_v;

    // 右侧竖线：在 (size.y - cut_y_px - 斜角偏移) 处结束
    float right_v = alpha_step(size.x - t, pixel_uv.x) * alpha_step(pixel_uv.x, size.x);
    float right_h = alpha_step(pixel_uv.y, (size.y - cut_y_px) - (size.x - pixel_uv.x) * (b/t));
    float right_line = right_v * right_h;

    // ----- 左下 L 型 -----
    // 底部横线
    float bottom_h = alpha_step(size.y - t, pixel_uv.y) * alpha_step(pixel_uv.y, size.y);
    float bottom_v = alpha_step(pixel_uv.x, (size.x - cut_x_px) - (pixel_uv.y - (size.y - t)) * (b/t));
    float bottom_line = bottom_h * bottom_v;

    // 左侧竖线
    float left_v = alpha_step(0.0, pixel_uv.x) * alpha_step(pixel_uv.x, t);
    float left_h = alpha_step(cut_y_px + pixel_uv.x * (b/t), pixel_uv.y);
    float left_line = left_v * left_h;

    final_mask = clamp(top_line + right_line + bottom_line + left_line, 0.0, 1.0);

    COLOR *= vec4(line_color.rgb, final_mask * line_color.a);
}
