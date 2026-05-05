# ArrayMesh

## Meta

- Name: ArrayMesh
- Source: ArrayMesh.xml
- Inherits: Mesh
- Inheritance Chain: ArrayMesh -> Mesh -> Resource -> RefCounted -> Object

## Brief Description

Mesh type that provides utility for constructing a surface from arrays.

## Description

The ArrayMesh is used to construct a Mesh by specifying the attributes as arrays. The most basic example is the creation of a single triangle:

```
var vertices = PackedVector3Array()
vertices.push_back(Vector3(0, 1, 0))
vertices.push_back(Vector3(1, 0, 0))
vertices.push_back(Vector3(0, 0, 1))

# Initialize the ArrayMesh.
var arr_mesh = ArrayMesh.new()
var arrays = []
arrays.resize(Mesh.ARRAY_MAX)
arraysMesh.ARRAY_VERTEX = vertices

# Create the Mesh.
arr_mesh.add_surface_from_arrays(Mesh.PRIMITIVE_TRIANGLES, arrays)
var m = MeshInstance3D.new()
m.mesh = arr_mesh
```

```
Vector3[] vertices =
[
    new Vector3(0, 1, 0),
    new Vector3(1, 0, 0),
    new Vector3(0, 0, 1),
];

// Initialize the ArrayMesh.
var arrMesh = new ArrayMesh();
Godot.Collections.Array arrays = [];
arrays.Resize((int)Mesh.ArrayType.Max);
arrays[(int)Mesh.ArrayType.Vertex] = vertices;

// Create the Mesh.
arrMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);
var m = new MeshInstance3D();
m.Mesh = arrMesh;
```

The MeshInstance3D is ready to be added to the SceneTree to be shown. See also ImmediateMesh, MeshDataTool and SurfaceTool for procedural geometry generation. **Note:** Godot uses clockwise [winding order](https://learnopengl.com/Advanced-OpenGL/Face-culling) for front faces of triangle primitive modes.

## Quick Reference

```
[methods]
add_blend_shape(name: StringName) -> void
add_surface_from_arrays(primitive: int (Mesh.PrimitiveType), arrays: Array, blend_shapes: Array[] = [], lods: Dictionary = {}, flags: int (Mesh.ArrayFormat) = 0) -> void
clear_blend_shapes() -> void
clear_surfaces() -> void
get_blend_shape_count() -> int [const]
get_blend_shape_name(index: int) -> StringName [const]
lightmap_unwrap(transform: Transform3D, texel_size: float) -> int (Error)
regen_normal_maps() -> void
set_blend_shape_name(index: int, name: StringName) -> void
surface_find_by_name(name: String) -> int [const]
surface_get_array_index_len(surf_idx: int) -> int [const]
surface_get_array_len(surf_idx: int) -> int [const]
surface_get_format(surf_idx: int) -> int (Mesh.ArrayFormat) [const]
surface_get_name(surf_idx: int) -> String [const]
surface_get_primitive_type(surf_idx: int) -> int (Mesh.PrimitiveType) [const]
surface_remove(surf_idx: int) -> void
surface_set_name(surf_idx: int, name: String) -> void
surface_update_attribute_region(surf_idx: int, offset: int, data: PackedByteArray) -> void
surface_update_skin_region(surf_idx: int, offset: int, data: PackedByteArray) -> void
surface_update_vertex_region(surf_idx: int, offset: int, data: PackedByteArray) -> void

[properties]
blend_shape_mode: int (Mesh.BlendShapeMode) = 1
custom_aabb: AABB = AABB(0, 0, 0, 0, 0, 0)
shadow_mesh: ArrayMesh
```

## Tutorials

- [Procedural geometry using the ArrayMesh]($DOCS_URL/tutorials/3d/procedural_geometry/arraymesh.html)

## Methods

- add_blend_shape(name: StringName) -> void
  Adds name for a blend shape that will be added with add_surface_from_arrays(). Must be called before surface is added.

- add_surface_from_arrays(primitive: int (Mesh.PrimitiveType), arrays: Array, blend_shapes: Array[] = [], lods: Dictionary = {}, flags: int (Mesh.ArrayFormat) = 0) -> void
  Creates a new surface. Mesh.get_surface_count() will become the surf_idx for this new surface. Surfaces are created to be rendered using a primitive, which may be any of the values defined in Mesh.PrimitiveType. The arrays argument is an array of arrays. Each of the Mesh.ARRAY_MAX elements contains an array with some of the mesh data for this surface as described by the corresponding member of Mesh.ArrayType or null if it is not used by the surface. For example, arrays0 is the array of vertices. That first vertex sub-array is always required; the others are optional. Adding an index array puts this surface into "index mode" where the vertex and other arrays become the sources of data and the index array defines the vertex order. All sub-arrays must have the same length as the vertex array (or be an exact multiple of the vertex array's length, when multiple elements of a sub-array correspond to a single vertex) or be empty, except for Mesh.ARRAY_INDEX if it is used. The blend_shapes argument is an array of vertex data for each blend shape. Each element is an array of the same structure as arrays, but Mesh.ARRAY_VERTEX, Mesh.ARRAY_NORMAL, and Mesh.ARRAY_TANGENT are set if and only if they are set in arrays and all other entries are null. The lods argument is a dictionary with float keys and PackedInt32Array values. Each entry in the dictionary represents an LOD level of the surface, where the value is the Mesh.ARRAY_INDEX array to use for the LOD level and the key is roughly proportional to the distance at which the LOD stats being used. I.e., increasing the key of an LOD also increases the distance that the objects has to be from the camera before the LOD is used. The flags argument is the bitwise OR of, as required: One value of Mesh.ArrayCustomFormat left shifted by ARRAY_FORMAT_CUSTOMn_SHIFT for each custom channel in use, Mesh.ARRAY_FLAG_USE_DYNAMIC_UPDATE, Mesh.ARRAY_FLAG_USE_8_BONE_WEIGHTS, or Mesh.ARRAY_FLAG_USES_EMPTY_VERTEX_ARRAY. **Note:** When using indices, it is recommended to only use points, lines, or triangles.

- clear_blend_shapes() -> void
  Removes all blend shapes from this ArrayMesh.

- clear_surfaces() -> void
  Removes all surfaces from this ArrayMesh.

- get_blend_shape_count() -> int [const]
  Returns the number of blend shapes that the ArrayMesh holds.

- get_blend_shape_name(index: int) -> StringName [const]
  Returns the name of the blend shape at this index.

- lightmap_unwrap(transform: Transform3D, texel_size: float) -> int (Error)
  Performs a UV unwrap on the ArrayMesh to prepare the mesh for lightmapping.

- regen_normal_maps() -> void
  Regenerates tangents for each of the ArrayMesh's surfaces.

- set_blend_shape_name(index: int, name: StringName) -> void
  Sets the name of the blend shape at this index.

- surface_find_by_name(name: String) -> int [const]
  Returns the index of the first surface with this name held within this ArrayMesh. If none are found, -1 is returned.

- surface_get_array_index_len(surf_idx: int) -> int [const]
  Returns the length in indices of the index array in the requested surface (see add_surface_from_arrays()).

- surface_get_array_len(surf_idx: int) -> int [const]
  Returns the length in vertices of the vertex array in the requested surface (see add_surface_from_arrays()).

- surface_get_format(surf_idx: int) -> int (Mesh.ArrayFormat) [const]
  Returns the format mask of the requested surface (see add_surface_from_arrays()).

- surface_get_name(surf_idx: int) -> String [const]
  Gets the name assigned to this surface.

- surface_get_primitive_type(surf_idx: int) -> int (Mesh.PrimitiveType) [const]
  Returns the primitive type of the requested surface (see add_surface_from_arrays()).

- surface_remove(surf_idx: int) -> void
  Removes the surface at the given index from the Mesh, shifting surfaces with higher index down by one.

- surface_set_name(surf_idx: int, name: String) -> void
  Sets a name for a given surface.

- surface_update_attribute_region(surf_idx: int, offset: int, data: PackedByteArray) -> void

- surface_update_skin_region(surf_idx: int, offset: int, data: PackedByteArray) -> void

- surface_update_vertex_region(surf_idx: int, offset: int, data: PackedByteArray) -> void

## Properties

- blend_shape_mode: int (Mesh.BlendShapeMode) = 1 [set set_blend_shape_mode; get get_blend_shape_mode]
  The blend shape mode.

- custom_aabb: AABB = AABB(0, 0, 0, 0, 0, 0) [set set_custom_aabb; get get_custom_aabb]
  Overrides the AABB with one defined by user for use with frustum culling. Especially useful to avoid unexpected culling when using a shader to offset vertices.

- shadow_mesh: ArrayMesh [set set_shadow_mesh; get get_shadow_mesh]
  An optional mesh which can be used for rendering shadows and the depth prepass. Can be used to increase performance by supplying a mesh with fused vertices and only vertex position data (without normals, UVs, colors, etc.). **Note:** This mesh must have exactly the same vertex positions as the source mesh (including the source mesh's LODs, if present). If vertex positions differ, then the mesh will not draw correctly.
