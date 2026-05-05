# CompressedTexture2D

## Meta

- Name: CompressedTexture2D
- Source: CompressedTexture2D.xml
- Inherits: Texture2D
- Inheritance Chain: CompressedTexture2D -> Texture2D -> Texture -> Resource -> RefCounted -> Object

## Brief Description

Texture with 2 dimensions, optionally compressed.

## Description

A texture that is loaded from a .ctex file. This file format is internal to Godot; it is created by importing other image formats with the import system. CompressedTexture2D can use one of 4 compression methods (including a lack of any compression): - Lossless (WebP or PNG, uncompressed on the GPU) - Lossy (WebP, uncompressed on the GPU) - VRAM Compressed (compressed on the GPU) - VRAM Uncompressed (uncompressed on the GPU) - Basis Universal (compressed on the GPU. Lower file sizes than VRAM Compressed, but slower to compress and lower quality than VRAM Compressed) Only **VRAM Compressed** actually reduces the memory usage on the GPU. The **Lossless** and **Lossy** compression methods will reduce the required storage on disk, but they will not reduce memory usage on the GPU as the texture is sent to the GPU uncompressed. Using **VRAM Compressed** also improves loading times, as VRAM-compressed textures are faster to load compared to textures using lossless or lossy compression. VRAM compression can exhibit noticeable artifacts and is intended to be used for 3D rendering, not 2D.

## Quick Reference

```
[methods]
load(path: String) -> int (Error)

[properties]
load_path: String = ""
resource_local_to_scene: bool = false
```

## Methods

- load(path: String) -> int (Error)
  Loads the texture from the specified path.

## Properties

- load_path: String = "" [set load; get get_load_path]
  The CompressedTexture2D's file path to a .ctex file.

- resource_local_to_scene: bool = false [set set_local_to_scene; get is_local_to_scene; override Resource]
