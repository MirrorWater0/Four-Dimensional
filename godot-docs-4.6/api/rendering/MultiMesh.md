# MultiMesh

## Meta

- Name: MultiMesh
- Source: MultiMesh.xml
- Inherits: Resource
- Inheritance Chain: MultiMesh -> Resource -> RefCounted -> Object

## Brief Description

Provides high-performance drawing of a mesh multiple times using GPU instancing.

## Description

MultiMesh provides low-level mesh instancing. Drawing thousands of MeshInstance3D nodes can be slow, since each object is submitted to the GPU then drawn individually. MultiMesh is much faster as it can draw thousands of instances with a single draw call, resulting in less API overhead. As a drawback, if the instances are too far away from each other, performance may be reduced as every single instance will always render (they are spatially indexed as one, for the whole object). Since instances may have any behavior, the AABB used for visibility must be provided by the user. **Note:** A MultiMesh is a single object, therefore the same maximum lights per object restriction applies. This means, that once the maximum lights are consumed by one or more instances, the rest of the MultiMesh instances will **not** receive any lighting. **Note:** Blend Shapes will be ignored if used in a MultiMesh.

## Quick Reference

```
[methods]
get_aabb() -> AABB [const]
get_instance_color(instance: int) -> Color [const]
get_instance_custom_data(instance: int) -> Color [const]
get_instance_transform(instance: int) -> Transform3D [const]
get_instance_transform_2d(instance: int) -> Transform2D [const]
reset_instance_physics_interpolation(instance: int) -> void
reset_instances_physics_interpolation() -> void
set_buffer_interpolated(buffer_curr: PackedFloat32Array, buffer_prev: PackedFloat32Array) -> void
set_instance_color(instance: int, color: Color) -> void
set_instance_custom_data(instance: int, custom_data: Color) -> void
set_instance_transform(instance: int, transform: Transform3D) -> void
set_instance_transform_2d(instance: int, transform: Transform2D) -> void

[properties]
buffer: PackedFloat32Array = PackedFloat32Array()
color_array: PackedColorArray
custom_aabb: AABB = AABB(0, 0, 0, 0, 0, 0)
custom_data_array: PackedColorArray
instance_count: int = 0
mesh: Mesh
physics_interpolation_quality: int (MultiMesh.PhysicsInterpolationQuality) = 0
transform_2d_array: PackedVector2Array
transform_array: PackedVector3Array
transform_format: int (MultiMesh.TransformFormat) = 0
use_colors: bool = false
use_custom_data: bool = false
visible_instance_count: int = -1
```

## Tutorials

- [Using MultiMeshInstance]($DOCS_URL/tutorials/3d/using_multi_mesh_instance.html)
- [Optimization using MultiMeshes]($DOCS_URL/tutorials/performance/using_multimesh.html)
- [Animating thousands of fish with MultiMeshInstance]($DOCS_URL/tutorials/performance/vertex_animation/animating_thousands_of_fish.html)

## Methods

- get_aabb() -> AABB [const]
  Returns the visibility axis-aligned bounding box in local space.

- get_instance_color(instance: int) -> Color [const]
  Gets a specific instance's color multiplier.

- get_instance_custom_data(instance: int) -> Color [const]
  Returns the custom data that has been set for a specific instance.

- get_instance_transform(instance: int) -> Transform3D [const]
  Returns the Transform3D of a specific instance.

- get_instance_transform_2d(instance: int) -> Transform2D [const]
  Returns the Transform2D of a specific instance.

- reset_instance_physics_interpolation(instance: int) -> void
  When using *physics interpolation*, this function allows you to prevent interpolation on an instance in the current physics tick. This allows you to move instances instantaneously, and should usually be used when initially placing an instance such as a bullet to prevent graphical glitches.

- reset_instances_physics_interpolation() -> void
  When using *physics interpolation*, this function allows you to prevent interpolation for all instances in the current physics tick. This allows you to move all instances instantaneously, and should usually be used when initially placing instances to prevent graphical glitches.

- set_buffer_interpolated(buffer_curr: PackedFloat32Array, buffer_prev: PackedFloat32Array) -> void
  An alternative to setting the buffer property, which can be used with *physics interpolation*. This method takes two arrays, and can set the data for the current and previous tick in one go. The renderer will automatically interpolate the data at each frame. This is useful for situations where the order of instances may change from physics tick to tick, such as particle systems. When the order of instances is coherent, the simpler alternative of setting buffer can still be used with interpolation.

- set_instance_color(instance: int, color: Color) -> void
  Sets the color of a specific instance by *multiplying* the mesh's existing vertex colors. This allows for different color tinting per instance. **Note:** Each component is stored in 32 bits in the Forward+ and Mobile rendering methods, but is packed into 16 bits in the Compatibility rendering method. For the color to take effect, ensure that use_colors is true on the MultiMesh and BaseMaterial3D.vertex_color_use_as_albedo is true on the material. If you intend to set an absolute color instead of tinting, make sure the material's albedo color is set to pure white (Color(1, 1, 1)).

- set_instance_custom_data(instance: int, custom_data: Color) -> void
  Sets custom data for a specific instance. custom_data is a Color type only to contain 4 floating-point numbers. **Note:** Each number is stored in 32 bits in the Forward+ and Mobile rendering methods, but is packed into 16 bits in the Compatibility rendering method. For the custom data to be used, ensure that use_custom_data is true. This custom instance data has to be manually accessed in your custom shader using INSTANCE_CUSTOM.

- set_instance_transform(instance: int, transform: Transform3D) -> void
  Sets the Transform3D for a specific instance.

- set_instance_transform_2d(instance: int, transform: Transform2D) -> void
  Sets the Transform2D for a specific instance.

## Properties

- buffer: PackedFloat32Array = PackedFloat32Array() [set set_buffer; get get_buffer]

- color_array: PackedColorArray [set _set_color_array; get _get_color_array]
  Array containing each Color used by all instances of this mesh.

- custom_aabb: AABB = AABB(0, 0, 0, 0, 0, 0) [set set_custom_aabb; get get_custom_aabb]
  Custom AABB for this MultiMesh resource. Setting this manually prevents costly runtime AABB recalculations.

- custom_data_array: PackedColorArray [set _set_custom_data_array; get _get_custom_data_array]
  Array containing each custom data value used by all instances of this mesh, as a PackedColorArray.

- instance_count: int = 0 [set set_instance_count; get get_instance_count]
  Number of instances that will get drawn. This clears and (re)sizes the buffers. Setting data format or flags afterwards will have no effect. By default, all instances are drawn but you can limit this with visible_instance_count.

- mesh: Mesh [set set_mesh; get get_mesh]
  Mesh resource to be instanced. The looks of the individual instances can be modified using set_instance_color() and set_instance_custom_data().

- physics_interpolation_quality: int (MultiMesh.PhysicsInterpolationQuality) = 0 [set set_physics_interpolation_quality; get get_physics_interpolation_quality]
  Choose whether to use an interpolation method that favors speed or quality. When using low physics tick rates (typically below 20) or high rates of object rotation, you may get better results from the high quality setting. **Note:** Fast quality does not equate to low quality. Except in the special cases mentioned above, the quality should be comparable to high quality.

- transform_2d_array: PackedVector2Array [set _set_transform_2d_array; get _get_transform_2d_array]
  Array containing each Transform2D value used by all instances of this mesh, as a PackedVector2Array. Each transform is divided into 3 Vector2 values corresponding to the transforms' x, y, and origin.

- transform_array: PackedVector3Array [set _set_transform_array; get _get_transform_array]
  Array containing each Transform3D value used by all instances of this mesh, as a PackedVector3Array. Each transform is divided into 4 Vector3 values corresponding to the transforms' x, y, z, and origin.

- transform_format: int (MultiMesh.TransformFormat) = 0 [set set_transform_format; get get_transform_format]
  Format of transform used to transform mesh, either 2D or 3D.

- use_colors: bool = false [set set_use_colors; get is_using_colors]
  If true, the MultiMesh will use color data (see set_instance_color()). Can only be set when instance_count is 0 or less. This means that you need to call this method before setting the instance count, or temporarily reset it to 0.

- use_custom_data: bool = false [set set_use_custom_data; get is_using_custom_data]
  If true, the MultiMesh will use custom data (see set_instance_custom_data()). Can only be set when instance_count is 0 or less. This means that you need to call this method before setting the instance count, or temporarily reset it to 0.

- visible_instance_count: int = -1 [set set_visible_instance_count; get get_visible_instance_count]
  Limits the number of instances drawn, -1 draws all instances. Changing this does not change the sizes of the buffers.

## Constants

### Enum TransformFormat

- TRANSFORM_2D = 0
  Use this when using 2D transforms.

- TRANSFORM_3D = 1
  Use this when using 3D transforms.

### Enum PhysicsInterpolationQuality

- INTERP_QUALITY_FAST = 0
  Always interpolate using Basis lerping, which can produce warping artifacts in some situations.

- INTERP_QUALITY_HIGH = 1
  Attempt to interpolate using Basis slerping (spherical linear interpolation) where possible, otherwise fall back to lerping.
