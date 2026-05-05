# PhysicsRayQueryParameters3D

## Meta

- Name: PhysicsRayQueryParameters3D
- Source: PhysicsRayQueryParameters3D.xml
- Inherits: RefCounted
- Inheritance Chain: PhysicsRayQueryParameters3D -> RefCounted -> Object

## Brief Description

Provides parameters for PhysicsDirectSpaceState3D.intersect_ray().

## Description

By changing various properties of this object, such as the ray position, you can configure the parameters for PhysicsDirectSpaceState3D.intersect_ray().

## Quick Reference

```
[methods]
create(from: Vector3, to: Vector3, collision_mask: int = 4294967295, exclude: RID[] = []) -> PhysicsRayQueryParameters3D [static]

[properties]
collide_with_areas: bool = false
collide_with_bodies: bool = true
collision_mask: int = 4294967295
exclude: RID[] = []
from: Vector3 = Vector3(0, 0, 0)
hit_back_faces: bool = true
hit_from_inside: bool = false
to: Vector3 = Vector3(0, 0, 0)
```

## Methods

- create(from: Vector3, to: Vector3, collision_mask: int = 4294967295, exclude: RID[] = []) -> PhysicsRayQueryParameters3D [static]
  Returns a new, pre-configured PhysicsRayQueryParameters3D object. Use it to quickly create query parameters using the most common options.


```
  var query = PhysicsRayQueryParameters3D.create(position, position + Vector3(0, -10, 0))
  var collision = get_world_3d().direct_space_state.intersect_ray(query)

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

- from: Vector3 = Vector3(0, 0, 0) [set set_from; get get_from]
  The starting point of the ray being queried for, in global coordinates.

- hit_back_faces: bool = true [set set_hit_back_faces; get is_hit_back_faces_enabled]
  If true, the query will hit back faces with concave polygon shapes with back face enabled or heightmap shapes.

- hit_from_inside: bool = false [set set_hit_from_inside; get is_hit_from_inside_enabled]
  If true, the query will detect a hit when starting inside shapes. In this case the collision normal will be Vector3(0, 0, 0). Does not affect concave polygon shapes or heightmap shapes.

- to: Vector3 = Vector3(0, 0, 0) [set set_to; get get_to]
  The ending point of the ray being queried for, in global coordinates.
