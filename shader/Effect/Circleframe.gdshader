shader_type canvas_item;

uniform vec4 arrow_color : source_color = vec4(1.0, 0.2, 0.2, 1.0);
uniform float spin_speed : hint_range(0.0, 10.0) = 3.0; // 旋转速度
uniform float arrow_size : hint_range(0.1, 1.0) = 0.5;  // 箭头总长度
uniform float shaft_width : hint_range(0.01, 0.5) = 0.1; // 箭身宽度
uniform float head_width : hint_range(0.1, 0.8) = 0.3;  // 箭头最宽处

void fragment() {
    // 1. 坐标居中 (-0.5 到 0.5)
    vec2 p = UV - vec2(0.5);
    
    // 2. 模拟绕 Y 轴旋转的核心逻辑
    // 计算余弦值作为 X 轴的缩放因子
    float c = cos(TIME * spin_speed);
    
    // 如果想要处理“背面”效果（比如背面颜色变深），可以记录符号
    float side = sign(c); 
    
    // 缩放 X 坐标实现翻转感
    // 注意：除以 c 会让形状随旋转拉伸，乘以 c 会让形状变瘦（我们选变瘦）
    p.x /= c; 

    // 3. 绘制朝下的箭头
    // 在这个缩放后的坐标系里画一个静态朝下的箭头
    
    // 箭身 (Shaft) - 位于上方
    float shaft = step(abs(p.x), shaft_width * 0.5) * 
                  step(p.y, 0.1) * step(-arrow_size * 0.5, p.y);
    
    // 箭头 (Head) - 位于下方，尖端朝下
    // 顶点在 p.y = arrow_size * 0.5
    float head_start_y = 0.1;
    float head_end_y = arrow_size * 0.5;
    float head_t = (p.y - head_start_y) / (head_end_y - head_start_y);
    float head = step(head_start_y, p.y) * step(p.y, head_end_y) * 
                 step(abs(p.x), mix(head_width * 0.5, 0.0, head_t));

    // 合并形状
    float mask = clamp(shaft + head, 0.0, 1.0);
    
    // 4. 裁剪多余像素 (当 cos 接近 0 时，p.x 会变得无限大，需要切掉)
    if (abs(c) < 0.01) mask = 0.0;
    
    // 5. 增加简单的 3D 阴影感
    // 当翻转到背面或侧面时，降低颜色亮度
    vec3 final_color = arrow_color.rgb * mix(0.6, 1.0, abs(c));
    
    COLOR = vec4(final_color, mask * arrow_color.a);
}