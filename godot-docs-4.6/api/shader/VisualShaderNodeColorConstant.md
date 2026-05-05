# VisualShaderNodeColorConstant

## Meta

- Name: VisualShaderNodeColorConstant
- Source: VisualShaderNodeColorConstant.xml
- Inherits: VisualShaderNodeConstant
- Inheritance Chain: VisualShaderNodeColorConstant -> VisualShaderNodeConstant -> VisualShaderNode -> Resource -> RefCounted -> Object

## Brief Description

A Color constant to be used within the visual shader graph.

## Description

Has two output ports representing RGB and alpha channels of Color. Translated to vec3 rgb and float alpha in the shader language.

## Quick Reference

```
[properties]
constant: Color = Color(1, 1, 1, 1)
```

## Properties

- constant: Color = Color(1, 1, 1, 1) [set set_constant; get get_constant]
  A Color constant which represents a state of this node.
