# RDSamplerState

## Meta

- Name: RDSamplerState
- Source: RDSamplerState.xml
- Inherits: RefCounted
- Inheritance Chain: RDSamplerState -> RefCounted -> Object

## Brief Description

Sampler state (used by RenderingDevice).

## Description

This object is used by RenderingDevice.

## Quick Reference

```
[properties]
anisotropy_max: float = 1.0
border_color: int (RenderingDevice.SamplerBorderColor) = 2
compare_op: int (RenderingDevice.CompareOperator) = 7
enable_compare: bool = false
lod_bias: float = 0.0
mag_filter: int (RenderingDevice.SamplerFilter) = 0
max_lod: float = 1e+20
min_filter: int (RenderingDevice.SamplerFilter) = 0
min_lod: float = 0.0
mip_filter: int (RenderingDevice.SamplerFilter) = 0
repeat_u: int (RenderingDevice.SamplerRepeatMode) = 2
repeat_v: int (RenderingDevice.SamplerRepeatMode) = 2
repeat_w: int (RenderingDevice.SamplerRepeatMode) = 2
unnormalized_uvw: bool = false
use_anisotropy: bool = false
```

## Properties

- anisotropy_max: float = 1.0 [set set_anisotropy_max; get get_anisotropy_max]
  Maximum anisotropy that can be used when sampling. Only effective if use_anisotropy is true. Higher values result in a sharper sampler at oblique angles, at the cost of performance (due to memory bandwidth). This value may be limited by the graphics hardware in use. Most graphics hardware only supports values up to 16.0. If anisotropy_max is 1.0, forcibly disables anisotropy even if use_anisotropy is true.

- border_color: int (RenderingDevice.SamplerBorderColor) = 2 [set set_border_color; get get_border_color]
  The border color that will be returned when sampling outside the sampler's bounds and the repeat_u, repeat_v or repeat_w modes have repeating disabled.

- compare_op: int (RenderingDevice.CompareOperator) = 7 [set set_compare_op; get get_compare_op]
  The compare operation to use. Only effective if enable_compare is true.

- enable_compare: bool = false [set set_enable_compare; get get_enable_compare]
  If true, returned values will be based on the comparison operation defined in compare_op. This is a hardware-based approach and is therefore faster than performing this manually in a shader. For example, compare operations are used for shadow map rendering by comparing depth values from a shadow sampler.

- lod_bias: float = 0.0 [set set_lod_bias; get get_lod_bias]
  The mipmap LOD bias to use. Positive values will make the sampler blurrier at a given distance, while negative values will make the sampler sharper at a given distance (at the risk of looking grainy). Recommended values are between -0.5 and 0.0. Only effective if the sampler has mipmaps available.

- mag_filter: int (RenderingDevice.SamplerFilter) = 0 [set set_mag_filter; get get_mag_filter]
  The sampler's magnification filter. It is the filtering method used when sampling texels that appear bigger than on-screen pixels.

- max_lod: float = 1e+20 [set set_max_lod; get get_max_lod]
  The maximum mipmap LOD bias to display (lowest resolution). Only effective if the sampler has mipmaps available.

- min_filter: int (RenderingDevice.SamplerFilter) = 0 [set set_min_filter; get get_min_filter]
  The sampler's minification filter. It is the filtering method used when sampling texels that appear smaller than on-screen pixels.

- min_lod: float = 0.0 [set set_min_lod; get get_min_lod]
  The minimum mipmap LOD bias to display (highest resolution). Only effective if the sampler has mipmaps available.

- mip_filter: int (RenderingDevice.SamplerFilter) = 0 [set set_mip_filter; get get_mip_filter]
  The filtering method to use for mipmaps.

- repeat_u: int (RenderingDevice.SamplerRepeatMode) = 2 [set set_repeat_u; get get_repeat_u]
  The repeat mode to use along the U axis of UV coordinates. This affects the returned values if sampling outside the UV bounds.

- repeat_v: int (RenderingDevice.SamplerRepeatMode) = 2 [set set_repeat_v; get get_repeat_v]
  The repeat mode to use along the V axis of UV coordinates. This affects the returned values if sampling outside the UV bounds.

- repeat_w: int (RenderingDevice.SamplerRepeatMode) = 2 [set set_repeat_w; get get_repeat_w]
  The repeat mode to use along the W axis of UV coordinates. This affects the returned values if sampling outside the UV bounds. Only effective for 3D samplers.

- unnormalized_uvw: bool = false [set set_unnormalized_uvw; get get_unnormalized_uvw]
  If true, the texture will be sampled with coordinates ranging from 0 to the texture's resolution. Otherwise, the coordinates will be normalized and range from 0 to 1.

- use_anisotropy: bool = false [set set_use_anisotropy; get get_use_anisotropy]
  If true, perform anisotropic sampling. See anisotropy_max.
