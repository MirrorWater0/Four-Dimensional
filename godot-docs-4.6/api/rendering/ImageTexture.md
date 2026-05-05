# ImageTexture

## Meta

- Name: ImageTexture
- Source: ImageTexture.xml
- Inherits: Texture2D
- Inheritance Chain: ImageTexture -> Texture2D -> Texture -> Resource -> RefCounted -> Object

## Brief Description

A Texture2D based on an Image.

## Description

A Texture2D based on an Image. For an image to be displayed, an ImageTexture has to be created from it using the create_from_image() method:

```
var image = Image.load_from_file("res://icon.svg")
var texture = ImageTexture.create_from_image(image)
$Sprite2D.texture = texture
```

This way, textures can be created at run-time by loading images both from within the editor and externally. **Warning:** Prefer to load imported textures with @GDScript.load() over loading them from within the filesystem dynamically with Image.load(), as it may not work in exported projects:

```
var texture = load("res://icon.svg")
$Sprite2D.texture = texture
```

This is because images have to be imported as a CompressedTexture2D first to be loaded with @GDScript.load(). If you'd still like to load an image file just like any other Resource, import it as an Image resource instead, and then load it normally using the @GDScript.load() method. **Note:** The image can be retrieved from an imported texture using the Texture2D.get_image() method, which returns a copy of the image:

```
var texture = load("res://icon.svg")
var image = texture.get_image()
```

An ImageTexture is not meant to be operated from within the editor interface directly, and is mostly useful for rendering images on screen dynamically via code. If you need to generate images procedurally from within the editor, consider saving and importing images as custom texture resources implementing a new EditorImportPlugin. **Note:** The maximum texture size is 16384×16384 pixels due to graphics hardware limitations.

## Quick Reference

```
[methods]
create_from_image(image: Image) -> ImageTexture [static]
get_format() -> int (Image.Format) [const]
set_image(image: Image) -> void
set_size_override(size: Vector2i) -> void
update(image: Image) -> void

[properties]
resource_local_to_scene: bool = false
```

## Tutorials

- [Importing images]($DOCS_URL/tutorials/assets_pipeline/importing_images.html)

## Methods

- create_from_image(image: Image) -> ImageTexture [static]
  Creates a new ImageTexture and initializes it by allocating and setting the data from an Image.

- get_format() -> int (Image.Format) [const]
  Returns the format of the texture.

- set_image(image: Image) -> void
  Replaces the texture's data with a new Image. This will re-allocate new memory for the texture. If you want to update the image, but don't need to change its parameters (format, size), use update() instead for better performance.

- set_size_override(size: Vector2i) -> void
  Resizes the texture to the specified dimensions.

- update(image: Image) -> void
  Replaces the texture's data with a new Image. **Note:** The texture has to be created using create_from_image() or initialized first with the set_image() method before it can be updated. The new image dimensions, format, and mipmaps configuration should match the existing texture's image configuration. Use this method over set_image() if you need to update the texture frequently, which is faster than allocating additional memory for a new texture each time.

## Properties

- resource_local_to_scene: bool = false [set set_local_to_scene; get is_local_to_scene; override Resource]
