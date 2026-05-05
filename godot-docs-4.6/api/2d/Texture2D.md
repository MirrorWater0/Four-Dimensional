# Texture2D

## Meta

- Name: Texture2D
- Source: Texture2D.xml
- Inherits: Texture
- Inheritance Chain: Texture2D -> Texture -> Resource -> RefCounted -> Object

## Brief Description

Texture for 2D and 3D.

## Description

A texture works by registering an image in the video hardware, which then can be used in 3D models or 2D Sprite2D or GUI Control. Textures are often created by loading them from a file. See @GDScript.load(). Texture2D is a base for other resources. It cannot be used directly. **Note:** The maximum texture size is 16384×16384 pixels due to graphics hardware limitations. Larger textures may fail to import.

## Quick Reference

```
[methods]
_draw(to_canvas_item: RID, pos: Vector2, modulate: Color, transpose: bool) -> void [virtual const]
_draw_rect(to_canvas_item: RID, rect: Rect2, tile: bool, modulate: Color, transpose: bool) -> void [virtual const]
_draw_rect_region(to_canvas_item: RID, rect: Rect2, src_rect: Rect2, modulate: Color, transpose: bool, clip_uv: bool) -> void [virtual const]
_get_height() -> int [virtual required const]
_get_width() -> int [virtual required const]
_has_alpha() -> bool [virtual const]
_is_pixel_opaque(x: int, y: int) -> bool [virtual const]
create_placeholder() -> Resource [const]
draw(canvas_item: RID, position: Vector2, modulate: Color = Color(1, 1, 1, 1), transpose: bool = false) -> void [const]
draw_rect(canvas_item: RID, rect: Rect2, tile: bool, modulate: Color = Color(1, 1, 1, 1), transpose: bool = false) -> void [const]
draw_rect_region(canvas_item: RID, rect: Rect2, src_rect: Rect2, modulate: Color = Color(1, 1, 1, 1), transpose: bool = false, clip_uv: bool = true) -> void [const]
get_height() -> int [const]
get_image() -> Image [const]
get_size() -> Vector2 [const]
get_width() -> int [const]
has_alpha() -> bool [const]
```

## Methods

- _draw(to_canvas_item: RID, pos: Vector2, modulate: Color, transpose: bool) -> void [virtual const]
  Called when the entire Texture2D is requested to be drawn over a CanvasItem, with the top-left offset specified in pos. modulate specifies a multiplier for the colors being drawn, while transpose specifies whether drawing should be performed in column-major order instead of row-major order (resulting in 90-degree clockwise rotation). **Note:** This is only used in 2D rendering, not 3D.

- _draw_rect(to_canvas_item: RID, rect: Rect2, tile: bool, modulate: Color, transpose: bool) -> void [virtual const]
  Called when the Texture2D is requested to be drawn onto CanvasItem's specified rect. modulate specifies a multiplier for the colors being drawn, while transpose specifies whether drawing should be performed in column-major order instead of row-major order (resulting in 90-degree clockwise rotation). **Note:** This is only used in 2D rendering, not 3D.

- _draw_rect_region(to_canvas_item: RID, rect: Rect2, src_rect: Rect2, modulate: Color, transpose: bool, clip_uv: bool) -> void [virtual const]
  Called when a part of the Texture2D specified by src_rect's coordinates is requested to be drawn onto CanvasItem's specified rect. modulate specifies a multiplier for the colors being drawn, while transpose specifies whether drawing should be performed in column-major order instead of row-major order (resulting in 90-degree clockwise rotation). **Note:** This is only used in 2D rendering, not 3D.

- _get_height() -> int [virtual required const]
  Called when the Texture2D's height is queried.

- _get_width() -> int [virtual required const]
  Called when the Texture2D's width is queried.

- _has_alpha() -> bool [virtual const]
  Called when the presence of an alpha channel in the Texture2D is queried.

- _is_pixel_opaque(x: int, y: int) -> bool [virtual const]
  Called when a pixel's opaque state in the Texture2D is queried at the specified (x, y) position.

- create_placeholder() -> Resource [const]
  Creates a placeholder version of this resource (PlaceholderTexture2D).

- draw(canvas_item: RID, position: Vector2, modulate: Color = Color(1, 1, 1, 1), transpose: bool = false) -> void [const]
  Draws the texture using a CanvasItem with the RenderingServer API at the specified position.

- draw_rect(canvas_item: RID, rect: Rect2, tile: bool, modulate: Color = Color(1, 1, 1, 1), transpose: bool = false) -> void [const]
  Draws the texture using a CanvasItem with the RenderingServer API.

- draw_rect_region(canvas_item: RID, rect: Rect2, src_rect: Rect2, modulate: Color = Color(1, 1, 1, 1), transpose: bool = false, clip_uv: bool = true) -> void [const]
  Draws a part of the texture using a CanvasItem with the RenderingServer API.

- get_height() -> int [const]
  Returns the texture height in pixels.

- get_image() -> Image [const]
  Returns an Image that is a copy of data from this Texture2D (a new Image is created each time). Images can be accessed and manipulated directly. **Note:** This will return null if this Texture2D is invalid. **Note:** This will fetch the texture data from the GPU, which might cause performance problems when overused. Avoid calling get_image() every frame, especially on large textures.

- get_size() -> Vector2 [const]
  Returns the texture size in pixels.

- get_width() -> int [const]
  Returns the texture width in pixels.

- has_alpha() -> bool [const]
  Returns true if this Texture2D has an alpha channel.
