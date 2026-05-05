# RDShaderFile

## Meta

- Name: RDShaderFile
- Source: RDShaderFile.xml
- Inherits: Resource
- Inheritance Chain: RDShaderFile -> Resource -> RefCounted -> Object

## Brief Description

Compiled shader file in SPIR-V form (used by RenderingDevice). Not to be confused with Godot's own Shader.

## Description

Compiled shader file in SPIR-V form. See also RDShaderSource. RDShaderFile is only meant to be used with the RenderingDevice API. It should not be confused with Godot's own Shader resource, which is what Godot's various nodes use for high-level shader programming.

## Quick Reference

```
[methods]
get_spirv(version: StringName = &"") -> RDShaderSPIRV [const]
get_version_list() -> StringName[] [const]
set_bytecode(bytecode: RDShaderSPIRV, version: StringName = &"") -> void

[properties]
base_error: String = ""
```

## Methods

- get_spirv(version: StringName = &"") -> RDShaderSPIRV [const]
  Returns the SPIR-V intermediate representation for the specified shader version.

- get_version_list() -> StringName[] [const]
  Returns the list of compiled versions for this shader.

- set_bytecode(bytecode: RDShaderSPIRV, version: StringName = &"") -> void
  Sets the SPIR-V bytecode that will be compiled for the specified version.

## Properties

- base_error: String = "" [set set_base_error; get get_base_error]
  The base compilation error message, which indicates errors not related to a specific shader stage if non-empty. If empty, shader compilation is not necessarily successful (check RDShaderSPIRV's error message members).
