# ShapeCast3D

## Meta

- Name: ShapeCast3D
- Source: ShapeCast3D.xml
- Inherits: Node3D
- Inheritance Chain: ShapeCast3D -> Node3D -> Node -> Object

## Brief Description

A 3D shape that sweeps a region of space to detect CollisionObject3Ds.

## Description

Shape casting allows to detect collision objects by sweeping its shape along the cast direction determined by target_position. This is similar to RayCast3D, but it allows for sweeping a region of space, rather than just a straight line. ShapeCast3D can detect multiple collision objects. It is useful for things like wide laser beams or snapping a simple shape to a floor. Immediate collision overlaps can be done with the target_position set to Vector3(0, 0, 0) and by calling force_shapecast_update() within the same physics frame. This helps to overcome some limitations of Area3D when used as an instantaneous detection area, as collision information isn't immediately available to it. **Note:** Shape casting is more computationally expensive than ray casting.

## Quick Reference

```
[methods]
add_exception(node: CollisionObject3D) -> void
add_exception_rid(rid: RID) -> void
clear_exceptions() -> void
force_shapecast_update() -> void
get_closest_collision_safe_fraction() -> float [const]
get_closest_collision_unsafe_fraction() -> float [const]
get_collider(index: int) -> Object [const]
get_collider_rid(index: int) -> RID [const]
get_collider_shape(index: int) -> int [const]
get_collision_count() -> int [const]
get_collision_mask_value(layer_number: int) -> bool [const]
get_collision_normal(index: int) -> Vector3 [const]
get_collision_point(index: int) -> Vector3 [const]
is_colliding() -> bool [const]
remove_exception(node: CollisionObject3D) -> void
remove_exception_rid(rid: RID) -> void
resource_changed(resource: Resource) -> void
set_collision_mask_value(layer_number: int, value: bool) -> void

[properties]
collide_with_areas: bool = false
collide_with_bodies: bool = true
collision_mask: int = 1
collision_result: Array = []
debug_shape_custom_color: Color = Color(0, 0, 0, 1)
enabled: bool = true
exclude_parent: bool = true
margin: float = 0.0
max_results: int = 32
shape: Shape3D
target_position: Vector3 = Vector3(0, -1, 0)
```

## Methods

- add_exception(node: CollisionObject3D) -> void
  Adds a collision exception so the shape does not report collisions with the specified node.

- add_exception_rid(rid: RID) -> void
  Adds a collision exception so the shape does not report collisions with the specified RID.

- clear_exceptions() -> void
  Removes all collision exceptions for this shape.

- force_shapecast_update() -> void
  Updates the collision information for the shape immediately, without waiting for the next _physics_process call. Use this method, for example, when the shape or its parent has changed state. **Note:** Setting enabled to true is not required for this to work.

- get_closest_collision_safe_fraction() -> float [const]
  Returns the fraction from this cast's origin to its target_position of how far the shape can move without triggering a collision, as a value between 0.0 and 1.0.

- get_closest_collision_unsafe_fraction() -> float [const]
  Returns the fraction from this cast's origin to its target_position of how far the shape must move to trigger a collision, as a value between 0.0 and 1.0. In ideal conditions this would be the same as get_closest_collision_safe_fraction(), however shape casting is calculated in discrete steps, so the precise point of collision can occur between two calculated positions.

- get_collider(index: int) -> Object [const]
  Returns the collided Object of one of the multiple collisions at index, or null if no object is intersecting the shape (i.e. is_colliding() returns false).

- get_collider_rid(index: int) -> RID [const]
  Returns the RID of the collided object of one of the multiple collisions at index.

- get_collider_shape(index: int) -> int [const]
  Returns the shape ID of the colliding shape of one of the multiple collisions at index, or 0 if no object is intersecting the shape (i.e. is_colliding() returns false).

- get_collision_count() -> int [const]
  The number of collisions detected at the point of impact. Use this to iterate over multiple collisions as provided by get_collider(), get_collider_shape(), get_collision_point(), and get_collision_normal() methods.

- get_collision_mask_value(layer_number: int) -> bool [const]
  Returns whether or not the specified layer of the collision_mask is enabled, given a layer_number between 1 and 32.

- get_collision_normal(index: int) -> Vector3 [const]
  Returns the normal of one of the multiple collisions at index of the intersecting object.

- get_collision_point(index: int) -> Vector3 [const]
  Returns the collision point of one of the multiple collisions at index where the shape intersects the colliding object. **Note:** This point is in the **global** coordinate system.

- is_colliding() -> bool [const]
  Returns whether any object is intersecting with the shape's vector (considering the vector length).

- remove_exception(node: CollisionObject3D) -> void
  Removes a collision exception so the shape does report collisions with the specified node.

- remove_exception_rid(rid: RID) -> void
  Removes a collision exception so the shape does report collisions with the specified RID.

- resource_changed(resource: Resource) -> void
  This method does nothing.

- set_collision_mask_value(layer_number: int, value: bool) -> void
  Based on value, enables or disables the specified layer in the collision_mask, given a layer_number between 1 and 32.

## Properties

- collide_with_areas: bool = false [set set_collide_with_areas; get is_collide_with_areas_enabled]
  If true, collisions with Area3Ds will be reported.

- collide_with_bodies: bool = true [set set_collide_with_bodies; get is_collide_with_bodies_enabled]
  If true, collisions with PhysicsBody3Ds will be reported.

- collision_mask: int = 1 [set set_collision_mask; get get_collision_mask]
  The shape's collision mask. Only objects in at least one collision layer enabled in the mask will be detected. See [Collision layers and masks]($DOCS_URL/tutorials/physics/physics_introduction.html#collision-layers-and-masks) in the documentation for more information.

- collision_result: Array = [] [get get_collision_result]
  Returns the complete collision information from the collision sweep. The data returned is the same as in the PhysicsDirectSpaceState3D.get_rest_info() method.

- debug_shape_custom_color: Color = Color(0, 0, 0, 1) [set set_debug_shape_custom_color; get get_debug_shape_custom_color]
  The custom color to use to draw the shape in the editor and at run-time if **Visible Collision Shapes** is enabled in the **Debug** menu. This color will be highlighted at run-time if the ShapeCast3D is colliding with something. If set to Color(0.0, 0.0, 0.0) (by default), the color set in ProjectSettings.debug/shapes/collision/shape_color is used.

- enabled: bool = true [set set_enabled; get is_enabled]
  If true, collisions will be reported.

- exclude_parent: bool = true [set set_exclude_parent_body; get get_exclude_parent_body]
  If true, the parent node will be excluded from collision detection.

- margin: float = 0.0 [set set_margin; get get_margin]
  The collision margin for the shape. A larger margin helps detecting collisions more consistently, at the cost of precision.

- max_results: int = 32 [set set_max_results; get get_max_results]
  The number of intersections can be limited with this parameter, to reduce the processing time.

- shape: Shape3D [set set_shape; get get_shape]
  The shape to be used for collision queries.

- target_position: Vector3 = Vector3(0, -1, 0) [set set_target_position; get get_target_position]
  The shape's destination point, relative to this node's Node3D.position.
