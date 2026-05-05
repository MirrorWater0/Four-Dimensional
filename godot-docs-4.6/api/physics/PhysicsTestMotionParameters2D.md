# PhysicsTestMotionParameters2D

## Meta

- Name: PhysicsTestMotionParameters2D
- Source: PhysicsTestMotionParameters2D.xml
- Inherits: RefCounted
- Inheritance Chain: PhysicsTestMotionParameters2D -> RefCounted -> Object

## Brief Description

Provides parameters for PhysicsServer2D.body_test_motion().

## Description

By changing various properties of this object, such as the motion, you can configure the parameters for PhysicsServer2D.body_test_motion().

## Quick Reference

```
[properties]
collide_separation_ray: bool = false
exclude_bodies: RID[] = []
exclude_objects: int[] = []
from: Transform2D = Transform2D(1, 0, 0, 1, 0, 0)
margin: float = 0.08
motion: Vector2 = Vector2(0, 0)
recovery_as_collision: bool = false
```

## Properties

- collide_separation_ray: bool = false [set set_collide_separation_ray_enabled; get is_collide_separation_ray_enabled]
  If set to true, shapes of type PhysicsServer2D.SHAPE_SEPARATION_RAY are used to detect collisions and can stop the motion. Can be useful when snapping to the ground. If set to false, shapes of type PhysicsServer2D.SHAPE_SEPARATION_RAY are only used for separation when overlapping with other bodies. That's the main use for separation ray shapes.

- exclude_bodies: RID[] = [] [set set_exclude_bodies; get get_exclude_bodies]
  Optional array of body RID to exclude from collision. Use CollisionObject2D.get_rid() to get the RID associated with a CollisionObject2D-derived node.

- exclude_objects: int[] = [] [set set_exclude_objects; get get_exclude_objects]
  Optional array of object unique instance ID to exclude from collision. See Object.get_instance_id().

- from: Transform2D = Transform2D(1, 0, 0, 1, 0, 0) [set set_from; get get_from]
  Transform in global space where the motion should start. Usually set to Node2D.global_transform for the current body's transform.

- margin: float = 0.08 [set set_margin; get get_margin]
  Increases the size of the shapes involved in the collision detection.

- motion: Vector2 = Vector2(0, 0) [set set_motion; get get_motion]
  Motion vector to define the length and direction of the motion to test.

- recovery_as_collision: bool = false [set set_recovery_as_collision_enabled; get is_recovery_as_collision_enabled]
  If set to true, any depenetration from the recovery phase is reported as a collision; this is used e.g. by CharacterBody2D for improving floor detection during floor snapping. If set to false, only collisions resulting from the motion are reported, which is generally the desired behavior.
