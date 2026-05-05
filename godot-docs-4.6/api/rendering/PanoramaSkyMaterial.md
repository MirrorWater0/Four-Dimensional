# PanoramaSkyMaterial

## Meta

- Name: PanoramaSkyMaterial
- Source: PanoramaSkyMaterial.xml
- Inherits: Material
- Inheritance Chain: PanoramaSkyMaterial -> Material -> Resource -> RefCounted -> Object

## Brief Description

A material that provides a special texture to a Sky, usually an HDR panorama.

## Description

A resource referenced in a Sky that is used to draw a background. PanoramaSkyMaterial functions similar to skyboxes in other engines, except it uses an equirectangular sky map instead of a Cubemap. Using an HDR panorama is strongly recommended for accurate, high-quality reflections. Godot supports the Radiance HDR (.hdr) and OpenEXR (.exr) image formats for this purpose. You can use [this tool](https://danilw.github.io/GLSL-howto/cubemap_to_panorama_js/cubemap_to_panorama.html) to convert a cubemap to an equirectangular sky map.

## Quick Reference

```
[properties]
energy_multiplier: float = 1.0
filter: bool = true
panorama: Texture2D
```

## Properties

- energy_multiplier: float = 1.0 [set set_energy_multiplier; get get_energy_multiplier]
  The sky's overall brightness multiplier. Higher values result in a brighter sky.

- filter: bool = true [set set_filtering_enabled; get is_filtering_enabled]
  A boolean value to determine if the background texture should be filtered or not.

- panorama: Texture2D [set set_panorama; get get_panorama]
  Texture2D to be applied to the PanoramaSkyMaterial.
