# VisualShaderNodeBooleanParameter

## Meta

- Name: VisualShaderNodeBooleanParameter
- Source: VisualShaderNodeBooleanParameter.xml
- Inherits: VisualShaderNodeParameter
- Inheritance Chain: VisualShaderNodeBooleanParameter -> VisualShaderNodeParameter -> VisualShaderNode -> Resource -> RefCounted -> Object

## Brief Description

A boolean parameter to be used within the visual shader graph.

## Description

Translated to uniform bool in the shader language.

## Quick Reference

```
[properties]
default_value: bool = false
default_value_enabled: bool = false
```

## Properties

- default_value: bool = false [set set_default_value; get get_default_value]
  A default value to be assigned within the shader.

- default_value_enabled: bool = false [set set_default_value_enabled; get is_default_value_enabled]
  Enables usage of the default_value.
