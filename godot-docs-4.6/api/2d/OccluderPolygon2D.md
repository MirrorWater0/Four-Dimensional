# OccluderPolygon2D

## Meta

- Name: OccluderPolygon2D
- Source: OccluderPolygon2D.xml
- Inherits: Resource
- Inheritance Chain: OccluderPolygon2D -> Resource -> RefCounted -> Object

## Brief Description

Defines a 2D polygon for LightOccluder2D.

## Description

Editor facility that helps you draw a 2D polygon used as resource for LightOccluder2D.

## Quick Reference

```
[properties]
closed: bool = true
cull_mode: int (OccluderPolygon2D.CullMode) = 0
polygon: PackedVector2Array = PackedVector2Array()
```

## Properties

- closed: bool = true [set set_closed; get is_closed]
  If true, closes the polygon. A closed OccluderPolygon2D occludes the light coming from any direction. An opened OccluderPolygon2D occludes the light only at its outline's direction.

- cull_mode: int (OccluderPolygon2D.CullMode) = 0 [set set_cull_mode; get get_cull_mode]
  The culling mode to use.

- polygon: PackedVector2Array = PackedVector2Array() [set set_polygon; get get_polygon]
  A Vector2 array with the index for polygon's vertices positions.

## Constants

### Enum CullMode

- CULL_DISABLED = 0
  Culling is disabled. See cull_mode.

- CULL_CLOCKWISE = 1
  Culling is performed in the clockwise direction. See cull_mode.

- CULL_COUNTER_CLOCKWISE = 2
  Culling is performed in the counterclockwise direction. See cull_mode.
