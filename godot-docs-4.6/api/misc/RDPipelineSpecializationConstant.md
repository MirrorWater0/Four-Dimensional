# RDPipelineSpecializationConstant

## Meta

- Name: RDPipelineSpecializationConstant
- Source: RDPipelineSpecializationConstant.xml
- Inherits: RefCounted
- Inheritance Chain: RDPipelineSpecializationConstant -> RefCounted -> Object

## Brief Description

Pipeline specialization constant (used by RenderingDevice).

## Description

A *specialization constant* is a way to create additional variants of shaders without actually increasing the number of shader versions that are compiled. This allows improving performance by reducing the number of shader versions and reducing if branching, while still allowing shaders to be flexible for different use cases. This object is used by RenderingDevice.

## Quick Reference

```
[properties]
constant_id: int = 0
value: Variant
```

## Properties

- constant_id: int = 0 [set set_constant_id; get get_constant_id]
  The identifier of the specialization constant. This is a value starting from 0 and that increments for every different specialization constant for a given shader.

- value: Variant [set set_value; get get_value]
  The specialization constant's value. Only bool, int and float types are valid for specialization constants.
