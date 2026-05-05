# VisualShaderNodeVec3Parameter

## Meta

- Name: VisualShaderNodeVec3Parameter
- Source: VisualShaderNodeVec3Parameter.xml
- Inherits: VisualShaderNodeParameter
- Inheritance Chain: VisualShaderNodeVec3Parameter -> VisualShaderNodeParameter -> VisualShaderNode -> Resource -> RefCounted -> Object

## Brief Description

A Vector3 parameter to be used within the visual shader graph.

## Description

Translated to uniform vec3 in the shader language.

## Quick Reference

```
[properties]
default_value: Vector3 = Vector3(0, 0, 0)
default_value_enabled: bool = false
```

## Properties

- default_value: Vector3 = Vector3(0, 0, 0) [set set_default_value; get get_default_value]
  A default value to be assigned within the shader.

- default_value_enabled: bool = false [set set_default_value_enabled; get is_default_value_enabled]
  Enables usage of the default_value.
