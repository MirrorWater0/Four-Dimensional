# RenderSceneBuffersConfiguration

## Meta

- Name: RenderSceneBuffersConfiguration
- Source: RenderSceneBuffersConfiguration.xml
- Inherits: RefCounted
- Inheritance Chain: RenderSceneBuffersConfiguration -> RefCounted -> Object

## Brief Description

Configuration object used to setup a RenderSceneBuffers object.

## Description

This configuration object is created and populated by the render engine on a viewport change and used to (re)configure a RenderSceneBuffers object.

## Quick Reference

```
[properties]
anisotropic_filtering_level: int (RenderingServer.ViewportAnisotropicFiltering) = 2
fsr_sharpness: float = 0.0
internal_size: Vector2i = Vector2i(0, 0)
msaa_3d: int (RenderingServer.ViewportMSAA) = 0
render_target: RID = RID()
scaling_3d_mode: int (RenderingServer.ViewportScaling3DMode) = 255
screen_space_aa: int (RenderingServer.ViewportScreenSpaceAA) = 0
target_size: Vector2i = Vector2i(0, 0)
texture_mipmap_bias: float = 0.0
view_count: int = 1
```

## Properties

- anisotropic_filtering_level: int (RenderingServer.ViewportAnisotropicFiltering) = 2 [set set_anisotropic_filtering_level; get get_anisotropic_filtering_level]
  Level of the anisotropic filter.

- fsr_sharpness: float = 0.0 [set set_fsr_sharpness; get get_fsr_sharpness]
  FSR Sharpness applicable if FSR upscaling is used.

- internal_size: Vector2i = Vector2i(0, 0) [set set_internal_size; get get_internal_size]
  The size of the 3D render buffer used for rendering.

- msaa_3d: int (RenderingServer.ViewportMSAA) = 0 [set set_msaa_3d; get get_msaa_3d]
  The MSAA mode we're using for 3D rendering.

- render_target: RID = RID() [set set_render_target; get get_render_target]
  The render target associated with these buffer.

- scaling_3d_mode: int (RenderingServer.ViewportScaling3DMode) = 255 [set set_scaling_3d_mode; get get_scaling_3d_mode]
  The requested scaling mode with which we upscale/downscale if internal_size and target_size are not equal.

- screen_space_aa: int (RenderingServer.ViewportScreenSpaceAA) = 0 [set set_screen_space_aa; get get_screen_space_aa]
  The requested screen space AA applied in post processing.

- target_size: Vector2i = Vector2i(0, 0) [set set_target_size; get get_target_size]
  The target (upscale) size if scaling is used.

- texture_mipmap_bias: float = 0.0 [set set_texture_mipmap_bias; get get_texture_mipmap_bias]
  Bias applied to mipmaps.

- view_count: int = 1 [set set_view_count; get get_view_count]
  The number of views we're rendering.
