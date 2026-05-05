# LightOccluder2D

## Meta

- Name: LightOccluder2D
- Source: LightOccluder2D.xml
- Inherits: Node2D
- Inheritance Chain: LightOccluder2D -> Node2D -> CanvasItem -> Node -> Object

## Brief Description

Occludes light cast by a Light2D, casting shadows.

## Description

Occludes light cast by a Light2D, casting shadows. The LightOccluder2D must be provided with an OccluderPolygon2D in order for the shadow to be computed.

## Quick Reference

```
[properties]
occluder: OccluderPolygon2D
occluder_light_mask: int = 1
sdf_collision: bool = true
```

## Tutorials

- [2D lights and shadows]($DOCS_URL/tutorials/2d/2d_lights_and_shadows.html)

## Properties

- occluder: OccluderPolygon2D [set set_occluder_polygon; get get_occluder_polygon]
  The OccluderPolygon2D used to compute the shadow.

- occluder_light_mask: int = 1 [set set_occluder_light_mask; get get_occluder_light_mask]
  The LightOccluder2D's occluder light mask. The LightOccluder2D will cast shadows only from Light2D(s) that have the same light mask(s).

- sdf_collision: bool = true [set set_as_sdf_collision; get is_set_as_sdf_collision]
  If enabled, the occluder will be part of a real-time generated signed distance field that can be used in custom shaders.
