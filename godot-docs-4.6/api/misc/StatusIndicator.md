# StatusIndicator

## Meta

- Name: StatusIndicator
- Source: StatusIndicator.xml
- Inherits: Node
- Inheritance Chain: StatusIndicator -> Node -> Object

## Brief Description

Application status indicator (aka notification area icon). **Note:** Status indicator is implemented on macOS and Windows.

## Quick Reference

```
[methods]
get_rect() -> Rect2 [const]

[properties]
icon: Texture2D
menu: NodePath = NodePath("")
tooltip: String = ""
visible: bool = true
```

## Methods

- get_rect() -> Rect2 [const]
  Returns the status indicator rectangle in screen coordinates. If this status indicator is not visible, returns an empty Rect2.

## Properties

- icon: Texture2D [set set_icon; get get_icon]
  Status indicator icon.

- menu: NodePath = NodePath("") [set set_menu; get get_menu]
  Status indicator native popup menu. If this is set, the pressed signal is not emitted. **Note:** Native popup is only supported if NativeMenu supports NativeMenu.FEATURE_POPUP_MENU feature.

- tooltip: String = "" [set set_tooltip; get get_tooltip]
  Status indicator tooltip.

- visible: bool = true [set set_visible; get is_visible]
  If true, the status indicator is visible.

## Signals

- pressed(mouse_button: int, mouse_position: Vector2i)
  Emitted when the status indicator is pressed.
