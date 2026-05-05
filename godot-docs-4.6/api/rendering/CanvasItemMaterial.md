# CanvasItemMaterial

## Meta

- Name: CanvasItemMaterial
- Source: CanvasItemMaterial.xml
- Inherits: Material
- Inheritance Chain: CanvasItemMaterial -> Material -> Resource -> RefCounted -> Object

## Brief Description

A material for CanvasItems.

## Description

CanvasItemMaterials provide a means of modifying the textures associated with a CanvasItem. They specialize in describing blend and lighting behaviors for textures. Use a ShaderMaterial to more fully customize a material's interactions with a CanvasItem.

## Quick Reference

```
[properties]
blend_mode: int (CanvasItemMaterial.BlendMode) = 0
light_mode: int (CanvasItemMaterial.LightMode) = 0
particles_anim_h_frames: int
particles_anim_loop: bool
particles_anim_v_frames: int
particles_animation: bool = false
```

## Properties

- blend_mode: int (CanvasItemMaterial.BlendMode) = 0 [set set_blend_mode; get get_blend_mode]
  The manner in which a material's rendering is applied to underlying textures.

- light_mode: int (CanvasItemMaterial.LightMode) = 0 [set set_light_mode; get get_light_mode]
  The manner in which material reacts to lighting.

- particles_anim_h_frames: int [set set_particles_anim_h_frames; get get_particles_anim_h_frames]
  The number of columns in the spritesheet assigned as Texture2D for a GPUParticles2D or CPUParticles2D. **Note:** This property is only used and visible in the editor if particles_animation is true.

- particles_anim_loop: bool [set set_particles_anim_loop; get get_particles_anim_loop]
  If true, the particles animation will loop. **Note:** This property is only used and visible in the editor if particles_animation is true.

- particles_anim_v_frames: int [set set_particles_anim_v_frames; get get_particles_anim_v_frames]
  The number of rows in the spritesheet assigned as Texture2D for a GPUParticles2D or CPUParticles2D. **Note:** This property is only used and visible in the editor if particles_animation is true.

- particles_animation: bool = false [set set_particles_animation; get get_particles_animation]
  If true, enable spritesheet-based animation features when assigned to GPUParticles2D and CPUParticles2D nodes. The ParticleProcessMaterial.anim_speed_max or CPUParticles2D.anim_speed_max should also be set to a positive value for the animation to play. This property (and other particles_anim_* properties that depend on it) has no effect on other types of nodes.

## Constants

### Enum BlendMode

- BLEND_MODE_MIX = 0
  Mix blending mode. Colors are assumed to be independent of the alpha (opacity) value.

- BLEND_MODE_ADD = 1
  Additive blending mode.

- BLEND_MODE_SUB = 2
  Subtractive blending mode.

- BLEND_MODE_MUL = 3
  Multiplicative blending mode.

- BLEND_MODE_PREMULT_ALPHA = 4
  Mix blending mode. Colors are assumed to be premultiplied by the alpha (opacity) value.

### Enum LightMode

- LIGHT_MODE_NORMAL = 0
  Render the material using both light and non-light sensitive material properties.

- LIGHT_MODE_UNSHADED = 1
  Render the material as if there were no light.

- LIGHT_MODE_LIGHT_ONLY = 2
  Render the material as if there were only light.
