# Path2D

## Meta

- Name: Path2D
- Source: Path2D.xml
- Inherits: Node2D
- Inheritance Chain: Path2D -> Node2D -> CanvasItem -> Node -> Object

## Brief Description

Contains a Curve2D path for PathFollow2D nodes to follow.

## Description

Can have PathFollow2D child nodes moving along the Curve2D. See PathFollow2D for more information on usage. **Note:** The path is considered as relative to the moved nodes (children of PathFollow2D). As such, the curve should usually start with a zero vector ((0, 0)).

## Quick Reference

```
[properties]
curve: Curve2D
```

## Properties

- curve: Curve2D [set set_curve; get get_curve]
  A Curve2D describing the path.
