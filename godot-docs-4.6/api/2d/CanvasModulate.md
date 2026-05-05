# CanvasModulate

## Meta

- Name: CanvasModulate
- Source: CanvasModulate.xml
- Inherits: Node2D
- Inheritance Chain: CanvasModulate -> Node2D -> CanvasItem -> Node -> Object

## Brief Description

A node that applies a color tint to a canvas.

## Description

CanvasModulate applies a color tint to all nodes on a canvas. Only one can be used to tint a canvas, but CanvasLayers can be used to render things independently.

## Quick Reference

```
[properties]
color: Color = Color(1, 1, 1, 1)
```

## Tutorials

- [2D lights and shadows]($DOCS_URL/tutorials/2d/2d_lights_and_shadows.html)

## Properties

- color: Color = Color(1, 1, 1, 1) [set set_color; get get_color]
  The tint color to apply.
