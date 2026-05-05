# GraphElement

## Meta

- Name: GraphElement
- Source: GraphElement.xml
- Inherits: Container
- Inheritance Chain: GraphElement -> Container -> Control -> CanvasItem -> Node -> Object

## Brief Description

A container that represents a basic element that can be placed inside a GraphEdit control.

## Description

GraphElement allows to create custom elements for a GraphEdit graph. By default such elements can be selected, resized, and repositioned, but they cannot be connected. For a graph element that allows for connections see GraphNode.

## Quick Reference

```
[properties]
draggable: bool = true
position_offset: Vector2 = Vector2(0, 0)
resizable: bool = false
scaling_menus: bool = false
selectable: bool = true
selected: bool = false
```

## Properties

- draggable: bool = true [set set_draggable; get is_draggable]
  If true, the user can drag the GraphElement.

- position_offset: Vector2 = Vector2(0, 0) [set set_position_offset; get get_position_offset]
  The offset of the GraphElement, relative to the scroll offset of the GraphEdit.

- resizable: bool = false [set set_resizable; get is_resizable]
  If true, the user can resize the GraphElement. **Note:** Dragging the handle will only emit the resize_request and resize_end signals, the GraphElement needs to be resized manually.

- scaling_menus: bool = false [set set_scaling_menus; get is_scaling_menus]
  If true, PopupMenus that are descendants of the GraphElement are scaled with the GraphEdit zoom.

- selectable: bool = true [set set_selectable; get is_selectable]
  If true, the user can select the GraphElement.

- selected: bool = false [set set_selected; get is_selected]
  If true, the GraphElement is selected.

## Signals

- delete_request()
  Emitted when removing the GraphElement is requested.

- dragged(from: Vector2, to: Vector2)
  Emitted when the GraphElement is dragged.

- node_deselected()
  Emitted when the GraphElement is deselected.

- node_selected()
  Emitted when the GraphElement is selected.

- position_offset_changed()
  Emitted when the GraphElement is moved.

- raise_request()
  Emitted when displaying the GraphElement over other ones is requested. Happens on focusing (clicking into) the GraphElement.

- resize_end(new_size: Vector2)
  Emitted when releasing the mouse button after dragging the resizer handle (see resizable).

- resize_request(new_size: Vector2)
  Emitted when resizing the GraphElement is requested. Happens on dragging the resizer handle (see resizable).

## Theme Items

- resizer: Texture2D [icon]
  The icon used for the resizer, visible when resizable is enabled.
