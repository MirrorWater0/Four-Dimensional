# VScrollBar

## Meta

- Name: VScrollBar
- Source: VScrollBar.xml
- Inherits: ScrollBar
- Inheritance Chain: VScrollBar -> ScrollBar -> Range -> Control -> CanvasItem -> Node -> Object

## Brief Description

A vertical scrollbar that goes from top (min) to bottom (max).

## Description

A vertical scrollbar, typically used to navigate through content that extends beyond the visible height of a control. It is a Range-based control and goes from top (min) to bottom (max). Note that this direction is the opposite of VSlider's.

## Quick Reference

```
[properties]
size_flags_horizontal: int (Control.SizeFlags) = 0
size_flags_vertical: int (Control.SizeFlags) = 1
```

## Properties

- size_flags_horizontal: int (Control.SizeFlags) = 0 [set set_h_size_flags; get get_h_size_flags; override Control]

- size_flags_vertical: int (Control.SizeFlags) = 1 [set set_v_size_flags; get get_v_size_flags; override Control]

## Theme Items

- padding_left: int [constant] = 0
  Padding between the left of the [theme_item ScrollBar.scroll] element and the [theme_item ScrollBar.grabber]. **Note:** To apply vertical padding, modify the top/bottom content margins of [theme_item ScrollBar.scroll] instead.

- padding_right: int [constant] = 0
  Padding between the right of the [theme_item ScrollBar.scroll] element and the [theme_item ScrollBar.grabber]. **Note:** To apply vertical padding, modify the top/bottom content margins of [theme_item ScrollBar.scroll] instead.
