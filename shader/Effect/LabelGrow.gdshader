shader_type canvas_item;
render_mode unshaded;

uniform vec4 glow_color : source_color = vec4(0.0, 0.5, 1.0, 1.0);
uniform float intensity : hint_range(0.0, 20.0) = 5.0;       // 亮度强度
uniform float beam_width : hint_range(0.0, 1.0) = 0.2;      // 光柱的核心宽度
uniform float horizontal_softness : hint_range(0.1, 10.0) = 3.0; // 横向边缘柔和度
uniform float vertical_softness : hint_range(0.1, 10.0) = 1.5;   // 纵向边缘柔和度

void fragment() {
    // 1. 获取坐标，归一化到 -1.0 到 1.0 范围
    vec2 uv = UV * 2.0 - 1.0;
    
    // 2. 计算横向(X)的衰减
    // 距离中心线(x=0)越远，亮度越低
    float x_dist = abs(uv.x);
    // 使用 exp 或 pow 函数制造“中间亮，两边极快变暗”的霓虹感
    float x_glow = exp(-x_dist * horizontal_softness / beam_width);
    
    // 3. 计算纵向(Y)的衰减
    // 防止光柱在 ColorRect 边缘被硬生生切断，制造上下消失感
    float y_dist = abs(uv.y);
    float y_glow = pow(clamp(1.0 - y_dist, 0.0, 1.0), vertical_softness);
    
    // 4. 合成最终光场
    float beam = x_glow * y_glow;
    
    // 5. 核心高亮：在最中心的地方额外增加一层白光，模拟“过载”
    float core = exp(-x_dist * 20.0) * y_glow;
    
    // 合成颜色：(基础颜色 * 强度 + 核心白光) * 整体形状
    vec3 final_rgb = (glow_color.rgb * intensity + vec3(1.0) * core) * beam;
    
    // 透明度计算：直接使用 beam 作为 alpha，
    // 乘以 intensity 的一部分，让亮度高的时候发光范围更明显
    float final_a = clamp(beam * intensity * 0.5, 0.0, 1.0);
    
    COLOR = vec4(final_rgb, final_a);
}