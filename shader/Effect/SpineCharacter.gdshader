shader_type canvas_item;

// 这里我们需要手动设置角色的估计高度（像素单位）
// 比如你的角色在 Godot 里大约高 200 像素
uniform float character_height = 2500.0; 
uniform float progress : hint_range(0.0, 1.0) = 0.0;
uniform float line_density = 80.0;
uniform vec4 beam_color : source_color = vec4(0.0, 1.0, 1.0, 1.0);

// 我们需要通过 varying 把局部坐标传给 fragment
varying float v_y_pos;
varying vec4 modulate;
void vertex() {
    // 获取相对于节点中心的 Y 坐标
    v_y_pos = VERTEX.y;
	modulate = COLOR;
}

void fragment() {
    // 将 v_y_pos 映射到 0.0 - 1.0 范围
    // 注意：Godot 2D 中 Y 轴向下为正，所以需要调整逻辑
    // 假设我们要从脚部（正 Y）向头部（负 Y）传送
    float normalized_y = (v_y_pos / (character_height * 0.5)) * -0.5 + 0.5;
    
    float p = progress;
    float mask_p = p * 1.2 - 0.1;
    
    // 消融遮罩
    float alpha_mask = smoothstep(mask_p, mask_p + 0.1, normalized_y);
    
    // 采样原始颜色 (Spine 的贴图采样)
    vec4 color = texture(TEXTURE, UV);
    
    // 竖线逻辑
    float lines = step(0.95, sin(UV.x * line_density + TIME * 10.0));
    float line_effect = lines * color.a * p;

    vec3 final_rgb = mix(color.rgb, beam_color.rgb * 4.0, line_effect);
    
    COLOR = vec4(final_rgb, color.a * alpha_mask)*modulate;
}
