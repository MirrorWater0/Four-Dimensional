# PrimitiveMesh

## Meta

- Name: PrimitiveMesh
- Source: PrimitiveMesh.xml
- Inherits: Mesh
- Inheritance Chain: PrimitiveMesh -> Mesh -> Resource -> RefCounted -> Object

## Brief Description

Base class for all primitive meshes. Handles applying a Material to a primitive mesh.

## Description

Base class for all primitive meshes. Handles applying a Material to a primitive mesh. Examples include BoxMesh, CapsuleMesh, CylinderMesh, PlaneMesh, PrismMesh, and SphereMesh.

## Quick Reference

```
[methods]
_create_mesh_array() -> Array [virtual const]
get_mesh_arrays() -> Array [const]
request_update() -> void

[properties]
add_uv2: bool = false
custom_aabb: AABB = AABB(0, 0, 0, 0, 0, 0)
flip_faces: bool = false
material: Material
uv2_padding: float = 2.0
```

## Methods

- _create_mesh_array() -> Array [virtual const]
  Override this method to customize how this primitive mesh should be generated. Should return an Array where each element is another Array of values required for the mesh (see the Mesh.ArrayType constants).

- get_mesh_arrays() -> Array [const]
  Returns the mesh arrays used to make up the surface of this primitive mesh. **Example:** Pass the result to ArrayMesh.add_surface_from_arrays() to create a new surface:


```
  var c = CylinderMesh.new()
  var arr_mesh = ArrayMesh.new()
  arr_mesh.add_surface_from_arrays(Mesh.PRIMITIVE_TRIANGLES, c.get_mesh_arrays())

```

```
  var c = new CylinderMesh();
  var arrMesh = new ArrayMesh();
  arrMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, c.GetMeshArrays());

```

- request_update() -> void
  Request an update of this primitive mesh based on its properties.

## Properties

- add_uv2: bool = false [set set_add_uv2; get get_add_uv2]
  If set, generates UV2 UV coordinates applying a padding using the uv2_padding setting. UV2 is needed for lightmapping.

- custom_aabb: AABB = AABB(0, 0, 0, 0, 0, 0) [set set_custom_aabb; get get_custom_aabb]
  Overrides the AABB with one defined by user for use with frustum culling. Especially useful to avoid unexpected culling when using a shader to offset vertices.

- flip_faces: bool = false [set set_flip_faces; get get_flip_faces]
  If true, the order of the vertices in each triangle is reversed, resulting in the backside of the mesh being drawn. This gives the same result as using BaseMaterial3D.CULL_FRONT in BaseMaterial3D.cull_mode.

- material: Material [set set_material; get get_material]
  The current Material of the primitive mesh.

- uv2_padding: float = 2.0 [set set_uv2_padding; get get_uv2_padding]
  If add_uv2 is set, specifies the padding in pixels applied along seams of the mesh. Lower padding values allow making better use of the lightmap texture (resulting in higher texel density), but may introduce visible lightmap bleeding along edges. If the size of the lightmap texture can't be determined when generating the mesh, UV2 is calculated assuming a texture size of 1024x1024.
