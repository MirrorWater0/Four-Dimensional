# GraphFrame

## Meta

- Name: GraphFrame
- Source: GraphFrame.xml
- Inherits: GraphElement
- Inheritance Chain: GraphFrame -> GraphElement -> Container -> Control -> CanvasItem -> Node -> Object

## Brief Description

GraphFrame is a special GraphElement that can be used to organize other GraphElements inside a GraphEdit.

## Description

GraphFrame is a special GraphElement to which other GraphElements can be attached. It can be configured to automatically resize to enclose all attached GraphElements. If the frame is moved, all the attached GraphElements inside it will be moved as well. A GraphFrame is always kept behind the connection layer and other GraphElements inside a GraphEdit.

## Quick Reference

```
[methods]
get_titlebar_hbox() -> HBoxContainer

[properties]
autoshrink_enabled: bool = true
autoshrink_margin: int = 40
drag_margin: int = 16
mouse_filter: int (Control.MouseFilter) = 0
tint_color: Color = Color(0.3, 0.3, 0.3, 0.75)
tint_color_enabled: bool = false
title: String = ""
```

## Methods

- get_titlebar_hbox() -> HBoxContainer
  Returns the HBoxContainer used for the title bar, only containing a Label for displaying the title by default. This can be used to add custom controls to the title bar such as option or close buttons.

## Properties

- autoshrink_enabled: bool = true [set set_autoshrink_enabled; get is_autoshrink_enabled]
  If true, the frame's rect will be adjusted automatically to enclose all attached GraphElements.

- autoshrink_margin: int = 40 [set set_autoshrink_margin; get get_autoshrink_margin]
  The margin around the attached nodes that is used to calculate the size of the frame when autoshrink_enabled is true.

- drag_margin: int = 16 [set set_drag_margin; get get_drag_margin]
  The margin inside the frame that can be used to drag the frame.

- mouse_filter: int (Control.MouseFilter) = 0 [set set_mouse_filter; get get_mouse_filter; override Control]

- tint_color: Color = Color(0.3, 0.3, 0.3, 0.75) [set set_tint_color; get get_tint_color]
  The color of the frame when tint_color_enabled is true.

- tint_color_enabled: bool = false [set set_tint_color_enabled; get is_tint_color_enabled]
  If true, the tint color will be used to tint the frame.

- title: String = "" [set set_title; get get_title]
  Title of the frame.

## Signals

- autoshrink_changed()
  Emitted when autoshrink_enabled or autoshrink_margin changes.

## Theme Items

- resizer_color: Color [color] = Color(0.875, 0.875, 0.875, 1)
  The color modulation applied to the resizer icon.

- panel: StyleBox [style]
  The default StyleBox used for the background of the GraphFrame.

- panel_selected: StyleBox [style]
  The StyleBox used for the background of the GraphFrame when it is selected.

- titlebar: StyleBox [style]
  The StyleBox used for the title bar of the GraphFrame.

- titlebar_selected: StyleBox [style]
  The StyleBox used for the title bar of the GraphFrame when it is selected.
