# RDUniform

## Meta

- Name: RDUniform
- Source: RDUniform.xml
- Inherits: RefCounted
- Inheritance Chain: RDUniform -> RefCounted -> Object

## Brief Description

Shader uniform (used by RenderingDevice).

## Description

This object is used by RenderingDevice.

## Quick Reference

```
[methods]
add_id(id: RID) -> void
clear_ids() -> void
get_ids() -> RID[] [const]

[properties]
binding: int = 0
uniform_type: int (RenderingDevice.UniformType) = 3
```

## Methods

- add_id(id: RID) -> void
  Binds the given id to the uniform. The data associated with the id is then used when the uniform is passed to a shader.

- clear_ids() -> void
  Unbinds all ids currently bound to the uniform.

- get_ids() -> RID[] [const]
  Returns an array of all ids currently bound to the uniform.

## Properties

- binding: int = 0 [set set_binding; get get_binding]
  The uniform's binding.

- uniform_type: int (RenderingDevice.UniformType) = 3 [set set_uniform_type; get get_uniform_type]
  The uniform's data type.
