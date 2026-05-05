# VisualShaderNodeParameterRef

## Meta

- Name: VisualShaderNodeParameterRef
- Source: VisualShaderNodeParameterRef.xml
- Inherits: VisualShaderNode
- Inheritance Chain: VisualShaderNodeParameterRef -> VisualShaderNode -> Resource -> RefCounted -> Object

## Brief Description

A reference to an existing VisualShaderNodeParameter.

## Description

Creating a reference to a VisualShaderNodeParameter allows you to reuse this parameter in different shaders or shader stages easily.

## Quick Reference

```
[properties]
parameter_name: String = "[None]"
```

## Properties

- parameter_name: String = "[None]" [set set_parameter_name; get get_parameter_name]
  The name of the parameter which this reference points to.
