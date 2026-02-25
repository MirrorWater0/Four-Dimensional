extends SpineSprite


func _ready():
		# 1. 必须确保 Skeleton 实例已经创建
	var skeleton = get_skeleton()
	if skeleton == null:
		# 如果 ready 时还没生成，等一帧
		await get_tree().process_frame
		skeleton = get_skeleton()

	if skeleton != null:
		# 2. 从 Skeleton 实例中获取 Data 对象
		var data = skeleton.get_data()
		# 3. 从 Data 对象获取动画列表
		var animations = data.get_animations()
		
		if animations.size() > 0:
			var first_anim_name = animations[0].get_name()
			# 4. 获取状态并播放
			var state = get_animation_state()
			state.set_animation(first_anim_name, true, 0)
			print("成功播放第一个动画: ", first_anim_name)
	else:
		push_error("无法获取 Skeleton 实例，请确认 Inspector 中已设置 Skeleton Data")
	
	normal_material.set_shader_parameter("progress", 1.0)
	create_tween().tween_property(normal_material, "shader_parameter/progress", 0.0, 1)
	pass # Replace with function body.
