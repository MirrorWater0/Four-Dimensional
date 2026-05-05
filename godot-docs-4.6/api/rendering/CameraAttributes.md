# CameraAttributes

## Meta

- Name: CameraAttributes
- Source: CameraAttributes.xml
- Inherits: Resource
- Inheritance Chain: CameraAttributes -> Resource -> RefCounted -> Object

## Brief Description

Parent class for camera settings.

## Description

Controls camera-specific attributes such as depth of field and exposure override. When used in a WorldEnvironment it provides default settings for exposure, auto-exposure, and depth of field that will be used by all cameras without their own CameraAttributes, including the editor camera. When used in a Camera3D it will override any CameraAttributes set in the WorldEnvironment. When used in VoxelGI or LightmapGI, only the exposure settings will be used. See also Environment for general 3D environment settings. This is a pure virtual class that is inherited by CameraAttributesPhysical and CameraAttributesPractical.

## Quick Reference

```
[properties]
auto_exposure_enabled: bool = false
auto_exposure_scale: float = 0.4
auto_exposure_speed: float = 0.5
exposure_multiplier: float = 1.0
exposure_sensitivity: float = 100.0
```

## Properties

- auto_exposure_enabled: bool = false [set set_auto_exposure_enabled; get is_auto_exposure_enabled]
  If true, enables the tonemapping auto exposure mode of the scene renderer. If true, the renderer will automatically determine the exposure setting to adapt to the scene's illumination and the observed light.

- auto_exposure_scale: float = 0.4 [set set_auto_exposure_scale; get get_auto_exposure_scale]
  The scale of the auto exposure effect. Affects the intensity of auto exposure.

- auto_exposure_speed: float = 0.5 [set set_auto_exposure_speed; get get_auto_exposure_speed]
  The speed of the auto exposure effect. Affects the time needed for the camera to perform auto exposure.

- exposure_multiplier: float = 1.0 [set set_exposure_multiplier; get get_exposure_multiplier]
  Multiplier for the exposure amount. A higher value results in a brighter image.

- exposure_sensitivity: float = 100.0 [set set_exposure_sensitivity; get get_exposure_sensitivity]
  Sensitivity of camera sensors, measured in ISO. A higher sensitivity results in a brighter image. If auto_exposure_enabled is true, this can be used as a method of exposure compensation, doubling the value will increase the exposure value (measured in EV100) by 1 stop. **Note:** Only available when ProjectSettings.rendering/lights_and_shadows/use_physical_light_units is enabled.
