# VisualShaderNodeVectorBase

## Meta

- Name: VisualShaderNodeVectorBase
- Source: VisualShaderNodeVectorBase.xml
- Inherits: VisualShaderNode
- Inheritance Chain: VisualShaderNodeVectorBase -> VisualShaderNode -> Resource -> RefCounted -> Object

## Brief Description

A base type for the nodes that perform vector operations within the visual shader graph.

## Description

This is an abstract class. See the derived types for descriptions of the possible operations.

## Quick Reference

```
[properties]
op_type: int (VisualShaderNodeVectorBase.OpType) = 1
```

## Properties

- op_type: int (VisualShaderNodeVectorBase.OpType) = 1 [set set_op_type; get get_op_type]
  A vector type that this operation is performed on.

## Constants

### Enum OpType

- OP_TYPE_VECTOR_2D = 0
  A 2D vector type.

- OP_TYPE_VECTOR_3D = 1
  A 3D vector type.

- OP_TYPE_VECTOR_4D = 2
  A 4D vector type.

- OP_TYPE_MAX = 3
  Represents the size of the OpType enum.
