# GPUParticlesCollisionSphere3D

## Meta

- Name: GPUParticlesCollisionSphere3D
- Source: GPUParticlesCollisionSphere3D.xml
- Inherits: GPUParticlesCollision3D
- Inheritance Chain: GPUParticlesCollisionSphere3D -> GPUParticlesCollision3D -> VisualInstance3D -> Node3D -> Node -> Object

## Brief Description

A sphere-shaped 3D particle collision shape affecting GPUParticles3D nodes.

## Description

A sphere-shaped 3D particle collision shape affecting GPUParticles3D nodes. Particle collision shapes work in real-time and can be moved, rotated and scaled during gameplay. Unlike attractors, non-uniform scaling of collision shapes is *not* supported. **Note:** ParticleProcessMaterial.collision_mode must be ParticleProcessMaterial.COLLISION_RIGID or ParticleProcessMaterial.COLLISION_HIDE_ON_CONTACT on the GPUParticles3D's process material for collision to work. **Note:** Particle collision only affects GPUParticles3D, not CPUParticles3D.

## Quick Reference

```
[properties]
radius: float = 1.0
```

## Properties

- radius: float = 1.0 [set set_radius; get get_radius]
  The collision sphere's radius in 3D units.
