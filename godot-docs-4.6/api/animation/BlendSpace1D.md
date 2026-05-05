# AnimationNodeBlendSpace1D

## Meta

- Name: AnimationNodeBlendSpace1D
- Source: AnimationNodeBlendSpace1D.xml
- Inherits: AnimationRootNode
- Inheritance Chain: AnimationNodeBlendSpace1D -> AnimationRootNode -> AnimationNode -> Resource -> RefCounted -> Object

## Brief Description

A set of AnimationRootNodes placed on a virtual axis, crossfading between the two adjacent ones. Used by AnimationTree.

## Description

A resource used by AnimationNodeBlendTree. AnimationNodeBlendSpace1D represents a virtual axis on which any type of AnimationRootNodes can be added using add_blend_point(). Outputs the linear blend of the two AnimationRootNodes adjacent to the current value. You can set the extents of the axis with min_space and max_space.

## Quick Reference

```
[methods]
add_blend_point(node: AnimationRootNode, pos: float, at_index: int = -1) -> void
get_blend_point_count() -> int [const]
get_blend_point_node(point: int) -> AnimationRootNode [const]
get_blend_point_position(point: int) -> float [const]
remove_blend_point(point: int) -> void
set_blend_point_node(point: int, node: AnimationRootNode) -> void
set_blend_point_position(point: int, pos: float) -> void

[properties]
blend_mode: int (AnimationNodeBlendSpace1D.BlendMode) = 0
max_space: float = 1.0
min_space: float = -1.0
snap: float = 0.1
sync: bool = false
value_label: String = "value"
```

## Tutorials

- [Using AnimationTree]($DOCS_URL/tutorials/animation/animation_tree.html)

## Methods

- add_blend_point(node: AnimationRootNode, pos: float, at_index: int = -1) -> void
  Adds a new point that represents a node on the virtual axis at a given position set by pos. You can insert it at a specific index using the at_index argument. If you use the default value for at_index, the point is inserted at the end of the blend points array.

- get_blend_point_count() -> int [const]
  Returns the number of points on the blend axis.

- get_blend_point_node(point: int) -> AnimationRootNode [const]
  Returns the AnimationNode referenced by the point at index point.

- get_blend_point_position(point: int) -> float [const]
  Returns the position of the point at index point.

- remove_blend_point(point: int) -> void
  Removes the point at index point from the blend axis.

- set_blend_point_node(point: int, node: AnimationRootNode) -> void
  Changes the AnimationNode referenced by the point at index point.

- set_blend_point_position(point: int, pos: float) -> void
  Updates the position of the point at index point on the blend axis.

## Properties

- blend_mode: int (AnimationNodeBlendSpace1D.BlendMode) = 0 [set set_blend_mode; get get_blend_mode]
  Controls the interpolation between animations.

- max_space: float = 1.0 [set set_max_space; get get_max_space]
  The blend space's axis's upper limit for the points' position. See add_blend_point().

- min_space: float = -1.0 [set set_min_space; get get_min_space]
  The blend space's axis's lower limit for the points' position. See add_blend_point().

- snap: float = 0.1 [set set_snap; get get_snap]
  Position increment to snap to when moving a point on the axis.

- sync: bool = false [set set_use_sync; get is_using_sync]
  If false, the blended animations' frame are stopped when the blend value is 0. If true, forcing the blended animations to advance frame.

- value_label: String = "value" [set set_value_label; get get_value_label]
  Label of the virtual axis of the blend space.

## Constants

### Enum BlendMode

- BLEND_MODE_INTERPOLATED = 0
  The interpolation between animations is linear.

- BLEND_MODE_DISCRETE = 1
  The blend space plays the animation of the animation node which blending position is closest to. Useful for frame-by-frame 2D animations.

- BLEND_MODE_DISCRETE_CARRY = 2
  Similar to BLEND_MODE_DISCRETE, but starts the new animation at the last animation's playback position.
