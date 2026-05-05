# FontVariation

## Meta

- Name: FontVariation
- Source: FontVariation.xml
- Inherits: Font
- Inheritance Chain: FontVariation -> Font -> Resource -> RefCounted -> Object

## Brief Description

A variation of a font with additional settings.

## Description

Provides OpenType variations, simulated bold / slant, and additional font settings like OpenType features and extra spacing. To use simulated bold font variant:

```
var fv = FontVariation.new()
fv.base_font = load("res://BarlowCondensed-Regular.ttf")
fv.variation_embolden = 1.2
$Label.add_theme_font_override("font", fv)
$Label.add_theme_font_size_override("font_size", 64)
```

```
var fv = new FontVariation();
fv.SetBaseFont(ResourceLoader.Load<FontFile>("res://BarlowCondensed-Regular.ttf"));
fv.SetVariationEmbolden(1.2);
GetNode("Label").AddThemeFontOverride("font", fv);
GetNode("Label").AddThemeFontSizeOverride("font_size", 64);
```

To set the coordinate of multiple variation axes:

```
var fv = FontVariation.new();
var ts = TextServerManager.get_primary_interface()
fv.base_font = load("res://BarlowCondensed-Regular.ttf")
fv.variation_opentype = { ts.name_to_tag("wght"): 900, ts.name_to_tag("custom_hght"): 900 }
```

## Quick Reference

```
[methods]
set_spacing(spacing: int (TextServer.SpacingType), value: int) -> void

[properties]
base_font: Font
baseline_offset: float = 0.0
opentype_features: Dictionary = {}
spacing_bottom: int = 0
spacing_glyph: int = 0
spacing_space: int = 0
spacing_top: int = 0
variation_embolden: float = 0.0
variation_face_index: int = 0
variation_opentype: Dictionary = {}
variation_transform: Transform2D = Transform2D(1, 0, 0, 1, 0, 0)
```

## Methods

- set_spacing(spacing: int (TextServer.SpacingType), value: int) -> void
  Sets the spacing for spacing to value in pixels (not relative to the font size).

## Properties

- base_font: Font [set set_base_font; get get_base_font]
  Base font used to create a variation. If not set, default Theme font is used.

- baseline_offset: float = 0.0 [set set_baseline_offset; get get_baseline_offset]
  Extra baseline offset (as a fraction of font height).

- opentype_features: Dictionary = {} [set set_opentype_features; get get_opentype_features]
  A set of OpenType feature tags. More info: [OpenType feature tags](https://docs.microsoft.com/en-us/typography/opentype/spec/featuretags).

- spacing_bottom: int = 0 [set set_spacing; get get_spacing]
  Extra spacing at the bottom of the line in pixels.

- spacing_glyph: int = 0 [set set_spacing; get get_spacing]
  Extra spacing between graphical glyphs.

- spacing_space: int = 0 [set set_spacing; get get_spacing]
  Extra width of the space glyphs.

- spacing_top: int = 0 [set set_spacing; get get_spacing]
  Extra spacing at the top of the line in pixels.

- variation_embolden: float = 0.0 [set set_variation_embolden; get get_variation_embolden]
  If is not equal to zero, emboldens the font outlines. Negative values reduce the outline thickness. **Note:** Emboldened fonts might have self-intersecting outlines, which will prevent MSDF fonts and TextMesh from working correctly.

- variation_face_index: int = 0 [set set_variation_face_index; get get_variation_face_index]
  Active face index in the TrueType / OpenType collection file.

- variation_opentype: Dictionary = {} [set set_variation_opentype; get get_variation_opentype]
  Font OpenType variation coordinates. More info: [OpenType variation tags](https://docs.microsoft.com/en-us/typography/opentype/spec/dvaraxisreg). **Note:** This Dictionary uses OpenType tags as keys. Variation axes can be identified both by tags (int, e.g. 0x77678674) and names (String, e.g. wght). Some axes might be accessible by multiple names. For example, wght refers to the same axis as weight. Tags on the other hand are unique. To convert between names and tags, use TextServer.name_to_tag() and TextServer.tag_to_name(). **Note:** To get available variation axes of a font, use Font.get_supported_variation_list().

- variation_transform: Transform2D = Transform2D(1, 0, 0, 1, 0, 0) [set set_variation_transform; get get_variation_transform]
  2D transform, applied to the font outlines, can be used for slanting, flipping and rotating glyphs. For example, to simulate italic typeface by slanting, apply the following transform Transform2D(1.0, slant, 0.0, 1.0, 0.0, 0.0).
