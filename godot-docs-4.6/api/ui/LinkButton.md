# LinkButton

## Meta

- Name: LinkButton
- Source: LinkButton.xml
- Inherits: BaseButton
- Inheritance Chain: LinkButton -> BaseButton -> Control -> CanvasItem -> Node -> Object

## Brief Description

A button that represents a link.

## Description

A button that represents a link. This type of button is primarily used for interactions that cause a context change (like linking to a web page). See also BaseButton which contains common properties and methods associated with this node.

## Quick Reference

```
[properties]
ellipsis_char: String = "…"
focus_mode: int (Control.FocusMode) = 3
language: String = ""
mouse_default_cursor_shape: int (Control.CursorShape) = 2
structured_text_bidi_override: int (TextServer.StructuredTextParser) = 0
structured_text_bidi_override_options: Array = []
text: String = ""
text_direction: int (Control.TextDirection) = 0
text_overrun_behavior: int (TextServer.OverrunBehavior) = 0
underline: int (LinkButton.UnderlineMode) = 0
uri: String = ""
```

## Properties

- ellipsis_char: String = "…" [set set_ellipsis_char; get get_ellipsis_char]
  Ellipsis character used for text clipping.

- focus_mode: int (Control.FocusMode) = 3 [set set_focus_mode; get get_focus_mode; override Control]

- language: String = "" [set set_language; get get_language]
  Language code used for line-breaking and text shaping algorithms. If left empty, the current locale is used instead.

- mouse_default_cursor_shape: int (Control.CursorShape) = 2 [set set_default_cursor_shape; get get_default_cursor_shape; override Control]

- structured_text_bidi_override: int (TextServer.StructuredTextParser) = 0 [set set_structured_text_bidi_override; get get_structured_text_bidi_override]
  Set BiDi algorithm override for the structured text.

- structured_text_bidi_override_options: Array = [] [set set_structured_text_bidi_override_options; get get_structured_text_bidi_override_options]
  Set additional options for BiDi override.

- text: String = "" [set set_text; get get_text]
  The button's text that will be displayed inside the button's area.

- text_direction: int (Control.TextDirection) = 0 [set set_text_direction; get get_text_direction]
  Base text writing direction.

- text_overrun_behavior: int (TextServer.OverrunBehavior) = 0 [set set_text_overrun_behavior; get get_text_overrun_behavior]
  Sets the clipping behavior when the text exceeds the node's bounding rectangle.

- underline: int (LinkButton.UnderlineMode) = 0 [set set_underline_mode; get get_underline_mode]
  The underline mode to use for the text.

- uri: String = "" [set set_uri; get get_uri]
  The URI(https://en.wikipedia.org/wiki/Uniform_Resource_Identifier) for this LinkButton. If set to a valid URI, pressing the button opens the URI using the operating system's default program for the protocol (via OS.shell_open()). HTTP and HTTPS URLs open the default web browser.

```
uri = "https://godotengine.org"  # Opens the URL in the default web browser.
uri = "C:\SomeFolder"  # Opens the file explorer at the given path.
uri = "C:\SomeImage.png"  # Opens the given image in the default viewing app.
```

```
Uri = "https://godotengine.org"; // Opens the URL in the default web browser.
Uri = "C:\SomeFolder"; // Opens the file explorer at the given path.
Uri = "C:\SomeImage.png"; // Opens the given image in the default viewing app.
```

## Constants

### Enum UnderlineMode

- UNDERLINE_MODE_ALWAYS = 0
  The LinkButton will always show an underline at the bottom of its text.

- UNDERLINE_MODE_ON_HOVER = 1
  The LinkButton will show an underline at the bottom of its text when the mouse cursor is over it.

- UNDERLINE_MODE_NEVER = 2
  The LinkButton will never show an underline at the bottom of its text.

## Theme Items

- font_color: Color [color] = Color(0.875, 0.875, 0.875, 1)
  Default text Color of the LinkButton.

- font_disabled_color: Color [color] = Color(0, 0, 0, 1)
  Text Color used when the LinkButton is disabled.

- font_focus_color: Color [color] = Color(0.95, 0.95, 0.95, 1)
  Text Color used when the LinkButton is focused. Only replaces the normal text color of the button. Disabled, hovered, and pressed states take precedence over this color.

- font_hover_color: Color [color] = Color(0.95, 0.95, 0.95, 1)
  Text Color used when the LinkButton is being hovered.

- font_hover_pressed_color: Color [color] = Color(0, 0, 0, 1)
  Text Color used when the LinkButton is being hovered and pressed.

- font_outline_color: Color [color] = Color(0, 0, 0, 1)
  The tint of text outline of the LinkButton.

- font_pressed_color: Color [color] = Color(1, 1, 1, 1)
  Text Color used when the LinkButton is being pressed.

- outline_size: int [constant] = 0
  The size of the text outline. **Note:** If using a font with FontFile.multichannel_signed_distance_field enabled, its FontFile.msdf_pixel_range must be set to at least *twice* the value of [theme_item outline_size] for outline rendering to look correct. Otherwise, the outline may appear to be cut off earlier than intended.

- underline_spacing: int [constant] = 2
  The vertical space between the baseline of text and the underline.

- font: Font [font]
  Font of the LinkButton's text.

- font_size: int [font_size]
  Font size of the LinkButton's text.

- focus: StyleBox [style]
  StyleBox used when the LinkButton is focused. The [theme_item focus] StyleBox is displayed *over* the base StyleBox, so a partially transparent StyleBox should be used to ensure the base StyleBox remains visible. A StyleBox that represents an outline or an underline works well for this purpose. To disable the focus visual effect, assign a StyleBoxEmpty resource. Note that disabling the focus visual effect will harm keyboard/controller navigation usability, so this is not recommended for accessibility reasons.
