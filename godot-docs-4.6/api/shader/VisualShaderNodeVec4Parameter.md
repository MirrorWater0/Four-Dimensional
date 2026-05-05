# VisualShaderNodeVec4Parameter

## Meta

- Name: VisualShaderNodeVec4Parameter
- Source: VisualShaderNodeVec4Parameter.xml
- Inherits: VisualShaderNodeParameter
- Inheritance Chain: VisualShaderNodeVec4Parameter -> VisualShaderNodeParameter -> VisualShaderNode -> Resource -> RefCounted -> Object

## Brief Description

A 4D vector parameter to be used within the visual shader graph.

## Description

Translated to uniform vec4 in the shader language.

## Quick Reference

```
[properties]
default_value: Vector4 = Vector4(0, 0, 0, 0)
default_value_enabled: bool = false
```

## Properties

- default_value: Vector4 = Vector4(0, 0, 0, 0) [set set_default_value; get get_default_value]
  A default value to be assigned within the shader.

- default_value_enabled: bool = false [set set_default_value_enabled; get is_default_value_enabled]
  Enables usage of the default_value.
