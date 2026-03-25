shader_type canvas_item;

uniform vec4 block_color : source_color = vec4(0.6, 0.8, 1.0, 1.0); // 方块颜色
uniform vec4 bg_color : source_color = vec4(0.0, 0.0, 0.0, 0.0);    // 背景颜色（默认透明）
uniform float scale : hint_range(0.5, 1.0) = 0.8;                   // 图案整体缩放
uniform float gap : hint_range(0.0, 0.5) = 0.05;                    // 四个方块之间的间距
uniform float margin : hint_range(0.0, 0.5) = 0.02;                 // 整体四周的留白

void fragment() {
    vec2 uv = (UV - vec2(0.5)) / max(scale, 0.001) + vec2(0.5);
    bool in_bounds = uv.x >= 0.0 && uv.x <= 1.0 && uv.y >= 0.0 && uv.y <= 1.0;
    
    // 计算方块的有效绘制区间
    // 横向逻辑：
    // 左侧方块：从 margin 到 0.5 - gap/2
    // 右侧方块：从 0.5 + gap/2 到 1.0 - margin
    bool in_x = (uv.x > margin && uv.x < 0.5 - gap * 0.5) || 
                (uv.x > 0.5 + gap * 0.5 && uv.x < 1.0 - margin);
                
    // 纵向逻辑：
    // 上侧方块：从 margin 到 0.5 - gap/2
    // 下侧方块：从 0.5 + gap/2 到 1.0 - margin
    bool in_y = (uv.y > margin && uv.y < 0.5 - gap * 0.5) || 
                (uv.y > 0.5 + gap * 0.5 && uv.y < 1.0 - margin);

    // 只有当横向和纵向都在方块范围内时，才涂上颜色
    if (in_bounds && in_x && in_y) {
        COLOR *= block_color;
    } else {
        COLOR *= bg_color;
    }
}
