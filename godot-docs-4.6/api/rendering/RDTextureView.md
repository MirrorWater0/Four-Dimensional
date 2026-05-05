# RDTextureView

## Meta

- Name: RDTextureView
- Source: RDTextureView.xml
- Inherits: RefCounted
- Inheritance Chain: RDTextureView -> RefCounted -> Object

## Brief Description

Texture view (used by RenderingDevice).

## Description

This object is used by RenderingDevice.

## Quick Reference

```
[properties]
format_override: int (RenderingDevice.DataFormat) = 232
swizzle_a: int (RenderingDevice.TextureSwizzle) = 6
swizzle_b: int (RenderingDevice.TextureSwizzle) = 5
swizzle_g: int (RenderingDevice.TextureSwizzle) = 4
swizzle_r: int (RenderingDevice.TextureSwizzle) = 3
```

## Properties

- format_override: int (RenderingDevice.DataFormat) = 232 [set set_format_override; get get_format_override]
  Optional override for the data format to return sampled values in. The corresponding RDTextureFormat must have had this added as a shareable format. The default value of RenderingDevice.DATA_FORMAT_MAX does not override the format.

- swizzle_a: int (RenderingDevice.TextureSwizzle) = 6 [set set_swizzle_a; get get_swizzle_a]
  The channel to sample when sampling the alpha channel.

- swizzle_b: int (RenderingDevice.TextureSwizzle) = 5 [set set_swizzle_b; get get_swizzle_b]
  The channel to sample when sampling the blue color channel.

- swizzle_g: int (RenderingDevice.TextureSwizzle) = 4 [set set_swizzle_g; get get_swizzle_g]
  The channel to sample when sampling the green color channel.

- swizzle_r: int (RenderingDevice.TextureSwizzle) = 3 [set set_swizzle_r; get get_swizzle_r]
  The channel to sample when sampling the red color channel.
