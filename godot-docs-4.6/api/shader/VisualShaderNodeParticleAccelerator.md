# VisualShaderNodeParticleAccelerator

## Meta

- Name: VisualShaderNodeParticleAccelerator
- Source: VisualShaderNodeParticleAccelerator.xml
- Inherits: VisualShaderNode
- Inheritance Chain: VisualShaderNodeParticleAccelerator -> VisualShaderNode -> Resource -> RefCounted -> Object

## Brief Description

A visual shader node that accelerates particles.

## Description

Particle accelerator can be used in "process" step of particle shader. It will accelerate the particles. Connect it to the Velocity output port.

## Quick Reference

```
[properties]
mode: int (VisualShaderNodeParticleAccelerator.Mode) = 0
```

## Properties

- mode: int (VisualShaderNodeParticleAccelerator.Mode) = 0 [set set_mode; get get_mode]
  Defines in what manner the particles will be accelerated.

## Constants

### Enum Mode

- MODE_LINEAR = 0
  The particles will be accelerated based on their velocity.

- MODE_RADIAL = 1
  The particles will be accelerated towards or away from the center.

- MODE_TANGENTIAL = 2
  The particles will be accelerated tangentially to the radius vector from center to their position.

- MODE_MAX = 3
  Represents the size of the Mode enum.
