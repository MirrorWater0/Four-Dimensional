extends SpineSprite

func _ready():
	get_animation_state().set_animation("newAnimation", true)
	normal_material.set_shader_parameter("progress", 1.0)
	create_tween().tween_property(normal_material, "shader_parameter/progress", 0.0, 1)
	pass # Replace with function body.
