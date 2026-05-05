# Polygon2D

## Meta

- Name: Polygon2D
- Source: Polygon2D.xml
- Inherits: Node2D
- Inheritance Chain: Polygon2D -> Node2D -> CanvasItem -> Node -> Object

## Brief Description

A 2D polygon.

## Description

A Polygon2D is defined by a set of points. Each point is connected to the next, with the final point being connected to the first, resulting in a closed polygon. Polygon2Ds can be filled with color (solid or gradient) or filled with a given texture.

## Quick Reference

```
[methods]
add_bone(path: NodePath, weights: PackedFloat32Array) -> void
clear_bones() -> void
erase_bone(index: int) -> void
get_bone_count() -> int [const]
get_bone_path(index: int) -> NodePath [const]
get_bone_weights(index: int) -> PackedFloat32Array [const]
set_bone_path(index: int, path: NodePath) -> void
set_bone_weights(index: int, weights: PackedFloat32Array) -> void

[properties]
antialiased: bool = false
color: Color = Color(1, 1, 1, 1)
internal_vertex_count: int = 0
invert_border: float = 100.0
invert_enabled: bool = false
offset: Vector2 = Vector2(0, 0)
polygon: PackedVector2Array = PackedVector2Array()
polygons: Array = []
skeleton: NodePath = NodePath("")
texture: Texture2D
texture_offset: Vector2 = Vector2(0, 0)
texture_rotation: float = 0.0
texture_scale: Vector2 = Vector2(1, 1)
uv: PackedVector2Array = PackedVector2Array()
vertex_colors: PackedColorArray = PackedColorArray()
```

## Methods

- add_bone(path: NodePath, weights: PackedFloat32Array) -> void
  Adds a bone with the specified path and weights.

- clear_bones() -> void
  Removes all bones from this Polygon2D.

- erase_bone(index: int) -> void
  Removes the specified bone from this Polygon2D.

- get_bone_count() -> int [const]
  Returns the number of bones in this Polygon2D.

- get_bone_path(index: int) -> NodePath [const]
  Returns the path to the node associated with the specified bone.

- get_bone_weights(index: int) -> PackedFloat32Array [const]
  Returns the weight values of the specified bone.

- set_bone_path(index: int, path: NodePath) -> void
  Sets the path to the node associated with the specified bone.

- set_bone_weights(index: int, weights: PackedFloat32Array) -> void
  Sets the weight values for the specified bone.

## Properties

- antialiased: bool = false [set set_antialiased; get get_antialiased]
  If true, polygon edges will be anti-aliased.

- color: Color = Color(1, 1, 1, 1) [set set_color; get get_color]
  The polygon's fill color. If texture is set, it will be multiplied by this color. It will also be the default color for vertices not set in vertex_colors.

- internal_vertex_count: int = 0 [set set_internal_vertex_count; get get_internal_vertex_count]
  Number of internal vertices, used for UV mapping.

- invert_border: float = 100.0 [set set_invert_border; get get_invert_border]
  Added padding applied to the bounding box when invert_enabled is set to true. Setting this value too small may result in a "Bad Polygon" error.

- invert_enabled: bool = false [set set_invert_enabled; get get_invert_enabled]
  If true, the polygon will be inverted, containing the area outside the defined points and extending to the invert_border.

- offset: Vector2 = Vector2(0, 0) [set set_offset; get get_offset]
  The offset applied to each vertex.

- polygon: PackedVector2Array = PackedVector2Array() [set set_polygon; get get_polygon]
  The polygon's list of vertices. The final point will be connected to the first.

- polygons: Array = [] [set set_polygons; get get_polygons]
  The list of polygons, in case more than one is being represented. Every individual polygon is stored as a PackedInt32Array where each int is an index to a point in polygon. If empty, this property will be ignored, and the resulting single polygon will be composed of all points in polygon, using the order they are stored in.

- skeleton: NodePath = NodePath("") [set set_skeleton; get get_skeleton]
  Path to a Skeleton2D node used for skeleton-based deformations of this polygon. If empty or invalid, skeletal deformations will not be used.

- texture: Texture2D [set set_texture; get get_texture]
  The polygon's fill texture. Use uv to set texture coordinates.

- texture_offset: Vector2 = Vector2(0, 0) [set set_texture_offset; get get_texture_offset]
  Amount to offset the polygon's texture. If set to Vector2(0, 0), the texture's origin (its top-left corner) will be placed at the polygon's position.

- texture_rotation: float = 0.0 [set set_texture_rotation; get get_texture_rotation]
  The texture's rotation in radians.

- texture_scale: Vector2 = Vector2(1, 1) [set set_texture_scale; get get_texture_scale]
  Amount to multiply the uv coordinates when using texture. Larger values make the texture smaller, and vice versa.

- uv: PackedVector2Array = PackedVector2Array() [set set_uv; get get_uv]
  Texture coordinates for each vertex of the polygon. There should be one UV value per polygon vertex. If there are fewer, undefined vertices will use Vector2(0, 0).

- vertex_colors: PackedColorArray = PackedColorArray() [set set_vertex_colors; get get_vertex_colors]
  Color for each vertex. Colors are interpolated between vertices, resulting in smooth gradients. There should be one per polygon vertex. If there are fewer, undefined vertices will use color.
