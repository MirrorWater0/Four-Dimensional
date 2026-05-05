# VisualShaderNodeVec4Constant

## Meta

- Name: VisualShaderNodeVec4Constant
- Source: VisualShaderNodeVec4Constant.xml
- Inherits: VisualShaderNodeConstant
- Inheritance Chain: VisualShaderNodeVec4Constant -> VisualShaderNodeConstant -> VisualShaderNode -> Resource -> RefCounted -> Object

## Brief Description

A 4D vector constant to be used within the visual shader graph.

## Description

A constant 4D vector, which can be used as an input node.

## Quick Reference

```
[properties]
constant: Quaternion = Quaternion(0, 0, 0, 1)
```

## Properties

- constant: Quaternion = Quaternion(0, 0, 0, 1) [set set_constant; get get_constant]
  A 4D vector (represented as a Quaternion) constant which represents the state of this node.
