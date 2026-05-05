# CompressedTexture3D

## Meta

- Name: CompressedTexture3D
- Source: CompressedTexture3D.xml
- Inherits: Texture3D
- Inheritance Chain: CompressedTexture3D -> Texture3D -> Texture -> Resource -> RefCounted -> Object

## Brief Description

Texture with 3 dimensions, optionally compressed.

## Description

CompressedTexture3D is the VRAM-compressed counterpart of ImageTexture3D. The file extension for CompressedTexture3D files is .ctex3d. This file format is internal to Godot; it is created by importing other image formats with the import system. CompressedTexture3D uses VRAM compression, which allows to reduce memory usage on the GPU when rendering the texture. This also improves loading times, as VRAM-compressed textures are faster to load compared to textures using lossless compression. VRAM compression can exhibit noticeable artifacts and is intended to be used for 3D rendering, not 2D. See Texture3D for a general description of 3D textures.

## Quick Reference

```
[methods]
load(path: String) -> int (Error)

[properties]
load_path: String = ""
```

## Methods

- load(path: String) -> int (Error)
  Loads the texture from the specified path.

## Properties

- load_path: String = "" [set load; get get_load_path]
  The CompressedTexture3D's file path to a .ctex3d file.
