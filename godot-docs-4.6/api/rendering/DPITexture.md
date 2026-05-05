# DPITexture

## Meta

- Name: DPITexture
- Source: DPITexture.xml
- Inherits: Texture2D
- Inheritance Chain: DPITexture -> Texture2D -> Texture -> Resource -> RefCounted -> Object

## Brief Description

An automatically scalable Texture2D based on an SVG image.

## Description

An automatically scalable Texture2D based on an SVG image. DPITextures are used to automatically re-rasterize icons and other texture based UI theme elements to match viewport scale and font oversampling. See also ProjectSettings.display/window/stretch/mode ("canvas_items" mode) and Viewport.oversampling_override.

## Quick Reference

```
[methods]
create_from_string(source: String, scale: float = 1.0, saturation: float = 1.0, color_map: Dictionary = {}) -> DPITexture [static]
get_scaled_rid() -> RID [const]
get_source() -> String [const]
set_size_override(size: Vector2i) -> void
set_source(source: String) -> void

[properties]
base_scale: float = 1.0
color_map: Dictionary = {}
resource_local_to_scene: bool = false
saturation: float = 1.0
```

## Methods

- create_from_string(source: String, scale: float = 1.0, saturation: float = 1.0, color_map: Dictionary = {}) -> DPITexture [static]
  Creates a new DPITexture and initializes it by allocating and setting the SVG data to source.

- get_scaled_rid() -> RID [const]
  Returns the RID of the texture rasterized to match the oversampling of the currently drawn canvas item.

- get_source() -> String [const]
  Returns this SVG texture's source code.

- set_size_override(size: Vector2i) -> void
  Resizes the texture to the specified dimensions.

- set_source(source: String) -> void
  Sets this SVG texture's source code.

## Properties

- base_scale: float = 1.0 [set set_base_scale; get get_base_scale]
  Texture scale. 1.0 is the original SVG size. Higher values result in a larger image.

- color_map: Dictionary = {} [set set_color_map; get get_color_map]
  If set, remaps texture colors according to Color-Color map.

- resource_local_to_scene: bool = false [set set_local_to_scene; get is_local_to_scene; override Resource]

- saturation: float = 1.0 [set set_saturation; get get_saturation]
  Overrides texture saturation.
