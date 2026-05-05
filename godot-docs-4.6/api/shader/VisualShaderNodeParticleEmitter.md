# VisualShaderNodeParticleEmitter

## Meta

- Name: VisualShaderNodeParticleEmitter
- Source: VisualShaderNodeParticleEmitter.xml
- Inherits: VisualShaderNode
- Inheritance Chain: VisualShaderNodeParticleEmitter -> VisualShaderNode -> Resource -> RefCounted -> Object

## Brief Description

A base class for particle emitters.

## Description

Particle emitter nodes can be used in "start" step of particle shaders and they define the starting position of the particles. Connect them to the Position output port.

## Quick Reference

```
[properties]
mode_2d: bool = false
```

## Properties

- mode_2d: bool = false [set set_mode_2d; get is_mode_2d]
  If true, the result of this emitter is projected to 2D space. By default it is false and meant for use in 3D space.
