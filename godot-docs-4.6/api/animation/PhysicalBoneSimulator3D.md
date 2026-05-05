# PhysicalBoneSimulator3D

## Meta

- Name: PhysicalBoneSimulator3D
- Source: PhysicalBoneSimulator3D.xml
- Inherits: SkeletonModifier3D
- Inheritance Chain: PhysicalBoneSimulator3D -> SkeletonModifier3D -> Node3D -> Node -> Object

## Brief Description

Node that can be the parent of PhysicalBone3D and can apply the simulation results to Skeleton3D.

## Description

Node that can be the parent of PhysicalBone3D and can apply the simulation results to Skeleton3D.

## Quick Reference

```
[methods]
is_simulating_physics() -> bool [const]
physical_bones_add_collision_exception(exception: RID) -> void
physical_bones_remove_collision_exception(exception: RID) -> void
physical_bones_start_simulation(bones: StringName[] = []) -> void
physical_bones_stop_simulation() -> void
```

## Methods

- is_simulating_physics() -> bool [const]
  Returns a boolean that indicates whether the PhysicalBoneSimulator3D is running and simulating.

- physical_bones_add_collision_exception(exception: RID) -> void
  Adds a collision exception to the physical bone. Works just like the RigidBody3D node.

- physical_bones_remove_collision_exception(exception: RID) -> void
  Removes a collision exception to the physical bone. Works just like the RigidBody3D node.

- physical_bones_start_simulation(bones: StringName[] = []) -> void
  Tells the PhysicalBone3D nodes in the Skeleton to start simulating and reacting to the physics world. Optionally, a list of bone names can be passed-in, allowing only the passed-in bones to be simulated.

- physical_bones_stop_simulation() -> void
  Tells the PhysicalBone3D nodes in the Skeleton to stop simulating.
