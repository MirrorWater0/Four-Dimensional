# VisualShaderNodeInput

## Meta

- Name: VisualShaderNodeInput
- Source: VisualShaderNodeInput.xml
- Inherits: VisualShaderNode
- Inheritance Chain: VisualShaderNodeInput -> VisualShaderNode -> Resource -> RefCounted -> Object

## Brief Description

Represents the input shader parameter within the visual shader graph.

## Description

Gives access to input variables (built-ins) available for the shader. See the shading reference for the list of available built-ins for each shader type (check Tutorials section for link).

## Quick Reference

```
[methods]
get_input_real_name() -> String [const]

[properties]
input_name: String = "[None]"
```

## Tutorials

- [Shading reference index]($DOCS_URL/tutorials/shaders/shader_reference/index.html)

## Methods

- get_input_real_name() -> String [const]
  Returns a translated name of the current constant in the Godot Shader Language. E.g. "ALBEDO" if the input_name equal to "albedo".

## Properties

- input_name: String = "[None]" [set set_input_name; get get_input_name]
  One of the several input constants in lower-case style like: "vertex" (VERTEX) or "point_size" (POINT_SIZE).

## Signals

- input_type_changed()
  Emitted when input is changed via input_name.
