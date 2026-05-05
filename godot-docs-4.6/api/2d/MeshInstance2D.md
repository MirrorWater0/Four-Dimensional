# MeshInstance2D

## Meta

- Name: MeshInstance2D
- Source: MeshInstance2D.xml
- Inherits: Node2D
- Inheritance Chain: MeshInstance2D -> Node2D -> CanvasItem -> Node -> Object

## Brief Description

Node used for displaying a Mesh in 2D.

## Description

Node used for displaying a Mesh in 2D. This can be faster to render compared to displaying a Sprite2D node with large transparent areas, especially if the node takes up a lot of space on screen at high viewport resolutions. This is because using a mesh designed to fit the sprite's opaque areas will reduce GPU fill rate utilization (at the cost of increased vertex processing utilization). When a Mesh has to be instantiated more than thousands of times close to each other, consider using a MultiMesh in a MultiMeshInstance2D instead. A MeshInstance2D can be created from an existing Sprite2D via a tool in the editor toolbar. Select the Sprite2D node, then choose **Sprite2D > Convert to MeshInstance2D** at the top of the 2D editor viewport.

## Quick Reference

```
[properties]
mesh: Mesh
texture: Texture2D
```

## Tutorials

- [2D meshes]($DOCS_URL/tutorials/2d/2d_meshes.html)

## Properties

- mesh: Mesh [set set_mesh; get get_mesh]
  The Mesh that will be drawn by the MeshInstance2D.

- texture: Texture2D [set set_texture; get get_texture]
  The Texture2D that will be used if using the default CanvasItemMaterial. Can be accessed as TEXTURE in CanvasItem shader.

## Signals

- texture_changed()
  Emitted when the texture is changed.
