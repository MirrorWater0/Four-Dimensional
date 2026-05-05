# PhysicsPointQueryParameters2D

## Meta

- Name: PhysicsPointQueryParameters2D
- Source: PhysicsPointQueryParameters2D.xml
- Inherits: RefCounted
- Inheritance Chain: PhysicsPointQueryParameters2D -> RefCounted -> Object

## Brief Description

Provides parameters for PhysicsDirectSpaceState2D.intersect_point().

## Description

By changing various properties of this object, such as the point position, you can configure the parameters for PhysicsDirectSpaceState2D.intersect_point().

## Quick Reference

```
[properties]
canvas_instance_id: int = 0
collide_with_areas: bool = false
collide_with_bodies: bool = true
collision_mask: int = 4294967295
exclude: RID[] = []
position: Vector2 = Vector2(0, 0)
```

## Properties

- canvas_instance_id: int = 0 [set set_canvas_instance_id; get get_canvas_instance_id]
  If different from 0, restricts the query to a specific canvas layer specified by its instance ID. See Object.get_instance_id(). If 0, restricts the query to the Viewport's default canvas layer.

- collide_with_areas: bool = false [set set_collide_with_areas; get is_collide_with_areas_enabled]
  If true, the query will take Area2Ds into account.

- collide_with_bodies: bool = true [set set_collide_with_bodies; get is_collide_with_bodies_enabled]
  If true, the query will take PhysicsBody2Ds into account.

- collision_mask: int = 4294967295 [set set_collision_mask; get get_collision_mask]
  The physics layers the query will detect (as a bitmask). By default, all collision layers are detected. See [Collision layers and masks]($DOCS_URL/tutorials/physics/physics_introduction.html#collision-layers-and-masks) in the documentation for more information.

- exclude: RID[] = [] [set set_exclude; get get_exclude]
  The list of object RIDs that will be excluded from collisions. Use CollisionObject2D.get_rid() to get the RID associated with a CollisionObject2D-derived node. **Note:** The returned array is copied and any changes to it will not update the original property value. To update the value you need to modify the returned array, and then assign it to the property again.

- position: Vector2 = Vector2(0, 0) [set set_position; get get_position]
  The position being queried for, in global coordinates.
