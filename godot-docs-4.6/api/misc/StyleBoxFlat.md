# StyleBoxFlat

## Meta

- Name: StyleBoxFlat
- Source: StyleBoxFlat.xml
- Inherits: StyleBox
- Inheritance Chain: StyleBoxFlat -> StyleBox -> Resource -> RefCounted -> Object

## Brief Description

A customizable StyleBox that doesn't use a texture.

## Description

By configuring various properties of this style box, you can achieve many common looks without the need of a texture. This includes optionally rounded borders, antialiasing, shadows, and skew. Setting corner radius to high values is allowed. As soon as corners overlap, the stylebox will switch to a relative system: [codeblock lang=text] height = 30 corner_radius_top_left = 50 corner_radius_bottom_left = 100

```
The relative system now would take the 1:2 ratio of the two left corners to calculate the actual corner width. Both corners added will **never** be more than the height. Result:
[codeblock lang=text]
corner_radius_top_left: 10
corner_radius_bottom_left: 20
```

## Quick Reference

```
[methods]
get_border_width(margin: int (Side)) -> int [const]
get_border_width_min() -> int [const]
get_corner_radius(corner: int (Corner)) -> int [const]
get_expand_margin(margin: int (Side)) -> float [const]
set_border_width(margin: int (Side), width: int) -> void
set_border_width_all(width: int) -> void
set_corner_radius(corner: int (Corner), radius: int) -> void
set_corner_radius_all(radius: int) -> void
set_expand_margin(margin: int (Side), size: float) -> void
set_expand_margin_all(size: float) -> void

[properties]
anti_aliasing: bool = true
anti_aliasing_size: float = 1.0
bg_color: Color = Color(0.6, 0.6, 0.6, 1)
border_blend: bool = false
border_color: Color = Color(0.8, 0.8, 0.8, 1)
border_width_bottom: int = 0
border_width_left: int = 0
border_width_right: int = 0
border_width_top: int = 0
corner_detail: int = 8
corner_radius_bottom_left: int = 0
corner_radius_bottom_right: int = 0
corner_radius_top_left: int = 0
corner_radius_top_right: int = 0
draw_center: bool = true
expand_margin_bottom: float = 0.0
expand_margin_left: float = 0.0
expand_margin_right: float = 0.0
expand_margin_top: float = 0.0
shadow_color: Color = Color(0, 0, 0, 0.6)
shadow_offset: Vector2 = Vector2(0, 0)
shadow_size: int = 0
skew: Vector2 = Vector2(0, 0)
```

## Methods

- get_border_width(margin: int (Side)) -> int [const]
  Returns the specified Side's border width.

- get_border_width_min() -> int [const]
  Returns the smallest border width out of all four borders.

- get_corner_radius(corner: int (Corner)) -> int [const]
  Returns the given corner's radius.

- get_expand_margin(margin: int (Side)) -> float [const]
  Returns the size of the specified Side's expand margin.

- set_border_width(margin: int (Side), width: int) -> void
  Sets the specified Side's border width to width pixels.

- set_border_width_all(width: int) -> void
  Sets the border width to width pixels for all sides.

- set_corner_radius(corner: int (Corner), radius: int) -> void
  Sets the corner radius to radius pixels for the given corner.

- set_corner_radius_all(radius: int) -> void
  Sets the corner radius to radius pixels for all corners.

- set_expand_margin(margin: int (Side), size: float) -> void
  Sets the expand margin to size pixels for the specified Side.

- set_expand_margin_all(size: float) -> void
  Sets the expand margin to size pixels for all sides.

## Properties

- anti_aliasing: bool = true [set set_anti_aliased; get is_anti_aliased]
  Antialiasing draws a small ring around the edges, which fades to transparency. As a result, edges look much smoother. This is only noticeable when using rounded corners or skew. **Note:** When using beveled corners with 45-degree angles (corner_detail = 1), it is recommended to set anti_aliasing to false to ensure crisp visuals and avoid possible visual glitches.

- anti_aliasing_size: float = 1.0 [set set_aa_size; get get_aa_size]
  This changes the size of the antialiasing effect. 1.0 is recommended for an optimal result at 100% scale, identical to how rounded rectangles are rendered in web browsers and most vector drawing software. **Note:** Higher values may produce a blur effect but can also create undesired artifacts on small boxes with large-radius corners.

- bg_color: Color = Color(0.6, 0.6, 0.6, 1) [set set_bg_color; get get_bg_color]
  The background color of the stylebox.

- border_blend: bool = false [set set_border_blend; get get_border_blend]
  If true, the border will fade into the background color.

- border_color: Color = Color(0.8, 0.8, 0.8, 1) [set set_border_color; get get_border_color]
  Sets the color of the border.

- border_width_bottom: int = 0 [set set_border_width; get get_border_width]
  Border width for the bottom border.

- border_width_left: int = 0 [set set_border_width; get get_border_width]
  Border width for the left border.

- border_width_right: int = 0 [set set_border_width; get get_border_width]
  Border width for the right border.

- border_width_top: int = 0 [set set_border_width; get get_border_width]
  Border width for the top border.

- corner_detail: int = 8 [set set_corner_detail; get get_corner_detail]
  This sets the number of vertices used for each corner. Higher values result in rounder corners but take more processing power to compute. When choosing a value, you should take the corner radius (set_corner_radius_all()) into account. For corner radii less than 10, 4 or 5 should be enough. For corner radii less than 30, values between 8 and 12 should be enough. A corner detail of 1 will result in chamfered corners instead of rounded corners, which is useful for some artistic effects.

- corner_radius_bottom_left: int = 0 [set set_corner_radius; get get_corner_radius]
  The bottom-left corner's radius. If 0, the corner is not rounded.

- corner_radius_bottom_right: int = 0 [set set_corner_radius; get get_corner_radius]
  The bottom-right corner's radius. If 0, the corner is not rounded.

- corner_radius_top_left: int = 0 [set set_corner_radius; get get_corner_radius]
  The top-left corner's radius. If 0, the corner is not rounded.

- corner_radius_top_right: int = 0 [set set_corner_radius; get get_corner_radius]
  The top-right corner's radius. If 0, the corner is not rounded.

- draw_center: bool = true [set set_draw_center; get is_draw_center_enabled]
  Toggles drawing of the inner part of the stylebox.

- expand_margin_bottom: float = 0.0 [set set_expand_margin; get get_expand_margin]
  Expands the stylebox outside of the control rect on the bottom edge. Useful in combination with border_width_bottom to draw a border outside the control rect. **Note:** Unlike StyleBox.content_margin_bottom, expand_margin_bottom does *not* affect the size of the clickable area for Controls. This can negatively impact usability if used wrong, as the user may try to click an area of the StyleBox that cannot actually receive clicks.

- expand_margin_left: float = 0.0 [set set_expand_margin; get get_expand_margin]
  Expands the stylebox outside of the control rect on the left edge. Useful in combination with border_width_left to draw a border outside the control rect. **Note:** Unlike StyleBox.content_margin_left, expand_margin_left does *not* affect the size of the clickable area for Controls. This can negatively impact usability if used wrong, as the user may try to click an area of the StyleBox that cannot actually receive clicks.

- expand_margin_right: float = 0.0 [set set_expand_margin; get get_expand_margin]
  Expands the stylebox outside of the control rect on the right edge. Useful in combination with border_width_right to draw a border outside the control rect. **Note:** Unlike StyleBox.content_margin_right, expand_margin_right does *not* affect the size of the clickable area for Controls. This can negatively impact usability if used wrong, as the user may try to click an area of the StyleBox that cannot actually receive clicks.

- expand_margin_top: float = 0.0 [set set_expand_margin; get get_expand_margin]
  Expands the stylebox outside of the control rect on the top edge. Useful in combination with border_width_top to draw a border outside the control rect. **Note:** Unlike StyleBox.content_margin_top, expand_margin_top does *not* affect the size of the clickable area for Controls. This can negatively impact usability if used wrong, as the user may try to click an area of the StyleBox that cannot actually receive clicks.

- shadow_color: Color = Color(0, 0, 0, 0.6) [set set_shadow_color; get get_shadow_color]
  The color of the shadow. This has no effect if shadow_size is lower than 1.

- shadow_offset: Vector2 = Vector2(0, 0) [set set_shadow_offset; get get_shadow_offset]
  The shadow offset in pixels. Adjusts the position of the shadow relatively to the stylebox.

- shadow_size: int = 0 [set set_shadow_size; get get_shadow_size]
  The shadow size in pixels.

- skew: Vector2 = Vector2(0, 0) [set set_skew; get get_skew]
  If set to a non-zero value on either axis, skew distorts the StyleBox horizontally and/or vertically. This can be used for "futuristic"-style UIs. Positive values skew the StyleBox towards the right (X axis) and upwards (Y axis), while negative values skew the StyleBox towards the left (X axis) and downwards (Y axis). **Note:** To ensure text does not touch the StyleBox's edges, consider increasing the StyleBox's content margin (see StyleBox.content_margin_bottom). It is preferable to increase the content margin instead of the expand margin (see expand_margin_bottom), as increasing the expand margin does not increase the size of the clickable area for Controls.
