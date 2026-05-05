# InputEventPanGesture

## Meta

- Name: InputEventPanGesture
- Source: InputEventPanGesture.xml
- Inherits: InputEventGesture
- Inheritance Chain: InputEventPanGesture -> InputEventGesture -> InputEventWithModifiers -> InputEventFromWindow -> InputEvent -> Resource -> RefCounted -> Object

## Brief Description

Represents a panning touch gesture.

## Description

Stores information about pan gestures. A pan gesture is performed when the user swipes the touch screen with two fingers. It's typically used for panning/scrolling. **Note:** On Android, this requires the ProjectSettings.input_devices/pointing/android/enable_pan_and_scale_gestures project setting to be enabled.

## Quick Reference

```
[properties]
delta: Vector2 = Vector2(0, 0)
```

## Tutorials

- [Using InputEvent]($DOCS_URL/tutorials/inputs/inputevent.html)

## Properties

- delta: Vector2 = Vector2(0, 0) [set set_delta; get get_delta]
  Panning amount since last pan event.
