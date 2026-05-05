# ResourceImporterBitMap

## Meta

- Name: ResourceImporterBitMap
- Source: ResourceImporterBitMap.xml
- Inherits: ResourceImporter
- Inheritance Chain: ResourceImporterBitMap -> ResourceImporter -> RefCounted -> Object

## Brief Description

Imports a BitMap resource (2D array of boolean values).

## Description

BitMap resources are typically used as click masks in TextureButton and TouchScreenButton.

## Quick Reference

```
[properties]
create_from: int = 0
threshold: float = 0.5
```

## Tutorials

- [Importing images]($DOCS_URL/tutorials/assets_pipeline/importing_images.html)

## Properties

- create_from: int = 0
  The data source to use for generating the bitmap. **Black & White:** Pixels whose HSV value is greater than the threshold will be considered as "enabled" (bit is true). If the pixel is lower than or equal to the threshold, it will be considered as "disabled" (bit is false). **Alpha:** Pixels whose alpha value is greater than the threshold will be considered as "enabled" (bit is true). If the pixel is lower than or equal to the threshold, it will be considered as "disabled" (bit is false).

- threshold: float = 0.5
  The threshold to use to determine which bits should be considered enabled or disabled. See also create_from.
