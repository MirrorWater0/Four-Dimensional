# FoldableContainer

## Meta

- Name: FoldableContainer
- Source: FoldableContainer.xml
- Inherits: Container
- Inheritance Chain: FoldableContainer -> Container -> Control -> CanvasItem -> Node -> Object

## Brief Description

A container that can be expanded/collapsed.

## Description

A container that can be expanded/collapsed, with a title that can be filled with controls, such as buttons. This is also called an accordion. The title can be positioned at the top or bottom of the container. The container can be expanded or collapsed by clicking the title or by pressing ui_accept when focused. Child control nodes are hidden when the container is collapsed. Ignores non-control children. A FoldableContainer can be grouped with other FoldableContainers so that only one of them can be opened at a time; see foldable_group and FoldableGroup.

## Quick Reference

```
[methods]
add_title_bar_control(control: Control) -> void
expand() -> void
fold() -> void
remove_title_bar_control(control: Control) -> void

[properties]
focus_mode: int (Control.FocusMode) = 2
foldable_group: FoldableGroup
folded: bool = false
language: String = ""
mouse_filter: int (Control.MouseFilter) = 0
title: String = ""
title_alignment: int (HorizontalAlignment) = 0
title_position: int (FoldableContainer.TitlePosition) = 0
title_text_direction: int (Control.TextDirection) = 0
title_text_overrun_behavior: int (TextServer.OverrunBehavior) = 0
```

## Methods

- add_title_bar_control(control: Control) -> void
  Adds a Control that will be placed next to the container's title, obscuring the clickable area. Prime usage is adding Button nodes, but it can be any Control. The control will be added as a child of this container and removed from previous parent if necessary. The controls will be placed aligned to the right, with the first added control being the leftmost one.

- expand() -> void
  Expands the container and emits folding_changed.

- fold() -> void
  Folds the container and emits folding_changed.

- remove_title_bar_control(control: Control) -> void
  Removes a Control added with add_title_bar_control(). The node is not freed automatically, you need to use Node.queue_free().

## Properties

- focus_mode: int (Control.FocusMode) = 2 [set set_focus_mode; get get_focus_mode; override Control]

- foldable_group: FoldableGroup [set set_foldable_group; get get_foldable_group]
  The FoldableGroup associated with the container. When multiple FoldableContainer nodes share the same group, only one of them is allowed to be unfolded.

- folded: bool = false [set set_folded; get is_folded]
  If true, the container will becomes folded and will hide all its children.

- language: String = "" [set set_language; get get_language]
  Language code used for text shaping algorithms. If left empty, the current locale is used instead.

- mouse_filter: int (Control.MouseFilter) = 0 [set set_mouse_filter; get get_mouse_filter; override Control]

- title: String = "" [set set_title; get get_title]
  The container's title text.

- title_alignment: int (HorizontalAlignment) = 0 [set set_title_alignment; get get_title_alignment]
  Title's horizontal text alignment.

- title_position: int (FoldableContainer.TitlePosition) = 0 [set set_title_position; get get_title_position]
  Title's position.

- title_text_direction: int (Control.TextDirection) = 0 [set set_title_text_direction; get get_title_text_direction]
  Title text writing direction.

- title_text_overrun_behavior: int (TextServer.OverrunBehavior) = 0 [set set_title_text_overrun_behavior; get get_title_text_overrun_behavior]
  Defines the behavior of the title when the text is longer than the available space.

## Signals

- folding_changed(is_folded: bool)
  Emitted when the container is folded/expanded.

## Constants

### Enum TitlePosition

- POSITION_TOP = 0
  Makes the title appear at the top of the container.

- POSITION_BOTTOM = 1
  Makes the title appear at the bottom of the container. Also makes all StyleBoxes flipped vertically.

## Theme Items

- collapsed_font_color: Color [color] = Color(1, 1, 1, 1)
  The title's font color when collapsed.

- font_color: Color [color] = Color(0.875, 0.875, 0.875, 1)
  The title's font color when expanded.

- font_outline_color: Color [color] = Color(1, 1, 1, 1)
  The title's font outline color.

- hover_font_color: Color [color] = Color(0.95, 0.95, 0.95, 1)
  The title's font hover color.

- h_separation: int [constant] = 2
  The horizontal separation between the title's icon and text, and between title bar controls.

- outline_size: int [constant] = 0
  The title's font outline size.

- font: Font [font]
  The title's font.

- font_size: int [font_size]
  The title's font size.

- expanded_arrow: Texture2D [icon]
  The title's icon used when expanded.

- expanded_arrow_mirrored: Texture2D [icon]
  The title's icon used when expanded (for bottom title).

- folded_arrow: Texture2D [icon]
  The title's icon used when folded (for left-to-right layouts).

- folded_arrow_mirrored: Texture2D [icon]
  The title's icon used when collapsed (for right-to-left layouts).

- focus: StyleBox [style]
  Background used when FoldableContainer has GUI focus. The [theme_item focus] StyleBox is displayed *over* the base StyleBox, so a partially transparent StyleBox should be used to ensure the base StyleBox remains visible. A StyleBox that represents an outline or an underline works well for this purpose. To disable the focus visual effect, assign a StyleBoxEmpty resource. Note that disabling the focus visual effect will harm keyboard/controller navigation usability, so this is not recommended for accessibility reasons.

- panel: StyleBox [style]
  Default background for the FoldableContainer.

- title_collapsed_hover_panel: StyleBox [style]
  Background used when the mouse cursor enters the title's area when collapsed.

- title_collapsed_panel: StyleBox [style]
  Default background for the FoldableContainer's title when collapsed.

- title_hover_panel: StyleBox [style]
  Background used when the mouse cursor enters the title's area when expanded.

- title_panel: StyleBox [style]
  Default background for the FoldableContainer's title when expanded.
