# TextureLayered

## Meta

- Name: TextureLayered
- Source: TextureLayered.xml
- Inherits: Texture
- Inheritance Chain: TextureLayered -> Texture -> Resource -> RefCounted -> Object

## Brief Description

Base class for texture types which contain the data of multiple Images. Each image is of the same size and format.

## Description

Base class for ImageTextureLayered and CompressedTextureLayered. Cannot be used directly, but contains all the functions necessary for accessing the derived resource types. See also Texture3D. Data is set on a per-layer basis. For Texture2DArrays, the layer specifies the array layer. All images need to have the same width, height and number of mipmap levels. A TextureLayered can be loaded with ResourceLoader.load(). Internally, Godot maps these files to their respective counterparts in the target rendering driver (Vulkan, OpenGL3).

## Quick Reference

```
[methods]
_get_format() -> int (Image.Format) [virtual required const]
_get_height() -> int [virtual required const]
_get_layer_data(layer_index: int) -> Image [virtual required const]
_get_layered_type() -> int [virtual required const]
_get_layers() -> int [virtual required const]
_get_width() -> int [virtual required const]
_has_mipmaps() -> bool [virtual required const]
get_format() -> int (Image.Format) [const]
get_height() -> int [const]
get_layer_data(layer: int) -> Image [const]
get_layered_type() -> int (TextureLayered.LayeredType) [const]
get_layers() -> int [const]
get_width() -> int [const]
has_mipmaps() -> bool [const]
```

## Methods

- _get_format() -> int (Image.Format) [virtual required const]
  Called when the TextureLayered's format is queried.

- _get_height() -> int [virtual required const]
  Called when the TextureLayered's height is queried.

- _get_layer_data(layer_index: int) -> Image [virtual required const]
  Called when the data for a layer in the TextureLayered is queried.

- _get_layered_type() -> int [virtual required const]
  Called when the layers' type in the TextureLayered is queried.

- _get_layers() -> int [virtual required const]
  Called when the number of layers in the TextureLayered is queried.

- _get_width() -> int [virtual required const]
  Called when the TextureLayered's width queried.

- _has_mipmaps() -> bool [virtual required const]
  Called when the presence of mipmaps in the TextureLayered is queried.

- get_format() -> int (Image.Format) [const]
  Returns the current format being used by this texture.

- get_height() -> int [const]
  Returns the height of the texture in pixels. Height is typically represented by the Y axis.

- get_layer_data(layer: int) -> Image [const]
  Returns an Image resource with the data from specified layer.

- get_layered_type() -> int (TextureLayered.LayeredType) [const]
  Returns the TextureLayered's type. The type determines how the data is accessed, with cubemaps having special types.

- get_layers() -> int [const]
  Returns the number of referenced Images.

- get_width() -> int [const]
  Returns the width of the texture in pixels. Width is typically represented by the X axis.

- has_mipmaps() -> bool [const]
  Returns true if the layers have generated mipmaps.

## Constants

### Enum LayeredType

- LAYERED_TYPE_2D_ARRAY = 0
  Texture is a generic Texture2DArray.

- LAYERED_TYPE_CUBEMAP = 1
  Texture is a Cubemap, with each side in its own layer (6 in total).

- LAYERED_TYPE_CUBEMAP_ARRAY = 2
  Texture is a CubemapArray, with each cubemap being made of 6 layers.
