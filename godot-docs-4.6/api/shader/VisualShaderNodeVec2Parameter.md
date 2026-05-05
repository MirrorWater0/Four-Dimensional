# VisualShaderNodeVec2Parameter

## Meta

- Name: VisualShaderNodeVec2Parameter
- Source: VisualShaderNodeVec2Parameter.xml
- Inherits: VisualShaderNodeParameter
- Inheritance Chain: VisualShaderNodeVec2Parameter -> VisualShaderNodeParameter -> VisualShaderNode -> Resource -> RefCounted -> Object

## Brief Description

A Vector2 parameter to be used within the visual shader graph.

## Description

Translated to uniform vec2 in the shader language.

## Quick Reference

```
[properties]
default_value: Vector2 = Vector2(0, 0)
default_value_enabled: bool = false
```

## Properties

- default_value: Vector2 = Vector2(0, 0) [set set_default_value; get get_default_value]
  A default value to be assigned within the shader.

- default_value_enabled: bool = false [set set_default_value_enabled; get is_default_value_enabled]
  Enables usage of the default_value.
