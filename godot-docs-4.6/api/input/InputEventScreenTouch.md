# InputEventScreenTouch

## Meta

- Name: InputEventScreenTouch
- Source: InputEventScreenTouch.xml
- Inherits: InputEventFromWindow
- Inheritance Chain: InputEventScreenTouch -> InputEventFromWindow -> InputEvent -> Resource -> RefCounted -> Object

## Brief Description

Represents a screen touch event.

## Description

Stores information about multi-touch press/release input events. Supports touch press, touch release and index for multi-touch count and order.

## Quick Reference

```
[properties]
canceled: bool = false
double_tap: bool = false
index: int = 0
position: Vector2 = Vector2(0, 0)
pressed: bool = false
```

## Tutorials

- [Using InputEvent]($DOCS_URL/tutorials/inputs/inputevent.html)

## Properties

- canceled: bool = false [set set_canceled; get is_canceled]
  If true, the touch event has been canceled.

- double_tap: bool = false [set set_double_tap; get is_double_tap]
  If true, the touch's state is a double tap.

- index: int = 0 [set set_index; get get_index]
  The touch index in the case of a multi-touch event. One index = one finger.

- position: Vector2 = Vector2(0, 0) [set set_position; get get_position]
  The touch position in the viewport the node is in, using the coordinate system of this viewport.

- pressed: bool = false [set set_pressed; get is_pressed]
  If true, the touch's state is pressed. If false, the touch's state is released.
