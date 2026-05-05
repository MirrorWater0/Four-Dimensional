# VisualShaderNodeSwitch

## Meta

- Name: VisualShaderNodeSwitch
- Source: VisualShaderNodeSwitch.xml
- Inherits: VisualShaderNode
- Inheritance Chain: VisualShaderNodeSwitch -> VisualShaderNode -> Resource -> RefCounted -> Object

## Brief Description

A selector function for use within the visual shader graph.

## Description

Returns an associated value of the op_type type if the provided boolean value is true or false.

## Quick Reference

```
[properties]
op_type: int (VisualShaderNodeSwitch.OpType) = 0
```

## Properties

- op_type: int (VisualShaderNodeSwitch.OpType) = 0 [set set_op_type; get get_op_type]
  A type of operands and returned value.

## Constants

### Enum OpType

- OP_TYPE_FLOAT = 0
  A floating-point scalar.

- OP_TYPE_INT = 1
  An integer scalar.

- OP_TYPE_UINT = 2
  An unsigned integer scalar.

- OP_TYPE_VECTOR_2D = 3
  A 2D vector type.

- OP_TYPE_VECTOR_3D = 4
  A 3D vector type.

- OP_TYPE_VECTOR_4D = 5
  A 4D vector type.

- OP_TYPE_BOOLEAN = 6
  A boolean type.

- OP_TYPE_TRANSFORM = 7
  A transform type.

- OP_TYPE_MAX = 8
  Represents the size of the OpType enum.
