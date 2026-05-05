# StandardMaterial3D

## Meta

- Name: StandardMaterial3D
- Source: StandardMaterial3D.xml
- Inherits: BaseMaterial3D
- Inheritance Chain: StandardMaterial3D -> BaseMaterial3D -> Material -> Resource -> RefCounted -> Object

## Brief Description

A PBR (Physically Based Rendering) material to be used on 3D objects.

## Description

StandardMaterial3D's properties are inherited from BaseMaterial3D. StandardMaterial3D uses separate textures for ambient occlusion, roughness and metallic maps. To use a single ORM map for all 3 textures, use an ORMMaterial3D instead.

## Tutorials

- [Standard Material 3D and ORM Material 3D]($DOCS_URL/tutorials/3d/standard_material_3d.html)
