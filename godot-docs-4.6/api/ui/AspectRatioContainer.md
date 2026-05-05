# AspectRatioContainer

## Meta

- Name: AspectRatioContainer
- Source: AspectRatioContainer.xml
- Inherits: Container
- Inheritance Chain: AspectRatioContainer -> Container -> Control -> CanvasItem -> Node -> Object

## Brief Description

A container that preserves the proportions of its child controls.

## Description

A container type that arranges its child controls in a way that preserves their proportions automatically when the container is resized. Useful when a container has a dynamic size and the child nodes must adjust their sizes accordingly without losing their aspect ratios.

## Quick Reference

```
[properties]
alignment_horizontal: int (AspectRatioContainer.AlignmentMode) = 1
alignment_vertical: int (AspectRatioContainer.AlignmentMode) = 1
ratio: float = 1.0
stretch_mode: int (AspectRatioContainer.StretchMode) = 2
```

## Tutorials

- [Using Containers]($DOCS_URL/tutorials/ui/gui_containers.html)

## Properties

- alignment_horizontal: int (AspectRatioContainer.AlignmentMode) = 1 [set set_alignment_horizontal; get get_alignment_horizontal]
  Specifies the horizontal relative position of child controls.

- alignment_vertical: int (AspectRatioContainer.AlignmentMode) = 1 [set set_alignment_vertical; get get_alignment_vertical]
  Specifies the vertical relative position of child controls.

- ratio: float = 1.0 [set set_ratio; get get_ratio]
  The aspect ratio to enforce on child controls. This is the width divided by the height. The ratio depends on the stretch_mode.

- stretch_mode: int (AspectRatioContainer.StretchMode) = 2 [set set_stretch_mode; get get_stretch_mode]
  The stretch mode used to align child controls.

## Constants

### Enum StretchMode

- STRETCH_WIDTH_CONTROLS_HEIGHT = 0
  The height of child controls is automatically adjusted based on the width of the container.

- STRETCH_HEIGHT_CONTROLS_WIDTH = 1
  The width of child controls is automatically adjusted based on the height of the container.

- STRETCH_FIT = 2
  The bounding rectangle of child controls is automatically adjusted to fit inside the container while keeping the aspect ratio.

- STRETCH_COVER = 3
  The width and height of child controls is automatically adjusted to make their bounding rectangle cover the entire area of the container while keeping the aspect ratio. When the bounding rectangle of child controls exceed the container's size and Control.clip_contents is enabled, this allows to show only the container's area restricted by its own bounding rectangle.

### Enum AlignmentMode

- ALIGNMENT_BEGIN = 0
  Aligns child controls with the beginning (left or top) of the container.

- ALIGNMENT_CENTER = 1
  Aligns child controls with the center of the container.

- ALIGNMENT_END = 2
  Aligns child controls with the end (right or bottom) of the container.
