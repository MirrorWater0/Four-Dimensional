# VisualShaderNodeDerivativeFunc

## Meta

- Name: VisualShaderNodeDerivativeFunc
- Source: VisualShaderNodeDerivativeFunc.xml
- Inherits: VisualShaderNode
- Inheritance Chain: VisualShaderNodeDerivativeFunc -> VisualShaderNode -> Resource -> RefCounted -> Object

## Brief Description

Calculates a derivative within the visual shader graph.

## Description

This node is only available in Fragment and Light visual shaders.

## Quick Reference

```
[properties]
function: int (VisualShaderNodeDerivativeFunc.Function) = 0
op_type: int (VisualShaderNodeDerivativeFunc.OpType) = 0
precision: int (VisualShaderNodeDerivativeFunc.Precision) = 0
```

## Properties

- function: int (VisualShaderNodeDerivativeFunc.Function) = 0 [set set_function; get get_function]
  A derivative function type.

- op_type: int (VisualShaderNodeDerivativeFunc.OpType) = 0 [set set_op_type; get get_op_type]
  A type of operands and returned value.

- precision: int (VisualShaderNodeDerivativeFunc.Precision) = 0 [set set_precision; get get_precision]
  Sets the level of precision to use for the derivative function. When using the Compatibility renderer, this setting has no effect.

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

### Enum Function

- FUNC_SUM = 0
  Sum of absolute derivative in x and y.

- FUNC_X = 1
  Derivative in x using local differencing.

- FUNC_Y = 2
  Derivative in y using local differencing.

- FUNC_MAX = 3
  Represents the size of the Function enum.

### Enum Precision

- PRECISION_NONE = 0
  No precision is specified, the GPU driver is allowed to use whatever level of precision it chooses. This is the default option and is equivalent to using dFdx() or dFdy() in text shaders.

- PRECISION_COARSE = 1
  The derivative will be calculated using the current fragment's neighbors (which may not include the current fragment). This tends to be faster than using PRECISION_FINE, but may not be suitable when more precision is needed. This is equivalent to using dFdxCoarse() or dFdyCoarse() in text shaders.

- PRECISION_FINE = 2
  The derivative will be calculated using the current fragment and its immediate neighbors. This tends to be slower than using PRECISION_COARSE, but may be necessary when more precision is needed. This is equivalent to using dFdxFine() or dFdyFine() in text shaders.

- PRECISION_MAX = 3
  Represents the size of the Precision enum.
