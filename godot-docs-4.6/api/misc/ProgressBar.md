# ProgressBar

## Meta

- Name: ProgressBar
- Source: ProgressBar.xml
- Inherits: Range
- Inheritance Chain: ProgressBar -> Range -> Control -> CanvasItem -> Node -> Object

## Brief Description

A control used for visual representation of a percentage.

## Description

A control used for visual representation of a percentage. Shows the fill percentage in the center. Can also be used to show indeterminate progress. For more fill modes, use TextureProgressBar instead.

## Quick Reference

```
[properties]
editor_preview_indeterminate: bool
fill_mode: int = 0
indeterminate: bool = false
show_percentage: bool = true
```

## Properties

- editor_preview_indeterminate: bool [set set_editor_preview_indeterminate; get is_editor_preview_indeterminate_enabled]
  If false, the indeterminate animation will be paused in the editor.

- fill_mode: int = 0 [set set_fill_mode; get get_fill_mode]
  The fill direction. See FillMode for possible values.

- indeterminate: bool = false [set set_indeterminate; get is_indeterminate]
  When set to true, the progress bar indicates that something is happening with an animation, but does not show the fill percentage or value.

- show_percentage: bool = true [set set_show_percentage; get is_percentage_shown]
  If true, the fill percentage is displayed on the bar.

## Constants

### Enum FillMode

- FILL_BEGIN_TO_END = 0
  The progress bar fills from begin to end horizontally, according to the language direction. If Control.is_layout_rtl() returns false, it fills from left to right, and if it returns true, it fills from right to left.

- FILL_END_TO_BEGIN = 1
  The progress bar fills from end to begin horizontally, according to the language direction. If Control.is_layout_rtl() returns false, it fills from right to left, and if it returns true, it fills from left to right.

- FILL_TOP_TO_BOTTOM = 2
  The progress fills from top to bottom.

- FILL_BOTTOM_TO_TOP = 3
  The progress fills from bottom to top.

## Theme Items

- font_color: Color [color] = Color(0.95, 0.95, 0.95, 1)
  The color of the text.

- font_outline_color: Color [color] = Color(0, 0, 0, 1)
  The tint of text outline of the ProgressBar.

- outline_size: int [constant] = 0
  The size of the text outline. **Note:** If using a font with FontFile.multichannel_signed_distance_field enabled, its FontFile.msdf_pixel_range must be set to at least *twice* the value of [theme_item outline_size] for outline rendering to look correct. Otherwise, the outline may appear to be cut off earlier than intended.

- font: Font [font]
  Font used to draw the fill percentage if show_percentage is true.

- font_size: int [font_size]
  Font size used to draw the fill percentage if show_percentage is true.

- background: StyleBox [style]
  The style of the background.

- fill: StyleBox [style]
  The style of the progress (i.e. the part that fills the bar).
