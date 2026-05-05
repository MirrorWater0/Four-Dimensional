# RDTextureFormat

## Meta

- Name: RDTextureFormat
- Source: RDTextureFormat.xml
- Inherits: RefCounted
- Inheritance Chain: RDTextureFormat -> RefCounted -> Object

## Brief Description

Texture format (used by RenderingDevice).

## Description

This object is used by RenderingDevice.

## Quick Reference

```
[methods]
add_shareable_format(format: int (RenderingDevice.DataFormat)) -> void
remove_shareable_format(format: int (RenderingDevice.DataFormat)) -> void

[properties]
array_layers: int = 1
depth: int = 1
format: int (RenderingDevice.DataFormat) = 8
height: int = 1
is_discardable: bool = false
is_resolve_buffer: bool = false
mipmaps: int = 1
samples: int (RenderingDevice.TextureSamples) = 0
texture_type: int (RenderingDevice.TextureType) = 1
usage_bits: int (RenderingDevice.TextureUsageBits) = 0
width: int = 1
```

## Methods

- add_shareable_format(format: int (RenderingDevice.DataFormat)) -> void
  Adds format as a valid format for the corresponding RDTextureView's RDTextureView.format_override property. If any format is added as shareable, then the main format must also be added.

- remove_shareable_format(format: int (RenderingDevice.DataFormat)) -> void
  Removes format from the list of valid formats that the corresponding RDTextureView's RDTextureView.format_override property can be set to.

## Properties

- array_layers: int = 1 [set set_array_layers; get get_array_layers]
  The number of layers in the texture. Only relevant for 2D texture arrays.

- depth: int = 1 [set set_depth; get get_depth]
  The texture's depth (in pixels). This is always 1 for 2D textures.

- format: int (RenderingDevice.DataFormat) = 8 [set set_format; get get_format]
  The texture's pixel data format.

- height: int = 1 [set set_height; get get_height]
  The texture's height (in pixels).

- is_discardable: bool = false [set set_is_discardable; get get_is_discardable]
  If a texture is discardable, its contents do not need to be preserved between frames. This flag is only relevant when the texture is used as target in a draw list. This information is used by RenderingDevice to figure out if a texture's contents can be discarded, eliminating unnecessary writes to memory and boosting performance.

- is_resolve_buffer: bool = false [set set_is_resolve_buffer; get get_is_resolve_buffer]
  The texture will be used as the destination of a resolve operation.

- mipmaps: int = 1 [set set_mipmaps; get get_mipmaps]
  The number of mipmaps available in the texture.

- samples: int (RenderingDevice.TextureSamples) = 0 [set set_samples; get get_samples]
  The number of samples used when sampling the texture.

- texture_type: int (RenderingDevice.TextureType) = 1 [set set_texture_type; get get_texture_type]
  The texture type.

- usage_bits: int (RenderingDevice.TextureUsageBits) = 0 [set set_usage_bits; get get_usage_bits]
  The texture's usage bits, which determine what can be done using the texture.

- width: int = 1 [set set_width; get get_width]
  The texture's width (in pixels).
