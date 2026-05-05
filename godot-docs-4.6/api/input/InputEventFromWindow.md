# InputEventFromWindow

## Meta

- Name: InputEventFromWindow
- Source: InputEventFromWindow.xml
- Inherits: InputEvent
- Inheritance Chain: InputEventFromWindow -> InputEvent -> Resource -> RefCounted -> Object

## Brief Description

Abstract base class for Viewport-based input events.

## Description

InputEventFromWindow represents events specifically received by windows. This includes mouse events, keyboard events in focused windows or touch screen actions.

## Quick Reference

```
[properties]
window_id: int = 0
```

## Properties

- window_id: int = 0 [set set_window_id; get get_window_id]
  The ID of a Window that received this event.
