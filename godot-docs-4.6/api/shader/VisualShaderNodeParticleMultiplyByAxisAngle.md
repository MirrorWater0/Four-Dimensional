# VisualShaderNodeParticleMultiplyByAxisAngle

## Meta

- Name: VisualShaderNodeParticleMultiplyByAxisAngle
- Source: VisualShaderNodeParticleMultiplyByAxisAngle.xml
- Inherits: VisualShaderNode
- Inheritance Chain: VisualShaderNodeParticleMultiplyByAxisAngle -> VisualShaderNode -> Resource -> RefCounted -> Object

## Brief Description

A visual shader helper node for multiplying position and rotation of particles.

## Description

This node helps to multiply a position input vector by rotation using specific axis. Intended to work with emitters.

## Quick Reference

```
[properties]
degrees_mode: bool = true
```

## Properties

- degrees_mode: bool = true [set set_degrees_mode; get is_degrees_mode]
  If true, the angle will be interpreted in degrees instead of radians.
