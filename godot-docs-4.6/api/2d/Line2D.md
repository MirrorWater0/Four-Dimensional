# Line2D

## Meta

- Name: Line2D
- Source: Line2D.xml
- Inherits: Node2D
- Inheritance Chain: Line2D -> Node2D -> CanvasItem -> Node -> Object

## Brief Description

A 2D polyline that can optionally be textured.

## Description

This node draws a 2D polyline, i.e. a shape consisting of several points connected by segments. Line2D is not a mathematical polyline, i.e. the segments are not infinitely thin. It is intended for rendering and it can be colored and optionally textured. **Warning:** Certain configurations may be impossible to draw nicely, such as very sharp angles. In these situations, the node uses fallback drawing logic to look decent. **Note:** Line2D is drawn using a 2D mesh.

## Quick Reference

```
[methods]
add_point(position: Vector2, index: int = -1) -> void
clear_points() -> void
get_point_count() -> int [const]
get_point_position(index: int) -> Vector2 [const]
remove_point(index: int) -> void
set_point_position(index: int, position: Vector2) -> void

[properties]
antialiased: bool = false
begin_cap_mode: int (Line2D.LineCapMode) = 0
closed: bool = false
default_color: Color = Color(1, 1, 1, 1)
end_cap_mode: int (Line2D.LineCapMode) = 0
gradient: Gradient
joint_mode: int (Line2D.LineJointMode) = 0
points: PackedVector2Array = PackedVector2Array()
round_precision: int = 8
sharp_limit: float = 2.0
texture: Texture2D
texture_mode: int (Line2D.LineTextureMode) = 0
width: float = 10.0
width_curve: Curve
```

## Tutorials

- [Matrix Transform Demo](https://godotengine.org/asset-library/asset/2787)
- [2.5D Game Demo](https://godotengine.org/asset-library/asset/2783)

## Methods

- add_point(position: Vector2, index: int = -1) -> void
  Adds a point with the specified position relative to the polyline's own position. If no index is provided, the new point will be added to the end of the points array. If index is given, the new point is inserted before the existing point identified by index index. The indices of the points after the new point get increased by 1. The provided index must not exceed the number of existing points in the polyline. See get_point_count().

- clear_points() -> void
  Removes all points from the polyline, making it empty.

- get_point_count() -> int [const]
  Returns the number of points in the polyline.

- get_point_position(index: int) -> Vector2 [const]
  Returns the position of the point at index index.

- remove_point(index: int) -> void
  Removes the point at index index from the polyline.

- set_point_position(index: int, position: Vector2) -> void
  Overwrites the position of the point at the given index with the supplied position.

## Properties

- antialiased: bool = false [set set_antialiased; get get_antialiased]
  If true, the polyline's border will be anti-aliased. **Note:** Line2D is not accelerated by batching when being anti-aliased.

- begin_cap_mode: int (Line2D.LineCapMode) = 0 [set set_begin_cap_mode; get get_begin_cap_mode]
  The style of the beginning of the polyline, if closed is false.

- closed: bool = false [set set_closed; get is_closed]
  If true and the polyline has more than 2 points, the last point and the first one will be connected by a segment. **Note:** The shape of the closing segment is not guaranteed to be seamless if a width_curve is provided. **Note:** The joint between the closing segment and the first segment is drawn first and it samples the gradient and the width_curve at the beginning. This is an implementation detail that might change in a future version.

- default_color: Color = Color(1, 1, 1, 1) [set set_default_color; get get_default_color]
  The color of the polyline. Will not be used if a gradient is set.

- end_cap_mode: int (Line2D.LineCapMode) = 0 [set set_end_cap_mode; get get_end_cap_mode]
  The style of the end of the polyline, if closed is false.

- gradient: Gradient [set set_gradient; get get_gradient]
  The gradient is drawn through the whole line from start to finish. The default_color will not be used if this property is set.

- joint_mode: int (Line2D.LineJointMode) = 0 [set set_joint_mode; get get_joint_mode]
  The style of the connections between segments of the polyline.

- points: PackedVector2Array = PackedVector2Array() [set set_points; get get_points]
  The points of the polyline, interpreted in local 2D coordinates. Segments are drawn between the adjacent points in this array.

- round_precision: int = 8 [set set_round_precision; get get_round_precision]
  The smoothness used for rounded joints and caps. Higher values result in smoother corners, but are more demanding to render and update.

- sharp_limit: float = 2.0 [set set_sharp_limit; get get_sharp_limit]
  Determines the miter limit of the polyline. Normally, when joint_mode is set to LINE_JOINT_SHARP, sharp angles fall back to using the logic of LINE_JOINT_BEVEL joints to prevent very long miters. Higher values of this property mean that the fallback to a bevel joint will happen at sharper angles.

- texture: Texture2D [set set_texture; get get_texture]
  The texture used for the polyline. Uses texture_mode for drawing style.

- texture_mode: int (Line2D.LineTextureMode) = 0 [set set_texture_mode; get get_texture_mode]
  The style to render the texture of the polyline.

- width: float = 10.0 [set set_width; get get_width]
  The polyline's width.

- width_curve: Curve [set set_curve; get get_curve]
  The polyline's width curve. The width of the polyline over its length will be equivalent to the value of the width curve over its domain. The width curve should be a unit Curve.

## Constants

### Enum LineJointMode

- LINE_JOINT_SHARP = 0
  Makes the polyline's joints pointy, connecting the sides of the two segments by extending them until they intersect. If the rotation of a joint is too big (based on sharp_limit), the joint falls back to LINE_JOINT_BEVEL to prevent very long miters.

- LINE_JOINT_BEVEL = 1
  Makes the polyline's joints bevelled/chamfered, connecting the sides of the two segments with a simple line.

- LINE_JOINT_ROUND = 2
  Makes the polyline's joints rounded, connecting the sides of the two segments with an arc. The detail of this arc depends on round_precision.

### Enum LineCapMode

- LINE_CAP_NONE = 0
  Draws no line cap.

- LINE_CAP_BOX = 1
  Draws the line cap as a box, slightly extending the first/last segment.

- LINE_CAP_ROUND = 2
  Draws the line cap as a semicircle attached to the first/last segment.

### Enum LineTextureMode

- LINE_TEXTURE_NONE = 0
  Takes the left pixels of the texture and renders them over the whole polyline.

- LINE_TEXTURE_TILE = 1
  Tiles the texture over the polyline. CanvasItem.texture_repeat of the Line2D node must be CanvasItem.TEXTURE_REPEAT_ENABLED or CanvasItem.TEXTURE_REPEAT_MIRROR for it to work properly.

- LINE_TEXTURE_STRETCH = 2
  Stretches the texture across the polyline. CanvasItem.texture_repeat of the Line2D node must be CanvasItem.TEXTURE_REPEAT_DISABLED for best results.
