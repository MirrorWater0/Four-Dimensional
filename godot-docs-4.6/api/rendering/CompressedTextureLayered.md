# CompressedTextureLayered

## Meta

- Name: CompressedTextureLayered
- Source: CompressedTextureLayered.xml
- Inherits: TextureLayered
- Inheritance Chain: CompressedTextureLayered -> TextureLayered -> Texture -> Resource -> RefCounted -> Object

## Brief Description

Base class for texture arrays that can optionally be compressed.

## Description

Base class for CompressedTexture2DArray and CompressedTexture3D. Cannot be used directly, but contains all the functions necessary for accessing the derived resource types. See also TextureLayered.

## Quick Reference

```
[methods]
load(path: String) -> int (Error)

[properties]
load_path: String = ""
```

## Methods

- load(path: String) -> int (Error)
  Loads the texture at path.

## Properties

- load_path: String = "" [set load; get get_load_path]
  The path the texture should be loaded from.
