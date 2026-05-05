# SubViewportContainer

## Meta

- Name: SubViewportContainer
- Source: SubViewportContainer.xml
- Inherits: Container
- Inheritance Chain: SubViewportContainer -> Container -> Control -> CanvasItem -> Node -> Object

## Brief Description

A container used for displaying the contents of a SubViewport.

## Description

A container that displays the contents of underlying SubViewport child nodes. It uses the combined size of the SubViewports as minimum size, unless stretch is enabled. **Note:** Changing a SubViewportContainer's Control.scale will cause its contents to appear distorted. To change its visual size without causing distortion, adjust the node's margins instead (if it's not already in a container). **Note:** The SubViewportContainer forwards mouse-enter and mouse-exit notifications to its sub-viewports.

## Quick Reference

```
[methods]
_propagate_input_event(event: InputEvent) -> bool [virtual const]

[properties]
focus_mode: int (Control.FocusMode) = 1
mouse_target: bool = false
stretch: bool = false
stretch_shrink: int = 1
```

## Methods

- _propagate_input_event(event: InputEvent) -> bool [virtual const]
  Virtual method to be implemented by the user. If it returns true, the event is propagated to SubViewport children. Propagation doesn't happen if it returns false. If the function is not implemented, all events are propagated to SubViewports.

## Properties

- focus_mode: int (Control.FocusMode) = 1 [set set_focus_mode; get get_focus_mode; override Control]

- mouse_target: bool = false [set set_mouse_target; get is_mouse_target_enabled]
  Configure, if either the SubViewportContainer or alternatively the Control nodes of its SubViewport children should be available as targets of mouse-related functionalities, like identifying the drop target in drag-and-drop operations or cursor shape of hovered Control node. If false, the Control nodes inside its SubViewport children are considered as targets. If true, the SubViewportContainer itself will be considered as a target.

- stretch: bool = false [set set_stretch; get is_stretch_enabled]
  If true, the sub-viewport will be automatically resized to the control's size. **Note:** If true, this will prohibit changing SubViewport.size of its children manually.

- stretch_shrink: int = 1 [set set_stretch_shrink; get get_stretch_shrink]
  Divides the sub-viewport's effective resolution by this value while preserving its scale. This can be used to speed up rendering. For example, a 1280×720 sub-viewport with stretch_shrink set to 2 will be rendered at 640×360 while occupying the same size in the container. **Note:** stretch must be true for this property to work.
