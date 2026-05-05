# ImageFormatLoader

## Meta

- Name: ImageFormatLoader
- Source: ImageFormatLoader.xml
- Inherits: RefCounted
- Inheritance Chain: ImageFormatLoader -> RefCounted -> Object

## Brief Description

Base class to add support for specific image formats.

## Description

The engine supports multiple image formats out of the box (PNG, SVG, JPEG, WebP to name a few), but you can choose to implement support for additional image formats by extending ImageFormatLoaderExtension.

## Constants

### Enum LoaderFlags

- FLAG_NONE = 0 [bitfield]

- FLAG_FORCE_LINEAR = 1 [bitfield]

- FLAG_CONVERT_COLORS = 2 [bitfield]
