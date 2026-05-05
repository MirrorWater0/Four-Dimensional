# MarginContainer

## Meta

- Name: MarginContainer
- Source: MarginContainer.xml
- Inherits: Container
- Inheritance Chain: MarginContainer -> Container -> Control -> CanvasItem -> Node -> Object

## Brief Description

A container that keeps a margin around its child controls.

## Description

MarginContainer adds an adjustable margin on each side of its child controls. The margins are added around all children, not around each individual one. To control the MarginContainer's margins, use the margin_* theme properties listed below. **Note:** The margin sizes are theme overrides, not normal properties. This is an example of how to change them in code:

```
# This code sample assumes the current script is extending MarginContainer.
var margin_value = 100
add_theme_constant_override("margin_top", margin_value)
add_theme_constant_override("margin_left", margin_value)
add_theme_constant_override("margin_bottom", margin_value)
add_theme_constant_override("margin_right", margin_value)
```

```
// This code sample assumes the current script is extending MarginContainer.
int marginValue = 100;
AddThemeConstantOverride("margin_top", marginValue);
AddThemeConstantOverride("margin_left", marginValue);
AddThemeConstantOverride("margin_bottom", marginValue);
AddThemeConstantOverride("margin_right", marginValue);
```

## Tutorials

- [Using Containers]($DOCS_URL/tutorials/ui/gui_containers.html)

## Theme Items

- margin_bottom: int [constant] = 0
  Offsets towards the inside direct children of the container by this amount of pixels from the bottom.

- margin_left: int [constant] = 0
  Offsets towards the inside direct children of the container by this amount of pixels from the left.

- margin_right: int [constant] = 0
  Offsets towards the inside direct children of the container by this amount of pixels from the right.

- margin_top: int [constant] = 0
  Offsets towards the inside direct children of the container by this amount of pixels from the top.
