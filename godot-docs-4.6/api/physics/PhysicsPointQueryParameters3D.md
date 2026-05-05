# PhysicsPointQueryParameters3D

## Meta

- Name: PhysicsPointQueryParameters3D
- Source: PhysicsPointQueryParameters3D.xml
- Inherits: RefCounted
- Inheritance Chain: PhysicsPointQueryParameters3D -> RefCounted -> Object

## Brief Description

Provides parameters for PhysicsDirectSpaceState3D.intersect_point().

## Description

By changing various properties of this object, such as the point position, you can configure the parameters for PhysicsDirectSpaceState3D.intersect_point().

## Quick Reference

```
[properties]
collide_with_areas: bool = false
collide_with_bodies: bool = true
collision_mask: int = 4294967295
exclude: RID[] = []
position: Vector3 = Vector3(0, 0, 0)
```

## Properties

- collide_with_areas: bool = false [set set_collide_with_areas; get is_collide_with_areas_enabled]
  If true, the query will take Area3Ds into account.

- collide_with_bodies: bool = true [set set_collide_with_bodies; get is_collide_with_bodies_enabled]
  If true, the query will take PhysicsBody3Ds into account.

- collision_mask: int = 4294967295 [set set_collision_mask; get get_collision_mask]
  The physics layers the query will detect (as a bitmask). By default, all collision layers are detected. See [Collision layers and masks]($DOCS_URL/tutorials/physics/physics_introduction.html#collision-layers-and-masks) in the documentation for more information.

- exclude: RID[] = [] [set set_exclude; get get_exclude]
  The list of object RIDs that will be excluded from collisions. Use CollisionObject3D.get_rid() to get the RID associated with a CollisionObject3D-derived node. **Note:** The returned array is copied and any changes to it will not update the original property value. To update the value you need to modify the returned array, and then assign it to the property again.

- position: Vector3 = Vector3(0, 0, 0) [set set_position; get get_position]
  The position being queried for, in global coordinates.
