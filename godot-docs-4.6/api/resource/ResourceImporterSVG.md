# ResourceImporterSVG

## Meta

- Name: ResourceImporterSVG
- Source: ResourceImporterSVG.xml
- Inherits: ResourceImporter
- Inheritance Chain: ResourceImporterSVG -> ResourceImporter -> RefCounted -> Object

## Brief Description

Imports an SVG file as an automatically scalable texture for use in UI elements and 2D rendering.

## Description

This importer imports DPITexture resources. See also ResourceImporterTexture and ResourceImporterImage.

## Quick Reference

```
[properties]
base_scale: float = 1.0
color_map: Dictionary = {}
compress: bool = true
saturation: float = 1.0
```

## Properties

- base_scale: float = 1.0
  Texture scale. 1.0 is the original SVG size. Higher values result in a larger image.

- color_map: Dictionary = {}
  If set, remaps texture colors according to Color-Color map.

- compress: bool = true
  If true, uses lossless compression for the SVG source.

- saturation: float = 1.0
  Overrides texture saturation.
