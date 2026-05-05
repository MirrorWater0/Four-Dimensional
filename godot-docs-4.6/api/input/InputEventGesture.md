# InputEventGesture

## Meta

- Name: InputEventGesture
- Source: InputEventGesture.xml
- Inherits: InputEventWithModifiers
- Inheritance Chain: InputEventGesture -> InputEventWithModifiers -> InputEventFromWindow -> InputEvent -> Resource -> RefCounted -> Object

## Brief Description

Abstract base class for touch gestures.

## Description

InputEventGestures are sent when a user performs a supported gesture on a touch screen. Gestures can't be emulated using mouse, because they typically require multi-touch.

## Quick Reference

```
[properties]
position: Vector2 = Vector2(0, 0)
```

## Tutorials

- [Using InputEvent]($DOCS_URL/tutorials/inputs/inputevent.html)

## Properties

- position: Vector2 = Vector2(0, 0) [set set_position; get get_position]
  The local gesture position relative to the Viewport. If used in Control._gui_input(), the position is relative to the current Control that received this gesture.
