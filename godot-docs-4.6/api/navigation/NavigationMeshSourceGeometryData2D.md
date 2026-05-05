# NavigationMeshSourceGeometryData2D

## Meta

- Name: NavigationMeshSourceGeometryData2D
- Source: NavigationMeshSourceGeometryData2D.xml
- Inherits: Resource
- Inheritance Chain: NavigationMeshSourceGeometryData2D -> Resource -> RefCounted -> Object

## Brief Description

Container for parsed source geometry data used in navigation mesh baking.

## Description

Container for parsed source geometry data used in navigation mesh baking.

## Quick Reference

```
[methods]
add_obstruction_outline(shape_outline: PackedVector2Array) -> void
add_projected_obstruction(vertices: PackedVector2Array, carve: bool) -> void
add_traversable_outline(shape_outline: PackedVector2Array) -> void
append_obstruction_outlines(obstruction_outlines: PackedVector2Array[]) -> void
append_traversable_outlines(traversable_outlines: PackedVector2Array[]) -> void
clear() -> void
clear_projected_obstructions() -> void
get_bounds() -> Rect2
get_obstruction_outlines() -> PackedVector2Array[] [const]
get_projected_obstructions() -> Array [const]
get_traversable_outlines() -> PackedVector2Array[] [const]
has_data() -> bool
merge(other_geometry: NavigationMeshSourceGeometryData2D) -> void
set_obstruction_outlines(obstruction_outlines: PackedVector2Array[]) -> void
set_projected_obstructions(projected_obstructions: Array) -> void
set_traversable_outlines(traversable_outlines: PackedVector2Array[]) -> void
```

## Methods

- add_obstruction_outline(shape_outline: PackedVector2Array) -> void
  Adds the outline points of a shape as obstructed area.

- add_projected_obstruction(vertices: PackedVector2Array, carve: bool) -> void
  Adds a projected obstruction shape to the source geometry. If carve is true the carved shape will not be affected by additional offsets (e.g. agent radius) of the navigation mesh baking process.

- add_traversable_outline(shape_outline: PackedVector2Array) -> void
  Adds the outline points of a shape as traversable area.

- append_obstruction_outlines(obstruction_outlines: PackedVector2Array[]) -> void
  Appends another array of obstruction_outlines at the end of the existing obstruction outlines array.

- append_traversable_outlines(traversable_outlines: PackedVector2Array[]) -> void
  Appends another array of traversable_outlines at the end of the existing traversable outlines array.

- clear() -> void
  Clears the internal data.

- clear_projected_obstructions() -> void
  Clears all projected obstructions.

- get_bounds() -> Rect2
  Returns an axis-aligned bounding box that covers all the stored geometry data. The bounds are calculated when calling this function with the result cached until further geometry changes are made.

- get_obstruction_outlines() -> PackedVector2Array[] [const]
  Returns all the obstructed area outlines arrays.

- get_projected_obstructions() -> Array [const]
  Returns the projected obstructions as an Array of dictionaries. Each Dictionary contains the following entries: - vertices - A PackedFloat32Array that defines the outline points of the projected shape. - carve - A bool that defines how the projected shape affects the navigation mesh baking. If true the projected shape will not be affected by addition offsets, e.g. agent radius.

- get_traversable_outlines() -> PackedVector2Array[] [const]
  Returns all the traversable area outlines arrays.

- has_data() -> bool
  Returns true when parsed source geometry data exists.

- merge(other_geometry: NavigationMeshSourceGeometryData2D) -> void
  Adds the geometry data of another NavigationMeshSourceGeometryData2D to the navigation mesh baking data.

- set_obstruction_outlines(obstruction_outlines: PackedVector2Array[]) -> void
  Sets all the obstructed area outlines arrays.

- set_projected_obstructions(projected_obstructions: Array) -> void
  Sets the projected obstructions with an Array of Dictionaries with the following key value pairs:


```
  "vertices" : PackedFloat32Array
  "carve" : bool

```

- set_traversable_outlines(traversable_outlines: PackedVector2Array[]) -> void
  Sets all the traversable area outlines arrays.
