# ShaderIncludeDB

## Meta

- Name: ShaderIncludeDB
- Source: ShaderIncludeDB.xml
- Inherits: Object
- Inheritance Chain: ShaderIncludeDB -> Object

## Brief Description

Internal database of built in shader include files.

## Description

This object contains shader fragments from Godot's internal shaders. These can be used when access to internal uniform buffers and/or internal functions is required for instance when composing compositor effects or compute shaders. Only fragments for the current rendering device are loaded.

## Quick Reference

```
[methods]
get_built_in_include_file(filename: String) -> String [static]
has_built_in_include_file(filename: String) -> bool [static]
list_built_in_include_files() -> PackedStringArray [static]
```

## Methods

- get_built_in_include_file(filename: String) -> String [static]
  Returns the code for the built-in shader fragment. You can also access this in your shader code through #include "filename".

- has_built_in_include_file(filename: String) -> bool [static]
  Returns true if an include file with this name exists.

- list_built_in_include_files() -> PackedStringArray [static]
  Returns a list of built-in include files that are currently registered.
