# Marker2D

## Meta

- Name: Marker2D
- Source: Marker2D.xml
- Inherits: Node2D
- Inheritance Chain: Marker2D -> Node2D -> CanvasItem -> Node -> Object

## Brief Description

Generic 2D position hint for editing.

## Description

Generic 2D position hint for editing. It's just like a plain Node2D, but it displays as a cross in the 2D editor at all times. You can set the cross' visual size by using the gizmo in the 2D editor while the node is selected.

## Quick Reference

```
[properties]
gizmo_extents: float = 10.0
```

## Properties

- gizmo_extents: float = 10.0 [set set_gizmo_extents; get get_gizmo_extents]
  Size of the gizmo cross that appears in the editor.
