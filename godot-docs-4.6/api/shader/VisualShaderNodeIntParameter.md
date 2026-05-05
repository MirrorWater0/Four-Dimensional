# VisualShaderNodeIntParameter

## Meta

- Name: VisualShaderNodeIntParameter
- Source: VisualShaderNodeIntParameter.xml
- Inherits: VisualShaderNodeParameter
- Inheritance Chain: VisualShaderNodeIntParameter -> VisualShaderNodeParameter -> VisualShaderNode -> Resource -> RefCounted -> Object

## Brief Description

A visual shader node for shader parameter (uniform) of type int.

## Description

A VisualShaderNodeParameter of type int. Offers additional customization for range of accepted values.

## Quick Reference

```
[properties]
default_value: int = 0
default_value_enabled: bool = false
enum_names: PackedStringArray = PackedStringArray()
hint: int (VisualShaderNodeIntParameter.Hint) = 0
max: int = 100
min: int = 0
step: int = 1
```

## Properties

- default_value: int = 0 [set set_default_value; get get_default_value]
  Default value of this parameter, which will be used if not set externally. default_value_enabled must be enabled; defaults to 0 otherwise.

- default_value_enabled: bool = false [set set_default_value_enabled; get is_default_value_enabled]
  If true, the node will have a custom default value.

- enum_names: PackedStringArray = PackedStringArray() [set set_enum_names; get get_enum_names]
  The names used for the enum select in the editor. hint must be HINT_ENUM for this to take effect.

- hint: int (VisualShaderNodeIntParameter.Hint) = 0 [set set_hint; get get_hint]
  Range hint of this node. Use it to customize valid parameter range.

- max: int = 100 [set set_max; get get_max]
  The maximum value this parameter can take. hint must be either HINT_RANGE or HINT_RANGE_STEP for this to take effect.

- min: int = 0 [set set_min; get get_min]
  The minimum value this parameter can take. hint must be either HINT_RANGE or HINT_RANGE_STEP for this to take effect.

- step: int = 1 [set set_step; get get_step]
  The step between parameter's values. Forces the parameter to be a multiple of the given value. hint must be HINT_RANGE_STEP for this to take effect.

## Constants

### Enum Hint

- HINT_NONE = 0
  The parameter will not constrain its value.

- HINT_RANGE = 1
  The parameter's value must be within the specified min/max range.

- HINT_RANGE_STEP = 2
  The parameter's value must be within the specified range, with the given step between values.

- HINT_ENUM = 3
  The parameter uses an enum to associate preset values to names in the editor.

- HINT_MAX = 4
  Represents the size of the Hint enum.
