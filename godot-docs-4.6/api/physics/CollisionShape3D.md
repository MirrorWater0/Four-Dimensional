# CollisionShape3D

## Meta

- Name: CollisionShape3D
- Source: CollisionShape3D.xml
- Inherits: Node3D
- Inheritance Chain: CollisionShape3D -> Node3D -> Node -> Object

## Brief Description

A node that provides a Shape3D to a CollisionObject3D parent.

## Description

A node that provides a Shape3D to a CollisionObject3D parent and allows it to be edited. This can give a detection shape to an Area3D or turn a PhysicsBody3D into a solid object. **Warning:** A non-uniformly scaled CollisionShape3D will likely not behave as expected. Make sure to keep its scale the same on all axes and adjust its shape resource instead.

## Quick Reference

```
[methods]
make_convex_from_siblings() -> void
resource_changed(resource: Resource) -> void

[properties]
debug_color: Color = Color(0, 0, 0, 0)
debug_fill: bool = true
disabled: bool = false
shape: Shape3D
```

## Tutorials

- [Physics introduction]($DOCS_URL/tutorials/physics/physics_introduction.html)
- [3D Kinematic Character Demo](https://godotengine.org/asset-library/asset/2739)
- [3D Platformer Demo](https://godotengine.org/asset-library/asset/2748)
- [Third Person Shooter (TPS) Demo](https://godotengine.org/asset-library/asset/2710)

## Methods

- make_convex_from_siblings() -> void
  Sets the collision shape's shape to the addition of all its convexed MeshInstance3D siblings geometry.

- resource_changed(resource: Resource) -> void
  This method does nothing.

## Properties

- debug_color: Color = Color(0, 0, 0, 0) [set set_debug_color; get get_debug_color]
  The collision shape color that is displayed in the editor, or in the running project if **Debug > Visible Collision Shapes** is checked at the top of the editor. **Note:** The default value is ProjectSettings.debug/shapes/collision/shape_color. The Color(0, 0, 0, 0) value documented here is a placeholder, and not the actual default debug color.

- debug_fill: bool = true [set set_enable_debug_fill; get get_enable_debug_fill]
  If true, when the shape is displayed, it will show a solid fill color in addition to its wireframe.

- disabled: bool = false [set set_disabled; get is_disabled]
  A disabled collision shape has no effect in the world. This property should be changed with Object.set_deferred().

- shape: Shape3D [set set_shape; get get_shape]
  The actual shape owned by this collision shape.
