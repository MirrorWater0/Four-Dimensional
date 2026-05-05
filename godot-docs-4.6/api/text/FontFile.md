# FontFile

## Meta

- Name: FontFile
- Source: FontFile.xml
- Inherits: Font
- Inheritance Chain: FontFile -> Font -> Resource -> RefCounted -> Object

## Brief Description

Holds font source data and prerendered glyph cache, imported from a dynamic or a bitmap font.

## Description

FontFile contains a set of glyphs to represent Unicode characters imported from a font file, as well as a cache of rasterized glyphs, and a set of fallback Fonts to use. Use FontVariation to access specific OpenType variation of the font, create simulated bold / slanted version, and draw lines of text. For more complex text processing, use FontVariation in conjunction with TextLine or TextParagraph. Supported font formats: - Dynamic font importer: TrueType (.ttf), TrueType collection (.ttc), OpenType (.otf), OpenType collection (.otc), WOFF (.woff), WOFF2 (.woff2), Type 1 (.pfb, .pfm). - Bitmap font importer: AngelCode BMFont (.fnt, .font), text and binary (version 3) format variants. - Monospace image font importer: All supported image formats. **Note:** A character is a symbol that represents an item (letter, digit etc.) in an abstract way. **Note:** A glyph is a bitmap or a shape used to draw one or more characters in a context-dependent manner. Glyph indices are bound to the specific font data source. **Note:** If none of the font data sources contain glyphs for a character used in a string, the character in question will be replaced with a box displaying its hexadecimal code.

```
var f = load("res://BarlowCondensed-Bold.ttf")
$Label.add_theme_font_override("font", f)
$Label.add_theme_font_size_override("font_size", 64)
```

```
var f = ResourceLoader.Load<FontFile>("res://BarlowCondensed-Bold.ttf");
GetNode("Label").AddThemeFontOverride("font", f);
GetNode("Label").AddThemeFontSizeOverride("font_size", 64);
```

## Quick Reference

```
[methods]
clear_cache() -> void
clear_glyphs(cache_index: int, size: Vector2i) -> void
clear_kerning_map(cache_index: int, size: int) -> void
clear_size_cache(cache_index: int) -> void
clear_textures(cache_index: int, size: Vector2i) -> void
get_cache_ascent(cache_index: int, size: int) -> float [const]
get_cache_count() -> int [const]
get_cache_descent(cache_index: int, size: int) -> float [const]
get_cache_scale(cache_index: int, size: int) -> float [const]
get_cache_underline_position(cache_index: int, size: int) -> float [const]
get_cache_underline_thickness(cache_index: int, size: int) -> float [const]
get_char_from_glyph_index(size: int, glyph_index: int) -> int [const]
get_embolden(cache_index: int) -> float [const]
get_extra_baseline_offset(cache_index: int) -> float [const]
get_extra_spacing(cache_index: int, spacing: int (TextServer.SpacingType)) -> int [const]
get_face_index(cache_index: int) -> int [const]
get_glyph_advance(cache_index: int, size: int, glyph: int) -> Vector2 [const]
get_glyph_index(size: int, char: int, variation_selector: int) -> int [const]
get_glyph_list(cache_index: int, size: Vector2i) -> PackedInt32Array [const]
get_glyph_offset(cache_index: int, size: Vector2i, glyph: int) -> Vector2 [const]
get_glyph_size(cache_index: int, size: Vector2i, glyph: int) -> Vector2 [const]
get_glyph_texture_idx(cache_index: int, size: Vector2i, glyph: int) -> int [const]
get_glyph_uv_rect(cache_index: int, size: Vector2i, glyph: int) -> Rect2 [const]
get_kerning(cache_index: int, size: int, glyph_pair: Vector2i) -> Vector2 [const]
get_kerning_list(cache_index: int, size: int) -> Vector2i[] [const]
get_language_support_override(language: String) -> bool [const]
get_language_support_overrides() -> PackedStringArray [const]
get_script_support_override(script: String) -> bool [const]
get_script_support_overrides() -> PackedStringArray [const]
get_size_cache_list(cache_index: int) -> Vector2i[] [const]
get_texture_count(cache_index: int, size: Vector2i) -> int [const]
get_texture_image(cache_index: int, size: Vector2i, texture_index: int) -> Image [const]
get_texture_offsets(cache_index: int, size: Vector2i, texture_index: int) -> PackedInt32Array [const]
get_transform(cache_index: int) -> Transform2D [const]
get_variation_coordinates(cache_index: int) -> Dictionary [const]
load_bitmap_font(path: String) -> int (Error)
load_dynamic_font(path: String) -> int (Error)
remove_cache(cache_index: int) -> void
remove_glyph(cache_index: int, size: Vector2i, glyph: int) -> void
remove_kerning(cache_index: int, size: int, glyph_pair: Vector2i) -> void
remove_language_support_override(language: String) -> void
remove_script_support_override(script: String) -> void
remove_size_cache(cache_index: int, size: Vector2i) -> void
remove_texture(cache_index: int, size: Vector2i, texture_index: int) -> void
render_glyph(cache_index: int, size: Vector2i, index: int) -> void
render_range(cache_index: int, size: Vector2i, start: int, end: int) -> void
set_cache_ascent(cache_index: int, size: int, ascent: float) -> void
set_cache_descent(cache_index: int, size: int, descent: float) -> void
set_cache_scale(cache_index: int, size: int, scale: float) -> void
set_cache_underline_position(cache_index: int, size: int, underline_position: float) -> void
set_cache_underline_thickness(cache_index: int, size: int, underline_thickness: float) -> void
set_embolden(cache_index: int, strength: float) -> void
set_extra_baseline_offset(cache_index: int, baseline_offset: float) -> void
set_extra_spacing(cache_index: int, spacing: int (TextServer.SpacingType), value: int) -> void
set_face_index(cache_index: int, face_index: int) -> void
set_glyph_advance(cache_index: int, size: int, glyph: int, advance: Vector2) -> void
set_glyph_offset(cache_index: int, size: Vector2i, glyph: int, offset: Vector2) -> void
set_glyph_size(cache_index: int, size: Vector2i, glyph: int, gl_size: Vector2) -> void
set_glyph_texture_idx(cache_index: int, size: Vector2i, glyph: int, texture_idx: int) -> void
set_glyph_uv_rect(cache_index: int, size: Vector2i, glyph: int, uv_rect: Rect2) -> void
set_kerning(cache_index: int, size: int, glyph_pair: Vector2i, kerning: Vector2) -> void
set_language_support_override(language: String, supported: bool) -> void
set_script_support_override(script: String, supported: bool) -> void
set_texture_image(cache_index: int, size: Vector2i, texture_index: int, image: Image) -> void
set_texture_offsets(cache_index: int, size: Vector2i, texture_index: int, offset: PackedInt32Array) -> void
set_transform(cache_index: int, transform: Transform2D) -> void
set_variation_coordinates(cache_index: int, variation_coordinates: Dictionary) -> void

[properties]
allow_system_fallback: bool = true
antialiasing: int (TextServer.FontAntialiasing) = 1
data: PackedByteArray = PackedByteArray()
disable_embedded_bitmaps: bool = true
fixed_size: int = 0
fixed_size_scale_mode: int (TextServer.FixedSizeScaleMode) = 0
font_name: String = ""
font_stretch: int = 100
font_style: int (TextServer.FontStyle) = 0
font_weight: int = 400
force_autohinter: bool = false
generate_mipmaps: bool = false
hinting: int (TextServer.Hinting) = 1
keep_rounding_remainders: bool = true
modulate_color_glyphs: bool = false
msdf_pixel_range: int = 16
msdf_size: int = 48
multichannel_signed_distance_field: bool = false
opentype_feature_overrides: Dictionary = {}
oversampling: float = 0.0
style_name: String = ""
subpixel_positioning: int (TextServer.SubpixelPositioning) = 1
```

## Tutorials

- [Runtime file loading and saving]($DOCS_URL/tutorials/io/runtime_file_loading_and_saving.html)

## Methods

- clear_cache() -> void
  Removes all font cache entries.

- clear_glyphs(cache_index: int, size: Vector2i) -> void
  Removes all rendered glyph information from the cache entry. **Note:** This function will not remove textures associated with the glyphs, use remove_texture() to remove them manually.

- clear_kerning_map(cache_index: int, size: int) -> void
  Removes all kerning overrides.

- clear_size_cache(cache_index: int) -> void
  Removes all font sizes from the cache entry.

- clear_textures(cache_index: int, size: Vector2i) -> void
  Removes all textures from font cache entry. **Note:** This function will not remove glyphs associated with the texture, use remove_glyph() to remove them manually.

- get_cache_ascent(cache_index: int, size: int) -> float [const]
  Returns the font ascent (number of pixels above the baseline).

- get_cache_count() -> int [const]
  Returns number of the font cache entries.

- get_cache_descent(cache_index: int, size: int) -> float [const]
  Returns the font descent (number of pixels below the baseline).

- get_cache_scale(cache_index: int, size: int) -> float [const]
  Returns scaling factor of the color bitmap font.

- get_cache_underline_position(cache_index: int, size: int) -> float [const]
  Returns pixel offset of the underline below the baseline.

- get_cache_underline_thickness(cache_index: int, size: int) -> float [const]
  Returns thickness of the underline in pixels.

- get_char_from_glyph_index(size: int, glyph_index: int) -> int [const]
  Returns character code associated with glyph_index, or 0 if glyph_index is invalid. See get_glyph_index().

- get_embolden(cache_index: int) -> float [const]
  Returns embolden strength, if is not equal to zero, emboldens the font outlines. Negative values reduce the outline thickness.

- get_extra_baseline_offset(cache_index: int) -> float [const]
  Returns extra baseline offset (as a fraction of font height).

- get_extra_spacing(cache_index: int, spacing: int (TextServer.SpacingType)) -> int [const]
  Returns spacing for spacing in pixels (not relative to the font size).

- get_face_index(cache_index: int) -> int [const]
  Returns an active face index in the TrueType / OpenType collection.

- get_glyph_advance(cache_index: int, size: int, glyph: int) -> Vector2 [const]
  Returns glyph advance (offset of the next glyph). **Note:** Advance for glyphs outlines is the same as the base glyph advance and is not saved.

- get_glyph_index(size: int, char: int, variation_selector: int) -> int [const]
  Returns the glyph index of a char, optionally modified by the variation_selector.

- get_glyph_list(cache_index: int, size: Vector2i) -> PackedInt32Array [const]
  Returns list of rendered glyphs in the cache entry.

- get_glyph_offset(cache_index: int, size: Vector2i, glyph: int) -> Vector2 [const]
  Returns glyph offset from the baseline.

- get_glyph_size(cache_index: int, size: Vector2i, glyph: int) -> Vector2 [const]
  Returns glyph size.

- get_glyph_texture_idx(cache_index: int, size: Vector2i, glyph: int) -> int [const]
  Returns index of the cache texture containing the glyph.

- get_glyph_uv_rect(cache_index: int, size: Vector2i, glyph: int) -> Rect2 [const]
  Returns rectangle in the cache texture containing the glyph.

- get_kerning(cache_index: int, size: int, glyph_pair: Vector2i) -> Vector2 [const]
  Returns kerning for the pair of glyphs.

- get_kerning_list(cache_index: int, size: int) -> Vector2i[] [const]
  Returns list of the kerning overrides.

- get_language_support_override(language: String) -> bool [const]
  Returns true if support override is enabled for the language.

- get_language_support_overrides() -> PackedStringArray [const]
  Returns list of language support overrides.

- get_script_support_override(script: String) -> bool [const]
  Returns true if support override is enabled for the script.

- get_script_support_overrides() -> PackedStringArray [const]
  Returns list of script support overrides.

- get_size_cache_list(cache_index: int) -> Vector2i[] [const]
  Returns list of the font sizes in the cache. Each size is Vector2i with font size and outline size.

- get_texture_count(cache_index: int, size: Vector2i) -> int [const]
  Returns number of textures used by font cache entry.

- get_texture_image(cache_index: int, size: Vector2i, texture_index: int) -> Image [const]
  Returns a copy of the font cache texture image.

- get_texture_offsets(cache_index: int, size: Vector2i, texture_index: int) -> PackedInt32Array [const]
  Returns a copy of the array containing glyph packing data.

- get_transform(cache_index: int) -> Transform2D [const]
  Returns 2D transform, applied to the font outlines, can be used for slanting, flipping and rotating glyphs.

- get_variation_coordinates(cache_index: int) -> Dictionary [const]
  Returns variation coordinates for the specified font cache entry. See Font.get_supported_variation_list() for more info.

- load_bitmap_font(path: String) -> int (Error)
  Loads an AngelCode BMFont (.fnt, .font) bitmap font from file path. **Warning:** This method should only be used in the editor or in cases when you need to load external fonts at run-time, such as fonts located at the user:// directory.

- load_dynamic_font(path: String) -> int (Error)
  Loads a TrueType (.ttf), OpenType (.otf), WOFF (.woff), WOFF2 (.woff2) or Type 1 (.pfb, .pfm) dynamic font from file path. **Warning:** This method should only be used in the editor or in cases when you need to load external fonts at run-time, such as fonts located at the user:// directory.

- remove_cache(cache_index: int) -> void
  Removes specified font cache entry.

- remove_glyph(cache_index: int, size: Vector2i, glyph: int) -> void
  Removes specified rendered glyph information from the cache entry. **Note:** This function will not remove textures associated with the glyphs, use remove_texture() to remove them manually.

- remove_kerning(cache_index: int, size: int, glyph_pair: Vector2i) -> void
  Removes kerning override for the pair of glyphs.

- remove_language_support_override(language: String) -> void
  Remove language support override.

- remove_script_support_override(script: String) -> void
  Removes script support override.

- remove_size_cache(cache_index: int, size: Vector2i) -> void
  Removes specified font size from the cache entry.

- remove_texture(cache_index: int, size: Vector2i, texture_index: int) -> void
  Removes specified texture from the cache entry. **Note:** This function will not remove glyphs associated with the texture. Remove them manually using remove_glyph().

- render_glyph(cache_index: int, size: Vector2i, index: int) -> void
  Renders specified glyph to the font cache texture.

- render_range(cache_index: int, size: Vector2i, start: int, end: int) -> void
  Renders the range of characters to the font cache texture.

- set_cache_ascent(cache_index: int, size: int, ascent: float) -> void
  Sets the font ascent (number of pixels above the baseline).

- set_cache_descent(cache_index: int, size: int, descent: float) -> void
  Sets the font descent (number of pixels below the baseline).

- set_cache_scale(cache_index: int, size: int, scale: float) -> void
  Sets scaling factor of the color bitmap font.

- set_cache_underline_position(cache_index: int, size: int, underline_position: float) -> void
  Sets pixel offset of the underline below the baseline.

- set_cache_underline_thickness(cache_index: int, size: int, underline_thickness: float) -> void
  Sets thickness of the underline in pixels.

- set_embolden(cache_index: int, strength: float) -> void
  Sets embolden strength, if is not equal to zero, emboldens the font outlines. Negative values reduce the outline thickness.

- set_extra_baseline_offset(cache_index: int, baseline_offset: float) -> void
  Sets extra baseline offset (as a fraction of font height).

- set_extra_spacing(cache_index: int, spacing: int (TextServer.SpacingType), value: int) -> void
  Sets the spacing for spacing to value in pixels (not relative to the font size).

- set_face_index(cache_index: int, face_index: int) -> void
  Sets an active face index in the TrueType / OpenType collection.

- set_glyph_advance(cache_index: int, size: int, glyph: int, advance: Vector2) -> void
  Sets glyph advance (offset of the next glyph). **Note:** Advance for glyphs outlines is the same as the base glyph advance and is not saved.

- set_glyph_offset(cache_index: int, size: Vector2i, glyph: int, offset: Vector2) -> void
  Sets glyph offset from the baseline.

- set_glyph_size(cache_index: int, size: Vector2i, glyph: int, gl_size: Vector2) -> void
  Sets glyph size.

- set_glyph_texture_idx(cache_index: int, size: Vector2i, glyph: int, texture_idx: int) -> void
  Sets index of the cache texture containing the glyph.

- set_glyph_uv_rect(cache_index: int, size: Vector2i, glyph: int, uv_rect: Rect2) -> void
  Sets rectangle in the cache texture containing the glyph.

- set_kerning(cache_index: int, size: int, glyph_pair: Vector2i, kerning: Vector2) -> void
  Sets kerning for the pair of glyphs.

- set_language_support_override(language: String, supported: bool) -> void
  Adds override for Font.is_language_supported().

- set_script_support_override(script: String, supported: bool) -> void
  Adds override for Font.is_script_supported().

- set_texture_image(cache_index: int, size: Vector2i, texture_index: int, image: Image) -> void
  Sets font cache texture image.

- set_texture_offsets(cache_index: int, size: Vector2i, texture_index: int, offset: PackedInt32Array) -> void
  Sets array containing glyph packing data.

- set_transform(cache_index: int, transform: Transform2D) -> void
  Sets 2D transform, applied to the font outlines, can be used for slanting, flipping, and rotating glyphs.

- set_variation_coordinates(cache_index: int, variation_coordinates: Dictionary) -> void
  Sets variation coordinates for the specified font cache entry. See Font.get_supported_variation_list() for more info.

## Properties

- allow_system_fallback: bool = true [set set_allow_system_fallback; get is_allow_system_fallback]
  If set to true, system fonts can be automatically used as fallbacks.

- antialiasing: int (TextServer.FontAntialiasing) = 1 [set set_antialiasing; get get_antialiasing]
  Font anti-aliasing mode.

- data: PackedByteArray = PackedByteArray() [set set_data; get get_data]
  Contents of the dynamic font source file.

- disable_embedded_bitmaps: bool = true [set set_disable_embedded_bitmaps; get get_disable_embedded_bitmaps]
  If set to true, embedded font bitmap loading is disabled (bitmap-only and color fonts ignore this property).

- fixed_size: int = 0 [set set_fixed_size; get get_fixed_size]
  Font size, used only for the bitmap fonts.

- fixed_size_scale_mode: int (TextServer.FixedSizeScaleMode) = 0 [set set_fixed_size_scale_mode; get get_fixed_size_scale_mode]
  Scaling mode, used only for the bitmap fonts with fixed_size greater than zero.

- font_name: String = "" [set set_font_name; get get_font_name]
  Font family name.

- font_stretch: int = 100 [set set_font_stretch; get get_font_stretch]
  Font stretch amount, compared to a normal width. A percentage value between 50% and 200%.

- font_style: int (TextServer.FontStyle) = 0 [set set_font_style; get get_font_style]
  Font style flags.

- font_weight: int = 400 [set set_font_weight; get get_font_weight]
  Weight (boldness) of the font. A value in the 100...999 range, normal font weight is 400, bold font weight is 700.

- force_autohinter: bool = false [set set_force_autohinter; get is_force_autohinter]
  If set to true, auto-hinting is supported and preferred over font built-in hinting. Used by dynamic fonts only (MSDF fonts don't support hinting).

- generate_mipmaps: bool = false [set set_generate_mipmaps; get get_generate_mipmaps]
  If set to true, generate mipmaps for the font textures.

- hinting: int (TextServer.Hinting) = 1 [set set_hinting; get get_hinting]
  Font hinting mode. Used by dynamic fonts only.

- keep_rounding_remainders: bool = true [set set_keep_rounding_remainders; get get_keep_rounding_remainders]
  If set to true, when aligning glyphs to the pixel boundaries rounding remainders are accumulated to ensure more uniform glyph distribution. This setting has no effect if subpixel positioning is enabled.

- modulate_color_glyphs: bool = false [set set_modulate_color_glyphs; get is_modulate_color_glyphs]
  If set to true, color modulation is applied when drawing colored glyphs, otherwise it's applied to the monochrome glyphs only.

- msdf_pixel_range: int = 16 [set set_msdf_pixel_range; get get_msdf_pixel_range]
  The width of the range around the shape between the minimum and maximum representable signed distance. If using font outlines, msdf_pixel_range must be set to at least *twice* the size of the largest font outline. The default msdf_pixel_range value of 16 allows outline sizes up to 8 to look correct.

- msdf_size: int = 48 [set set_msdf_size; get get_msdf_size]
  Source font size used to generate MSDF textures. Higher values allow for more precision, but are slower to render and require more memory. Only increase this value if you notice a visible lack of precision in glyph rendering.

- multichannel_signed_distance_field: bool = false [set set_multichannel_signed_distance_field; get is_multichannel_signed_distance_field]
  If set to true, glyphs of all sizes are rendered using single multichannel signed distance field (MSDF) generated from the dynamic font vector data. Since this approach does not rely on rasterizing the font every time its size changes, this allows for resizing the font in real-time without any performance penalty. Text will also not look grainy for Controls that are scaled down (or for Label3Ds viewed from a long distance). As a downside, font hinting is not available with MSDF. The lack of font hinting may result in less crisp and less readable fonts at small sizes. **Note:** If using font outlines, msdf_pixel_range must be set to at least *twice* the size of the largest font outline. **Note:** MSDF font rendering does not render glyphs with overlapping shapes correctly. Overlapping shapes are not valid per the OpenType standard, but are still commonly found in many font files, especially those converted by Google Fonts. To avoid issues with overlapping glyphs, consider downloading the font file directly from the type foundry instead of relying on Google Fonts.

- opentype_feature_overrides: Dictionary = {} [set set_opentype_feature_overrides; get get_opentype_feature_overrides]
  Font OpenType feature set override.

- oversampling: float = 0.0 [set set_oversampling; get get_oversampling]
  If set to a positive value, overrides the oversampling factor of the viewport this font is used in. See Viewport.oversampling. This value doesn't override the [code skip-lint]oversampling[/code] parameter of [code skip-lint]draw_*[/code] methods.

- style_name: String = "" [set set_font_style_name; get get_font_style_name]
  Font style name.

- subpixel_positioning: int (TextServer.SubpixelPositioning) = 1 [set set_subpixel_positioning; get get_subpixel_positioning]
  Font glyph subpixel positioning mode. Subpixel positioning provides shaper text and better kerning for smaller font sizes, at the cost of higher memory usage and lower font rasterization speed. Use TextServer.SUBPIXEL_POSITIONING_AUTO to automatically enable it based on the font size.
