# HScrollBar

## Meta

- Name: HScrollBar
- Source: HScrollBar.xml
- Inherits: ScrollBar
- Inheritance Chain: HScrollBar -> ScrollBar -> Range -> Control -> CanvasItem -> Node -> Object

## Brief Description

A horizontal scrollbar that goes from left (min) to right (max).

## Description

A horizontal scrollbar, typically used to navigate through content that extends beyond the visible width of a control. It is a Range-based control and goes from left (min) to right (max).

## Theme Items

- padding_bottom: int [constant] = 0
  Padding between the bottom of the [theme_item ScrollBar.scroll] element and the [theme_item ScrollBar.grabber]. **Note:** To apply horizontal padding, modify the left/right content margins of [theme_item ScrollBar.scroll] instead.

- padding_top: int [constant] = 0
  Padding between the top of the [theme_item ScrollBar.scroll] element and the [theme_item ScrollBar.grabber]. **Note:** To apply horizontal padding, modify the left/right content margins of [theme_item ScrollBar.scroll] instead.
