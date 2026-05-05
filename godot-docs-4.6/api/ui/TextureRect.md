# TextureRect

## Meta

- Name: TextureRect
- Source: TextureRect.xml
- Inherits: Control
- Inheritance Chain: TextureRect -> Control -> CanvasItem -> Node -> Object

## Brief Description

A control that displays a texture.

## Description

A control that displays a texture, for example an icon inside a GUI. The texture's placement can be controlled with the stretch_mode property. It can scale, tile, or stay centered inside its bounding rectangle.

## Quick Reference

```
[properties]
expand_mode: int (TextureRect.ExpandMode) = 0
flip_h: bool = false
flip_v: bool = false
mouse_filter: int (Control.MouseFilter) = 1
stretch_mode: int (TextureRect.StretchMode) = 0
texture: Texture2D
```

## Tutorials

- [3D Voxel Demo](https://godotengine.org/asset-library/asset/2755)

## Properties

- expand_mode: int (TextureRect.ExpandMode) = 0 [set set_expand_mode; get get_expand_mode]
  Defines how minimum size is determined based on the texture's size.

- flip_h: bool = false [set set_flip_h; get is_flipped_h]
  If true, texture is flipped horizontally.

- flip_v: bool = false [set set_flip_v; get is_flipped_v]
  If true, texture is flipped vertically.

- mouse_filter: int (Control.MouseFilter) = 1 [set set_mouse_filter; get get_mouse_filter; override Control]

- stretch_mode: int (TextureRect.StretchMode) = 0 [set set_stretch_mode; get get_stretch_mode]
  Controls the texture's behavior when resizing the node's bounding rectangle.

- texture: Texture2D [set set_texture; get get_texture]
  The node's Texture2D resource.

## Constants

### Enum ExpandMode

- EXPAND_KEEP_SIZE = 0
  The minimum size will be equal to texture size, i.e. TextureRect can't be smaller than the texture.

- EXPAND_IGNORE_SIZE = 1
  The size of the texture won't be considered for minimum size calculation, so the TextureRect can be shrunk down past the texture size.

- EXPAND_FIT_WIDTH = 2
  The height of the texture will be ignored. Minimum width will be equal to the current height. Useful for horizontal layouts, e.g. inside HBoxContainer.

- EXPAND_FIT_WIDTH_PROPORTIONAL = 3
  Same as EXPAND_FIT_WIDTH, but keeps texture's aspect ratio.

- EXPAND_FIT_HEIGHT = 4
  The width of the texture will be ignored. Minimum height will be equal to the current width. Useful for vertical layouts, e.g. inside VBoxContainer.

- EXPAND_FIT_HEIGHT_PROPORTIONAL = 5
  Same as EXPAND_FIT_HEIGHT, but keeps texture's aspect ratio.

### Enum StretchMode

- STRETCH_SCALE = 0
  Scale to fit the node's bounding rectangle.

- STRETCH_TILE = 1
  Tile inside the node's bounding rectangle.

- STRETCH_KEEP = 2
  The texture keeps its original size and stays in the bounding rectangle's top-left corner.

- STRETCH_KEEP_CENTERED = 3
  The texture keeps its original size and stays centered in the node's bounding rectangle.

- STRETCH_KEEP_ASPECT = 4
  Scale the texture to fit the node's bounding rectangle, but maintain the texture's aspect ratio.

- STRETCH_KEEP_ASPECT_CENTERED = 5
  Scale the texture to fit the node's bounding rectangle, center it and maintain its aspect ratio.

- STRETCH_KEEP_ASPECT_COVERED = 6
  Scale the texture so that the shorter side fits the bounding rectangle. The other side clips to the node's limits.
