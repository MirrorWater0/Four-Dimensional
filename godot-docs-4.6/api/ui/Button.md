# Button

## Meta

- Name: Button
- Source: Button.xml
- Inherits: BaseButton
- Inheritance Chain: Button -> BaseButton -> Control -> CanvasItem -> Node -> Object

## Brief Description

A themed button that can contain text and an icon.

## Description

Button is the standard themed button. It can contain text and an icon, and it will display them according to the current Theme. **Example:** Create a button and connect a method that will be called when the button is pressed:

```
func _ready():
    var button = Button.new()
    button.text = "Click me"
    button.pressed.connect(_button_pressed)
    add_child(button)

func _button_pressed():
    print("Hello world!")
```

```
public override void _Ready()
{
    var button = new Button();
    button.Text = "Click me";
    button.Pressed += ButtonPressed;
    AddChild(button);
}

private void ButtonPressed()
{
    GD.Print("Hello world!");
}
```

See also BaseButton which contains common properties and methods associated with this node. **Note:** Buttons do not detect touch input and therefore don't support multitouch, since mouse emulation can only press one button at a given time. Use TouchScreenButton for buttons that trigger gameplay movement or actions.

## Quick Reference

```
[properties]
alignment: int (HorizontalAlignment) = 1
autowrap_mode: int (TextServer.AutowrapMode) = 0
autowrap_trim_flags: int (TextServer.LineBreakFlag) = 128
clip_text: bool = false
expand_icon: bool = false
flat: bool = false
icon: Texture2D
icon_alignment: int (HorizontalAlignment) = 0
language: String = ""
text: String = ""
text_direction: int (Control.TextDirection) = 0
text_overrun_behavior: int (TextServer.OverrunBehavior) = 0
vertical_icon_alignment: int (VerticalAlignment) = 1
```

## Tutorials

- [2D Dodge The Creeps Demo](https://godotengine.org/asset-library/asset/2712)
- [Operating System Testing Demo](https://godotengine.org/asset-library/asset/2789)

## Properties

- alignment: int (HorizontalAlignment) = 1 [set set_text_alignment; get get_text_alignment]
  Text alignment policy for the button's text.

- autowrap_mode: int (TextServer.AutowrapMode) = 0 [set set_autowrap_mode; get get_autowrap_mode]
  If set to something other than TextServer.AUTOWRAP_OFF, the text gets wrapped inside the node's bounding rectangle.

- autowrap_trim_flags: int (TextServer.LineBreakFlag) = 128 [set set_autowrap_trim_flags; get get_autowrap_trim_flags]
  Autowrap space trimming flags. See TextServer.BREAK_TRIM_START_EDGE_SPACES and TextServer.BREAK_TRIM_END_EDGE_SPACES for more info.

- clip_text: bool = false [set set_clip_text; get get_clip_text]
  If true, text that is too large to fit the button is clipped horizontally. If false, the button will always be wide enough to hold the text. The text is not vertically clipped, and the button's height is not affected by this property.

- expand_icon: bool = false [set set_expand_icon; get is_expand_icon]
  When enabled, the button's icon will expand/shrink to fit the button's size while keeping its aspect. See also [theme_item icon_max_width].

- flat: bool = false [set set_flat; get is_flat]
  Flat buttons don't display decoration.

- icon: Texture2D [set set_button_icon; get get_button_icon]
  Button's icon, if text is present the icon will be placed before the text. To edit margin and spacing of the icon, use [theme_item h_separation] theme property and content_margin_* properties of the used StyleBoxes.

- icon_alignment: int (HorizontalAlignment) = 0 [set set_icon_alignment; get get_icon_alignment]
  Specifies if the icon should be aligned horizontally to the left, right, or center of a button. Uses the same HorizontalAlignment constants as the text alignment. If centered horizontally and vertically, text will draw on top of the icon.

- language: String = "" [set set_language; get get_language]
  Language code used for line-breaking and text shaping algorithms. If left empty, the current locale is used instead.

- text: String = "" [set set_text; get get_text]
  The button's text that will be displayed inside the button's area.

- text_direction: int (Control.TextDirection) = 0 [set set_text_direction; get get_text_direction]
  Base text writing direction.

- text_overrun_behavior: int (TextServer.OverrunBehavior) = 0 [set set_text_overrun_behavior; get get_text_overrun_behavior]
  Sets the clipping behavior when the text exceeds the node's bounding rectangle.

- vertical_icon_alignment: int (VerticalAlignment) = 1 [set set_vertical_icon_alignment; get get_vertical_icon_alignment]
  Specifies if the icon should be aligned vertically to the top, bottom, or center of a button. Uses the same VerticalAlignment constants as the text alignment. If centered horizontally and vertically, text will draw on top of the icon.

## Theme Items

- font_color: Color [color] = Color(0.875, 0.875, 0.875, 1)
  Default text Color of the Button.

- font_disabled_color: Color [color] = Color(0.875, 0.875, 0.875, 0.5)
  Text Color used when the Button is disabled.

- font_focus_color: Color [color] = Color(0.95, 0.95, 0.95, 1)
  Text Color used when the Button is focused. Only replaces the normal text color of the button. Disabled, hovered, and pressed states take precedence over this color.

- font_hover_color: Color [color] = Color(0.95, 0.95, 0.95, 1)
  Text Color used when the Button is being hovered.

- font_hover_pressed_color: Color [color] = Color(1, 1, 1, 1)
  Text Color used when the Button is being hovered and pressed.

- font_outline_color: Color [color] = Color(0, 0, 0, 1)
  The tint of text outline of the Button.

- font_pressed_color: Color [color] = Color(1, 1, 1, 1)
  Text Color used when the Button is being pressed.

- icon_disabled_color: Color [color] = Color(1, 1, 1, 0.4)
  Icon modulate Color used when the Button is disabled.

- icon_focus_color: Color [color] = Color(1, 1, 1, 1)
  Icon modulate Color used when the Button is focused. Only replaces the normal modulate color of the button. Disabled, hovered, and pressed states take precedence over this color.

- icon_hover_color: Color [color] = Color(1, 1, 1, 1)
  Icon modulate Color used when the Button is being hovered.

- icon_hover_pressed_color: Color [color] = Color(1, 1, 1, 1)
  Icon modulate Color used when the Button is being hovered and pressed.

- icon_normal_color: Color [color] = Color(1, 1, 1, 1)
  Default icon modulate Color of the Button.

- icon_pressed_color: Color [color] = Color(1, 1, 1, 1)
  Icon modulate Color used when the Button is being pressed.

- align_to_largest_stylebox: int [constant] = 0
  This constant acts as a boolean. If true, the minimum size of the button and text/icon alignment is always based on the largest stylebox margins, otherwise it's based on the current button state stylebox margins.

- h_separation: int [constant] = 4
  The horizontal space between Button's icon and text. Negative values will be treated as 0 when used.

- icon_max_width: int [constant] = 0
  The maximum allowed width of the Button's icon. This limit is applied on top of the default size of the icon, or its expanded size if expand_icon is true. The height is adjusted according to the icon's ratio. If the button has additional icons (e.g. CheckBox), they will also be limited.

- line_spacing: int [constant] = 0
  Additional vertical spacing between lines (in pixels), spacing is added to line descent. This value can be negative.

- outline_size: int [constant] = 0
  The size of the text outline. **Note:** If using a font with FontFile.multichannel_signed_distance_field enabled, its FontFile.msdf_pixel_range must be set to at least *twice* the value of [theme_item outline_size] for outline rendering to look correct. Otherwise, the outline may appear to be cut off earlier than intended.

- font: Font [font]
  Font of the Button's text.

- font_size: int [font_size]
  Font size of the Button's text.

- icon: Texture2D [icon]
  Default icon for the Button. Appears only if icon is not assigned.

- disabled: StyleBox [style]
  StyleBox used when the Button is disabled.

- disabled_mirrored: StyleBox [style]
  StyleBox used when the Button is disabled (for right-to-left layouts).

- focus: StyleBox [style]
  StyleBox used when the Button is focused. The [theme_item focus] StyleBox is displayed *over* the base StyleBox, so a partially transparent StyleBox should be used to ensure the base StyleBox remains visible. A StyleBox that represents an outline or an underline works well for this purpose. To disable the focus visual effect, assign a StyleBoxEmpty resource. Note that disabling the focus visual effect will harm keyboard/controller navigation usability, so this is not recommended for accessibility reasons.

- hover: StyleBox [style]
  StyleBox used when the Button is being hovered.

- hover_mirrored: StyleBox [style]
  StyleBox used when the Button is being hovered (for right-to-left layouts).

- hover_pressed: StyleBox [style]
  StyleBox used when the Button is being pressed and hovered at the same time.

- hover_pressed_mirrored: StyleBox [style]
  StyleBox used when the Button is being pressed and hovered at the same time (for right-to-left layouts).

- normal: StyleBox [style]
  Default StyleBox for the Button.

- normal_mirrored: StyleBox [style]
  Default StyleBox for the Button (for right-to-left layouts).

- pressed: StyleBox [style]
  StyleBox used when the Button is being pressed.

- pressed_mirrored: StyleBox [style]
  StyleBox used when the Button is being pressed (for right-to-left layouts).
