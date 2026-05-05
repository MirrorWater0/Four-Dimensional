# BoxContainer

## Meta

- Name: BoxContainer
- Source: BoxContainer.xml
- Inherits: Container
- Inheritance Chain: BoxContainer -> Container -> Control -> CanvasItem -> Node -> Object

## Brief Description

A container that arranges its child controls horizontally or vertically.

## Description

A container that arranges its child controls horizontally or vertically, rearranging them automatically when their minimum size changes.

## Quick Reference

```
[methods]
add_spacer(begin: bool) -> Control

[properties]
alignment: int (BoxContainer.AlignmentMode) = 0
vertical: bool = false
```

## Tutorials

- [Using Containers]($DOCS_URL/tutorials/ui/gui_containers.html)

## Methods

- add_spacer(begin: bool) -> Control
  Adds a Control node to the box as a spacer. If begin is true, it will insert the Control node in front of all other children.

## Properties

- alignment: int (BoxContainer.AlignmentMode) = 0 [set set_alignment; get get_alignment]
  The alignment of the container's children (must be one of ALIGNMENT_BEGIN, ALIGNMENT_CENTER, or ALIGNMENT_END).

- vertical: bool = false [set set_vertical; get is_vertical]
  If true, the BoxContainer will arrange its children vertically, rather than horizontally. Can't be changed when using HBoxContainer and VBoxContainer.

## Constants

### Enum AlignmentMode

- ALIGNMENT_BEGIN = 0
  The child controls will be arranged at the beginning of the container, i.e. top if orientation is vertical, left if orientation is horizontal (right for RTL layout).

- ALIGNMENT_CENTER = 1
  The child controls will be centered in the container.

- ALIGNMENT_END = 2
  The child controls will be arranged at the end of the container, i.e. bottom if orientation is vertical, right if orientation is horizontal (left for RTL layout).

## Theme Items

- separation: int [constant] = 4
  The space between the BoxContainer's elements, in pixels.
