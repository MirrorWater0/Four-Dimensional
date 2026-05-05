# VisualShaderNodeParameter

## Meta

- Name: VisualShaderNodeParameter
- Source: VisualShaderNodeParameter.xml
- Inherits: VisualShaderNode
- Inheritance Chain: VisualShaderNodeParameter -> VisualShaderNode -> Resource -> RefCounted -> Object

## Brief Description

A base type for the parameters within the visual shader graph.

## Description

A parameter represents a variable in the shader which is set externally, i.e. from the ShaderMaterial. Parameters are exposed as properties in the ShaderMaterial and can be assigned from the Inspector or from a script.

## Quick Reference

```
[properties]
instance_index: int = 0
parameter_name: String = ""
qualifier: int (VisualShaderNodeParameter.Qualifier) = 0
```

## Properties

- instance_index: int = 0 [set set_instance_index; get get_instance_index]
  The index within 0-15 range, which is used to avoid clashes when shader used on multiple materials.

- parameter_name: String = "" [set set_parameter_name; get get_parameter_name]
  Name of the parameter, by which it can be accessed through the ShaderMaterial properties.

- qualifier: int (VisualShaderNodeParameter.Qualifier) = 0 [set set_qualifier; get get_qualifier]
  Defines the scope of the parameter.

## Constants

### Enum Qualifier

- QUAL_NONE = 0
  The parameter will be tied to the ShaderMaterial using this shader.

- QUAL_GLOBAL = 1
  The parameter will use a global value, defined in Project Settings.

- QUAL_INSTANCE = 2
  The parameter will be tied to the node with attached ShaderMaterial using this shader.

- QUAL_INSTANCE_INDEX = 3
  The parameter will be tied to the node with attached ShaderMaterial using this shader. Enables setting a instance_index property.

- QUAL_MAX = 4
  Represents the size of the Qualifier enum.
