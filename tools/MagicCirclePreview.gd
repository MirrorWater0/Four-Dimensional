extends Node2D

const EFFECT_SCENE = preload("res://battle/Effect/MagicCircleUnfoldVfx.tscn")
const CAPTURE_DIRECTORY = "res://asset/generated/magic_circle_preview"
var effects: Array[Node2D] = []
var elapsed := 0.0
var paused := false
var capturing := false
var scrub: HSlider
var time_label: Label
var replay_button: Button
var pause_button: Button

func _ready() -> void:
	capturing = "--capture-magic-circle" in OS.get_cmdline_user_args()
	RenderingServer.set_default_clear_color(Color("080d1b"))
	add_text("ARCANE / 01", Vector2(110, 72), 22, Color("79bdd7"))
	add_text("法阵展开", Vector2(108, 109), 58, Color("e5f3ff"))
	add_text("聚光  /  展环  /  符文点亮  /  成阵  /  消散", Vector2(112, 190), 23, Color("8c9db6"))
	create_effect(Vector2(565, 520), 254.0, 1.0)
	create_effect(Vector2(1370, 545), 302.0, 0.43)
	add_text("01  ·  俯视法阵", Vector2(415, 834), 27, Color("d3e9f4"))
	add_text("02  ·  地面投影", Vector2(1230, 834), 27, Color("d3e9f4"))
	add_text("24 枚符文 · 双向旋转 · 六角星阵", Vector2(365, 880), 20, Color("7d91aa"))
	add_text("可放在角色脚下或技能目标位置", Vector2(1193, 880), 20, Color("7d91aa"))
	build_controls()
	if capturing:
		set_process(false)
		await capture_frames()

func create_effect(center: Vector2, radius: float, ratio: float) -> void:
	var effect = EFFECT_SCENE.instantiate()
	effect.set("AutoPlay", false)
	effect.set("AutoFree", false)
	effect.set("Radius", radius)
	effect.set("GroundRatio", ratio)
	effect.position = center
	add_child(effect)
	effects.append(effect)

func add_text(text: String, location: Vector2, font_size: int, color: Color) -> Label:
	var label := Label.new()
	label.text = text
	label.position = location
	label.add_theme_font_size_override("font_size", font_size)
	label.add_theme_color_override("font_color", color)
	label.mouse_filter = Control.MOUSE_FILTER_IGNORE
	add_child(label)
	return label

func build_controls() -> void:
	replay_button = Button.new()
	replay_button.text = "重新展开"
	replay_button.position = Vector2(112, 972)
	replay_button.size = Vector2(165, 48)
	replay_button.add_theme_font_size_override("font_size", 22)
	replay_button.pressed.connect(replay)
	add_child(replay_button)
	pause_button = Button.new()
	pause_button.text = "暂停"
	pause_button.position = Vector2(295, 972)
	pause_button.size = Vector2(115, 48)
	pause_button.add_theme_font_size_override("font_size", 22)
	pause_button.pressed.connect(toggle_pause)
	add_child(pause_button)
	scrub = HSlider.new()
	scrub.position = Vector2(454, 980)
	scrub.size = Vector2(990, 32)
	scrub.min_value = 0.0
	scrub.max_value = 2.95
	scrub.step = 0.01
	scrub.value_changed.connect(scrub_to)
	add_child(scrub)
	time_label = add_text("", Vector2(1490, 977), 22, Color("9db8d0"))
	add_text("空格 暂停  ·  R 重播", Vector2(1490, 1020), 17, Color("627b96"))

func _draw() -> void:
	var faint := Color(0.13, 0.24, 0.36, 0.18)
	for x in range(110, 1811, 50):
		draw_line(Vector2(x, 265), Vector2(x, 797), faint, 1.0)
	for y in range(265, 798, 50):
		draw_line(Vector2(110, y), Vector2(1810, y), faint, 1.0)
	draw_line(Vector2(960, 288), Vector2(960, 770), Color("223347"), 1.0)
	draw_line(Vector2(110, 945), Vector2(1810, 945), Color("223347"), 1.0)
	for center in [Vector2(565, 520), Vector2(1370, 545)]:
		draw_line(center - Vector2(12, 0), center + Vector2(12, 0), Color("334a62"), 1.0)
		draw_line(center - Vector2(0, 12), center + Vector2(0, 12), Color("334a62"), 1.0)

func _process(delta: float) -> void:
	if not paused:
		elapsed = fmod(elapsed + delta, 3.65)
	show_time(minf(elapsed, 2.95))

func show_time(seconds: float) -> void:
	for effect in effects:
		effect.call("Seek", seconds)
	scrub.set_value_no_signal(seconds)
	var phase := "聚光"
	if seconds >= 2.95: phase = "等待重播"
	elif seconds >= 2.30: phase = "消散"
	elif seconds >= 1.05: phase = "成阵"
	elif seconds >= 0.48: phase = "符文点亮"
	elif seconds >= 0.12: phase = "展环"
	time_label.text = "%04.2f s  /  %s" % [seconds, phase]

func replay() -> void:
	elapsed = 0.0
	paused = false
	pause_button.text = "暂停"
	show_time(0.0)

func toggle_pause() -> void:
	paused = not paused
	pause_button.text = "继续" if paused else "暂停"

func scrub_to(seconds: float) -> void:
	paused = true
	pause_button.text = "继续"
	elapsed = seconds
	show_time(seconds)

func _unhandled_key_input(event: InputEvent) -> void:
	if event is InputEventKey and event.pressed and not event.echo:
		if event.keycode == KEY_SPACE: toggle_pause()
		elif event.keycode == KEY_R: replay()

func capture_frames() -> void:
	DirAccess.make_dir_recursive_absolute(ProjectSettings.globalize_path(CAPTURE_DIRECTORY))
	var times := [0.18, 0.55, 1.10, 1.85, 2.70]
	for i in range(times.size()):
		show_time(times[i])
		await get_tree().process_frame
		await RenderingServer.frame_post_draw
		var image := get_viewport().get_texture().get_image()
		var path := "%s/%02d.png" % [CAPTURE_DIRECTORY, i + 1]
		var error := image.save_png(path)
		if error != OK:
			push_error("法阵截图保存失败: %s" % error_string(error))
			get_tree().quit(1)
			return
	print("[MagicCirclePreview] Captured five animation stages: ", CAPTURE_DIRECTORY)
	get_tree().quit()
