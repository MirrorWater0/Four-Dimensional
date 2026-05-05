# StyleBoxTexture

## Meta

- Name: StyleBoxTexture
- Source: StyleBoxTexture.xml
- Inherits: StyleBox
- Inheritance Chain: StyleBoxTexture -> StyleBox -> Resource -> RefCounted -> Object

## Brief Description

A texture-based nine-patch StyleBox.

## Description

A texture-based nine-patch StyleBox, in a way similar to NinePatchRect. This stylebox performs a 3×3 scaling of a texture, where only the center cell is fully stretched. This makes it possible to design bordered styles regardless of the stylebox's size.

## Quick Reference

```
[methods]
get_expand_margin(margin: int (Side)) -> float [const]
get_texture_margin(margin: int (Side)) -> float [const]
set_expand_margin(margin: int (Side), size: float) -> void
set_expand_margin_all(size: float) -> void
set_texture_margin(margin: int (Side), size: float) -> void
set_texture_margin_all(size: float) -> void

[properties]
axis_stretch_horizontal: int (StyleBoxTexture.AxisStretchMode) = 0
axis_stretch_vertical: int (StyleBoxTexture.AxisStretchMode) = 0
draw_center: bool = true
expand_margin_bottom: float = 0.0
expand_margin_left: float = 0.0
expand_margin_right: float = 0.0
expand_margin_top: float = 0.0
modulate_color: Color = Color(1, 1, 1, 1)
region_rect: Rect2 = Rect2(0, 0, 0, 0)
texture: Texture2D
texture_margin_bottom: float = 0.0
texture_margin_left: float = 0.0
texture_margin_right: float = 0.0
texture_margin_top: float = 0.0
```

## Methods

- get_expand_margin(margin: int (Side)) -> float [const]
  Returns the expand margin size of the specified Side.

- get_texture_margin(margin: int (Side)) -> float [const]
  Returns the margin size of the specified Side.

- set_expand_margin(margin: int (Side), size: float) -> void
  Sets the expand margin to size pixels for the specified Side.

- set_expand_margin_all(size: float) -> void
  Sets the expand margin to size pixels for all sides.

- set_texture_margin(margin: int (Side), size: float) -> void
  Sets the margin to size pixels for the specified Side.

- set_texture_margin_all(size: float) -> void
  Sets the margin to size pixels for all sides.

## Properties

- axis_stretch_horizontal: int (StyleBoxTexture.AxisStretchMode) = 0 [set set_h_axis_stretch_mode; get get_h_axis_stretch_mode]
  Controls how the stylebox's texture will be stretched or tiled horizontally.

- axis_stretch_vertical: int (StyleBoxTexture.AxisStretchMode) = 0 [set set_v_axis_stretch_mode; get get_v_axis_stretch_mode]
  Controls how the stylebox's texture will be stretched or tiled vertically.

- draw_center: bool = true [set set_draw_center; get is_draw_center_enabled]
  If true, the nine-patch texture's center tile will be drawn.

- expand_margin_bottom: float = 0.0 [set set_expand_margin; get get_expand_margin]
  Expands the bottom margin of this style box when drawing, causing it to be drawn larger than requested.

- expand_margin_left: float = 0.0 [set set_expand_margin; get get_expand_margin]
  Expands the left margin of this style box when drawing, causing it to be drawn larger than requested.

- expand_margin_right: float = 0.0 [set set_expand_margin; get get_expand_margin]
  Expands the right margin of this style box when drawing, causing it to be drawn larger than requested.

- expand_margin_top: float = 0.0 [set set_expand_margin; get get_expand_margin]
  Expands the top margin of this style box when drawing, causing it to be drawn larger than requested.

- modulate_color: Color = Color(1, 1, 1, 1) [set set_modulate; get get_modulate]
  Modulates the color of the texture when this style box is drawn.

- region_rect: Rect2 = Rect2(0, 0, 0, 0) [set set_region_rect; get get_region_rect]
  The region to use from the texture. This is equivalent to first wrapping the texture in an AtlasTexture with the same region. If empty (Rect2(0, 0, 0, 0)), the whole texture is used.

- texture: Texture2D [set set_texture; get get_texture]
  The texture to use when drawing this style box.

- texture_margin_bottom: float = 0.0 [set set_texture_margin; get get_texture_margin]
  Increases the bottom margin of the 3×3 texture box. A higher value means more of the source texture is considered to be part of the bottom border of the 3×3 box. This is also the value used as fallback for StyleBox.content_margin_bottom if it is negative.

- texture_margin_left: float = 0.0 [set set_texture_margin; get get_texture_margin]
  Increases the left margin of the 3×3 texture box. A higher value means more of the source texture is considered to be part of the left border of the 3×3 box. This is also the value used as fallback for StyleBox.content_margin_left if it is negative.

- texture_margin_right: float = 0.0 [set set_texture_margin; get get_texture_margin]
  Increases the right margin of the 3×3 texture box. A higher value means more of the source texture is considered to be part of the right border of the 3×3 box. This is also the value used as fallback for StyleBox.content_margin_right if it is negative.

- texture_margin_top: float = 0.0 [set set_texture_margin; get get_texture_margin]
  Increases the top margin of the 3×3 texture box. A higher value means more of the source texture is considered to be part of the top border of the 3×3 box. This is also the value used as fallback for StyleBox.content_margin_top if it is negative.

## Constants

### Enum AxisStretchMode

- AXIS_STRETCH_MODE_STRETCH = 0
  Stretch the stylebox's texture. This results in visible distortion unless the texture size matches the stylebox's size perfectly.

- AXIS_STRETCH_MODE_TILE = 1
  Repeats the stylebox's texture to match the stylebox's size according to the nine-patch system.

- AXIS_STRETCH_MODE_TILE_FIT = 2
  Repeats the stylebox's texture to match the stylebox's size according to the nine-patch system. Unlike AXIS_STRETCH_MODE_TILE, the texture may be slightly stretched to make the nine-patch texture tile seamlessly.
