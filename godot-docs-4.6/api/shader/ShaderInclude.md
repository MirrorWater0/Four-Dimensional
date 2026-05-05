# ShaderInclude

## Meta

- Name: ShaderInclude
- Source: ShaderInclude.xml
- Inherits: Resource
- Inheritance Chain: ShaderInclude -> Resource -> RefCounted -> Object

## Brief Description

A snippet of shader code to be included in a Shader with #include.

## Description

A shader include file, saved with the .gdshaderinc extension. This class allows you to define a custom shader snippet that can be included in a Shader by using the preprocessor directive #include, followed by the file path (e.g. #include "res://shader_lib.gdshaderinc"). The snippet doesn't have to be a valid shader on its own.

## Quick Reference

```
[properties]
code: String = ""
```

## Tutorials

- [Shader preprocessor]($DOCS_URL/tutorials/shaders/shader_reference/shader_preprocessor.html)

## Properties

- code: String = "" [set set_code; get get_code]
  Returns the code of the shader include file. The returned text is what the user has written, not the full generated code used internally.
