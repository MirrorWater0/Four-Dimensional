# VisualShaderNodeParticleRandomness

## Meta

- Name: VisualShaderNodeParticleRandomness
- Source: VisualShaderNodeParticleRandomness.xml
- Inherits: VisualShaderNode
- Inheritance Chain: VisualShaderNodeParticleRandomness -> VisualShaderNode -> Resource -> RefCounted -> Object

## Brief Description

Visual shader node for randomizing particle values.

## Description

Randomness node will output pseudo-random values of the given type based on the specified minimum and maximum values.

## Quick Reference

```
[properties]
op_type: int (VisualShaderNodeParticleRandomness.OpType) = 0
```

## Properties

- op_type: int (VisualShaderNodeParticleRandomness.OpType) = 0 [set set_op_type; get get_op_type]
  A type of operands and returned value.

## Constants

### Enum OpType

- OP_TYPE_SCALAR = 0
  A floating-point scalar.

- OP_TYPE_VECTOR_2D = 1
  A 2D vector type.

- OP_TYPE_VECTOR_3D = 2
  A 3D vector type.

- OP_TYPE_VECTOR_4D = 3
  A 4D vector type.

- OP_TYPE_MAX = 4
  Represents the size of the OpType enum.
