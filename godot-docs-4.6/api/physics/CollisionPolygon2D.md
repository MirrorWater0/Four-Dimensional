# CollisionPolygon2D

## Meta

- Name: CollisionPolygon2D
- Source: CollisionPolygon2D.xml
- Inherits: Node2D
- Inheritance Chain: CollisionPolygon2D -> Node2D -> CanvasItem -> Node -> Object

## Brief Description

A node that provides a polygon shape to a CollisionObject2D parent.

## Description

A node that provides a polygon shape to a CollisionObject2D parent and allows it to be edited. The polygon can be concave or convex. This can give a detection shape to an Area2D, turn a PhysicsBody2D into a solid object, or give a hollow shape to a StaticBody2D. **Warning:** A non-uniformly scaled CollisionPolygon2D will likely not behave as expected. Make sure to keep its scale the same on all axes and adjust its polygon instead.

## Quick Reference

```
[properties]
build_mode: int (CollisionPolygon2D.BuildMode) = 0
disabled: bool = false
one_way_collision: bool = false
one_way_collision_margin: float = 1.0
polygon: PackedVector2Array = PackedVector2Array()
```

## Properties

- build_mode: int (CollisionPolygon2D.BuildMode) = 0 [set set_build_mode; get get_build_mode]
  Collision build mode.

- disabled: bool = false [set set_disabled; get is_disabled]
  If true, no collisions will be detected. This property should be changed with Object.set_deferred().

- one_way_collision: bool = false [set set_one_way_collision; get is_one_way_collision_enabled]
  If true, only edges that face up, relative to CollisionPolygon2D's rotation, will collide with other objects. **Note:** This property has no effect if this CollisionPolygon2D is a child of an Area2D node.

- one_way_collision_margin: float = 1.0 [set set_one_way_collision_margin; get get_one_way_collision_margin]
  The margin used for one-way collision (in pixels). Higher values will make the shape thicker, and work better for colliders that enter the polygon at a high velocity.

- polygon: PackedVector2Array = PackedVector2Array() [set set_polygon; get get_polygon]
  The polygon's list of vertices. Each point will be connected to the next, and the final point will be connected to the first. **Note:** The returned vertices are in the local coordinate space of the given CollisionPolygon2D.

## Constants

### Enum BuildMode

- BUILD_SOLIDS = 0
  Collisions will include the polygon and its contained area. In this mode the node has the same effect as several ConvexPolygonShape2D nodes, one for each convex shape in the convex decomposition of the polygon (but without the overhead of multiple nodes).

- BUILD_SEGMENTS = 1
  Collisions will only include the polygon edges. In this mode the node has the same effect as a single ConcavePolygonShape2D made of segments, with the restriction that each segment (after the first one) starts where the previous one ends, and the last one ends where the first one starts (forming a closed but hollow polygon).
