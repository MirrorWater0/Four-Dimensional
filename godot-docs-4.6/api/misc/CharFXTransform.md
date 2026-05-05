# CharFXTransform

## Meta

- Name: CharFXTransform
- Source: CharFXTransform.xml
- Inherits: RefCounted
- Inheritance Chain: CharFXTransform -> RefCounted -> Object

## Brief Description

Controls how an individual character will be displayed in a RichTextEffect.

## Description

By setting various properties on this object, you can control how individual characters will be displayed in a RichTextEffect.

## Quick Reference

```
[properties]
color: Color = Color(0, 0, 0, 1)
elapsed_time: float = 0.0
env: Dictionary = {}
font: RID = RID()
glyph_count: int = 0
glyph_flags: int = 0
glyph_index: int = 0
offset: Vector2 = Vector2(0, 0)
outline: bool = false
range: Vector2i = Vector2i(0, 0)
relative_index: int = 0
transform: Transform2D = Transform2D(1, 0, 0, 1, 0, 0)
visible: bool = true
```

## Tutorials

- [BBCode in RichTextLabel]($DOCS_URL/tutorials/ui/bbcode_in_richtextlabel.html)

## Properties

- color: Color = Color(0, 0, 0, 1) [set set_color; get get_color]
  The color the character will be drawn with.

- elapsed_time: float = 0.0 [set set_elapsed_time; get get_elapsed_time]
  The time elapsed since the RichTextLabel was added to the scene tree (in seconds). Time stops when the RichTextLabel is paused (see Node.process_mode). Resets when the text in the RichTextLabel is changed. **Note:** Time still passes while the RichTextLabel is hidden.

- env: Dictionary = {} [set set_environment; get get_environment]
  Contains the arguments passed in the opening BBCode tag. By default, arguments are strings; if their contents match a type such as bool, int or float, they will be converted automatically. Color codes in the form #rrggbb or #rgb will be converted to an opaque Color. String arguments may not contain spaces, even if they're quoted. If present, quotes will also be present in the final string. For example, the opening BBCode tag [example foo=hello bar=true baz=42 color=#ffffff] will map to the following Dictionary:

```
{"foo": "hello", "bar": true, "baz": 42, "color": Color(1, 1, 1, 1)}
```

- font: RID = RID() [set set_font; get get_font]
  TextServer RID of the font used to render glyph, this value can be used with TextServer.font_* methods to retrieve font information. **Note:** Read-only. Setting this property won't affect drawing.

- glyph_count: int = 0 [set set_glyph_count; get get_glyph_count]
  Number of glyphs in the grapheme cluster. This value is set in the first glyph of a cluster. **Note:** Read-only. Setting this property won't affect drawing.

- glyph_flags: int = 0 [set set_glyph_flags; get get_glyph_flags]
  Glyph flags. See TextServer.GraphemeFlag for more info. **Note:** Read-only. Setting this property won't affect drawing.

- glyph_index: int = 0 [set set_glyph_index; get get_glyph_index]
  Glyph index specific to the font. If you want to replace this glyph, use TextServer.font_get_glyph_index() with font to get a new glyph index for a single character.

- offset: Vector2 = Vector2(0, 0) [set set_offset; get get_offset]
  The position offset the character will be drawn with (in pixels).

- outline: bool = false [set set_outline; get is_outline]
  If true, FX transform is called for outline drawing. **Note:** Read-only. Setting this property won't affect drawing.

- range: Vector2i = Vector2i(0, 0) [set set_range; get get_range]
  Absolute character range in the string, corresponding to the glyph. **Note:** Read-only. Setting this property won't affect drawing.

- relative_index: int = 0 [set set_relative_index; get get_relative_index]
  The character offset of the glyph, relative to the current RichTextEffect custom block. **Note:** Read-only. Setting this property won't affect drawing.

- transform: Transform2D = Transform2D(1, 0, 0, 1, 0, 0) [set set_transform; get get_transform]
  The current transform of the current glyph. It can be overridden (for example, by driving the position and rotation from a curve). You can also alter the existing value to apply transforms on top of other effects.

- visible: bool = true [set set_visibility; get is_visible]
  If true, the character will be drawn. If false, the character will be hidden. Characters around hidden characters will reflow to take the space of hidden characters. If this is not desired, set their color to Color(1, 1, 1, 0) instead.
