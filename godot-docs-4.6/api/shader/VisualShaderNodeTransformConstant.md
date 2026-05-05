# VisualShaderNodeTransformConstant

## Meta

- Name: VisualShaderNodeTransformConstant
- Source: VisualShaderNodeTransformConstant.xml
- Inherits: VisualShaderNodeConstant
- Inheritance Chain: VisualShaderNodeTransformConstant -> VisualShaderNodeConstant -> VisualShaderNode -> Resource -> RefCounted -> Object

## Brief Description

A Transform3D constant for use within the visual shader graph.

## Description

A constant Transform3D, which can be used as an input node.

## Quick Reference

```
[properties]
constant: Transform3D = Transform3D(1, 0, 0, 0, 1, 0, 0, 0, 1, 0, 0, 0)
```

## Properties

- constant: Transform3D = Transform3D(1, 0, 0, 0, 1, 0, 0, 0, 1, 0, 0, 0) [set set_constant; get get_constant]
  A Transform3D constant which represents the state of this node.
