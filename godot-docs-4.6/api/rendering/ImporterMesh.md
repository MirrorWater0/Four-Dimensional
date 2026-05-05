# ImporterMesh

## Meta

- Name: ImporterMesh
- Source: ImporterMesh.xml
- Inherits: Resource
- Inheritance Chain: ImporterMesh -> Resource -> RefCounted -> Object

## Brief Description

A Resource that contains vertex array-based geometry during the import process.

## Description

ImporterMesh is a type of Resource analogous to ArrayMesh. It contains vertex array-based geometry, divided in *surfaces*. Each surface contains a completely separate array and a material used to draw it. Design wise, a mesh with multiple surfaces is preferred to a single surface, because objects created in 3D editing software commonly contain multiple materials. Unlike its runtime counterpart, ImporterMesh contains mesh data before various import steps, such as lod and shadow mesh generation, have taken place. Modify surface data by calling clear(), followed by add_surface() for each surface.

## Quick Reference

```
[methods]
add_blend_shape(name: String) -> void
add_surface(primitive: int (Mesh.PrimitiveType), arrays: Array, blend_shapes: Array[] = [], lods: Dictionary = {}, material: Material = null, name: String = "", flags: int = 0) -> void
clear() -> void
from_mesh(mesh: Mesh) -> ImporterMesh [static]
generate_lods(normal_merge_angle: float, normal_split_angle: float, bone_transform_array: Array) -> void
get_blend_shape_count() -> int [const]
get_blend_shape_mode() -> int (Mesh.BlendShapeMode) [const]
get_blend_shape_name(blend_shape_idx: int) -> String [const]
get_lightmap_size_hint() -> Vector2i [const]
get_mesh(base_mesh: ArrayMesh = null) -> ArrayMesh
get_surface_arrays(surface_idx: int) -> Array [const]
get_surface_blend_shape_arrays(surface_idx: int, blend_shape_idx: int) -> Array [const]
get_surface_count() -> int [const]
get_surface_format(surface_idx: int) -> int [const]
get_surface_lod_count(surface_idx: int) -> int [const]
get_surface_lod_indices(surface_idx: int, lod_idx: int) -> PackedInt32Array [const]
get_surface_lod_size(surface_idx: int, lod_idx: int) -> float [const]
get_surface_material(surface_idx: int) -> Material [const]
get_surface_name(surface_idx: int) -> String [const]
get_surface_primitive_type(surface_idx: int) -> int (Mesh.PrimitiveType)
set_blend_shape_mode(mode: int (Mesh.BlendShapeMode)) -> void
set_lightmap_size_hint(size: Vector2i) -> void
set_surface_material(surface_idx: int, material: Material) -> void
set_surface_name(surface_idx: int, name: String) -> void
```

## Methods

- add_blend_shape(name: String) -> void
  Adds name for a blend shape that will be added with add_surface(). Must be called before surface is added.

- add_surface(primitive: int (Mesh.PrimitiveType), arrays: Array, blend_shapes: Array[] = [], lods: Dictionary = {}, material: Material = null, name: String = "", flags: int = 0) -> void
  Creates a new surface. Mesh.get_surface_count() will become the surf_idx for this new surface. Surfaces are created to be rendered using a primitive, which may be any of the values defined in Mesh.PrimitiveType. The arrays argument is an array of arrays. Each of the Mesh.ARRAY_MAX elements contains an array with some of the mesh data for this surface as described by the corresponding member of Mesh.ArrayType or null if it is not used by the surface. For example, arrays0 is the array of vertices. That first vertex sub-array is always required; the others are optional. Adding an index array puts this surface into "index mode" where the vertex and other arrays become the sources of data and the index array defines the vertex order. All sub-arrays must have the same length as the vertex array (or be an exact multiple of the vertex array's length, when multiple elements of a sub-array correspond to a single vertex) or be empty, except for Mesh.ARRAY_INDEX if it is used. The blend_shapes argument is an array of vertex data for each blend shape. Each element is an array of the same structure as arrays, but Mesh.ARRAY_VERTEX, Mesh.ARRAY_NORMAL, and Mesh.ARRAY_TANGENT are set if and only if they are set in arrays and all other entries are null. The lods argument is a dictionary with float keys and PackedInt32Array values. Each entry in the dictionary represents an LOD level of the surface, where the value is the Mesh.ARRAY_INDEX array to use for the LOD level and the key is roughly proportional to the distance at which the LOD stats being used. I.e., increasing the key of an LOD also increases the distance that the objects has to be from the camera before the LOD is used. The flags argument is the bitwise OR of, as required: One value of Mesh.ArrayCustomFormat left shifted by ARRAY_FORMAT_CUSTOMn_SHIFT for each custom channel in use, Mesh.ARRAY_FLAG_USE_DYNAMIC_UPDATE, Mesh.ARRAY_FLAG_USE_8_BONE_WEIGHTS, or Mesh.ARRAY_FLAG_USES_EMPTY_VERTEX_ARRAY. **Note:** When using indices, it is recommended to only use points, lines, or triangles.

- clear() -> void
  Removes all surfaces and blend shapes from this ImporterMesh.

- from_mesh(mesh: Mesh) -> ImporterMesh [static]
  Converts the given Mesh into an ImporterMesh by copying all its surfaces, blend shapes, materials, and metadata into a new ImporterMesh object.

- generate_lods(normal_merge_angle: float, normal_split_angle: float, bone_transform_array: Array) -> void
  Generates all lods for this ImporterMesh. normal_merge_angle is in degrees and used in the same way as the importer settings in lods. normal_split_angle is not used and only remains for compatibility with older versions of the API. The number of generated lods can be accessed using get_surface_lod_count(), and each LOD is available in get_surface_lod_size() and get_surface_lod_indices(). bone_transform_array is an Array which can be either empty or contain Transform3Ds which, for each of the mesh's bone IDs, will apply mesh skinning when generating the LOD mesh variations. This is usually used to account for discrepancies in scale between the mesh itself and its skinning data.

- get_blend_shape_count() -> int [const]
  Returns the number of blend shapes that the mesh holds.

- get_blend_shape_mode() -> int (Mesh.BlendShapeMode) [const]
  Returns the blend shape mode for this Mesh.

- get_blend_shape_name(blend_shape_idx: int) -> String [const]
  Returns the name of the blend shape at this index.

- get_lightmap_size_hint() -> Vector2i [const]
  Returns the size hint of this mesh for lightmap-unwrapping in UV-space.

- get_mesh(base_mesh: ArrayMesh = null) -> ArrayMesh
  Returns the mesh data represented by this ImporterMesh as a usable ArrayMesh. This method caches the returned mesh, and subsequent calls will return the cached data until clear() is called. If not yet cached and base_mesh is provided, base_mesh will be used and mutated.

- get_surface_arrays(surface_idx: int) -> Array [const]
  Returns the arrays for the vertices, normals, UVs, etc. that make up the requested surface. See add_surface().

- get_surface_blend_shape_arrays(surface_idx: int, blend_shape_idx: int) -> Array [const]
  Returns a single set of blend shape arrays for the requested blend shape index for a surface.

- get_surface_count() -> int [const]
  Returns the number of surfaces that the mesh holds.

- get_surface_format(surface_idx: int) -> int [const]
  Returns the format of the surface that the mesh holds.

- get_surface_lod_count(surface_idx: int) -> int [const]
  Returns the number of lods that the mesh holds on a given surface.

- get_surface_lod_indices(surface_idx: int, lod_idx: int) -> PackedInt32Array [const]
  Returns the index buffer of a lod for a surface.

- get_surface_lod_size(surface_idx: int, lod_idx: int) -> float [const]
  Returns the screen ratio which activates a lod for a surface.

- get_surface_material(surface_idx: int) -> Material [const]
  Returns a Material in a given surface. Surface is rendered using this material.

- get_surface_name(surface_idx: int) -> String [const]
  Gets the name assigned to this surface.

- get_surface_primitive_type(surface_idx: int) -> int (Mesh.PrimitiveType)
  Returns the primitive type of the requested surface (see add_surface()).

- set_blend_shape_mode(mode: int (Mesh.BlendShapeMode)) -> void
  Sets the blend shape mode.

- set_lightmap_size_hint(size: Vector2i) -> void
  Sets the size hint of this mesh for lightmap-unwrapping in UV-space.

- set_surface_material(surface_idx: int, material: Material) -> void
  Sets a Material for a given surface. Surface will be rendered using this material.

- set_surface_name(surface_idx: int, name: String) -> void
  Sets a name for a given surface.
