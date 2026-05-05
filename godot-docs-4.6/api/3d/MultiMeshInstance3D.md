# MultiMeshInstance3D

## Meta

- Name: MultiMeshInstance3D
- Source: MultiMeshInstance3D.xml
- Inherits: GeometryInstance3D
- Inheritance Chain: MultiMeshInstance3D -> GeometryInstance3D -> VisualInstance3D -> Node3D -> Node -> Object

## Brief Description

Node that instances a MultiMesh.

## Description

MultiMeshInstance3D is a specialized node to instance GeometryInstance3Ds based on a MultiMesh resource. This is useful to optimize the rendering of a high number of instances of a given mesh (for example trees in a forest or grass strands).

## Quick Reference

```
[properties]
multimesh: MultiMesh
```

## Tutorials

- [Using MultiMeshInstance]($DOCS_URL/tutorials/3d/using_multi_mesh_instance.html)
- [Optimization using MultiMeshes]($DOCS_URL/tutorials/performance/using_multimesh.html)
- [Animating thousands of fish with MultiMeshInstance]($DOCS_URL/tutorials/performance/vertex_animation/animating_thousands_of_fish.html)

## Properties

- multimesh: MultiMesh [set set_multimesh; get get_multimesh]
  The MultiMesh resource that will be used and shared among all instances of the MultiMeshInstance3D.
