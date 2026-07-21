extends SceneTree

func _initialize() -> void:
    root.size = Vector2i(420, 520)

    var background := ColorRect.new()
    background.color = Color(0.28, 0.28, 0.28, 1.0)
    background.size = Vector2(420, 520)
    root.add_child(background)

    var effect_scene := load("res://battle/Effect/BuffGainParticle.tscn") as PackedScene
    var effect := effect_scene.instantiate() as Node2D
    effect.position = Vector2(210, 330)
    root.add_child(effect)

    await process_frame
    await create_timer(0.24).timeout

    var image := root.get_texture().get_image()
    var error := image.save_png("C:/godot_project/Four-Dimensional/tmp/buff_gain_capture.png")
    print("buff_gain_capture save error: ", error)
    quit()
