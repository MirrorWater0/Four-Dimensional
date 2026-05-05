# AtlasTexture

## Meta

- Name: AtlasTexture
- Source: AtlasTexture.xml
- Inherits: Texture2D
- Inheritance Chain: AtlasTexture -> Texture2D -> Texture -> Resource -> RefCounted -> Object

## Brief Description

A texture that crops out part of another Texture2D.

## Description

Texture2D resource that draws only part of its atlas texture, as defined by the region. An additional margin can also be set, which is useful for small adjustments. Multiple AtlasTexture resources can be cropped from the same atlas. Packing many smaller textures into a singular large texture helps to optimize video memory costs and render calls. **Note:** AtlasTexture cannot be used in an AnimatedTexture, and will not tile properly in nodes such as TextureRect or Sprite2D. To tile an AtlasTexture, modify its region instead.

## Quick Reference

```
[properties]
atlas: Texture2D
filter_clip: bool = false
margin: Rect2 = Rect2(0, 0, 0, 0)
region: Rect2 = Rect2(0, 0, 0, 0)
resource_local_to_scene: bool = false
```

## Properties

- atlas: Texture2D [set set_atlas; get get_atlas]
  The texture that contains the atlas. Can be any type inheriting from Texture2D, including another AtlasTexture.

- filter_clip: bool = false [set set_filter_clip; get has_filter_clip]
  If true, the area outside of the region is clipped to avoid bleeding of the surrounding texture pixels.

- margin: Rect2 = Rect2(0, 0, 0, 0) [set set_margin; get get_margin]
  The margin around the region. Useful for small adjustments. If the Rect2.size of this property ("w" and "h" in the editor) is set, the drawn texture is resized to fit within the margin.

- region: Rect2 = Rect2(0, 0, 0, 0) [set set_region; get get_region]
  The region used to draw the atlas. If either dimension of the region's size is 0, the value from atlas size will be used for that axis instead. **Note:** The image size is always an integer, so the actual region size is rounded down.

- resource_local_to_scene: bool = false [set set_local_to_scene; get is_local_to_scene; override Resource]
