# NinePatchRect

## Meta

- Name: NinePatchRect
- Source: NinePatchRect.xml
- Inherits: Control
- Inheritance Chain: NinePatchRect -> Control -> CanvasItem -> Node -> Object

## Brief Description

A control that displays a texture by keeping its corners intact, but tiling its edges and center.

## Description

Also known as 9-slice panels, NinePatchRect produces clean panels of any size based on a small texture. To do so, it splits the texture in a 3×3 grid. When you scale the node, it tiles the texture's edges horizontally or vertically, tiles the center on both axes, and leaves the corners unchanged.

## Quick Reference

```
[methods]
get_patch_margin(margin: int (Side)) -> int [const]
set_patch_margin(margin: int (Side), value: int) -> void

[properties]
axis_stretch_horizontal: int (NinePatchRect.AxisStretchMode) = 0
axis_stretch_vertical: int (NinePatchRect.AxisStretchMode) = 0
draw_center: bool = true
mouse_filter: int (Control.MouseFilter) = 2
patch_margin_bottom: int = 0
patch_margin_left: int = 0
patch_margin_right: int = 0
patch_margin_top: int = 0
region_rect: Rect2 = Rect2(0, 0, 0, 0)
texture: Texture2D
```

## Methods

- get_patch_margin(margin: int (Side)) -> int [const]
  Returns the size of the margin on the specified Side.

- set_patch_margin(margin: int (Side), value: int) -> void
  Sets the size of the margin on the specified Side to value pixels.

## Properties

- axis_stretch_horizontal: int (NinePatchRect.AxisStretchMode) = 0 [set set_h_axis_stretch_mode; get get_h_axis_stretch_mode]
  The stretch mode to use for horizontal stretching/tiling.

- axis_stretch_vertical: int (NinePatchRect.AxisStretchMode) = 0 [set set_v_axis_stretch_mode; get get_v_axis_stretch_mode]
  The stretch mode to use for vertical stretching/tiling.

- draw_center: bool = true [set set_draw_center; get is_draw_center_enabled]
  If true, draw the panel's center. Else, only draw the 9-slice's borders.

- mouse_filter: int (Control.MouseFilter) = 2 [set set_mouse_filter; get get_mouse_filter; override Control]

- patch_margin_bottom: int = 0 [set set_patch_margin; get get_patch_margin]
  The height of the 9-slice's bottom row. A margin of 16 means the 9-slice's bottom corners and side will have a height of 16 pixels. You can set all 4 margin values individually to create panels with non-uniform borders.

- patch_margin_left: int = 0 [set set_patch_margin; get get_patch_margin]
  The width of the 9-slice's left column. A margin of 16 means the 9-slice's left corners and side will have a width of 16 pixels. You can set all 4 margin values individually to create panels with non-uniform borders.

- patch_margin_right: int = 0 [set set_patch_margin; get get_patch_margin]
  The width of the 9-slice's right column. A margin of 16 means the 9-slice's right corners and side will have a width of 16 pixels. You can set all 4 margin values individually to create panels with non-uniform borders.

- patch_margin_top: int = 0 [set set_patch_margin; get get_patch_margin]
  The height of the 9-slice's top row. A margin of 16 means the 9-slice's top corners and side will have a height of 16 pixels. You can set all 4 margin values individually to create panels with non-uniform borders.

- region_rect: Rect2 = Rect2(0, 0, 0, 0) [set set_region_rect; get get_region_rect]
  Rectangular region of the texture to sample from. If you're working with an atlas, use this property to define the area the 9-slice should use. All other properties are relative to this one. If the rect is empty, NinePatchRect will use the whole texture.

- texture: Texture2D [set set_texture; get get_texture]
  The node's texture resource.

## Signals

- texture_changed()
  Emitted when the node's texture changes.

## Constants

### Enum AxisStretchMode

- AXIS_STRETCH_MODE_STRETCH = 0
  Stretches the center texture across the NinePatchRect. This may cause the texture to be distorted.

- AXIS_STRETCH_MODE_TILE = 1
  Repeats the center texture across the NinePatchRect. This won't cause any visible distortion. The texture must be seamless for this to work without displaying artifacts between edges.

- AXIS_STRETCH_MODE_TILE_FIT = 2
  Repeats the center texture across the NinePatchRect, but will also stretch the texture to make sure each tile is visible in full. This may cause the texture to be distorted, but less than AXIS_STRETCH_MODE_STRETCH. The texture must be seamless for this to work without displaying artifacts between edges.
