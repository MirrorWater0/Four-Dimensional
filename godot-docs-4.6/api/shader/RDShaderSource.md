# RDShaderSource

## Meta

- Name: RDShaderSource
- Source: RDShaderSource.xml
- Inherits: RefCounted
- Inheritance Chain: RDShaderSource -> RefCounted -> Object

## Brief Description

Shader source code (used by RenderingDevice).

## Description

Shader source code in text form. See also RDShaderFile. RDShaderSource is only meant to be used with the RenderingDevice API. It should not be confused with Godot's own Shader resource, which is what Godot's various nodes use for high-level shader programming.

## Quick Reference

```
[methods]
get_stage_source(stage: int (RenderingDevice.ShaderStage)) -> String [const]
set_stage_source(stage: int (RenderingDevice.ShaderStage), source: String) -> void

[properties]
language: int (RenderingDevice.ShaderLanguage) = 0
source_compute: String = ""
source_fragment: String = ""
source_tesselation_control: String = ""
source_tesselation_evaluation: String = ""
source_vertex: String = ""
```

## Methods

- get_stage_source(stage: int (RenderingDevice.ShaderStage)) -> String [const]
  Returns source code for the specified shader stage. Equivalent to getting one of source_compute, source_fragment, source_tesselation_control, source_tesselation_evaluation or source_vertex.

- set_stage_source(stage: int (RenderingDevice.ShaderStage), source: String) -> void
  Sets source code for the specified shader stage. Equivalent to setting one of source_compute, source_fragment, source_tesselation_control, source_tesselation_evaluation or source_vertex. **Note:** If you set the compute shader source code using this method directly, remember to remove the Godot-specific hint #compute.

## Properties

- language: int (RenderingDevice.ShaderLanguage) = 0 [set set_language; get get_language]
  The language the shader is written in.

- source_compute: String = "" [set set_stage_source; get get_stage_source]
  Source code for the shader's compute stage.

- source_fragment: String = "" [set set_stage_source; get get_stage_source]
  Source code for the shader's fragment stage.

- source_tesselation_control: String = "" [set set_stage_source; get get_stage_source]
  Source code for the shader's tessellation control stage.

- source_tesselation_evaluation: String = "" [set set_stage_source; get get_stage_source]
  Source code for the shader's tessellation evaluation stage.

- source_vertex: String = "" [set set_stage_source; get get_stage_source]
  Source code for the shader's vertex stage.
