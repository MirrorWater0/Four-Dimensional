# UniformSetCacheRD

## Meta

- Name: UniformSetCacheRD
- Source: UniformSetCacheRD.xml
- Inherits: Object
- Inheritance Chain: UniformSetCacheRD -> Object

## Brief Description

Uniform set cache manager for Rendering Device based renderers.

## Description

Uniform set cache manager for RenderingDevice-based renderers. Provides a way to create a uniform set and reuse it in subsequent calls for as long as the uniform set exists. Uniform set will automatically be cleaned up when dependent objects are freed.

## Quick Reference

```
[methods]
get_cache(shader: RID, set: int, uniforms: RDUniform[]) -> RID [static]
```

## Methods

- get_cache(shader: RID, set: int, uniforms: RDUniform[]) -> RID [static]
  Creates/returns a cached uniform set based on the provided uniforms for a given shader.
