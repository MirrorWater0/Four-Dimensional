# RibbonTrailMesh

## Meta

- Name: RibbonTrailMesh
- Source: RibbonTrailMesh.xml
- Inherits: PrimitiveMesh
- Inheritance Chain: RibbonTrailMesh -> PrimitiveMesh -> Mesh -> Resource -> RefCounted -> Object

## Brief Description

Represents a straight ribbon-shaped PrimitiveMesh with variable width.

## Description

RibbonTrailMesh represents a straight ribbon-shaped mesh with variable width. The ribbon is composed of a number of flat or cross-shaped sections, each with the same section_length and number of section_segments. A curve is sampled along the total length of the ribbon, meaning that the curve determines the size of the ribbon along its length. This primitive mesh is usually used for particle trails.

## Quick Reference

```
[properties]
curve: Curve
section_length: float = 0.2
section_segments: int = 3
sections: int = 5
shape: int (RibbonTrailMesh.Shape) = 1
size: float = 1.0
```

## Tutorials

- [3D Particle trails]($DOCS_URL/tutorials/3d/particles/trails.html)
- [Particle systems (3D)]($DOCS_URL/tutorials/3d/particles/index.html)

## Properties

- curve: Curve [set set_curve; get get_curve]
  Determines the size of the ribbon along its length. The size of a particular section segment is obtained by multiplying the baseline size by the value of this curve at the given distance. For values smaller than 0, the faces will be inverted. Should be a unit Curve.

- section_length: float = 0.2 [set set_section_length; get get_section_length]
  The length of a section of the ribbon.

- section_segments: int = 3 [set set_section_segments; get get_section_segments]
  The number of segments in a section. The curve is sampled on each segment to determine its size. Higher values result in a more detailed ribbon at the cost of performance.

- sections: int = 5 [set set_sections; get get_sections]
  The total number of sections on the ribbon.

- shape: int (RibbonTrailMesh.Shape) = 1 [set set_shape; get get_shape]
  Determines the shape of the ribbon.

- size: float = 1.0 [set set_size; get get_size]
  The baseline size of the ribbon. The size of a particular section segment is obtained by multiplying this size by the value of the curve at the given distance.

## Constants

### Enum Shape

- SHAPE_FLAT = 0
  Gives the mesh a single flat face.

- SHAPE_CROSS = 1
  Gives the mesh two perpendicular flat faces, making a cross shape.
