# VisualShaderNodeColorParameter

## Meta

- Name: VisualShaderNodeColorParameter
- Source: VisualShaderNodeColorParameter.xml
- Inherits: VisualShaderNodeParameter
- Inheritance Chain: VisualShaderNodeColorParameter -> VisualShaderNodeParameter -> VisualShaderNode -> Resource -> RefCounted -> Object

## Brief Description

A Color parameter to be used within the visual shader graph.

## Description

Translated to uniform vec4 in the shader language.

## Quick Reference

```
[properties]
default_value: Color = Color(1, 1, 1, 1)
default_value_enabled: bool = false
```

## Properties

- default_value: Color = Color(1, 1, 1, 1) [set set_default_value; get get_default_value]
  A default value to be assigned within the shader.

- default_value_enabled: bool = false [set set_default_value_enabled; get is_default_value_enabled]
  Enables usage of the default_value.
