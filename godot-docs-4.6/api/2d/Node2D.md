# Node2D

## Meta

- Name: Node2D
- Source: Node2D.xml
- Inherits: CanvasItem
- Inheritance Chain: Node2D -> CanvasItem -> Node -> Object

## Brief Description

A 2D game object, inherited by all 2D-related nodes. Has a position, rotation, scale, and skew.

## Description

A 2D game object, with a transform (position, rotation, and scale). All 2D nodes, including physics objects and sprites, inherit from Node2D. Use Node2D as a parent node to move, scale and rotate children in a 2D project. Also gives control of the node's render order. **Note:** Since both Node2D and Control inherit from CanvasItem, they share several concepts from the class such as the CanvasItem.z_index and CanvasItem.visible properties.

## Quick Reference

```
[methods]
apply_scale(ratio: Vector2) -> void
get_angle_to(point: Vector2) -> float [const]
get_relative_transform_to_parent(parent: Node) -> Transform2D [const]
global_translate(offset: Vector2) -> void
look_at(point: Vector2) -> void
move_local_x(delta: float, scaled: bool = false) -> void
move_local_y(delta: float, scaled: bool = false) -> void
rotate(radians: float) -> void
to_global(local_point: Vector2) -> Vector2 [const]
to_local(global_point: Vector2) -> Vector2 [const]
translate(offset: Vector2) -> void

[properties]
global_position: Vector2
global_rotation: float
global_rotation_degrees: float
global_scale: Vector2
global_skew: float
global_transform: Transform2D
position: Vector2 = Vector2(0, 0)
rotation: float = 0.0
rotation_degrees: float
scale: Vector2 = Vector2(1, 1)
skew: float = 0.0
transform: Transform2D
```

## Tutorials

- [Custom drawing in 2D]($DOCS_URL/tutorials/2d/custom_drawing_in_2d.html)
- [All 2D Demos](https://github.com/godotengine/godot-demo-projects/tree/master/2d)

## Methods

- apply_scale(ratio: Vector2) -> void
  Multiplies the current scale by the ratio vector.

- get_angle_to(point: Vector2) -> float [const]
  Returns the angle between the node and the point in radians. See also look_at(). [Illustration of the returned angle.](https://raw.githubusercontent.com/godotengine/godot-docs/master/img/node2d_get_angle_to.png)

- get_relative_transform_to_parent(parent: Node) -> Transform2D [const]
  Returns the Transform2D relative to this node's parent.

- global_translate(offset: Vector2) -> void
  Adds the offset vector to the node's global position.

- look_at(point: Vector2) -> void
  Rotates the node so that its local +X axis points towards the point, which is expected to use global coordinates. This method is a combination of both rotate() and get_angle_to(). point should not be the same as the node's position, otherwise the node always looks to the right.

- move_local_x(delta: float, scaled: bool = false) -> void
  Applies a local translation on the node's X axis with the amount specified in delta. If scaled is false, normalizes the movement to occur independently of the node's scale.

- move_local_y(delta: float, scaled: bool = false) -> void
  Applies a local translation on the node's Y axis with the amount specified in delta. If scaled is false, normalizes the movement to occur independently of the node's scale.

- rotate(radians: float) -> void
  Applies a rotation to the node, in radians, starting from its current rotation. This is equivalent to rotation += radians.

- to_global(local_point: Vector2) -> Vector2 [const]
  Transforms the provided local position into a position in global coordinate space. The input is expected to be local relative to the Node2D it is called on. e.g. Applying this method to the positions of child nodes will correctly transform their positions into the global coordinate space, but applying it to a node's own position will give an incorrect result, as it will incorporate the node's own transformation into its global position.

- to_local(global_point: Vector2) -> Vector2 [const]
  Transforms the provided global position into a position in local coordinate space. The output will be local relative to the Node2D it is called on. e.g. It is appropriate for determining the positions of child nodes, but it is not appropriate for determining its own position relative to its parent.

- translate(offset: Vector2) -> void
  Translates the node by the given offset in local coordinates. This is equivalent to position += offset.

## Properties

- global_position: Vector2 [set set_global_position; get get_global_position]
  Global position. See also position.

- global_rotation: float [set set_global_rotation; get get_global_rotation]
  Global rotation in radians. See also rotation.

- global_rotation_degrees: float [set set_global_rotation_degrees; get get_global_rotation_degrees]
  Helper property to access global_rotation in degrees instead of radians. See also rotation_degrees.

- global_scale: Vector2 [set set_global_scale; get get_global_scale]
  Global scale. See also scale.

- global_skew: float [set set_global_skew; get get_global_skew]
  Global skew in radians. See also skew.

- global_transform: Transform2D [set set_global_transform; get get_global_transform]
  Global Transform2D. See also transform.

- position: Vector2 = Vector2(0, 0) [set set_position; get get_position]
  Position, relative to the node's parent. See also global_position.

- rotation: float = 0.0 [set set_rotation; get get_rotation]
  Rotation in radians, relative to the node's parent. See also global_rotation. **Note:** This property is edited in the inspector in degrees. If you want to use degrees in a script, use rotation_degrees.

- rotation_degrees: float [set set_rotation_degrees; get get_rotation_degrees]
  Helper property to access rotation in degrees instead of radians. See also global_rotation_degrees.

- scale: Vector2 = Vector2(1, 1) [set set_scale; get get_scale]
  The node's scale, relative to the node's parent. Unscaled value: (1, 1). See also global_scale. **Note:** Negative X scales in 2D are not decomposable from the transformation matrix. Due to the way scale is represented with transformation matrices in Godot, negative scales on the X axis will be changed to negative scales on the Y axis and a rotation of 180 degrees when decomposed.

- skew: float = 0.0 [set set_skew; get get_skew]
  If set to a non-zero value, slants the node in one direction or another. This can be used for pseudo-3D effects. See also global_skew. **Note:** Skew is performed on the X axis only, and *between* rotation and scaling. **Note:** This property is edited in the inspector in degrees. If you want to use degrees in a script, use skew = deg_to_rad(value_in_degrees).

- transform: Transform2D [set set_transform; get get_transform]
  The node's Transform2D, relative to the node's parent. See also global_transform.
