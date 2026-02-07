shader_type canvas_item;

// 颜色控制
uniform vec4 ring_color : source_color = vec4(1.0, 1.0, 1.0, 1.0);
uniform vec4 inner_color : source_color = vec4(1.0, 1.0, 1.0, 1.0);

// 形状控制
uniform float ring_size : hint_range(0.1, 1.0) = 0.8;          // 整个环的大小
uniform float ring_thickness : hint_range(0.01, 0.5) = 0.05;   // 环的厚度
uniform float inner_square_size : hint_range(0.0, 1.0) = 0.4; // 内部实心正方形大小
uniform bool show_inner = true;                               // 内部显示开关

// 抗锯齿平滑度
uniform float smoothness_offset : hint_range(0.0, 2.0) = 1.0;

void fragment() {
	// 将 UV 居中并缩放到 [-1, 1] 范围
	vec2 uv = (UV - 0.5) * 2.0;
	
	// 计算菱形（旋转45度的正方形）距离场
	// |x| + |y| 形成一个对角线垂直的正方形
	float d = abs(uv.x) + abs(uv.y);
	
	// --- 高质量抗锯齿逻辑 ---
	// 使用 fwidth 计算像素梯度，确保在任何分辨率和缩放下边缘平滑且不抖动
	float fw = length(vec2(dFdx(d), dFdy(d))) * smoothness_offset;
	
	// 1. 计算外部环 (Ring) 的遮罩
	// 环由两个菱形相减而成
	float ring_outer_edge = ring_size;
	float ring_inner_edge = ring_size - ring_thickness;
	
	float ring_outer_mask = clamp(0.5 - (d - ring_outer_edge) / fw, 0.0, 1.0);
	float ring_inner_mask = clamp(0.5 - (d - ring_inner_edge) / fw, 0.0, 1.0);
	float ring_final_mask = ring_outer_mask - ring_inner_mask;
	
	// 2. 计算内部实心正方形遮罩
	float inner_mask = clamp(0.5 - (d - inner_square_size) / fw, 0.0, 1.0);
	if (!show_inner) {
		inner_mask = 0.0;
	}

	// --- 颜色合成 ---
	vec4 final_color = vec4(0.0);
	
	// 混合环的颜色
	final_color = mix(final_color, ring_color, ring_final_mask);
	
	// 混合内部实心正方形颜色 (通过 Alpha 混合覆盖在上方)
	// 如果需要内部在环下方，可以交换顺序
	final_color = mix(final_color, inner_color, inner_mask);

	// 应用 Godot 节点的 Modulate 颜色
	COLOR = final_color * COLOR;
} 
