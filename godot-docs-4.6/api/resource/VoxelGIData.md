# VoxelGIData

## Meta

- Name: VoxelGIData
- Source: VoxelGIData.xml
- Inherits: Resource
- Inheritance Chain: VoxelGIData -> Resource -> RefCounted -> Object

## Brief Description

Contains baked voxel global illumination data for use in a VoxelGI node.

## Description

VoxelGIData contains baked voxel global illumination for use in a VoxelGI node. VoxelGIData also offers several properties to adjust the final appearance of the global illumination. These properties can be adjusted at run-time without having to bake the VoxelGI node again. **Note:** To prevent text-based scene files (.tscn) from growing too much and becoming slow to load and save, always save VoxelGIData to an external binary resource file (.res) instead of embedding it within the scene. This can be done by clicking the dropdown arrow next to the VoxelGIData resource, choosing **Edit**, clicking the floppy disk icon at the top of the Inspector then choosing **Save As...**.

## Quick Reference

```
[methods]
allocate(to_cell_xform: Transform3D, aabb: AABB, octree_size: Vector3, octree_cells: PackedByteArray, data_cells: PackedByteArray, distance_field: PackedByteArray, level_counts: PackedInt32Array) -> void
get_bounds() -> AABB [const]
get_data_cells() -> PackedByteArray [const]
get_level_counts() -> PackedInt32Array [const]
get_octree_cells() -> PackedByteArray [const]
get_octree_size() -> Vector3 [const]
get_to_cell_xform() -> Transform3D [const]

[properties]
bias: float = 1.5
dynamic_range: float = 2.0
energy: float = 1.0
interior: bool = false
normal_bias: float = 0.0
propagation: float = 0.5
use_two_bounces: bool = true
```

## Tutorials

- [Third Person Shooter (TPS) Demo](https://godotengine.org/asset-library/asset/2710)

## Methods

- allocate(to_cell_xform: Transform3D, aabb: AABB, octree_size: Vector3, octree_cells: PackedByteArray, data_cells: PackedByteArray, distance_field: PackedByteArray, level_counts: PackedInt32Array) -> void

- get_bounds() -> AABB [const]
  Returns the bounds of the baked voxel data as an AABB, which should match VoxelGI.size after being baked (which only contains the size as a Vector3). **Note:** If the size was modified without baking the VoxelGI data, then the value of get_bounds() and VoxelGI.size will not match.

- get_data_cells() -> PackedByteArray [const]

- get_level_counts() -> PackedInt32Array [const]

- get_octree_cells() -> PackedByteArray [const]

- get_octree_size() -> Vector3 [const]

- get_to_cell_xform() -> Transform3D [const]

## Properties

- bias: float = 1.5 [set set_bias; get get_bias]
  The normal bias to use for indirect lighting and reflections. Higher values reduce self-reflections visible in non-rough materials, at the cost of more visible light leaking and flatter-looking indirect lighting. To prioritize hiding self-reflections over lighting quality, set bias to 0.0 and normal_bias to a value between 1.0 and 2.0.

- dynamic_range: float = 2.0 [set set_dynamic_range; get get_dynamic_range]
  The dynamic range to use (1.0 represents a low dynamic range scene brightness). Higher values can be used to provide brighter indirect lighting, at the cost of more visible color banding in dark areas (both in indirect lighting and reflections). To avoid color banding, it's recommended to use the lowest value that does not result in visible light clipping.

- energy: float = 1.0 [set set_energy; get get_energy]
  The energy of the indirect lighting and reflections produced by the VoxelGI node. Higher values result in brighter indirect lighting. If indirect lighting looks too flat, try decreasing propagation while increasing energy at the same time. See also use_two_bounces which influences the indirect lighting's effective brightness.

- interior: bool = false [set set_interior; get is_interior]
  If true, Environment lighting is ignored by the VoxelGI node. If false, Environment lighting is taken into account by the VoxelGI node. Environment lighting updates in real-time, which means it can be changed without having to bake the VoxelGI node again.

- normal_bias: float = 0.0 [set set_normal_bias; get get_normal_bias]
  The normal bias to use for indirect lighting and reflections. Higher values reduce self-reflections visible in non-rough materials, at the cost of more visible light leaking and flatter-looking indirect lighting. See also bias. To prioritize hiding self-reflections over lighting quality, set bias to 0.0 and normal_bias to a value between 1.0 and 2.0.

- propagation: float = 0.5 [set set_propagation; get get_propagation]
  The multiplier to use when light bounces off a surface. Higher values result in brighter indirect lighting. If indirect lighting looks too flat, try decreasing propagation while increasing energy at the same time. See also use_two_bounces which influences the indirect lighting's effective brightness.

- use_two_bounces: bool = true [set set_use_two_bounces; get is_using_two_bounces]
  If true, performs two bounces of indirect lighting instead of one. This makes indirect lighting look more natural and brighter at a small performance cost. The second bounce is also visible in reflections. If the scene appears too bright after enabling use_two_bounces, adjust propagation and energy.
