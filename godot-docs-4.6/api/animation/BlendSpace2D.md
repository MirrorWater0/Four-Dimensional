# AnimationNodeBlendSpace2D

## Meta

- Name: AnimationNodeBlendSpace2D
- Source: AnimationNodeBlendSpace2D.xml
- Inherits: AnimationRootNode
- Inheritance Chain: AnimationNodeBlendSpace2D -> AnimationRootNode -> AnimationNode -> Resource -> RefCounted -> Object

## Brief Description

A set of AnimationRootNodes placed on 2D coordinates, crossfading between the three adjacent ones. Used by AnimationTree.

## Description

A resource used by AnimationNodeBlendTree. AnimationNodeBlendSpace2D represents a virtual 2D space on which AnimationRootNodes are placed. Outputs the linear blend of the three adjacent animations using a Vector2 weight. Adjacent in this context means the three AnimationRootNodes making up the triangle that contains the current value. You can add vertices to the blend space with add_blend_point() and automatically triangulate it by setting auto_triangles to true. Otherwise, use add_triangle() and remove_triangle() to triangulate the blend space by hand.

## Quick Reference

```
[methods]
add_blend_point(node: AnimationRootNode, pos: Vector2, at_index: int = -1) -> void
add_triangle(x: int, y: int, z: int, at_index: int = -1) -> void
get_blend_point_count() -> int [const]
get_blend_point_node(point: int) -> AnimationRootNode [const]
get_blend_point_position(point: int) -> Vector2 [const]
get_triangle_count() -> int [const]
get_triangle_point(triangle: int, point: int) -> int
remove_blend_point(point: int) -> void
remove_triangle(triangle: int) -> void
set_blend_point_node(point: int, node: AnimationRootNode) -> void
set_blend_point_position(point: int, pos: Vector2) -> void

[properties]
auto_triangles: bool = true
blend_mode: int (AnimationNodeBlendSpace2D.BlendMode) = 0
max_space: Vector2 = Vector2(1, 1)
min_space: Vector2 = Vector2(-1, -1)
snap: Vector2 = Vector2(0.1, 0.1)
sync: bool = false
x_label: String = "x"
y_label: String = "y"
```

## Tutorials

- [Using AnimationTree]($DOCS_URL/tutorials/animation/animation_tree.html)
- [Third Person Shooter (TPS) Demo](https://godotengine.org/asset-library/asset/2710)

## Methods

- add_blend_point(node: AnimationRootNode, pos: Vector2, at_index: int = -1) -> void
  Adds a new point that represents a node at the position set by pos. You can insert it at a specific index using the at_index argument. If you use the default value for at_index, the point is inserted at the end of the blend points array.

- add_triangle(x: int, y: int, z: int, at_index: int = -1) -> void
  Creates a new triangle using three points x, y, and z. Triangles can overlap. You can insert the triangle at a specific index using the at_index argument. If you use the default value for at_index, the point is inserted at the end of the blend points array.

- get_blend_point_count() -> int [const]
  Returns the number of points in the blend space.

- get_blend_point_node(point: int) -> AnimationRootNode [const]
  Returns the AnimationRootNode referenced by the point at index point.

- get_blend_point_position(point: int) -> Vector2 [const]
  Returns the position of the point at index point.

- get_triangle_count() -> int [const]
  Returns the number of triangles in the blend space.

- get_triangle_point(triangle: int, point: int) -> int
  Returns the position of the point at index point in the triangle of index triangle.

- remove_blend_point(point: int) -> void
  Removes the point at index point from the blend space.

- remove_triangle(triangle: int) -> void
  Removes the triangle at index triangle from the blend space.

- set_blend_point_node(point: int, node: AnimationRootNode) -> void
  Changes the AnimationNode referenced by the point at index point.

- set_blend_point_position(point: int, pos: Vector2) -> void
  Updates the position of the point at index point in the blend space.

## Properties

- auto_triangles: bool = true [set set_auto_triangles; get get_auto_triangles]
  If true, the blend space is triangulated automatically. The mesh updates every time you add or remove points with add_blend_point() and remove_blend_point().

- blend_mode: int (AnimationNodeBlendSpace2D.BlendMode) = 0 [set set_blend_mode; get get_blend_mode]
  Controls the interpolation between animations.

- max_space: Vector2 = Vector2(1, 1) [set set_max_space; get get_max_space]
  The blend space's X and Y axes' upper limit for the points' position. See add_blend_point().

- min_space: Vector2 = Vector2(-1, -1) [set set_min_space; get get_min_space]
  The blend space's X and Y axes' lower limit for the points' position. See add_blend_point().

- snap: Vector2 = Vector2(0.1, 0.1) [set set_snap; get get_snap]
  Position increment to snap to when moving a point.

- sync: bool = false [set set_use_sync; get is_using_sync]
  If false, the blended animations' frame are stopped when the blend value is 0. If true, forcing the blended animations to advance frame.

- x_label: String = "x" [set set_x_label; get get_x_label]
  Name of the blend space's X axis.

- y_label: String = "y" [set set_y_label; get get_y_label]
  Name of the blend space's Y axis.

## Signals

- triangles_updated()
  Emitted every time the blend space's triangles are created, removed, or when one of their vertices changes position.

## Constants

### Enum BlendMode

- BLEND_MODE_INTERPOLATED = 0
  The interpolation between animations is linear.

- BLEND_MODE_DISCRETE = 1
  The blend space plays the animation of the animation node which blending position is closest to. Useful for frame-by-frame 2D animations.

- BLEND_MODE_DISCRETE_CARRY = 2
  Similar to BLEND_MODE_DISCRETE, but starts the new animation at the last animation's playback position.
