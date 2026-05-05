# VisualShaderNodeParticleMeshEmitter

## Meta

- Name: VisualShaderNodeParticleMeshEmitter
- Source: VisualShaderNodeParticleMeshEmitter.xml
- Inherits: VisualShaderNodeParticleEmitter
- Inheritance Chain: VisualShaderNodeParticleMeshEmitter -> VisualShaderNodeParticleEmitter -> VisualShaderNode -> Resource -> RefCounted -> Object

## Brief Description

A visual shader node that makes particles emitted in a shape defined by a Mesh.

## Description

VisualShaderNodeParticleEmitter that makes the particles emitted in a shape of the assigned mesh. It will emit from the mesh's surfaces, either all or only the specified one.

## Quick Reference

```
[properties]
mesh: Mesh
surface_index: int = 0
use_all_surfaces: bool = true
```

## Properties

- mesh: Mesh [set set_mesh; get get_mesh]
  The Mesh that defines emission shape.

- surface_index: int = 0 [set set_surface_index; get get_surface_index]
  Index of the surface that emits particles. use_all_surfaces must be false for this to take effect.

- use_all_surfaces: bool = true [set set_use_all_surfaces; get is_use_all_surfaces]
  If true, the particles will emit from all surfaces of the mesh.
