# RenderSceneBuffersExtension

## Meta

- Name: RenderSceneBuffersExtension
- Source: RenderSceneBuffersExtension.xml
- Inherits: RenderSceneBuffers
- Inheritance Chain: RenderSceneBuffersExtension -> RenderSceneBuffers -> RefCounted -> Object

## Brief Description

This class allows for a RenderSceneBuffer implementation to be made in GDExtension.

## Description

This class allows for a RenderSceneBuffer implementation to be made in GDExtension.

## Quick Reference

```
[methods]
_configure(config: RenderSceneBuffersConfiguration) -> void [virtual]
_set_anisotropic_filtering_level(anisotropic_filtering_level: int) -> void [virtual]
_set_fsr_sharpness(fsr_sharpness: float) -> void [virtual]
_set_texture_mipmap_bias(texture_mipmap_bias: float) -> void [virtual]
_set_use_debanding(use_debanding: bool) -> void [virtual]
```

## Methods

- _configure(config: RenderSceneBuffersConfiguration) -> void [virtual]
  Implement this in GDExtension to handle the (re)sizing of a viewport.

- _set_anisotropic_filtering_level(anisotropic_filtering_level: int) -> void [virtual]
  Implement this in GDExtension to change the anisotropic filtering level.

- _set_fsr_sharpness(fsr_sharpness: float) -> void [virtual]
  Implement this in GDExtension to record a new FSR sharpness value.

- _set_texture_mipmap_bias(texture_mipmap_bias: float) -> void [virtual]
  Implement this in GDExtension to change the texture mipmap bias.

- _set_use_debanding(use_debanding: bool) -> void [virtual]
  Implement this in GDExtension to react to the debanding flag changing.
