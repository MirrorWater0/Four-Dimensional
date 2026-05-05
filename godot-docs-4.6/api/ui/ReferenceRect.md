# ReferenceRect

## Meta

- Name: ReferenceRect
- Source: ReferenceRect.xml
- Inherits: Control
- Inheritance Chain: ReferenceRect -> Control -> CanvasItem -> Node -> Object

## Brief Description

A rectangular box for designing UIs.

## Description

A rectangular box that displays only a colored border around its rectangle (see Control.get_rect()). It can be used to visualize the extents of a Control node, for testing purposes.

## Quick Reference

```
[properties]
border_color: Color = Color(1, 0, 0, 1)
border_width: float = 1.0
editor_only: bool = true
```

## Properties

- border_color: Color = Color(1, 0, 0, 1) [set set_border_color; get get_border_color]
  Sets the border color of the ReferenceRect.

- border_width: float = 1.0 [set set_border_width; get get_border_width]
  Sets the border width of the ReferenceRect. The border grows both inwards and outwards with respect to the rectangle box.

- editor_only: bool = true [set set_editor_only; get get_editor_only]
  If true, the ReferenceRect will only be visible while in editor. Otherwise, ReferenceRect will be visible in the running project.
