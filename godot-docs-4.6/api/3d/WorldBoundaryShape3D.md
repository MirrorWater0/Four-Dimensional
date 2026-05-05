# WorldBoundaryShape3D

## Meta

- Name: WorldBoundaryShape3D
- Source: WorldBoundaryShape3D.xml
- Inherits: Shape3D
- Inheritance Chain: WorldBoundaryShape3D -> Shape3D -> Resource -> RefCounted -> Object

## Brief Description

A 3D world boundary (half-space) shape used for physics collision.

## Description

A 3D world boundary shape, intended for use in physics. WorldBoundaryShape3D works like an infinite plane that forces all physics bodies to stay above it. The plane's normal determines which direction is considered as "above" and in the editor, the line over the plane represents this direction. It can for example be used for endless flat floors. **Note:** When the physics engine is set to **Jolt Physics** in the project settings (ProjectSettings.physics/3d/physics_engine), WorldBoundaryShape3D has a finite size (centered at the shape's origin). It can be adjusted by changing ProjectSettings.physics/jolt_physics_3d/limits/world_boundary_shape_size.

## Quick Reference

```
[properties]
plane: Plane = Plane(0, 1, 0, 0)
```

## Properties

- plane: Plane = Plane(0, 1, 0, 0) [set set_plane; get get_plane]
  The Plane used by the WorldBoundaryShape3D for collision.
