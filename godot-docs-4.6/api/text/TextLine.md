# TextLine

## Meta

- Name: TextLine
- Source: TextLine.xml
- Inherits: RefCounted
- Inheritance Chain: TextLine -> RefCounted -> Object

## Brief Description

Holds a line of text.

## Description

Abstraction over TextServer for handling a single line of text.

## Quick Reference

```
[methods]
add_object(key: Variant, size: Vector2, inline_align: int (InlineAlignment) = 5, length: int = 1, baseline: float = 0.0) -> bool
add_string(text: String, font: Font, font_size: int, language: String = "", meta: Variant = null) -> bool
clear() -> void
draw(canvas: RID, pos: Vector2, color: Color = Color(1, 1, 1, 1), oversampling: float = 0.0) -> void [const]
draw_outline(canvas: RID, pos: Vector2, outline_size: int = 1, color: Color = Color(1, 1, 1, 1), oversampling: float = 0.0) -> void [const]
duplicate() -> TextLine [const]
get_inferred_direction() -> int (TextServer.Direction) [const]
get_line_ascent() -> float [const]
get_line_descent() -> float [const]
get_line_underline_position() -> float [const]
get_line_underline_thickness() -> float [const]
get_line_width() -> float [const]
get_object_rect(key: Variant) -> Rect2 [const]
get_objects() -> Array [const]
get_rid() -> RID [const]
get_size() -> Vector2 [const]
has_object(key: Variant) -> bool [const]
hit_test(coords: float) -> int [const]
resize_object(key: Variant, size: Vector2, inline_align: int (InlineAlignment) = 5, baseline: float = 0.0) -> bool
set_bidi_override(override: Array) -> void
tab_align(tab_stops: PackedFloat32Array) -> void

[properties]
alignment: int (HorizontalAlignment) = 0
direction: int (TextServer.Direction) = 0
ellipsis_char: String = "…"
flags: int (TextServer.JustificationFlag) = 3
orientation: int (TextServer.Orientation) = 0
preserve_control: bool = false
preserve_invalid: bool = true
text_overrun_behavior: int (TextServer.OverrunBehavior) = 3
width: float = -1.0
```

## Methods

- add_object(key: Variant, size: Vector2, inline_align: int (InlineAlignment) = 5, length: int = 1, baseline: float = 0.0) -> bool
  Adds inline object to the text buffer, key must be unique. In the text, object is represented as length object replacement characters.

- add_string(text: String, font: Font, font_size: int, language: String = "", meta: Variant = null) -> bool
  Adds text span and font to draw it.

- clear() -> void
  Clears text line (removes text and inline objects).

- draw(canvas: RID, pos: Vector2, color: Color = Color(1, 1, 1, 1), oversampling: float = 0.0) -> void [const]
  Draw text into a canvas item at a given position, with color. pos specifies the top left corner of the bounding box. If oversampling is greater than zero, it is used as font oversampling factor, otherwise viewport oversampling settings are used.

- draw_outline(canvas: RID, pos: Vector2, outline_size: int = 1, color: Color = Color(1, 1, 1, 1), oversampling: float = 0.0) -> void [const]
  Draw text into a canvas item at a given position, with color. pos specifies the top left corner of the bounding box. If oversampling is greater than zero, it is used as font oversampling factor, otherwise viewport oversampling settings are used.

- duplicate() -> TextLine [const]
  Duplicates this TextLine.

- get_inferred_direction() -> int (TextServer.Direction) [const]
  Returns the text writing direction inferred by the BiDi algorithm.

- get_line_ascent() -> float [const]
  Returns the text ascent (number of pixels above the baseline for horizontal layout or to the left of baseline for vertical).

- get_line_descent() -> float [const]
  Returns the text descent (number of pixels below the baseline for horizontal layout or to the right of baseline for vertical).

- get_line_underline_position() -> float [const]
  Returns pixel offset of the underline below the baseline.

- get_line_underline_thickness() -> float [const]
  Returns thickness of the underline.

- get_line_width() -> float [const]
  Returns width (for horizontal layout) or height (for vertical) of the text.

- get_object_rect(key: Variant) -> Rect2 [const]
  Returns bounding rectangle of the inline object.

- get_objects() -> Array [const]
  Returns array of inline objects.

- get_rid() -> RID [const]
  Returns TextServer buffer RID.

- get_size() -> Vector2 [const]
  Returns size of the bounding box of the text.

- has_object(key: Variant) -> bool [const]
  Returns true if an object with key is embedded in this line.

- hit_test(coords: float) -> int [const]
  Returns caret character offset at the specified pixel offset at the baseline. This function always returns a valid position.

- resize_object(key: Variant, size: Vector2, inline_align: int (InlineAlignment) = 5, baseline: float = 0.0) -> bool
  Sets new size and alignment of embedded object.

- set_bidi_override(override: Array) -> void
  Overrides BiDi for the structured text. Override ranges should cover full source text without overlaps. BiDi algorithm will be used on each range separately.

- tab_align(tab_stops: PackedFloat32Array) -> void
  Aligns text to the given tab-stops.

## Properties

- alignment: int (HorizontalAlignment) = 0 [set set_horizontal_alignment; get get_horizontal_alignment]
  Sets text alignment within the line as if the line was horizontal.

- direction: int (TextServer.Direction) = 0 [set set_direction; get get_direction]
  Text writing direction.

- ellipsis_char: String = "…" [set set_ellipsis_char; get get_ellipsis_char]
  Ellipsis character used for text clipping.

- flags: int (TextServer.JustificationFlag) = 3 [set set_flags; get get_flags]
  Line alignment rules. For more info see TextServer.

- orientation: int (TextServer.Orientation) = 0 [set set_orientation; get get_orientation]
  Text orientation.

- preserve_control: bool = false [set set_preserve_control; get get_preserve_control]
  If set to true text will display control characters.

- preserve_invalid: bool = true [set set_preserve_invalid; get get_preserve_invalid]
  If set to true text will display invalid characters.

- text_overrun_behavior: int (TextServer.OverrunBehavior) = 3 [set set_text_overrun_behavior; get get_text_overrun_behavior]
  The clipping behavior when the text exceeds the text line's set width.

- width: float = -1.0 [set set_width; get get_width]
  Text line width.
