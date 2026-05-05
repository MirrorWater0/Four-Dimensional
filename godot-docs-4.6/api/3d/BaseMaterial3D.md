# BaseMaterial3D

## Meta

- Name: BaseMaterial3D
- Source: BaseMaterial3D.xml
- Inherits: Material
- Inheritance Chain: BaseMaterial3D -> Material -> Resource -> RefCounted -> Object

## Brief Description

Abstract base class for defining the 3D rendering properties of meshes.

## Description

This class serves as a default material with a wide variety of rendering features and properties without the need to write shader code. See the tutorial below for details.

## Quick Reference

```
[methods]
get_feature(feature: int (BaseMaterial3D.Feature)) -> bool [const]
get_flag(flag: int (BaseMaterial3D.Flags)) -> bool [const]
get_texture(param: int (BaseMaterial3D.TextureParam)) -> Texture2D [const]
set_feature(feature: int (BaseMaterial3D.Feature), enable: bool) -> void
set_flag(flag: int (BaseMaterial3D.Flags), enable: bool) -> void
set_texture(param: int (BaseMaterial3D.TextureParam), texture: Texture2D) -> void

[properties]
albedo_color: Color = Color(1, 1, 1, 1)
albedo_texture: Texture2D
albedo_texture_force_srgb: bool = false
albedo_texture_msdf: bool = false
alpha_antialiasing_edge: float
alpha_antialiasing_mode: int (BaseMaterial3D.AlphaAntiAliasing)
alpha_hash_scale: float
alpha_scissor_threshold: float
anisotropy: float = 0.0
anisotropy_enabled: bool = false
anisotropy_flowmap: Texture2D
ao_enabled: bool = false
ao_light_affect: float = 0.0
ao_on_uv2: bool = false
ao_texture: Texture2D
ao_texture_channel: int (BaseMaterial3D.TextureChannel) = 0
backlight: Color = Color(0, 0, 0, 1)
backlight_enabled: bool = false
backlight_texture: Texture2D
bent_normal_enabled: bool = false
bent_normal_texture: Texture2D
billboard_keep_scale: bool = false
billboard_mode: int (BaseMaterial3D.BillboardMode) = 0
blend_mode: int (BaseMaterial3D.BlendMode) = 0
clearcoat: float = 1.0
clearcoat_enabled: bool = false
clearcoat_roughness: float = 0.5
clearcoat_texture: Texture2D
cull_mode: int (BaseMaterial3D.CullMode) = 0
depth_draw_mode: int (BaseMaterial3D.DepthDrawMode) = 0
depth_test: int (BaseMaterial3D.DepthTest) = 0
detail_albedo: Texture2D
detail_blend_mode: int (BaseMaterial3D.BlendMode) = 0
detail_enabled: bool = false
detail_mask: Texture2D
detail_normal: Texture2D
detail_uv_layer: int (BaseMaterial3D.DetailUV) = 0
diffuse_mode: int (BaseMaterial3D.DiffuseMode) = 0
disable_ambient_light: bool = false
disable_fog: bool = false
disable_receive_shadows: bool = false
disable_specular_occlusion: bool = false
distance_fade_max_distance: float = 10.0
distance_fade_min_distance: float = 0.0
distance_fade_mode: int (BaseMaterial3D.DistanceFadeMode) = 0
emission: Color = Color(0, 0, 0, 1)
emission_enabled: bool = false
emission_energy_multiplier: float = 1.0
emission_intensity: float
emission_on_uv2: bool = false
emission_operator: int (BaseMaterial3D.EmissionOperator) = 0
emission_texture: Texture2D
fixed_size: bool = false
fov_override: float = 75.0
grow: bool = false
grow_amount: float = 0.0
heightmap_deep_parallax: bool = false
heightmap_enabled: bool = false
heightmap_flip_binormal: bool = false
heightmap_flip_tangent: bool = false
heightmap_flip_texture: bool = false
heightmap_max_layers: int
heightmap_min_layers: int
heightmap_scale: float = 5.0
heightmap_texture: Texture2D
metallic: float = 0.0
metallic_specular: float = 0.5
metallic_texture: Texture2D
metallic_texture_channel: int (BaseMaterial3D.TextureChannel) = 0
msdf_outline_size: float = 0.0
msdf_pixel_range: float = 4.0
no_depth_test: bool = false
normal_enabled: bool = false
normal_scale: float = 1.0
normal_texture: Texture2D
orm_texture: Texture2D
particles_anim_h_frames: int
particles_anim_loop: bool
particles_anim_v_frames: int
point_size: float = 1.0
proximity_fade_distance: float = 1.0
proximity_fade_enabled: bool = false
refraction_enabled: bool = false
refraction_scale: float = 0.05
refraction_texture: Texture2D
refraction_texture_channel: int (BaseMaterial3D.TextureChannel) = 0
rim: float = 1.0
rim_enabled: bool = false
rim_texture: Texture2D
rim_tint: float = 0.5
roughness: float = 1.0
roughness_texture: Texture2D
roughness_texture_channel: int (BaseMaterial3D.TextureChannel) = 0
shading_mode: int (BaseMaterial3D.ShadingMode) = 1
shadow_to_opacity: bool = false
specular_mode: int (BaseMaterial3D.SpecularMode) = 0
stencil_color: Color = Color(0, 0, 0, 1)
stencil_compare: int (BaseMaterial3D.StencilCompare) = 0
stencil_flags: int = 0
stencil_mode: int (BaseMaterial3D.StencilMode) = 0
stencil_outline_thickness: float = 0.01
stencil_reference: int = 1
subsurf_scatter_enabled: bool = false
subsurf_scatter_skin_mode: bool = false
subsurf_scatter_strength: float = 0.0
subsurf_scatter_texture: Texture2D
subsurf_scatter_transmittance_boost: float = 0.0
subsurf_scatter_transmittance_color: Color = Color(1, 1, 1, 1)
subsurf_scatter_transmittance_depth: float = 0.1
subsurf_scatter_transmittance_enabled: bool = false
subsurf_scatter_transmittance_texture: Texture2D
texture_filter: int (BaseMaterial3D.TextureFilter) = 3
texture_repeat: bool = true
transparency: int (BaseMaterial3D.Transparency) = 0
use_fov_override: bool = false
use_particle_trails: bool = false
use_point_size: bool = false
use_z_clip_scale: bool = false
uv1_offset: Vector3 = Vector3(0, 0, 0)
uv1_scale: Vector3 = Vector3(1, 1, 1)
uv1_triplanar: bool = false
uv1_triplanar_sharpness: float = 1.0
uv1_world_triplanar: bool = false
uv2_offset: Vector3 = Vector3(0, 0, 0)
uv2_scale: Vector3 = Vector3(1, 1, 1)
uv2_triplanar: bool = false
uv2_triplanar_sharpness: float = 1.0
uv2_world_triplanar: bool = false
vertex_color_is_srgb: bool = false
vertex_color_use_as_albedo: bool = false
z_clip_scale: float = 1.0
```

## Tutorials

- [Standard Material 3D and ORM Material 3D]($DOCS_URL/tutorials/3d/standard_material_3d.html)

## Methods

- get_feature(feature: int (BaseMaterial3D.Feature)) -> bool [const]
  Returns true if the specified feature is enabled.

- get_flag(flag: int (BaseMaterial3D.Flags)) -> bool [const]
  Returns true if the specified flag is enabled.

- get_texture(param: int (BaseMaterial3D.TextureParam)) -> Texture2D [const]
  Returns the Texture2D associated with the specified texture param.

- set_feature(feature: int (BaseMaterial3D.Feature), enable: bool) -> void
  If enable is true, enables the specified feature. Many features that are available in BaseMaterial3D need to be enabled before use. This way, the cost for using the feature is only incurred when specified. Features can also be enabled by setting their corresponding property to true.

- set_flag(flag: int (BaseMaterial3D.Flags), enable: bool) -> void
  If enable is true, enables the specified flag. Flags are optional behavior that can be turned on and off. Only one flag can be enabled at a time with this function, the flag enumerators cannot be bit-masked together to enable or disable multiple flags at once. Flags can also be enabled by setting their corresponding property to true.

- set_texture(param: int (BaseMaterial3D.TextureParam), texture: Texture2D) -> void
  Sets the texture for the slot specified by param.

## Properties

- albedo_color: Color = Color(1, 1, 1, 1) [set set_albedo; get get_albedo]
  The material's base color. **Note:** If detail_enabled is true and a detail_albedo texture is specified, albedo_color will *not* modulate the detail texture. This can be used to color partial areas of a material by not specifying an albedo texture and using a transparent detail_albedo texture instead.

- albedo_texture: Texture2D [set set_texture; get get_texture]
  Texture to multiply by albedo_color. Used for basic texturing of objects. If the texture appears unexpectedly too dark or too bright, check albedo_texture_force_srgb.

- albedo_texture_force_srgb: bool = false [set set_flag; get get_flag]
  If true, forces a conversion of the albedo_texture from nonlinear sRGB encoding to linear encoding. See also vertex_color_is_srgb. This should only be enabled when needed (typically when using a ViewportTexture as albedo_texture). If albedo_texture_force_srgb is true when it shouldn't be, the texture will appear to be too dark. If albedo_texture_force_srgb is false when it shouldn't be, the texture will appear to be too bright.

- albedo_texture_msdf: bool = false [set set_flag; get get_flag]
  Enables multichannel signed distance field rendering shader. Use msdf_pixel_range and msdf_outline_size to configure MSDF parameters.

- alpha_antialiasing_edge: float [set set_alpha_antialiasing_edge; get get_alpha_antialiasing_edge]
  Threshold at which antialiasing will be applied on the alpha channel.

- alpha_antialiasing_mode: int (BaseMaterial3D.AlphaAntiAliasing) [set set_alpha_antialiasing; get get_alpha_antialiasing]
  The type of alpha antialiasing to apply.

- alpha_hash_scale: float [set set_alpha_hash_scale; get get_alpha_hash_scale]
  The hashing scale for Alpha Hash. Recommended values between 0 and 2.

- alpha_scissor_threshold: float [set set_alpha_scissor_threshold; get get_alpha_scissor_threshold]
  Threshold at which the alpha scissor will discard values. Higher values will result in more pixels being discarded. If the material becomes too opaque at a distance, try increasing alpha_scissor_threshold. If the material disappears at a distance, try decreasing alpha_scissor_threshold.

- anisotropy: float = 0.0 [set set_anisotropy; get get_anisotropy]
  The strength of the anisotropy effect. This is multiplied by anisotropy_flowmap's alpha channel if a texture is defined there and the texture contains an alpha channel.

- anisotropy_enabled: bool = false [set set_feature; get get_feature]
  If true, anisotropy is enabled. Anisotropy changes the shape of the specular blob and aligns it to tangent space. This is useful for brushed aluminum and hair reflections. **Note:** Mesh tangents are needed for anisotropy to work. If the mesh does not contain tangents, the anisotropy effect will appear broken. **Note:** Material anisotropy should not to be confused with anisotropic texture filtering, which can be enabled by setting texture_filter to TEXTURE_FILTER_LINEAR_WITH_MIPMAPS_ANISOTROPIC.

- anisotropy_flowmap: Texture2D [set set_texture; get get_texture]
  Texture that offsets the tangent map for anisotropy calculations and optionally controls the anisotropy effect (if an alpha channel is present). The flowmap texture is expected to be a derivative map, with the red channel representing distortion on the X axis and green channel representing distortion on the Y axis. Values below 0.5 will result in negative distortion, whereas values above 0.5 will result in positive distortion. If present, the texture's alpha channel will be used to multiply the strength of the anisotropy effect. Fully opaque pixels will keep the anisotropy effect's original strength while fully transparent pixels will disable the anisotropy effect entirely. The flowmap texture's blue channel is ignored.

- ao_enabled: bool = false [set set_feature; get get_feature]
  If true, ambient occlusion is enabled. Ambient occlusion darkens areas based on the ao_texture.

- ao_light_affect: float = 0.0 [set set_ao_light_affect; get get_ao_light_affect]
  Amount that ambient occlusion affects lighting from lights. If 0, ambient occlusion only affects ambient light. If 1, ambient occlusion affects lights just as much as it affects ambient light. This can be used to impact the strength of the ambient occlusion effect, but typically looks unrealistic.

- ao_on_uv2: bool = false [set set_flag; get get_flag]
  If true, use UV2 coordinates to look up from the ao_texture.

- ao_texture: Texture2D [set set_texture; get get_texture]
  Texture that defines the amount of ambient occlusion for a given point on the object.

- ao_texture_channel: int (BaseMaterial3D.TextureChannel) = 0 [set set_ao_texture_channel; get get_ao_texture_channel]
  Specifies the channel of the ao_texture in which the ambient occlusion information is stored. This is useful when you store the information for multiple effects in a single texture. For example if you stored metallic in the red channel, roughness in the blue, and ambient occlusion in the green you could reduce the number of textures you use.

- backlight: Color = Color(0, 0, 0, 1) [set set_backlight; get get_backlight]
  The color used by the backlight effect. Represents the light passing through an object.

- backlight_enabled: bool = false [set set_feature; get get_feature]
  If true, the backlight effect is enabled. See also subsurf_scatter_transmittance_enabled.

- backlight_texture: Texture2D [set set_texture; get get_texture]
  Texture used to control the backlight effect per-pixel. Added to backlight.

- bent_normal_enabled: bool = false [set set_feature; get get_feature]
  If true, the bent normal map is enabled. This allows for more accurate indirect lighting and specular occlusion.

- bent_normal_texture: Texture2D [set set_texture; get get_texture]
  Texture that specifies the average direction of incoming ambient light at a given pixel. The bent_normal_texture only uses the red and green channels; the blue and alpha channels are ignored. The normal read from bent_normal_texture is oriented around the surface normal provided by the Mesh. **Note:** A bent normal map is different from a regular normal map. When baking a bent normal map make sure to use **a cosine distribution** for the bent normal map to work correctly. **Note:** The mesh must have both normals and tangents defined in its vertex data. Otherwise, the shading produced by the bent normal map will not look correct. If creating geometry with SurfaceTool, you can use SurfaceTool.generate_normals() and SurfaceTool.generate_tangents() to automatically generate normals and tangents respectively. **Note:** Godot expects the bent normal map to use X+, Y+, and Z+ coordinates. See [this page](http://wiki.polycount.com/wiki/Normal_Map_Technical_Details#Common_Swizzle_Coordinates) for a comparison of normal map coordinates expected by popular engines.

- billboard_keep_scale: bool = false [set set_flag; get get_flag]
  If true, the shader will keep the scale set for the mesh. Otherwise, the scale is lost when billboarding. Only applies when billboard_mode is not BILLBOARD_DISABLED.

- billboard_mode: int (BaseMaterial3D.BillboardMode) = 0 [set set_billboard_mode; get get_billboard_mode]
  Controls how the object faces the camera. **Note:** Billboard mode is not suitable for VR because the left-right vector of the camera is not horizontal when the screen is attached to your head instead of on the table. See [GitHub issue #41567](https://github.com/godotengine/godot/issues/41567) for details.

- blend_mode: int (BaseMaterial3D.BlendMode) = 0 [set set_blend_mode; get get_blend_mode]
  The material's blend mode. **Note:** Values other than Mix force the object into the transparent pipeline.

- clearcoat: float = 1.0 [set set_clearcoat; get get_clearcoat]
  Sets the strength of the clearcoat effect. Setting to 0 looks the same as disabling the clearcoat effect.

- clearcoat_enabled: bool = false [set set_feature; get get_feature]
  If true, clearcoat rendering is enabled. Adds a secondary transparent pass to the lighting calculation resulting in an added specular blob. This makes materials appear as if they have a clear layer on them that can be either glossy or rough. **Note:** Clearcoat rendering is not visible if the material's shading_mode is SHADING_MODE_UNSHADED.

- clearcoat_roughness: float = 0.5 [set set_clearcoat_roughness; get get_clearcoat_roughness]
  Sets the roughness of the clearcoat pass. A higher value results in a rougher clearcoat while a lower value results in a smoother clearcoat.

- clearcoat_texture: Texture2D [set set_texture; get get_texture]
  Texture that defines the strength of the clearcoat effect and the glossiness of the clearcoat. Strength is specified in the red channel while glossiness is specified in the green channel.

- cull_mode: int (BaseMaterial3D.CullMode) = 0 [set set_cull_mode; get get_cull_mode]
  Determines which side of the triangle to cull depending on whether the triangle faces towards or away from the camera.

- depth_draw_mode: int (BaseMaterial3D.DepthDrawMode) = 0 [set set_depth_draw_mode; get get_depth_draw_mode]
  Determines when depth rendering takes place. See also transparency.

- depth_test: int (BaseMaterial3D.DepthTest) = 0 [set set_depth_test; get get_depth_test]
  Determines which comparison operator is used when testing depth. **Note:** Changing depth_test to a non-default value only has a visible effect when used on a transparent material, or a material that has depth_draw_mode set to DEPTH_DRAW_DISABLED.

- detail_albedo: Texture2D [set set_texture; get get_texture]
  Texture that specifies the color of the detail overlay. detail_albedo's alpha channel is used as a mask, even when the material is opaque. To use a dedicated texture as a mask, see detail_mask. **Note:** detail_albedo is *not* modulated by albedo_color.

- detail_blend_mode: int (BaseMaterial3D.BlendMode) = 0 [set set_detail_blend_mode; get get_detail_blend_mode]
  Specifies how the detail_albedo should blend with the current ALBEDO.

- detail_enabled: bool = false [set set_feature; get get_feature]
  If true, enables the detail overlay. Detail is a second texture that gets mixed over the surface of the object based on detail_mask and detail_albedo's alpha channel. This can be used to add variation to objects, or to blend between two different albedo/normal textures.

- detail_mask: Texture2D [set set_texture; get get_texture]
  Texture used to specify how the detail textures get blended with the base textures. detail_mask can be used together with detail_albedo's alpha channel (if any).

- detail_normal: Texture2D [set set_texture; get get_texture]
  Texture that specifies the per-pixel normal of the detail overlay. The detail_normal texture only uses the red and green channels; the blue and alpha channels are ignored. The normal read from detail_normal is oriented around the surface normal provided by the Mesh. **Note:** Godot expects the normal map to use X+, Y+, and Z+ coordinates. See [this page](http://wiki.polycount.com/wiki/Normal_Map_Technical_Details#Common_Swizzle_Coordinates) for a comparison of normal map coordinates expected by popular engines.

- detail_uv_layer: int (BaseMaterial3D.DetailUV) = 0 [set set_detail_uv; get get_detail_uv]
  Specifies whether to use UV or UV2 for the detail layer.

- diffuse_mode: int (BaseMaterial3D.DiffuseMode) = 0 [set set_diffuse_mode; get get_diffuse_mode]
  The algorithm used for diffuse light scattering.

- disable_ambient_light: bool = false [set set_flag; get get_flag]
  If true, the object receives no ambient light.

- disable_fog: bool = false [set set_flag; get get_flag]
  If true, the object will not be affected by fog (neither volumetric nor depth fog). This is useful for unshaded or transparent materials (e.g. particles), which without this setting will be affected even if fully transparent.

- disable_receive_shadows: bool = false [set set_flag; get get_flag]
  If true, the object receives no shadow that would otherwise be cast onto it.

- disable_specular_occlusion: bool = false [set set_flag; get get_flag]
  If true, disables specular occlusion even if ProjectSettings.rendering/reflections/specular_occlusion/enabled is false.

- distance_fade_max_distance: float = 10.0 [set set_distance_fade_max_distance; get get_distance_fade_max_distance]
  Distance at which the object appears fully opaque. **Note:** If distance_fade_max_distance is less than distance_fade_min_distance, the behavior will be reversed. The object will start to fade away at distance_fade_max_distance and will fully disappear once it reaches distance_fade_min_distance.

- distance_fade_min_distance: float = 0.0 [set set_distance_fade_min_distance; get get_distance_fade_min_distance]
  Distance at which the object starts to become visible. If the object is less than this distance away, it will be invisible. **Note:** If distance_fade_min_distance is greater than distance_fade_max_distance, the behavior will be reversed. The object will start to fade away at distance_fade_max_distance and will fully disappear once it reaches distance_fade_min_distance.

- distance_fade_mode: int (BaseMaterial3D.DistanceFadeMode) = 0 [set set_distance_fade; get get_distance_fade]
  Specifies which type of fade to use. Can be any of the DistanceFadeModes.

- emission: Color = Color(0, 0, 0, 1) [set set_emission; get get_emission]
  The emitted light's color. See emission_enabled.

- emission_enabled: bool = false [set set_feature; get get_feature]
  If true, the body emits light. Emitting light makes the object appear brighter. The object can also cast light on other objects if a VoxelGI, SDFGI, or LightmapGI is used and this object is used in baked lighting.

- emission_energy_multiplier: float = 1.0 [set set_emission_energy_multiplier; get get_emission_energy_multiplier]
  Multiplier for emitted light. See emission_enabled.

- emission_intensity: float [set set_emission_intensity; get get_emission_intensity]
  Luminance of emitted light, measured in nits (candela per square meter). Only available when ProjectSettings.rendering/lights_and_shadows/use_physical_light_units is enabled. The default is roughly equivalent to an indoor lightbulb.

- emission_on_uv2: bool = false [set set_flag; get get_flag]
  Use UV2 to read from the emission_texture.

- emission_operator: int (BaseMaterial3D.EmissionOperator) = 0 [set set_emission_operator; get get_emission_operator]
  Sets how emission interacts with emission_texture. Can either add or multiply.

- emission_texture: Texture2D [set set_texture; get get_texture]
  Texture that specifies how much surface emits light at a given point.

- fixed_size: bool = false [set set_flag; get get_flag]
  If true, the object is rendered at the same size regardless of distance. The object's size on screen is the same as if the camera was 1.0 units away from the object's origin, regardless of the actual distance from the camera. The Camera3D's field of view (or Camera3D.size when in orthogonal/frustum mode) still affects the size the object is drawn at.

- fov_override: float = 75.0 [set set_fov_override; get get_fov_override]
  Overrides the Camera3D's field of view angle (in degrees). **Note:** This behaves as if the field of view is set on a Camera3D with Camera3D.keep_aspect set to Camera3D.KEEP_HEIGHT. Additionally, it may not look correct on a non-perspective camera where the field of view setting is ignored.

- grow: bool = false [set set_grow_enabled; get is_grow_enabled]
  If true, enables the vertex grow setting. This can be used to create mesh-based outlines using a second material pass and its cull_mode set to CULL_FRONT. See also grow_amount. **Note:** Vertex growth cannot create new vertices, which means that visible gaps may occur in sharp corners. This can be alleviated by designing the mesh to use smooth normals exclusively using [face weighted normals](http://wiki.polycount.com/wiki/Face_weighted_normals) in the 3D authoring software. In this case, grow will be able to join every outline together, just like in the original mesh.

- grow_amount: float = 0.0 [set set_grow; get get_grow]
  Grows object vertices in the direction of their normals. Only effective if grow is true.

- heightmap_deep_parallax: bool = false [set set_heightmap_deep_parallax; get is_heightmap_deep_parallax_enabled]
  If true, uses parallax occlusion mapping to represent depth in the material instead of simple offset mapping (see heightmap_enabled). This results in a more convincing depth effect, but is much more expensive on the GPU. Only enable this on materials where it makes a significant visual difference.

- heightmap_enabled: bool = false [set set_feature; get get_feature]
  If true, height mapping is enabled (also called "parallax mapping" or "depth mapping"). See also normal_enabled. Height mapping is a demanding feature on the GPU, so it should only be used on materials where it makes a significant visual difference. **Note:** Height mapping is not supported if triplanar mapping is used on the same material. The value of heightmap_enabled will be ignored if uv1_triplanar is enabled.

- heightmap_flip_binormal: bool = false [set set_heightmap_deep_parallax_flip_binormal; get get_heightmap_deep_parallax_flip_binormal]
  If true, flips the mesh's binormal vectors when interpreting the height map. If the heightmap effect looks strange when the camera moves (even with a reasonable heightmap_scale), try setting this to true.

- heightmap_flip_tangent: bool = false [set set_heightmap_deep_parallax_flip_tangent; get get_heightmap_deep_parallax_flip_tangent]
  If true, flips the mesh's tangent vectors when interpreting the height map. If the heightmap effect looks strange when the camera moves (even with a reasonable heightmap_scale), try setting this to true.

- heightmap_flip_texture: bool = false [set set_flag; get get_flag]
  If true, interprets the height map texture as a depth map, with brighter values appearing to be "lower" in altitude compared to darker values. This can be enabled for compatibility with some materials authored for Godot 3.x. This is not necessary if the Invert import option was used to invert the depth map in Godot 3.x, in which case heightmap_flip_texture should remain false.

- heightmap_max_layers: int [set set_heightmap_deep_parallax_max_layers; get get_heightmap_deep_parallax_max_layers]
  The number of layers to use for parallax occlusion mapping when the camera is up close to the material. Higher values result in a more convincing depth effect, especially in materials that have steep height changes. Higher values have a significant cost on the GPU, so it should only be increased on materials where it makes a significant visual difference. **Note:** Only effective if heightmap_deep_parallax is true.

- heightmap_min_layers: int [set set_heightmap_deep_parallax_min_layers; get get_heightmap_deep_parallax_min_layers]
  The number of layers to use for parallax occlusion mapping when the camera is far away from the material. Higher values result in a more convincing depth effect, especially in materials that have steep height changes. Higher values have a significant cost on the GPU, so it should only be increased on materials where it makes a significant visual difference. **Note:** Only effective if heightmap_deep_parallax is true.

- heightmap_scale: float = 5.0 [set set_heightmap_scale; get get_heightmap_scale]
  The heightmap scale to use for the parallax effect (see heightmap_enabled). The default value is tuned so that the highest point (value = 255) appears to be 5 cm higher than the lowest point (value = 0). Higher values result in a deeper appearance, but may result in artifacts appearing when looking at the material from oblique angles, especially when the camera moves. Negative values can be used to invert the parallax effect, but this is different from inverting the texture using heightmap_flip_texture as the material will also appear to be "closer" to the camera. In most cases, heightmap_scale should be kept to a positive value. **Note:** If the height map effect looks strange regardless of this value, try adjusting heightmap_flip_binormal and heightmap_flip_tangent. See also heightmap_texture for recommendations on authoring heightmap textures, as the way the heightmap texture is authored affects how heightmap_scale behaves.

- heightmap_texture: Texture2D [set set_texture; get get_texture]
  The texture to use as a height map. See also heightmap_enabled. For best results, the texture should be normalized (with heightmap_scale reduced to compensate). In GIMP(https://gimp.org), this can be done using **Colors > Auto > Equalize**. If the texture only uses a small part of its available range, the parallax effect may look strange, especially when the camera moves. **Note:** To reduce memory usage and improve loading times, you may be able to use a lower-resolution heightmap texture as most heightmaps are only comprised of low-frequency data.

- metallic: float = 0.0 [set set_metallic; get get_metallic]
  A high value makes the material appear more like a metal. Non-metals use their albedo as the diffuse color and add diffuse to the specular reflection. With non-metals, the reflection appears on top of the albedo color. Metals use their albedo as a multiplier to the specular reflection and set the diffuse color to black resulting in a tinted reflection. Materials work better when fully metal or fully non-metal, values between 0 and 1 should only be used for blending between metal and non-metal sections. To alter the amount of reflection use roughness.

- metallic_specular: float = 0.5 [set set_specular; get get_specular]
  Adjusts the strength of specular reflections. Specular reflections are composed of scene reflections and the specular lobe which is the bright spot that is reflected from light sources. When set to 0.0, no specular reflections will be visible. This differs from the SPECULAR_DISABLED SpecularMode as SPECULAR_DISABLED only applies to the specular lobe from the light source. **Note:** Unlike metallic, this is not energy-conserving, so it should be left at 0.5 in most cases. See also roughness.

- metallic_texture: Texture2D [set set_texture; get get_texture]
  Texture used to specify metallic for an object. This is multiplied by metallic.

- metallic_texture_channel: int (BaseMaterial3D.TextureChannel) = 0 [set set_metallic_texture_channel; get get_metallic_texture_channel]
  Specifies the channel of the metallic_texture in which the metallic information is stored. This is useful when you store the information for multiple effects in a single texture. For example if you stored metallic in the red channel, roughness in the blue, and ambient occlusion in the green you could reduce the number of textures you use.

- msdf_outline_size: float = 0.0 [set set_msdf_outline_size; get get_msdf_outline_size]
  The width of the shape outline.

- msdf_pixel_range: float = 4.0 [set set_msdf_pixel_range; get get_msdf_pixel_range]
  The width of the range around the shape between the minimum and maximum representable signed distance.

- no_depth_test: bool = false [set set_flag; get get_flag]
  If true, depth testing is disabled and the object will be drawn in render order.

- normal_enabled: bool = false [set set_feature; get get_feature]
  If true, normal mapping is enabled. This has a slight performance cost, especially on mobile GPUs.

- normal_scale: float = 1.0 [set set_normal_scale; get get_normal_scale]
  The strength of the normal map's effect.

- normal_texture: Texture2D [set set_texture; get get_texture]
  Texture used to specify the normal at a given pixel. The normal_texture only uses the red and green channels; the blue and alpha channels are ignored. The normal read from normal_texture is oriented around the surface normal provided by the Mesh. **Note:** The mesh must have both normals and tangents defined in its vertex data. Otherwise, the normal map won't render correctly and will only appear to darken the whole surface. If creating geometry with SurfaceTool, you can use SurfaceTool.generate_normals() and SurfaceTool.generate_tangents() to automatically generate normals and tangents respectively. **Note:** Godot expects the normal map to use X+, Y+, and Z+ coordinates. See [this page](http://wiki.polycount.com/wiki/Normal_Map_Technical_Details#Common_Swizzle_Coordinates) for a comparison of normal map coordinates expected by popular engines. **Note:** If detail_enabled is true, the detail_albedo texture is drawn *below* the normal_texture. To display a normal map *above* the detail_albedo texture, use detail_normal instead.

- orm_texture: Texture2D [set set_texture; get get_texture]
  The Occlusion/Roughness/Metallic texture to use. This is a more efficient replacement of ao_texture, roughness_texture and metallic_texture in ORMMaterial3D. Ambient occlusion is stored in the red channel. Roughness map is stored in the green channel. Metallic map is stored in the blue channel. The alpha channel is ignored.

- particles_anim_h_frames: int [set set_particles_anim_h_frames; get get_particles_anim_h_frames]
  The number of horizontal frames in the particle sprite sheet. Only enabled when using BILLBOARD_PARTICLES. See billboard_mode.

- particles_anim_loop: bool [set set_particles_anim_loop; get get_particles_anim_loop]
  If true, particle animations are looped. Only enabled when using BILLBOARD_PARTICLES. See billboard_mode.

- particles_anim_v_frames: int [set set_particles_anim_v_frames; get get_particles_anim_v_frames]
  The number of vertical frames in the particle sprite sheet. Only enabled when using BILLBOARD_PARTICLES. See billboard_mode.

- point_size: float = 1.0 [set set_point_size; get get_point_size]
  The point size in pixels. See use_point_size.

- proximity_fade_distance: float = 1.0 [set set_proximity_fade_distance; get get_proximity_fade_distance]
  Distance over which the fade effect takes place. The larger the distance the longer it takes for an object to fade.

- proximity_fade_enabled: bool = false [set set_proximity_fade_enabled; get is_proximity_fade_enabled]
  If true, the proximity fade effect is enabled. The proximity fade effect fades out each pixel based on its distance to another object.

- refraction_enabled: bool = false [set set_feature; get get_feature]
  If true, the refraction effect is enabled. Distorts transparency based on light from behind the object. **Note:** Refraction is implemented using the screen texture. Only opaque materials will appear in the refraction, since transparent materials do not appear in the screen texture.

- refraction_scale: float = 0.05 [set set_refraction; get get_refraction]
  The strength of the refraction effect.

- refraction_texture: Texture2D [set set_texture; get get_texture]
  Texture that controls the strength of the refraction per-pixel. Multiplied by refraction_scale.

- refraction_texture_channel: int (BaseMaterial3D.TextureChannel) = 0 [set set_refraction_texture_channel; get get_refraction_texture_channel]
  Specifies the channel of the refraction_texture in which the refraction information is stored. This is useful when you store the information for multiple effects in a single texture. For example if you stored refraction in the red channel, roughness in the blue, and ambient occlusion in the green you could reduce the number of textures you use.

- rim: float = 1.0 [set set_rim; get get_rim]
  Sets the strength of the rim lighting effect.

- rim_enabled: bool = false [set set_feature; get get_feature]
  If true, rim effect is enabled. Rim lighting increases the brightness at glancing angles on an object. **Note:** Rim lighting is not visible if the material's shading_mode is SHADING_MODE_UNSHADED.

- rim_texture: Texture2D [set set_texture; get get_texture]
  Texture used to set the strength of the rim lighting effect per-pixel. Multiplied by rim.

- rim_tint: float = 0.5 [set set_rim_tint; get get_rim_tint]
  The amount of to blend light and albedo color when rendering rim effect. If 0 the light color is used, while 1 means albedo color is used. An intermediate value generally works best.

- roughness: float = 1.0 [set set_roughness; get get_roughness]
  Surface reflection. A value of 0 represents a perfect mirror while a value of 1 completely blurs the reflection. See also metallic.

- roughness_texture: Texture2D [set set_texture; get get_texture]
  Texture used to control the roughness per-pixel. Multiplied by roughness.

- roughness_texture_channel: int (BaseMaterial3D.TextureChannel) = 0 [set set_roughness_texture_channel; get get_roughness_texture_channel]
  Specifies the channel of the roughness_texture in which the roughness information is stored. This is useful when you store the information for multiple effects in a single texture. For example if you stored metallic in the red channel, roughness in the blue, and ambient occlusion in the green you could reduce the number of textures you use.

- shading_mode: int (BaseMaterial3D.ShadingMode) = 1 [set set_shading_mode; get get_shading_mode]
  Sets whether the shading takes place, per-pixel, per-vertex or unshaded. Per-vertex lighting is faster, making it the best choice for mobile applications, however it looks considerably worse than per-pixel. Unshaded rendering is the fastest, but disables all interactions with lights.

- shadow_to_opacity: bool = false [set set_flag; get get_flag]
  If true, enables the "shadow to opacity" render mode where lighting modifies the alpha so shadowed areas are opaque and non-shadowed areas are transparent. Useful for overlaying shadows onto a camera feed in AR.

- specular_mode: int (BaseMaterial3D.SpecularMode) = 0 [set set_specular_mode; get get_specular_mode]
  The method for rendering the specular blob. **Note:** specular_mode only applies to the specular blob. It does not affect specular reflections from the sky, screen-space reflections, VoxelGI, SDFGI or ReflectionProbes. To disable reflections from these sources as well, set metallic_specular to 0.0 instead.

- stencil_color: Color = Color(0, 0, 0, 1) [set set_stencil_effect_color; get get_stencil_effect_color]
  The primary color of the stencil effect.

- stencil_compare: int (BaseMaterial3D.StencilCompare) = 0 [set set_stencil_compare; get get_stencil_compare]
  The comparison operator to use for stencil masking operations.

- stencil_flags: int = 0 [set set_stencil_flags; get get_stencil_flags]
  The flags dictating how the stencil operation behaves.

- stencil_mode: int (BaseMaterial3D.StencilMode) = 0 [set set_stencil_mode; get get_stencil_mode]
  The stencil effect mode.

- stencil_outline_thickness: float = 0.01 [set set_stencil_effect_outline_thickness; get get_stencil_effect_outline_thickness]
  The outline thickness for STENCIL_MODE_OUTLINE.

- stencil_reference: int = 1 [set set_stencil_reference; get get_stencil_reference]
  The stencil reference value (0-255). Typically a power of 2.

- subsurf_scatter_enabled: bool = false [set set_feature; get get_feature]
  If true, subsurface scattering is enabled. Emulates light that penetrates an object's surface, is scattered, and then emerges. Subsurface scattering quality is controlled by ProjectSettings.rendering/environment/subsurface_scattering/subsurface_scattering_quality. **Note:** Subsurface scattering is not supported on viewports that have a transparent background (where Viewport.transparent_bg is true).

- subsurf_scatter_skin_mode: bool = false [set set_flag; get get_flag]
  If true, subsurface scattering will use a special mode optimized for the color and density of human skin, such as boosting the intensity of the red channel in subsurface scattering.

- subsurf_scatter_strength: float = 0.0 [set set_subsurface_scattering_strength; get get_subsurface_scattering_strength]
  The strength of the subsurface scattering effect. The depth of the effect is also controlled by ProjectSettings.rendering/environment/subsurface_scattering/subsurface_scattering_scale, which is set globally.

- subsurf_scatter_texture: Texture2D [set set_texture; get get_texture]
  Texture used to control the subsurface scattering strength. Stored in the red texture channel. Multiplied by subsurf_scatter_strength.

- subsurf_scatter_transmittance_boost: float = 0.0 [set set_transmittance_boost; get get_transmittance_boost]
  The intensity of the subsurface scattering transmittance effect.

- subsurf_scatter_transmittance_color: Color = Color(1, 1, 1, 1) [set set_transmittance_color; get get_transmittance_color]
  The color to multiply the subsurface scattering transmittance effect with. Ignored if subsurf_scatter_skin_mode is true.

- subsurf_scatter_transmittance_depth: float = 0.1 [set set_transmittance_depth; get get_transmittance_depth]
  The depth of the subsurface scattering transmittance effect.

- subsurf_scatter_transmittance_enabled: bool = false [set set_feature; get get_feature]
  If true, enables subsurface scattering transmittance. Only effective if subsurf_scatter_enabled is true. See also backlight_enabled.

- subsurf_scatter_transmittance_texture: Texture2D [set set_texture; get get_texture]
  The texture to use for multiplying the intensity of the subsurface scattering transmittance intensity. See also subsurf_scatter_texture. Ignored if subsurf_scatter_skin_mode is true.

- texture_filter: int (BaseMaterial3D.TextureFilter) = 3 [set set_texture_filter; get get_texture_filter]
  Filter flags for the texture. **Note:** heightmap_texture is always sampled with linear filtering, even if nearest-neighbor filtering is selected here. This is to ensure the heightmap effect looks as intended. If you need sharper height transitions between pixels, resize the heightmap texture in an image editor with nearest-neighbor filtering.

- texture_repeat: bool = true [set set_flag; get get_flag]
  If true, the texture repeats when exceeding the texture's size. See FLAG_USE_TEXTURE_REPEAT.

- transparency: int (BaseMaterial3D.Transparency) = 0 [set set_transparency; get get_transparency]
  The material's transparency mode. Some transparency modes will disable shadow casting. Any transparency mode other than TRANSPARENCY_DISABLED has a greater performance impact compared to opaque rendering. See also blend_mode.

- use_fov_override: bool = false [set set_flag; get get_flag]
  If true use fov_override to override the Camera3D's field of view angle.

- use_particle_trails: bool = false [set set_flag; get get_flag]
  If true, enables parts of the shader required for GPUParticles3D trails to function. This also requires using a mesh with appropriate skinning, such as RibbonTrailMesh or TubeTrailMesh. Enabling this feature outside of materials used in GPUParticles3D meshes will break material rendering.

- use_point_size: bool = false [set set_flag; get get_flag]
  If true, render point size can be changed. **Note:** This is only effective for objects whose geometry is point-based rather than triangle-based. See also point_size.

- use_z_clip_scale: bool = false [set set_flag; get get_flag]
  If true use z_clip_scale to scale the object being rendered towards the camera to avoid clipping into things like walls.

- uv1_offset: Vector3 = Vector3(0, 0, 0) [set set_uv1_offset; get get_uv1_offset]
  How much to offset the UV coordinates. This amount will be added to UV in the vertex function. This can be used to offset a texture. The Z component is used when uv1_triplanar is enabled, but it is not used anywhere else.

- uv1_scale: Vector3 = Vector3(1, 1, 1) [set set_uv1_scale; get get_uv1_scale]
  How much to scale the UV coordinates. This is multiplied by UV in the vertex function. The Z component is used when uv1_triplanar is enabled, but it is not used anywhere else.

- uv1_triplanar: bool = false [set set_flag; get get_flag]
  If true, instead of using UV textures will use a triplanar texture lookup to determine how to apply textures. Triplanar uses the orientation of the object's surface to blend between texture coordinates. It reads from the source texture 3 times, once for each axis and then blends between the results based on how closely the pixel aligns with each axis. This is often used for natural features to get a realistic blend of materials. Because triplanar texturing requires many more texture reads per-pixel it is much slower than normal UV texturing. Additionally, because it is blending the texture between the three axes, it is unsuitable when you are trying to achieve crisp texturing.

- uv1_triplanar_sharpness: float = 1.0 [set set_uv1_triplanar_blend_sharpness; get get_uv1_triplanar_blend_sharpness]
  A lower number blends the texture more softly while a higher number blends the texture more sharply. **Note:** uv1_triplanar_sharpness is clamped between 0.0 and 150.0 (inclusive) as values outside that range can look broken depending on the mesh.

- uv1_world_triplanar: bool = false [set set_flag; get get_flag]
  If true, triplanar mapping for UV is calculated in world space rather than object local space. See also uv1_triplanar.

- uv2_offset: Vector3 = Vector3(0, 0, 0) [set set_uv2_offset; get get_uv2_offset]
  How much to offset the UV2 coordinates. This amount will be added to UV2 in the vertex function. This can be used to offset a texture. The Z component is used when uv2_triplanar is enabled, but it is not used anywhere else.

- uv2_scale: Vector3 = Vector3(1, 1, 1) [set set_uv2_scale; get get_uv2_scale]
  How much to scale the UV2 coordinates. This is multiplied by UV2 in the vertex function. The Z component is used when uv2_triplanar is enabled, but it is not used anywhere else.

- uv2_triplanar: bool = false [set set_flag; get get_flag]
  If true, instead of using UV2 textures will use a triplanar texture lookup to determine how to apply textures. Triplanar uses the orientation of the object's surface to blend between texture coordinates. It reads from the source texture 3 times, once for each axis and then blends between the results based on how closely the pixel aligns with each axis. This is often used for natural features to get a realistic blend of materials. Because triplanar texturing requires many more texture reads per-pixel it is much slower than normal UV texturing. Additionally, because it is blending the texture between the three axes, it is unsuitable when you are trying to achieve crisp texturing.

- uv2_triplanar_sharpness: float = 1.0 [set set_uv2_triplanar_blend_sharpness; get get_uv2_triplanar_blend_sharpness]
  A lower number blends the texture more softly while a higher number blends the texture more sharply. **Note:** uv2_triplanar_sharpness is clamped between 0.0 and 150.0 (inclusive) as values outside that range can look broken depending on the mesh.

- uv2_world_triplanar: bool = false [set set_flag; get get_flag]
  If true, triplanar mapping for UV2 is calculated in world space rather than object local space. See also uv2_triplanar.

- vertex_color_is_srgb: bool = false [set set_flag; get get_flag]
  If true, vertex colors are considered to be stored in nonlinear sRGB encoding and are converted to linear encoding during rendering. If false, vertex colors are considered to be stored in linear encoding and are rendered as-is. See also albedo_texture_force_srgb. **Note:** Only effective when using the Forward+ and Mobile rendering methods, not Compatibility.

- vertex_color_use_as_albedo: bool = false [set set_flag; get get_flag]
  If true, the vertex color is used as albedo color.

- z_clip_scale: float = 1.0 [set set_z_clip_scale; get get_z_clip_scale]
  Scales the object being rendered towards the camera to avoid clipping into things like walls. This is intended to be used for objects that are fixed with respect to the camera like player arms, tools, etc. Lighting and shadows will continue to work correctly when this setting is adjusted, but screen-space effects like SSAO and SSR may break with lower scales. Therefore, try to keep this setting as close to 1.0 as possible.

## Constants

### Enum TextureParam

- TEXTURE_ALBEDO = 0
  Texture specifying per-pixel color.

- TEXTURE_METALLIC = 1
  Texture specifying per-pixel metallic value.

- TEXTURE_ROUGHNESS = 2
  Texture specifying per-pixel roughness value.

- TEXTURE_EMISSION = 3
  Texture specifying per-pixel emission color.

- TEXTURE_NORMAL = 4
  Texture specifying per-pixel normal vector.

- TEXTURE_BENT_NORMAL = 18
  Texture specifying per-pixel bent normal vector.

- TEXTURE_RIM = 5
  Texture specifying per-pixel rim value.

- TEXTURE_CLEARCOAT = 6
  Texture specifying per-pixel clearcoat value.

- TEXTURE_FLOWMAP = 7
  Texture specifying per-pixel flowmap direction for use with anisotropy.

- TEXTURE_AMBIENT_OCCLUSION = 8
  Texture specifying per-pixel ambient occlusion value.

- TEXTURE_HEIGHTMAP = 9
  Texture specifying per-pixel height.

- TEXTURE_SUBSURFACE_SCATTERING = 10
  Texture specifying per-pixel subsurface scattering.

- TEXTURE_SUBSURFACE_TRANSMITTANCE = 11
  Texture specifying per-pixel transmittance for subsurface scattering.

- TEXTURE_BACKLIGHT = 12
  Texture specifying per-pixel backlight color.

- TEXTURE_REFRACTION = 13
  Texture specifying per-pixel refraction strength.

- TEXTURE_DETAIL_MASK = 14
  Texture specifying per-pixel detail mask blending value.

- TEXTURE_DETAIL_ALBEDO = 15
  Texture specifying per-pixel detail color.

- TEXTURE_DETAIL_NORMAL = 16
  Texture specifying per-pixel detail normal.

- TEXTURE_ORM = 17
  Texture holding ambient occlusion, roughness, and metallic.

- TEXTURE_MAX = 19
  Represents the size of the TextureParam enum.

### Enum TextureFilter

- TEXTURE_FILTER_NEAREST = 0
  The texture filter reads from the nearest pixel only. This makes the texture look pixelated from up close, and grainy from a distance (due to mipmaps not being sampled).

- TEXTURE_FILTER_LINEAR = 1
  The texture filter blends between the nearest 4 pixels. This makes the texture look smooth from up close, and grainy from a distance (due to mipmaps not being sampled).

- TEXTURE_FILTER_NEAREST_WITH_MIPMAPS = 2
  The texture filter reads from the nearest pixel and blends between the nearest 2 mipmaps (or uses the nearest mipmap if ProjectSettings.rendering/textures/default_filters/use_nearest_mipmap_filter is true). This makes the texture look pixelated from up close, and smooth from a distance.

- TEXTURE_FILTER_LINEAR_WITH_MIPMAPS = 3
  The texture filter blends between the nearest 4 pixels and between the nearest 2 mipmaps (or uses the nearest mipmap if ProjectSettings.rendering/textures/default_filters/use_nearest_mipmap_filter is true). This makes the texture look smooth from up close, and smooth from a distance.

- TEXTURE_FILTER_NEAREST_WITH_MIPMAPS_ANISOTROPIC = 4
  The texture filter reads from the nearest pixel and blends between 2 mipmaps (or uses the nearest mipmap if ProjectSettings.rendering/textures/default_filters/use_nearest_mipmap_filter is true) based on the angle between the surface and the camera view. This makes the texture look pixelated from up close, and smooth from a distance. Anisotropic filtering improves texture quality on surfaces that are almost in line with the camera, but is slightly slower. The anisotropic filtering level can be changed by adjusting ProjectSettings.rendering/textures/default_filters/anisotropic_filtering_level.

- TEXTURE_FILTER_LINEAR_WITH_MIPMAPS_ANISOTROPIC = 5
  The texture filter blends between the nearest 4 pixels and blends between 2 mipmaps (or uses the nearest mipmap if ProjectSettings.rendering/textures/default_filters/use_nearest_mipmap_filter is true) based on the angle between the surface and the camera view. This makes the texture look smooth from up close, and smooth from a distance. Anisotropic filtering improves texture quality on surfaces that are almost in line with the camera, but is slightly slower. The anisotropic filtering level can be changed by adjusting ProjectSettings.rendering/textures/default_filters/anisotropic_filtering_level.

- TEXTURE_FILTER_MAX = 6
  Represents the size of the TextureFilter enum.

### Enum DetailUV

- DETAIL_UV_1 = 0
  Use UV with the detail texture.

- DETAIL_UV_2 = 1
  Use UV2 with the detail texture.

### Enum Transparency

- TRANSPARENCY_DISABLED = 0
  The material will not use transparency. This is the fastest to render.

- TRANSPARENCY_ALPHA = 1
  The material will use the texture's alpha values for transparency. This is the slowest to render, and disables shadow casting.

- TRANSPARENCY_ALPHA_SCISSOR = 2
  The material will cut off all values below a threshold, the rest will remain opaque. The opaque portions will be rendered in the depth prepass. This is faster to render than alpha blending, but slower than opaque rendering. This also supports casting shadows.

- TRANSPARENCY_ALPHA_HASH = 3
  The material will cut off all values below a spatially-deterministic threshold, the rest will remain opaque. This is faster to render than alpha blending, but slower than opaque rendering. This also supports casting shadows. Alpha hashing is suited for hair rendering.

- TRANSPARENCY_ALPHA_DEPTH_PRE_PASS = 4
  The material will use the texture's alpha value for transparency, but will discard fragments with an alpha of less than 0.99 during the depth prepass and fragments with an alpha less than 0.1 during the shadow pass. This also supports casting shadows.

- TRANSPARENCY_MAX = 5
  Represents the size of the Transparency enum.

### Enum ShadingMode

- SHADING_MODE_UNSHADED = 0
  The object will not receive shadows. This is the fastest to render, but it disables all interactions with lights.

- SHADING_MODE_PER_PIXEL = 1
  The object will be shaded per pixel. Useful for realistic shading effects.

- SHADING_MODE_PER_VERTEX = 2
  The object will be shaded per vertex. Useful when you want cheaper shaders and do not care about visual quality.

- SHADING_MODE_MAX = 3
  Represents the size of the ShadingMode enum.

### Enum Feature

- FEATURE_EMISSION = 0
  Constant for setting emission_enabled.

- FEATURE_NORMAL_MAPPING = 1
  Constant for setting normal_enabled.

- FEATURE_RIM = 2
  Constant for setting rim_enabled.

- FEATURE_CLEARCOAT = 3
  Constant for setting clearcoat_enabled.

- FEATURE_ANISOTROPY = 4
  Constant for setting anisotropy_enabled.

- FEATURE_AMBIENT_OCCLUSION = 5
  Constant for setting ao_enabled.

- FEATURE_HEIGHT_MAPPING = 6
  Constant for setting heightmap_enabled.

- FEATURE_SUBSURFACE_SCATTERING = 7
  Constant for setting subsurf_scatter_enabled.

- FEATURE_SUBSURFACE_TRANSMITTANCE = 8
  Constant for setting subsurf_scatter_transmittance_enabled.

- FEATURE_BACKLIGHT = 9
  Constant for setting backlight_enabled.

- FEATURE_REFRACTION = 10
  Constant for setting refraction_enabled.

- FEATURE_DETAIL = 11
  Constant for setting detail_enabled.

- FEATURE_BENT_NORMAL_MAPPING = 12
  Constant for setting bent_normal_enabled.

- FEATURE_MAX = 13
  Represents the size of the Feature enum.

### Enum BlendMode

- BLEND_MODE_MIX = 0
  Default blend mode. The color of the object is blended over the background based on the object's alpha value.

- BLEND_MODE_ADD = 1
  The color of the object is added to the background.

- BLEND_MODE_SUB = 2
  The color of the object is subtracted from the background.

- BLEND_MODE_MUL = 3
  The color of the object is multiplied by the background.

- BLEND_MODE_PREMULT_ALPHA = 4
  The color of the object is added to the background and the alpha channel is used to mask out the background. This is effectively a hybrid of the blend mix and add modes, useful for effects like fire where you want the flame to add but the smoke to mix. By default, this works with unshaded materials using premultiplied textures. For shaded materials, use the PREMUL_ALPHA_FACTOR built-in so that lighting can be modulated as well.

### Enum AlphaAntiAliasing

- ALPHA_ANTIALIASING_OFF = 0
  Disables Alpha AntiAliasing for the material.

- ALPHA_ANTIALIASING_ALPHA_TO_COVERAGE = 1
  Enables AlphaToCoverage. Alpha values in the material are passed to the AntiAliasing sample mask.

- ALPHA_ANTIALIASING_ALPHA_TO_COVERAGE_AND_TO_ONE = 2
  Enables AlphaToCoverage and forces all non-zero alpha values to 1. Alpha values in the material are passed to the AntiAliasing sample mask.

### Enum DepthDrawMode

- DEPTH_DRAW_OPAQUE_ONLY = 0
  Default depth draw mode. Depth is drawn only for opaque objects during the opaque prepass (if any) and during the opaque pass.

- DEPTH_DRAW_ALWAYS = 1
  Objects will write to depth during the opaque and the transparent passes. Transparent objects that are close to the camera may obscure other transparent objects behind them. **Note:** This does not influence whether transparent objects are included in the depth prepass or not. For that, see Transparency.

- DEPTH_DRAW_DISABLED = 2
  Objects will not write their depth to the depth buffer, even during the depth prepass (if enabled).

### Enum DepthTest

- DEPTH_TEST_DEFAULT = 0
  Depth test will discard the pixel if it is behind other pixels.

- DEPTH_TEST_INVERTED = 1
  Depth test will discard the pixel if it is in front of other pixels. Useful for stencil effects.

### Enum CullMode

- CULL_BACK = 0
  Default cull mode. The back of the object is culled when not visible. Back face triangles will be culled when facing the camera. This results in only the front side of triangles being drawn. For closed-surface meshes, this means that only the exterior of the mesh will be visible.

- CULL_FRONT = 1
  Front face triangles will be culled when facing the camera. This results in only the back side of triangles being drawn. For closed-surface meshes, this means that the interior of the mesh will be drawn instead of the exterior.

- CULL_DISABLED = 2
  No face culling is performed; both the front face and back face will be visible.

### Enum Flags

- FLAG_DISABLE_DEPTH_TEST = 0
  Disables the depth test, so this object is drawn on top of all others drawn before it. This puts the object in the transparent draw pass where it is sorted based on distance to camera. Objects drawn after it in the draw order may cover it. This also disables writing to depth.

- FLAG_ALBEDO_FROM_VERTEX_COLOR = 1
  Set ALBEDO to the per-vertex color specified in the mesh.

- FLAG_SRGB_VERTEX_COLOR = 2
  Vertex colors are considered to be stored in nonlinear sRGB encoding and are converted to linear encoding during rendering. See also vertex_color_is_srgb. **Note:** Only effective when using the Forward+ and Mobile rendering methods.

- FLAG_USE_POINT_SIZE = 3
  Uses point size to alter the size of primitive points. Also changes the albedo texture lookup to use POINT_COORD instead of UV.

- FLAG_FIXED_SIZE = 4
  Object is scaled by depth so that it always appears the same size on screen.

- FLAG_BILLBOARD_KEEP_SCALE = 5
  Shader will keep the scale set for the mesh. Otherwise the scale is lost when billboarding. Only applies when billboard_mode is BILLBOARD_ENABLED.

- FLAG_UV1_USE_TRIPLANAR = 6
  Use triplanar texture lookup for all texture lookups that would normally use UV.

- FLAG_UV2_USE_TRIPLANAR = 7
  Use triplanar texture lookup for all texture lookups that would normally use UV2.

- FLAG_UV1_USE_WORLD_TRIPLANAR = 8
  Use triplanar texture lookup for all texture lookups that would normally use UV.

- FLAG_UV2_USE_WORLD_TRIPLANAR = 9
  Use triplanar texture lookup for all texture lookups that would normally use UV2.

- FLAG_AO_ON_UV2 = 10
  Use UV2 coordinates to look up from the ao_texture.

- FLAG_EMISSION_ON_UV2 = 11
  Use UV2 coordinates to look up from the emission_texture.

- FLAG_ALBEDO_TEXTURE_FORCE_SRGB = 12
  Forces the shader to convert albedo from nonlinear sRGB encoding to linear encoding. See also albedo_texture_force_srgb.

- FLAG_DONT_RECEIVE_SHADOWS = 13
  Disables receiving shadows from other objects.

- FLAG_DISABLE_AMBIENT_LIGHT = 14
  Disables receiving ambient light.

- FLAG_USE_SHADOW_TO_OPACITY = 15
  Enables the shadow to opacity feature.

- FLAG_USE_TEXTURE_REPEAT = 16
  Enables the texture to repeat when UV coordinates are outside the 0-1 range. If using one of the linear filtering modes, this can result in artifacts at the edges of a texture when the sampler filters across the edges of the texture.

- FLAG_INVERT_HEIGHTMAP = 17
  Invert values read from a depth texture to convert them to height values (heightmap).

- FLAG_SUBSURFACE_MODE_SKIN = 18
  Enables the skin mode for subsurface scattering which is used to improve the look of subsurface scattering when used for human skin.

- FLAG_PARTICLE_TRAILS_MODE = 19
  Enables parts of the shader required for GPUParticles3D trails to function. This also requires using a mesh with appropriate skinning, such as RibbonTrailMesh or TubeTrailMesh. Enabling this feature outside of materials used in GPUParticles3D meshes will break material rendering.

- FLAG_ALBEDO_TEXTURE_MSDF = 20
  Enables multichannel signed distance field rendering shader.

- FLAG_DISABLE_FOG = 21
  Disables receiving depth-based or volumetric fog.

- FLAG_DISABLE_SPECULAR_OCCLUSION = 22
  Disables specular occlusion.

- FLAG_USE_Z_CLIP_SCALE = 23
  Enables using z_clip_scale.

- FLAG_USE_FOV_OVERRIDE = 24
  Enables using fov_override.

- FLAG_MAX = 25
  Represents the size of the Flags enum.

### Enum DiffuseMode

- DIFFUSE_BURLEY = 0
  Default diffuse scattering algorithm.

- DIFFUSE_LAMBERT = 1
  Diffuse scattering ignores roughness.

- DIFFUSE_LAMBERT_WRAP = 2
  Extends Lambert to cover more than 90 degrees when roughness increases.

- DIFFUSE_TOON = 3
  Uses a hard cut for lighting, with smoothing affected by roughness.

### Enum SpecularMode

- SPECULAR_SCHLICK_GGX = 0
  Default specular blob. **Note:** Forward+ uses multiscattering for more accurate reflections, although the impact of multiscattering is more noticeable on rough metallic surfaces than on smooth, non-metallic surfaces. **Note:** Mobile and Compatibility don't perform multiscattering for performance reasons. Instead, they perform single scattering, which means rough metallic surfaces may look slightly darker than intended.

- SPECULAR_TOON = 1
  Toon blob which changes size based on roughness.

- SPECULAR_DISABLED = 2
  No specular blob. This is slightly faster to render than other specular modes.

### Enum BillboardMode

- BILLBOARD_DISABLED = 0
  Billboard mode is disabled.

- BILLBOARD_ENABLED = 1
  The object's Z axis will always face the camera.

- BILLBOARD_FIXED_Y = 2
  The object's X axis will always face the camera.

- BILLBOARD_PARTICLES = 3
  Used for particle systems when assigned to GPUParticles3D and CPUParticles3D nodes (flipbook animation). Enables particles_anim_* properties. The ParticleProcessMaterial.anim_speed_min or CPUParticles3D.anim_speed_min should also be set to a value bigger than zero for the animation to play.

### Enum TextureChannel

- TEXTURE_CHANNEL_RED = 0
  Used to read from the red channel of a texture.

- TEXTURE_CHANNEL_GREEN = 1
  Used to read from the green channel of a texture.

- TEXTURE_CHANNEL_BLUE = 2
  Used to read from the blue channel of a texture.

- TEXTURE_CHANNEL_ALPHA = 3
  Used to read from the alpha channel of a texture.

- TEXTURE_CHANNEL_GRAYSCALE = 4
  Used to read from the linear (non-perceptual) average of the red, green and blue channels of a texture.

### Enum EmissionOperator

- EMISSION_OP_ADD = 0
  Adds the emission color to the color from the emission texture.

- EMISSION_OP_MULTIPLY = 1
  Multiplies the emission color by the color from the emission texture.

### Enum DistanceFadeMode

- DISTANCE_FADE_DISABLED = 0
  Do not use distance fade.

- DISTANCE_FADE_PIXEL_ALPHA = 1
  Smoothly fades the object out based on each pixel's distance from the camera using the alpha channel.

- DISTANCE_FADE_PIXEL_DITHER = 2
  Smoothly fades the object out based on each pixel's distance from the camera using a dithering approach. Dithering discards pixels based on a set pattern to smoothly fade without enabling transparency. On certain hardware, this can be faster than DISTANCE_FADE_PIXEL_ALPHA.

- DISTANCE_FADE_OBJECT_DITHER = 3
  Smoothly fades the object out based on the object's distance from the camera using a dithering approach. Dithering discards pixels based on a set pattern to smoothly fade without enabling transparency. On certain hardware, this can be faster than DISTANCE_FADE_PIXEL_ALPHA and DISTANCE_FADE_PIXEL_DITHER.

### Enum StencilMode

- STENCIL_MODE_DISABLED = 0
  Disables stencil operations.

- STENCIL_MODE_OUTLINE = 1
  Stencil preset which applies an outline to the object. **Note:** Requires a Material.next_pass material which will be automatically applied. Any manual changes made to Material.next_pass will be lost when the stencil properties are modified or the scene is reloaded. To safely apply a Material.next_pass material on a material that uses stencil presets, use GeometryInstance3D.material_overlay instead.

- STENCIL_MODE_XRAY = 2
  Stencil preset which shows a silhouette of the object behind walls. **Note:** Requires a Material.next_pass material which will be automatically applied. Any manual changes made to Material.next_pass will be lost when the stencil properties are modified or the scene is reloaded. To safely apply a Material.next_pass material on a material that uses stencil presets, use GeometryInstance3D.material_overlay instead.

- STENCIL_MODE_CUSTOM = 3
  Enables stencil operations without a preset.

### Enum StencilFlags

- STENCIL_FLAG_READ = 1
  The material will only be rendered where it passes a stencil comparison with existing stencil buffer values.

- STENCIL_FLAG_WRITE = 2
  The material will write the reference value to the stencil buffer where it passes the depth test.

- STENCIL_FLAG_WRITE_DEPTH_FAIL = 4
  The material will write the reference value to the stencil buffer where it fails the depth test.

### Enum StencilCompare

- STENCIL_COMPARE_ALWAYS = 0
  Always passes the stencil test.

- STENCIL_COMPARE_LESS = 1
  Passes the stencil test when the reference value is less than the existing stencil value.

- STENCIL_COMPARE_EQUAL = 2
  Passes the stencil test when the reference value is equal to the existing stencil value.

- STENCIL_COMPARE_LESS_OR_EQUAL = 3
  Passes the stencil test when the reference value is less than or equal to the existing stencil value.

- STENCIL_COMPARE_GREATER = 4
  Passes the stencil test when the reference value is greater than the existing stencil value.

- STENCIL_COMPARE_NOT_EQUAL = 5
  Passes the stencil test when the reference value is not equal to the existing stencil value.

- STENCIL_COMPARE_GREATER_OR_EQUAL = 6
  Passes the stencil test when the reference value is greater than or equal to the existing stencil value.
