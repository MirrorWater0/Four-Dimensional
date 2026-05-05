# World2D

## Meta

- Name: World2D
- Source: World2D.xml
- Inherits: Resource
- Inheritance Chain: World2D -> Resource -> RefCounted -> Object

## Brief Description

A resource that holds all components of a 2D world, such as a canvas and a physics space.

## Description

Class that has everything pertaining to a 2D world: A physics space, a canvas, and a sound space. 2D nodes register their resources into the current 2D world.

## Quick Reference

```
[properties]
canvas: RID
direct_space_state: PhysicsDirectSpaceState2D
navigation_map: RID
space: RID
```

## Tutorials

- [Ray-casting]($DOCS_URL/tutorials/physics/ray-casting.html)

## Properties

- canvas: RID [get get_canvas]
  The RID of this world's canvas resource. Used by the RenderingServer for 2D drawing.

- direct_space_state: PhysicsDirectSpaceState2D [get get_direct_space_state]
  Direct access to the world's physics 2D space state. Used for querying current and potential collisions. When using multi-threaded physics, access is limited to Node._physics_process() in the main thread.

- navigation_map: RID [get get_navigation_map]
  The RID of this world's navigation map. Used by the NavigationServer2D.

- space: RID [get get_space]
  The RID of this world's physics space resource. Used by the PhysicsServer2D for 2D physics, treating it as both a space and an area.
