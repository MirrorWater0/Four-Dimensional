# EditorSpinSlider

## Meta

- Name: EditorSpinSlider
- Source: EditorSpinSlider.xml
- Inherits: Range
- Inheritance Chain: EditorSpinSlider -> Range -> Control -> CanvasItem -> Node -> Object

## Brief Description

Godot editor's control for editing numeric values.

## Description

This Control node is used in the editor's Inspector dock to allow editing of numeric values. Can be used with EditorInspectorPlugin to recreate the same behavior. If the Range.step value is 1, the EditorSpinSlider will display up/down arrows, similar to SpinBox. If the Range.step value is not 1, a slider will be displayed instead.

## Quick Reference

```
[properties]
control_state: int (EditorSpinSlider.ControlState) = 0
editing_integer: bool = false
flat: bool = false
focus_mode: int (Control.FocusMode) = 2
hide_slider: bool = false
label: String = ""
read_only: bool = false
size_flags_vertical: int (Control.SizeFlags) = 1
step: float = 1.0
suffix: String = ""
```

## Properties

- control_state: int (EditorSpinSlider.ControlState) = 0 [set set_control_state; get get_control_state]
  The state in which the control used to manipulate the value will be.

- editing_integer: bool = false [set set_editing_integer; get is_editing_integer]
  If true, the EditorSpinSlider is considered to be editing an integer value. If false, the EditorSpinSlider is considered to be editing a floating-point value. This is used to determine whether a slider should be drawn by default. The slider is only drawn for floats; integers use up-down arrows similar to SpinBox instead, unless control_state is set to CONTROL_STATE_PREFER_SLIDER. It will also use EditorSettings.interface/inspector/integer_drag_speed instead of EditorSettings.interface/inspector/float_drag_speed if the slider is available.

- flat: bool = false [set set_flat; get is_flat]
  If true, the slider will not draw background.

- focus_mode: int (Control.FocusMode) = 2 [set set_focus_mode; get get_focus_mode; override Control]

- hide_slider: bool = false [set set_hide_slider; get is_hiding_slider]
  If true, the slider and up/down arrows are hidden.

- label: String = "" [set set_label; get get_label]
  The text that displays to the left of the value.

- read_only: bool = false [set set_read_only; get is_read_only]
  If true, the slider can't be interacted with.

- size_flags_vertical: int (Control.SizeFlags) = 1 [set set_v_size_flags; get get_v_size_flags; override Control]

- step: float = 1.0 [set set_step; get get_step; override Range]

- suffix: String = "" [set set_suffix; get get_suffix]
  The suffix to display after the value (in a faded color). This should generally be a plural word. You may have to use an abbreviation if the suffix is too long to be displayed.

## Signals

- grabbed()
  Emitted when the spinner/slider is grabbed.

- ungrabbed()
  Emitted when the spinner/slider is ungrabbed.

- updown_pressed()
  Emitted when the updown button is pressed.

- value_focus_entered()
  Emitted when the value form gains focus.

- value_focus_exited()
  Emitted when the value form loses focus.

## Constants

### Enum ControlState

- CONTROL_STATE_DEFAULT = 0
  The type of control used will depend on the value of editing_integer. Up-down arrows if true, a slider if false.

- CONTROL_STATE_PREFER_SLIDER = 1
  A slider will always be used, even if editing_integer is enabled.

- CONTROL_STATE_HIDE = 2
  Neither the up-down arrows nor the slider will be shown.

## Theme Items

- updown: Texture2D [icon]
  Single texture representing both the up and down buttons.

- updown_disabled: Texture2D [icon]
  Single texture representing both the up and down buttons, when the control is readonly or disabled.
