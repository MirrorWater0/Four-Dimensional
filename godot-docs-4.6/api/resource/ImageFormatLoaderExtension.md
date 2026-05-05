# ImageFormatLoaderExtension

## Meta

- Name: ImageFormatLoaderExtension
- Source: ImageFormatLoaderExtension.xml
- Inherits: ImageFormatLoader
- Inheritance Chain: ImageFormatLoaderExtension -> ImageFormatLoader -> RefCounted -> Object

## Brief Description

Base class for creating ImageFormatLoader extensions (adding support for extra image formats).

## Description

The engine supports multiple image formats out of the box (PNG, SVG, JPEG, WebP to name a few), but you can choose to implement support for additional image formats by extending this class. Be sure to respect the documented return types and values. You should create an instance of it, and call add_format_loader() to register that loader during the initialization phase.

## Quick Reference

```
[methods]
_get_recognized_extensions() -> PackedStringArray [virtual const]
_load_image(image: Image, fileaccess: FileAccess, flags: int (ImageFormatLoader.LoaderFlags), scale: float) -> int (Error) [virtual]
add_format_loader() -> void
remove_format_loader() -> void
```

## Methods

- _get_recognized_extensions() -> PackedStringArray [virtual const]
  Returns the list of file extensions for this image format. Files with the given extensions will be treated as image file and loaded using this class.

- _load_image(image: Image, fileaccess: FileAccess, flags: int (ImageFormatLoader.LoaderFlags), scale: float) -> int (Error) [virtual]
  Loads the content of fileaccess into the provided image.

- add_format_loader() -> void
  Add this format loader to the engine, allowing it to recognize the file extensions returned by _get_recognized_extensions().

- remove_format_loader() -> void
  Remove this format loader from the engine.
