# Range

## Meta

- Name: Range
- Source: Range.xml
- Inherits: Control
- Inheritance Chain: Range -> Control -> CanvasItem -> Node -> Object

## Brief Description

Abstract base class for controls that represent a number within a range.

## Description

Range is an abstract base class for controls that represent a number within a range, using a configured step and page size. See e.g. ScrollBar and Slider for examples of higher-level nodes using Range.

## Quick Reference

```
[methods]
_value_changed(new_value: float) -> void [virtual]
set_value_no_signal(value: float) -> void
share(with: Node) -> void
unshare() -> void

[properties]
allow_greater: bool = false
allow_lesser: bool = false
exp_edit: bool = false
max_value: float = 100.0
min_value: float = 0.0
page: float = 0.0
ratio: float
rounded: bool = false
size_flags_vertical: int (Control.SizeFlags) = 0
step: float = 0.01
value: float = 0.0
```

## Methods

- _value_changed(new_value: float) -> void [virtual]
  Called when the Range's value is changed (following the same conditions as value_changed).

- set_value_no_signal(value: float) -> void
  Sets the Range's current value to the specified value, without emitting the value_changed signal.

- share(with: Node) -> void
  Binds two Ranges together along with any ranges previously grouped with either of them. When any of range's member variables change, it will share the new value with all other ranges in its group.

- unshare() -> void
  Stops the Range from sharing its member variables with any other.

## Properties

- allow_greater: bool = false [set set_allow_greater; get is_greater_allowed]
  If true, value may be greater than max_value.

- allow_lesser: bool = false [set set_allow_lesser; get is_lesser_allowed]
  If true, value may be less than min_value.

- exp_edit: bool = false [set set_exp_ratio; get is_ratio_exp]
  If true, and min_value is greater or equal to 0, value will be represented exponentially rather than linearly.

- max_value: float = 100.0 [set set_max; get get_max]
  Maximum value. Range is clamped if value is greater than max_value.

- min_value: float = 0.0 [set set_min; get get_min]
  Minimum value. Range is clamped if value is less than min_value.

- page: float = 0.0 [set set_page; get get_page]
  Page size. Used mainly for ScrollBar. A ScrollBar's grabber length is the ScrollBar's size multiplied by page over the difference between min_value and max_value.

- ratio: float [set set_as_ratio; get get_as_ratio]
  The value mapped between 0 and 1.

- rounded: bool = false [set set_use_rounded_values; get is_using_rounded_values]
  If true, value will always be rounded to the nearest integer.

- size_flags_vertical: int (Control.SizeFlags) = 0 [set set_v_size_flags; get get_v_size_flags; override Control]

- step: float = 0.01 [set set_step; get get_step]
  If greater than 0.0, value will always be rounded to a multiple of this property's value above min_value. For example, if min_value is 0.1 and step is 0.2, then value is limited to 0.1, 0.3, 0.5, and so on. If rounded is also true, value will first be rounded to a multiple of this property's value, then rounded to the nearest integer.

- value: float = 0.0 [set set_value; get get_value]
  Range's current value. Changing this property (even via code) will trigger value_changed signal. Use set_value_no_signal() if you want to avoid it.

## Signals

- changed()
  Emitted when min_value, max_value, page, or step change.

- value_changed(value: float)
  Emitted when value changes. When used on a Slider, this is called continuously while dragging (potentially every frame). If you are performing an expensive operation in a function connected to value_changed, consider using a *debouncing* Timer to call the function less often. **Note:** Unlike signals such as LineEdit.text_changed, value_changed is also emitted when value is set directly via code.
