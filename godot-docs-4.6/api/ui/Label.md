# Label

## Meta

- Name: Label
- Source: Label.xml
- Inherits: Control
- Inheritance Chain: Label -> Control -> CanvasItem -> Node -> Object

## Brief Description

A control for displaying plain text.

## Description

A control for displaying plain text. It gives you control over the horizontal and vertical alignment and can wrap the text inside the node's bounding rectangle. It doesn't support bold, italics, or other rich text formatting. For that, use RichTextLabel instead. **Note:** A single Label node is not designed to display huge amounts of text. To display large amounts of text in a single node, consider using RichTextLabel instead as it supports features like an integrated scroll bar and threading. RichTextLabel generally performs better when displaying large amounts of text (several pages or more).

## Quick Reference

```
[methods]
get_character_bounds(pos: int) -> Rect2 [const]
get_line_count() -> int [const]
get_line_height(line: int = -1) -> int [const]
get_total_character_count() -> int [const]
get_visible_line_count() -> int [const]

[properties]
autowrap_mode: int (TextServer.AutowrapMode) = 0
autowrap_trim_flags: int (TextServer.LineBreakFlag) = 192
clip_text: bool = false
ellipsis_char: String = "…"
horizontal_alignment: int (HorizontalAlignment) = 0
justification_flags: int (TextServer.JustificationFlag) = 163
label_settings: LabelSettings
language: String = ""
lines_skipped: int = 0
max_lines_visible: int = -1
mouse_filter: int (Control.MouseFilter) = 2
paragraph_separator: String = "\\n"
size_flags_vertical: int (Control.SizeFlags) = 4
structured_text_bidi_override: int (TextServer.StructuredTextParser) = 0
structured_text_bidi_override_options: Array = []
tab_stops: PackedFloat32Array = PackedFloat32Array()
text: String = ""
text_direction: int (Control.TextDirection) = 0
text_overrun_behavior: int (TextServer.OverrunBehavior) = 0
uppercase: bool = false
vertical_alignment: int (VerticalAlignment) = 0
visible_characters: int = -1
visible_characters_behavior: int (TextServer.VisibleCharactersBehavior) = 0
visible_ratio: float = 1.0
```

## Tutorials

- [2D Dodge The Creeps Demo](https://godotengine.org/asset-library/asset/2712)

## Methods

- get_character_bounds(pos: int) -> Rect2 [const]
  Returns the bounding rectangle of the character at position pos in the label's local coordinate system. If the character is a non-visual character or pos is outside the valid range, an empty Rect2 is returned. If the character is a part of a composite grapheme, the bounding rectangle of the whole grapheme is returned.

- get_line_count() -> int [const]
  Returns the number of lines of text the Label has.

- get_line_height(line: int = -1) -> int [const]
  Returns the height of the line line. If line is set to -1, returns the biggest line height. If there are no lines, returns font size in pixels.

- get_total_character_count() -> int [const]
  Returns the total number of printable characters in the text (excluding spaces and newlines).

- get_visible_line_count() -> int [const]
  Returns the number of lines shown. Useful if the Label's height cannot currently display all lines.

## Properties

- autowrap_mode: int (TextServer.AutowrapMode) = 0 [set set_autowrap_mode; get get_autowrap_mode]
  If set to something other than TextServer.AUTOWRAP_OFF, the text gets wrapped inside the node's bounding rectangle. If you resize the node, it will change its height automatically to show all the text.

- autowrap_trim_flags: int (TextServer.LineBreakFlag) = 192 [set set_autowrap_trim_flags; get get_autowrap_trim_flags]
  Autowrap space trimming flags. See TextServer.BREAK_TRIM_START_EDGE_SPACES and TextServer.BREAK_TRIM_END_EDGE_SPACES for more info.

- clip_text: bool = false [set set_clip_text; get is_clipping_text]
  If true, the Label only shows the text that fits inside its bounding rectangle and will clip text horizontally.

- ellipsis_char: String = "…" [set set_ellipsis_char; get get_ellipsis_char]
  Ellipsis character used for text clipping.

- horizontal_alignment: int (HorizontalAlignment) = 0 [set set_horizontal_alignment; get get_horizontal_alignment]
  Controls the text's horizontal alignment. Supports left, center, right, and fill (also known as justify).

- justification_flags: int (TextServer.JustificationFlag) = 163 [set set_justification_flags; get get_justification_flags]
  Line fill alignment rules.

- label_settings: LabelSettings [set set_label_settings; get get_label_settings]
  A LabelSettings resource that can be shared between multiple Label nodes. Takes priority over theme properties.

- language: String = "" [set set_language; get get_language]
  Language code used for line-breaking and text shaping algorithms. If left empty, the current locale is used instead.

- lines_skipped: int = 0 [set set_lines_skipped; get get_lines_skipped]
  The number of the lines ignored and not displayed from the start of the text value.

- max_lines_visible: int = -1 [set set_max_lines_visible; get get_max_lines_visible]
  Limits the lines of text the node shows on screen.

- mouse_filter: int (Control.MouseFilter) = 2 [set set_mouse_filter; get get_mouse_filter; override Control]

- paragraph_separator: String = "\\n" [set set_paragraph_separator; get get_paragraph_separator]
  String used as a paragraph separator. Each paragraph is processed independently, in its own BiDi context.

- size_flags_vertical: int (Control.SizeFlags) = 4 [set set_v_size_flags; get get_v_size_flags; override Control]

- structured_text_bidi_override: int (TextServer.StructuredTextParser) = 0 [set set_structured_text_bidi_override; get get_structured_text_bidi_override]
  Set BiDi algorithm override for the structured text.

- structured_text_bidi_override_options: Array = [] [set set_structured_text_bidi_override_options; get get_structured_text_bidi_override_options]
  Set additional options for BiDi override.

- tab_stops: PackedFloat32Array = PackedFloat32Array() [set set_tab_stops; get get_tab_stops]
  Aligns text to the given tab-stops.

- text: String = "" [set set_text; get get_text]
  The text to display on screen.

- text_direction: int (Control.TextDirection) = 0 [set set_text_direction; get get_text_direction]
  Base text writing direction.

- text_overrun_behavior: int (TextServer.OverrunBehavior) = 0 [set set_text_overrun_behavior; get get_text_overrun_behavior]
  The clipping behavior when the text exceeds the node's bounding rectangle.

- uppercase: bool = false [set set_uppercase; get is_uppercase]
  If true, all the text displays as UPPERCASE.

- vertical_alignment: int (VerticalAlignment) = 0 [set set_vertical_alignment; get get_vertical_alignment]
  Controls the text's vertical alignment. Supports top, center, bottom, and fill.

- visible_characters: int = -1 [set set_visible_characters; get get_visible_characters]
  The number of characters to display. If set to -1, all characters are displayed. This can be useful when animating the text appearing in a dialog box. **Note:** Setting this property updates visible_ratio accordingly. **Note:** Characters are counted as Unicode codepoints. A single visible grapheme may contain multiple codepoints (e.g. certain emoji use three codepoints). A single codepoint may contain two UTF-16 characters, which are used in C# strings.

- visible_characters_behavior: int (TextServer.VisibleCharactersBehavior) = 0 [set set_visible_characters_behavior; get get_visible_characters_behavior]
  The clipping behavior when visible_characters or visible_ratio is set.

- visible_ratio: float = 1.0 [set set_visible_ratio; get get_visible_ratio]
  The fraction of characters to display, relative to the total number of characters (see get_total_character_count()). If set to 1.0, all characters are displayed. If set to 0.5, only half of the characters will be displayed. This can be useful when animating the text appearing in a dialog box. **Note:** Setting this property updates visible_characters accordingly.

## Theme Items

- font_color: Color [color] = Color(1, 1, 1, 1)
  Default text Color of the Label.

- font_outline_color: Color [color] = Color(0, 0, 0, 1)
  The color of text outline.

- font_shadow_color: Color [color] = Color(0, 0, 0, 0)
  Color of the text's shadow effect.

- line_spacing: int [constant] = 3
  Additional vertical spacing between lines (in pixels), spacing is added to line descent. This value can be negative.

- outline_size: int [constant] = 0
  Text outline size. **Note:** If using a font with FontFile.multichannel_signed_distance_field enabled, its FontFile.msdf_pixel_range must be set to at least *twice* the value of [theme_item outline_size] for outline rendering to look correct. Otherwise, the outline may appear to be cut off earlier than intended. **Note:** Using a value that is larger than half the font size is not recommended, as the font outline may fail to be fully closed in this case.

- paragraph_spacing: int [constant] = 0
  Vertical space between paragraphs. Added on top of [theme_item line_spacing].

- shadow_offset_x: int [constant] = 1
  The horizontal offset of the text's shadow.

- shadow_offset_y: int [constant] = 1
  The vertical offset of the text's shadow.

- shadow_outline_size: int [constant] = 1
  The size of the shadow outline.

- font: Font [font]
  Font used for the Label's text.

- font_size: int [font_size]
  Font size of the Label's text.

- focus: StyleBox [style]
  StyleBox used when the Label is focused (when used with assistive apps).

- normal: StyleBox [style]
  Background StyleBox for the Label.
