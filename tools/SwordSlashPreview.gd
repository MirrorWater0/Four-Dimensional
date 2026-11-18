extends Control

@onready var effects: Array[Node2D] = [
	$Stages/Isolated/Slash, $Stages/Isolated/Slash2, $Stages/Isolated/Slash3, $Stages/Isolated/Slash4,
	$Stages/Target/Slash, $Stages/Target/Slash2, $Stages/Target/Slash3, $Stages/Target/Slash4
]
@onready var timeline: HSlider = $Controls/Timeline
@onready var pause_button: Button = $Controls/Pause
@onready var time_label: Label = $Controls/Time
@onready var speed: OptionButton = $Controls/Speed
@onready var palette: OptionButton = $Controls/Palette
@onready var pattern: OptionButton = $Controls/Pattern
@onready var length_slider: HSlider = $Controls/Length
@onready var thickness_slider: HSlider = $Controls/Thickness

const HIT_INTERVAL := 0.13
var elapsed := 0.0
var paused := false
var duration := 0.6
var playback_speed := 1.0
var scene_energy_color := Color.WHITE
var sequence_seed := 137
var hit_count := 4
var hit_durations: Array[float] = []

func _ready() -> void:
	var preset := effects[0]
	length_slider.set_value_no_signal(float(preset.get("Length")))
	thickness_slider.set_value_no_signal(float(preset.get("Thickness")))
	$Controls/LengthValue.text = "%d" % int(length_slider.value)
	$Controls/ThicknessValue.text = "%0.2f" % thickness_slider.value
	$Controls/Direction.set_value_no_signal(float(preset.get("AngleDegrees")))
	$Controls/Mirror.set_pressed_no_signal(bool(preset.get("Mirror")))
	$Controls/Style.select(int(preset.get("Style")))
	scene_energy_color = preset.get("EnergyColor")
	$Controls/Replay.pressed.connect(replay)
	pause_button.pressed.connect(toggle_pause)
	timeline.value_changed.connect(scrub_to)
	$Controls/Direction.value_changed.connect(change_angle)
	$Controls/Mirror.toggled.connect(change_mirror)
	speed.item_selected.connect(change_speed)
	palette.item_selected.connect(change_palette)
	$Controls/Style.item_selected.connect(change_style)
	pattern.item_selected.connect(change_pattern)
	$Controls/Shuffle.pressed.connect(shuffle_sequence)
	length_slider.value_changed.connect(change_length)
	thickness_slider.value_changed.connect(change_thickness)
	prepare_sequence(true)

func prepare_sequence(new_seed: bool = false) -> void:
	if new_seed:
		sequence_seed = randi_range(1, 2147483646)
	hit_count = [1, 2, 4][pattern.selected]
	duration = 0.0
	hit_durations.resize(effects.size())
	for index in range(effects.size()):
		var hit_index := index % 4
		var effect := effects[index]
		effect.set("Length", length_slider.value)
		effect.set("Thickness", thickness_slider.value)
		effect.call("SetSequenceVariant", sequence_seed, hit_index, hit_count)
		hit_durations[index] = float(effect.call("GetDuration"))
		if hit_index < hit_count:
			duration = maxf(duration, hit_index * HIT_INTERVAL + hit_durations[index])
	timeline.set_block_signals(true)
	timeline.max_value = duration
	timeline.set_block_signals(false)
	elapsed = 0.0
	show_time(0.0)

func _process(delta: float) -> void:
	if not paused:
		elapsed = fmod(elapsed + delta * playback_speed, duration + 0.5)
		show_time(minf(elapsed, duration))

func show_time(seconds: float) -> void:
	for index in range(effects.size()):
		var hit_index := index % 4
		var local_time := seconds - hit_index * HIT_INTERVAL
		if hit_index >= hit_count or local_time < 0.0 or local_time >= hit_durations[index]:
			effects[index].call("Stop")
		else:
			effects[index].call("Seek", local_time)
	timeline.set_value_no_signal(seconds)
	var frame_count := 4 if $Controls/Style.selected == 0 else 6
	var hit := mini(int(seconds / HIT_INTERVAL), hit_count - 1)
	var current_hit_time := maxf(seconds - hit * HIT_INTERVAL, 0.0)
	var frame := mini(int(current_hit_time / hit_durations[hit] * frame_count), frame_count - 1) + 1
	var phase := "%d/%d 刀 · %d/%d 帧" % [hit + 1, hit_count, frame, frame_count]
	if seconds >= duration: phase = "等待重播"
	time_label.text = "%0.2f s  ·  %s" % [seconds, phase]

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

func change_angle(degrees: float) -> void:
	for effect in effects:
		effect.set("AngleDegrees", degrees)
	show_time(minf(elapsed, duration))

func change_mirror(enabled: bool) -> void:
	for effect in effects:
		effect.set("Mirror", enabled)
	show_time(minf(elapsed, duration))

func change_length(value: float) -> void:
	for effect in effects:
		effect.set("Length", value)
	$Controls/LengthValue.text = "%d" % int(value)
	show_time(minf(elapsed, duration))

func change_thickness(value: float) -> void:
	for effect in effects:
		effect.set("Thickness", value)
	$Controls/ThicknessValue.text = "%0.2f" % value
	show_time(minf(elapsed, duration))

func change_speed(index: int) -> void:
	playback_speed = [0.25, 0.5, 1.0][index]

func change_palette(index: int) -> void:
	var colors := [scene_energy_color, Color("ff404b"), Color("80dbff"), Color("a380ff")]
	for effect in effects:
		effect.set("EnergyColor", colors[index])
	show_time(minf(elapsed, duration))

func change_style(index: int) -> void:
	for effect in effects:
		effect.set("Style", index)
	prepare_sequence()

func change_pattern(_index: int) -> void:
	prepare_sequence()

func shuffle_sequence() -> void:
	prepare_sequence(true)

func _unhandled_key_input(event: InputEvent) -> void:
	if event is InputEventKey and event.pressed and not event.echo:
		if event.keycode == KEY_SPACE: toggle_pause()
		elif event.keycode == KEY_R: replay()
