# MeshConvexDecompositionSettings

## Meta

- Name: MeshConvexDecompositionSettings
- Source: MeshConvexDecompositionSettings.xml
- Inherits: RefCounted
- Inheritance Chain: MeshConvexDecompositionSettings -> RefCounted -> Object

## Brief Description

Parameters to be used with a Mesh convex decomposition operation.

## Description

Parameters to be used with a Mesh convex decomposition operation.

## Quick Reference

```
[properties]
convex_hull_approximation: bool = true
convex_hull_downsampling: int = 4
max_concavity: float = 1.0
max_convex_hulls: int = 1
max_num_vertices_per_convex_hull: int = 32
min_volume_per_convex_hull: float = 0.0001
mode: int (MeshConvexDecompositionSettings.Mode) = 0
normalize_mesh: bool = false
plane_downsampling: int = 4
project_hull_vertices: bool = true
resolution: int = 10000
revolution_axes_clipping_bias: float = 0.05
symmetry_planes_clipping_bias: float = 0.05
```

## Properties

- convex_hull_approximation: bool = true [set set_convex_hull_approximation; get get_convex_hull_approximation]
  If true, uses approximation for computing convex hulls.

- convex_hull_downsampling: int = 4 [set set_convex_hull_downsampling; get get_convex_hull_downsampling]
  Controls the precision of the convex-hull generation process during the clipping plane selection stage. Ranges from 1 to 16.

- max_concavity: float = 1.0 [set set_max_concavity; get get_max_concavity]
  Maximum concavity. Ranges from 0.0 to 1.0.

- max_convex_hulls: int = 1 [set set_max_convex_hulls; get get_max_convex_hulls]
  The maximum number of convex hulls to produce from the merge operation.

- max_num_vertices_per_convex_hull: int = 32 [set set_max_num_vertices_per_convex_hull; get get_max_num_vertices_per_convex_hull]
  Controls the maximum number of triangles per convex-hull. Ranges from 4 to 1024.

- min_volume_per_convex_hull: float = 0.0001 [set set_min_volume_per_convex_hull; get get_min_volume_per_convex_hull]
  Controls the adaptive sampling of the generated convex-hulls. Ranges from 0.0 to 0.01.

- mode: int (MeshConvexDecompositionSettings.Mode) = 0 [set set_mode; get get_mode]
  Mode for the approximate convex decomposition.

- normalize_mesh: bool = false [set set_normalize_mesh; get get_normalize_mesh]
  If true, normalizes the mesh before applying the convex decomposition.

- plane_downsampling: int = 4 [set set_plane_downsampling; get get_plane_downsampling]
  Controls the granularity of the search for the "best" clipping plane. Ranges from 1 to 16.

- project_hull_vertices: bool = true [set set_project_hull_vertices; get get_project_hull_vertices]
  If true, projects output convex hull vertices onto the original source mesh to increase floating-point accuracy of the results.

- resolution: int = 10000 [set set_resolution; get get_resolution]
  Maximum number of voxels generated during the voxelization stage.

- revolution_axes_clipping_bias: float = 0.05 [set set_revolution_axes_clipping_bias; get get_revolution_axes_clipping_bias]
  Controls the bias toward clipping along revolution axes. Ranges from 0.0 to 1.0.

- symmetry_planes_clipping_bias: float = 0.05 [set set_symmetry_planes_clipping_bias; get get_symmetry_planes_clipping_bias]
  Controls the bias toward clipping along symmetry planes. Ranges from 0.0 to 1.0.

## Constants

### Enum Mode

- CONVEX_DECOMPOSITION_MODE_VOXEL = 0
  Constant for voxel-based approximate convex decomposition.

- CONVEX_DECOMPOSITION_MODE_TETRAHEDRON = 1
  Constant for tetrahedron-based approximate convex decomposition.
