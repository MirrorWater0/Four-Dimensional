# Joint3D

## Meta

- Name: Joint3D
- Source: Joint3D.xml
- Inherits: Node3D
- Inheritance Chain: Joint3D -> Node3D -> Node -> Object

## Brief Description

Abstract base class for all 3D physics joints.

## Description

Abstract base class for all joints in 3D physics. 3D joints bind together two physics bodies (node_a and node_b) and apply a constraint. If only one body is defined, it is attached to a fixed StaticBody3D without collision shapes.

## Quick Reference

```
[methods]
get_rid() -> RID [const]

[properties]
exclude_nodes_from_collision: bool = true
node_a: NodePath = NodePath("")
node_b: NodePath = NodePath("")
solver_priority: int = 1
```

## Tutorials

- [3D Truck Town Demo](https://godotengine.org/asset-library/asset/2752)

## Methods

- get_rid() -> RID [const]
  Returns the joint's internal RID from the PhysicsServer3D.

## Properties

- exclude_nodes_from_collision: bool = true [set set_exclude_nodes_from_collision; get get_exclude_nodes_from_collision]
  If true, the two bodies bound together do not collide with each other.

- node_a: NodePath = NodePath("") [set set_node_a; get get_node_a]
  Path to the first node (A) attached to the joint. The node must inherit PhysicsBody3D. If left empty and node_b is set, the body is attached to a fixed StaticBody3D without collision shapes.

- node_b: NodePath = NodePath("") [set set_node_b; get get_node_b]
  Path to the second node (B) attached to the joint. The node must inherit PhysicsBody3D. If left empty and node_a is set, the body is attached to a fixed StaticBody3D without collision shapes.

- solver_priority: int = 1 [set set_solver_priority; get get_solver_priority]
  The priority used to define which solver is executed first for multiple joints. The lower the value, the higher the priority.
