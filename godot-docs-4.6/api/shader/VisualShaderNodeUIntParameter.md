# VisualShaderNodeUIntParameter

## Meta

- Name: VisualShaderNodeUIntParameter
- Source: VisualShaderNodeUIntParameter.xml
- Inherits: VisualShaderNodeParameter
- Inheritance Chain: VisualShaderNodeUIntParameter -> VisualShaderNodeParameter -> VisualShaderNode -> Resource -> RefCounted -> Object

## Brief Description

A visual shader node for shader parameter (uniform) of type unsigned int.

## Description

A VisualShaderNodeParameter of type unsigned int. Offers additional customization for range of accepted values.

## Quick Reference

```
[properties]
default_value: int = 0
default_value_enabled: bool = false
```

## Properties

- default_value: int = 0 [set set_default_value; get get_default_value]
  Default value of this parameter, which will be used if not set externally. default_value_enabled must be enabled; defaults to 0 otherwise.

- default_value_enabled: bool = false [set set_default_value_enabled; get is_default_value_enabled]
  If true, the node will have a custom default value.
