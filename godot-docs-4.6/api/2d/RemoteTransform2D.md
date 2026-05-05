# RemoteTransform2D

## Meta

- Name: RemoteTransform2D
- Source: RemoteTransform2D.xml
- Inherits: Node2D
- Inheritance Chain: RemoteTransform2D -> Node2D -> CanvasItem -> Node -> Object

## Brief Description

RemoteTransform2D pushes its own Transform2D to another Node2D derived node in the scene.

## Description

RemoteTransform2D pushes its own Transform2D to another Node2D derived node (called the remote node) in the scene. It can be set to update another node's position, rotation and/or scale. It can use either global or local coordinates.

## Quick Reference

```
[methods]
force_update_cache() -> void

[properties]
remote_path: NodePath = NodePath("")
update_position: bool = true
update_rotation: bool = true
update_scale: bool = true
use_global_coordinates: bool = true
```

## Methods

- force_update_cache() -> void
  RemoteTransform2D caches the remote node. It may not notice if the remote node disappears; force_update_cache() forces it to update the cache again.

## Properties

- remote_path: NodePath = NodePath("") [set set_remote_node; get get_remote_node]
  The NodePath to the remote node, relative to the RemoteTransform2D's position in the scene.

- update_position: bool = true [set set_update_position; get get_update_position]
  If true, the remote node's position is updated.

- update_rotation: bool = true [set set_update_rotation; get get_update_rotation]
  If true, the remote node's rotation is updated.

- update_scale: bool = true [set set_update_scale; get get_update_scale]
  If true, the remote node's scale is updated.

- use_global_coordinates: bool = true [set set_use_global_coordinates; get get_use_global_coordinates]
  If true, global coordinates are used. If false, local coordinates are used.
