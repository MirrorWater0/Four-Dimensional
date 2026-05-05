# VisualShaderNodeTransformParameter

## Meta

- Name: VisualShaderNodeTransformParameter
- Source: VisualShaderNodeTransformParameter.xml
- Inherits: VisualShaderNodeParameter
- Inheritance Chain: VisualShaderNodeTransformParameter -> VisualShaderNodeParameter -> VisualShaderNode -> Resource -> RefCounted -> Object

## Brief Description

A Transform3D parameter for use within the visual shader graph.

## Description

Translated to uniform mat4 in the shader language.

## Quick Reference

```
[properties]
default_value: Transform3D = Transform3D(1, 0, 0, 0, 1, 0, 0, 0, 1, 0, 0, 0)
default_value_enabled: bool = false
```

## Properties

- default_value: Transform3D = Transform3D(1, 0, 0, 0, 1, 0, 0, 0, 1, 0, 0, 0) [set set_default_value; get get_default_value]
  A default value to be assigned within the shader.

- default_value_enabled: bool = false [set set_default_value_enabled; get is_default_value_enabled]
  Enables usage of the default_value.
