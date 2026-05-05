# RDShaderSPIRV

## Meta

- Name: RDShaderSPIRV
- Source: RDShaderSPIRV.xml
- Inherits: Resource
- Inheritance Chain: RDShaderSPIRV -> Resource -> RefCounted -> Object

## Brief Description

SPIR-V intermediate representation as part of an RDShaderFile (used by RenderingDevice).

## Description

RDShaderSPIRV represents an RDShaderFile's [SPIR-V](https://www.khronos.org/spir/) code for various shader stages, as well as possible compilation error messages. SPIR-V is a low-level intermediate shader representation. This intermediate representation is not used directly by GPUs for rendering, but it can be compiled into binary shaders that GPUs can understand. Unlike compiled shaders, SPIR-V is portable across GPU models and driver versions. This object is used by RenderingDevice.

## Quick Reference

```
[methods]
get_stage_bytecode(stage: int (RenderingDevice.ShaderStage)) -> PackedByteArray [const]
get_stage_compile_error(stage: int (RenderingDevice.ShaderStage)) -> String [const]
set_stage_bytecode(stage: int (RenderingDevice.ShaderStage), bytecode: PackedByteArray) -> void
set_stage_compile_error(stage: int (RenderingDevice.ShaderStage), compile_error: String) -> void

[properties]
bytecode_compute: PackedByteArray = PackedByteArray()
bytecode_fragment: PackedByteArray = PackedByteArray()
bytecode_tesselation_control: PackedByteArray = PackedByteArray()
bytecode_tesselation_evaluation: PackedByteArray = PackedByteArray()
bytecode_vertex: PackedByteArray = PackedByteArray()
compile_error_compute: String = ""
compile_error_fragment: String = ""
compile_error_tesselation_control: String = ""
compile_error_tesselation_evaluation: String = ""
compile_error_vertex: String = ""
```

## Methods

- get_stage_bytecode(stage: int (RenderingDevice.ShaderStage)) -> PackedByteArray [const]
  Equivalent to getting one of bytecode_compute, bytecode_fragment, bytecode_tesselation_control, bytecode_tesselation_evaluation, bytecode_vertex.

- get_stage_compile_error(stage: int (RenderingDevice.ShaderStage)) -> String [const]
  Returns the compilation error message for the given shader stage. Equivalent to getting one of compile_error_compute, compile_error_fragment, compile_error_tesselation_control, compile_error_tesselation_evaluation, compile_error_vertex.

- set_stage_bytecode(stage: int (RenderingDevice.ShaderStage), bytecode: PackedByteArray) -> void
  Sets the SPIR-V bytecode for the given shader stage. Equivalent to setting one of bytecode_compute, bytecode_fragment, bytecode_tesselation_control, bytecode_tesselation_evaluation, bytecode_vertex.

- set_stage_compile_error(stage: int (RenderingDevice.ShaderStage), compile_error: String) -> void
  Sets the compilation error message for the given shader stage to compile_error. Equivalent to setting one of compile_error_compute, compile_error_fragment, compile_error_tesselation_control, compile_error_tesselation_evaluation, compile_error_vertex.

## Properties

- bytecode_compute: PackedByteArray = PackedByteArray() [set set_stage_bytecode; get get_stage_bytecode]
  The SPIR-V bytecode for the compute shader stage.

- bytecode_fragment: PackedByteArray = PackedByteArray() [set set_stage_bytecode; get get_stage_bytecode]
  The SPIR-V bytecode for the fragment shader stage.

- bytecode_tesselation_control: PackedByteArray = PackedByteArray() [set set_stage_bytecode; get get_stage_bytecode]
  The SPIR-V bytecode for the tessellation control shader stage.

- bytecode_tesselation_evaluation: PackedByteArray = PackedByteArray() [set set_stage_bytecode; get get_stage_bytecode]
  The SPIR-V bytecode for the tessellation evaluation shader stage.

- bytecode_vertex: PackedByteArray = PackedByteArray() [set set_stage_bytecode; get get_stage_bytecode]
  The SPIR-V bytecode for the vertex shader stage.

- compile_error_compute: String = "" [set set_stage_compile_error; get get_stage_compile_error]
  The compilation error message for the compute shader stage (set by the SPIR-V compiler and Godot). If empty, shader compilation was successful.

- compile_error_fragment: String = "" [set set_stage_compile_error; get get_stage_compile_error]
  The compilation error message for the fragment shader stage (set by the SPIR-V compiler and Godot). If empty, shader compilation was successful.

- compile_error_tesselation_control: String = "" [set set_stage_compile_error; get get_stage_compile_error]
  The compilation error message for the tessellation control shader stage (set by the SPIR-V compiler and Godot). If empty, shader compilation was successful.

- compile_error_tesselation_evaluation: String = "" [set set_stage_compile_error; get get_stage_compile_error]
  The compilation error message for the tessellation evaluation shader stage (set by the SPIR-V compiler and Godot). If empty, shader compilation was successful.

- compile_error_vertex: String = "" [set set_stage_compile_error; get get_stage_compile_error]
  The compilation error message for the vertex shader stage (set by the SPIR-V compiler and Godot). If empty, shader compilation was successful.
