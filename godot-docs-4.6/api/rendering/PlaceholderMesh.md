# PlaceholderMesh

## Meta

- Name: PlaceholderMesh
- Source: PlaceholderMesh.xml
- Inherits: Mesh
- Inheritance Chain: PlaceholderMesh -> Mesh -> Resource -> RefCounted -> Object

## Brief Description

Placeholder class for a mesh.

## Description

This class is used when loading a project that uses a Mesh subclass in 2 conditions: - When running the project exported in dedicated server mode, only the texture's dimensions are kept (as they may be relied upon for gameplay purposes or positioning of other elements). This allows reducing the exported PCK's size significantly. - When this subclass is missing due to using a different engine version or build (e.g. modules disabled).

## Quick Reference

```
[properties]
aabb: AABB = AABB(0, 0, 0, 0, 0, 0)
```

## Properties

- aabb: AABB = AABB(0, 0, 0, 0, 0, 0) [set set_aabb; get get_aabb]
  The smallest AABB enclosing this mesh in local space.
