# StyleBoxLine

## Meta

- Name: StyleBoxLine
- Source: StyleBoxLine.xml
- Inherits: StyleBox
- Inheritance Chain: StyleBoxLine -> StyleBox -> Resource -> RefCounted -> Object

## Brief Description

A StyleBox that displays a single line of a given color and thickness.

## Description

A StyleBox that displays a single line of a given color and thickness. The line can be either horizontal or vertical. Useful for separators.

## Quick Reference

```
[properties]
color: Color = Color(0, 0, 0, 1)
grow_begin: float = 1.0
grow_end: float = 1.0
thickness: int = 1
vertical: bool = false
```

## Properties

- color: Color = Color(0, 0, 0, 1) [set set_color; get get_color]
  The line's color.

- grow_begin: float = 1.0 [set set_grow_begin; get get_grow_begin]
  The number of pixels the line will extend before the StyleBoxLine's bounds. If set to a negative value, the line will begin inside the StyleBoxLine's bounds.

- grow_end: float = 1.0 [set set_grow_end; get get_grow_end]
  The number of pixels the line will extend past the StyleBoxLine's bounds. If set to a negative value, the line will end inside the StyleBoxLine's bounds.

- thickness: int = 1 [set set_thickness; get get_thickness]
  The line's thickness in pixels.

- vertical: bool = false [set set_vertical; get is_vertical]
  If true, the line will be vertical. If false, the line will be horizontal.
