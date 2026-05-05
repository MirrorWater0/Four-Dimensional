# VisualShaderNodeFloatParameter

## Meta

- Name: VisualShaderNodeFloatParameter
- Source: VisualShaderNodeFloatParameter.xml
- Inherits: VisualShaderNodeParameter
- Inheritance Chain: VisualShaderNodeFloatParameter -> VisualShaderNodeParameter -> VisualShaderNode -> Resource -> RefCounted -> Object

## Brief Description

A scalar float parameter to be used within the visual shader graph.

## Description

Translated to uniform float in the shader language.

## Quick Reference

```
[properties]
default_value: float = 0.0
default_value_enabled: bool = false
hint: int (VisualShaderNodeFloatParameter.Hint) = 0
max: float = 1.0
min: float = 0.0
step: float = 0.1
```

## Properties

- default_value: float = 0.0 [set set_default_value; get get_default_value]
  A default value to be assigned within the shader.

- default_value_enabled: bool = false [set set_default_value_enabled; get is_default_value_enabled]
  Enables usage of the default_value.

- hint: int (VisualShaderNodeFloatParameter.Hint) = 0 [set set_hint; get get_hint]
  A hint applied to the uniform, which controls the values it can take when set through the Inspector.

- max: float = 1.0 [set set_max; get get_max]
  Minimum value for range hints. Used if hint is set to HINT_RANGE or HINT_RANGE_STEP.

- min: float = 0.0 [set set_min; get get_min]
  Maximum value for range hints. Used if hint is set to HINT_RANGE or HINT_RANGE_STEP.

- step: float = 0.1 [set set_step; get get_step]
  Step (increment) value for the range hint with step. Used if hint is set to HINT_RANGE_STEP.

## Constants

### Enum Hint

- HINT_NONE = 0
  No hint used.

- HINT_RANGE = 1
  A range hint for scalar value, which limits possible input values between min and max. Translated to hint_range(min, max) in shader code.

- HINT_RANGE_STEP = 2
  A range hint for scalar value with step, which limits possible input values between min and max, with a step (increment) of step). Translated to hint_range(min, max, step) in shader code.

- HINT_MAX = 3
  Represents the size of the Hint enum.
