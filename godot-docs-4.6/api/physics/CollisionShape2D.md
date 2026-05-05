# CollisionShape2D

## Meta

- Name: CollisionShape2D
- Source: CollisionShape2D.xml
- Inherits: Node2D
- Inheritance Chain: CollisionShape2D -> Node2D -> CanvasItem -> Node -> Object

## Brief Description

A node that provides a Shape2D to a CollisionObject2D parent.

## Description

A node that provides a Shape2D to a CollisionObject2D parent and allows it to be edited. This can give a detection shape to an Area2D or turn a PhysicsBody2D into a solid object.

## Quick Reference

```
[properties]
debug_color: Color = Color(0, 0, 0, 0)
disabled: bool = false
one_way_collision: bool = false
one_way_collision_margin: float = 1.0
shape: Shape2D
```

## Tutorials

- [Physics introduction]($DOCS_URL/tutorials/physics/physics_introduction.html)
- [2D Dodge The Creeps Demo](https://godotengine.org/asset-library/asset/2712)
- [2D Pong Demo](https://godotengine.org/asset-library/asset/2728)
- [2D Kinematic Character Demo](https://godotengine.org/asset-library/asset/2719)

## Properties

- debug_color: Color = Color(0, 0, 0, 0) [set set_debug_color; get get_debug_color]
  The collision shape color that is displayed in the editor, or in the running project if **Debug > Visible Collision Shapes** is checked at the top of the editor. **Note:** The default value is ProjectSettings.debug/shapes/collision/shape_color. The Color(0, 0, 0, 0) value documented here is a placeholder, and not the actual default debug color.

- disabled: bool = false [set set_disabled; get is_disabled]
  A disabled collision shape has no effect in the world. This property should be changed with Object.set_deferred().

- one_way_collision: bool = false [set set_one_way_collision; get is_one_way_collision_enabled]
  Sets whether this collision shape should only detect collision on one side (top or bottom). **Note:** This property has no effect if this CollisionShape2D is a child of an Area2D node.

- one_way_collision_margin: float = 1.0 [set set_one_way_collision_margin; get get_one_way_collision_margin]
  The margin used for one-way collision (in pixels). Higher values will make the shape thicker, and work better for colliders that enter the shape at a high velocity.

- shape: Shape2D [set set_shape; get get_shape]
  The actual shape owned by this collision shape.
