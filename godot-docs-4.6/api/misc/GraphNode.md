# GraphNode

## Meta

- Name: GraphNode
- Source: GraphNode.xml
- Inherits: GraphElement
- Inheritance Chain: GraphNode -> GraphElement -> Container -> Control -> CanvasItem -> Node -> Object

## Brief Description

A container with connection ports, representing a node in a GraphEdit.

## Description

GraphNode allows to create nodes for a GraphEdit graph with customizable content based on its child controls. GraphNode is derived from Container and it is responsible for placing its children on screen. This works similar to VBoxContainer. Children, in turn, provide GraphNode with so-called slots, each of which can have a connection port on either side. Each GraphNode slot is defined by its index and can provide the node with up to two ports: one on the left, and one on the right. By convention the left port is also referred to as the **input port** and the right port is referred to as the **output port**. Each port can be enabled and configured individually, using different type and color. The type is an arbitrary value that you can define using your own considerations. The parent GraphEdit will receive this information on each connect and disconnect request. Slots can be configured in the Inspector dock once you add at least one child Control. The properties are grouped by each slot's index in the "Slot" section. **Note:** While GraphNode is set up using slots and slot indices, connections are made between the ports which are enabled. Because of that GraphEdit uses the port's index and not the slot's index. You can use get_input_port_slot() and get_output_port_slot() to get the slot index from the port index.

## Quick Reference

```
[methods]
_draw_port(slot_index: int, position: Vector2i, left: bool, color: Color) -> void [virtual]
clear_all_slots() -> void
clear_slot(slot_index: int) -> void
get_input_port_color(port_idx: int) -> Color
get_input_port_count() -> int
get_input_port_position(port_idx: int) -> Vector2
get_input_port_slot(port_idx: int) -> int
get_input_port_type(port_idx: int) -> int
get_output_port_color(port_idx: int) -> Color
get_output_port_count() -> int
get_output_port_position(port_idx: int) -> Vector2
get_output_port_slot(port_idx: int) -> int
get_output_port_type(port_idx: int) -> int
get_slot_color_left(slot_index: int) -> Color [const]
get_slot_color_right(slot_index: int) -> Color [const]
get_slot_custom_icon_left(slot_index: int) -> Texture2D [const]
get_slot_custom_icon_right(slot_index: int) -> Texture2D [const]
get_slot_metadata_left(slot_index: int) -> Variant [const]
get_slot_metadata_right(slot_index: int) -> Variant [const]
get_slot_type_left(slot_index: int) -> int [const]
get_slot_type_right(slot_index: int) -> int [const]
get_titlebar_hbox() -> HBoxContainer
is_slot_draw_stylebox(slot_index: int) -> bool [const]
is_slot_enabled_left(slot_index: int) -> bool [const]
is_slot_enabled_right(slot_index: int) -> bool [const]
set_slot(slot_index: int, enable_left_port: bool, type_left: int, color_left: Color, enable_right_port: bool, type_right: int, color_right: Color, custom_icon_left: Texture2D = null, custom_icon_right: Texture2D = null, draw_stylebox: bool = true) -> void
set_slot_color_left(slot_index: int, color: Color) -> void
set_slot_color_right(slot_index: int, color: Color) -> void
set_slot_custom_icon_left(slot_index: int, custom_icon: Texture2D) -> void
set_slot_custom_icon_right(slot_index: int, custom_icon: Texture2D) -> void
set_slot_draw_stylebox(slot_index: int, enable: bool) -> void
set_slot_enabled_left(slot_index: int, enable: bool) -> void
set_slot_enabled_right(slot_index: int, enable: bool) -> void
set_slot_metadata_left(slot_index: int, value: Variant) -> void
set_slot_metadata_right(slot_index: int, value: Variant) -> void
set_slot_type_left(slot_index: int, type: int) -> void
set_slot_type_right(slot_index: int, type: int) -> void

[properties]
focus_mode: int (Control.FocusMode) = 3
ignore_invalid_connection_type: bool = false
mouse_filter: int (Control.MouseFilter) = 0
slots_focus_mode: int (Control.FocusMode) = 3
title: String = ""
```

## Methods

- _draw_port(slot_index: int, position: Vector2i, left: bool, color: Color) -> void [virtual]

- clear_all_slots() -> void
  Disables all slots of the GraphNode. This will remove all input/output ports from the GraphNode.

- clear_slot(slot_index: int) -> void
  Disables the slot with the given slot_index. This will remove the corresponding input and output port from the GraphNode.

- get_input_port_color(port_idx: int) -> Color
  Returns the Color of the input port with the given port_idx.

- get_input_port_count() -> int
  Returns the number of slots with an enabled input port.

- get_input_port_position(port_idx: int) -> Vector2
  Returns the position of the input port with the given port_idx.

- get_input_port_slot(port_idx: int) -> int
  Returns the corresponding slot index of the input port with the given port_idx.

- get_input_port_type(port_idx: int) -> int
  Returns the type of the input port with the given port_idx.

- get_output_port_color(port_idx: int) -> Color
  Returns the Color of the output port with the given port_idx.

- get_output_port_count() -> int
  Returns the number of slots with an enabled output port.

- get_output_port_position(port_idx: int) -> Vector2
  Returns the position of the output port with the given port_idx.

- get_output_port_slot(port_idx: int) -> int
  Returns the corresponding slot index of the output port with the given port_idx.

- get_output_port_type(port_idx: int) -> int
  Returns the type of the output port with the given port_idx.

- get_slot_color_left(slot_index: int) -> Color [const]
  Returns the left (input) Color of the slot with the given slot_index.

- get_slot_color_right(slot_index: int) -> Color [const]
  Returns the right (output) Color of the slot with the given slot_index.

- get_slot_custom_icon_left(slot_index: int) -> Texture2D [const]
  Returns the left (input) custom Texture2D of the slot with the given slot_index.

- get_slot_custom_icon_right(slot_index: int) -> Texture2D [const]
  Returns the right (output) custom Texture2D of the slot with the given slot_index.

- get_slot_metadata_left(slot_index: int) -> Variant [const]
  Returns the left (input) metadata of the slot with the given slot_index.

- get_slot_metadata_right(slot_index: int) -> Variant [const]
  Returns the right (output) metadata of the slot with the given slot_index.

- get_slot_type_left(slot_index: int) -> int [const]
  Returns the left (input) type of the slot with the given slot_index.

- get_slot_type_right(slot_index: int) -> int [const]
  Returns the right (output) type of the slot with the given slot_index.

- get_titlebar_hbox() -> HBoxContainer
  Returns the HBoxContainer used for the title bar, only containing a Label for displaying the title by default. This can be used to add custom controls to the title bar such as option or close buttons.

- is_slot_draw_stylebox(slot_index: int) -> bool [const]
  Returns true if the background StyleBox of the slot with the given slot_index is drawn.

- is_slot_enabled_left(slot_index: int) -> bool [const]
  Returns true if left (input) side of the slot with the given slot_index is enabled.

- is_slot_enabled_right(slot_index: int) -> bool [const]
  Returns true if right (output) side of the slot with the given slot_index is enabled.

- set_slot(slot_index: int, enable_left_port: bool, type_left: int, color_left: Color, enable_right_port: bool, type_right: int, color_right: Color, custom_icon_left: Texture2D = null, custom_icon_right: Texture2D = null, draw_stylebox: bool = true) -> void
  Sets properties of the slot with the given slot_index. If enable_left_port/enable_right_port is true, a port will appear and the slot will be able to be connected from this side. With type_left/type_right an arbitrary type can be assigned to each port. Two ports can be connected if they share the same type, or if the connection between their types is allowed in the parent GraphEdit (see GraphEdit.add_valid_connection_type()). Keep in mind that the GraphEdit has the final say in accepting the connection. Type compatibility simply allows the GraphEdit.connection_request signal to be emitted. Ports can be further customized using color_left/color_right and custom_icon_left/custom_icon_right. The color parameter adds a tint to the icon. The custom icon can be used to override the default port dot. Additionally, draw_stylebox can be used to enable or disable drawing of the background stylebox for each slot. See [theme_item slot]. Individual properties can also be set using one of the set_slot_* methods. **Note:** This method only sets properties of the slot. To create the slot itself, add a Control-derived child to the GraphNode.

- set_slot_color_left(slot_index: int, color: Color) -> void
  Sets the Color of the left (input) side of the slot with the given slot_index to color.

- set_slot_color_right(slot_index: int, color: Color) -> void
  Sets the Color of the right (output) side of the slot with the given slot_index to color.

- set_slot_custom_icon_left(slot_index: int, custom_icon: Texture2D) -> void
  Sets the custom Texture2D of the left (input) side of the slot with the given slot_index to custom_icon.

- set_slot_custom_icon_right(slot_index: int, custom_icon: Texture2D) -> void
  Sets the custom Texture2D of the right (output) side of the slot with the given slot_index to custom_icon.

- set_slot_draw_stylebox(slot_index: int, enable: bool) -> void
  Toggles the background StyleBox of the slot with the given slot_index.

- set_slot_enabled_left(slot_index: int, enable: bool) -> void
  Toggles the left (input) side of the slot with the given slot_index. If enable is true, a port will appear on the left side and the slot will be able to be connected from this side.

- set_slot_enabled_right(slot_index: int, enable: bool) -> void
  Toggles the right (output) side of the slot with the given slot_index. If enable is true, a port will appear on the right side and the slot will be able to be connected from this side.

- set_slot_metadata_left(slot_index: int, value: Variant) -> void
  Sets the custom metadata for the left (input) side of the slot with the given slot_index to value.

- set_slot_metadata_right(slot_index: int, value: Variant) -> void
  Sets the custom metadata for the right (output) side of the slot with the given slot_index to value.

- set_slot_type_left(slot_index: int, type: int) -> void
  Sets the left (input) type of the slot with the given slot_index to type. If the value is negative, all connections will be disallowed to be created via user inputs.

- set_slot_type_right(slot_index: int, type: int) -> void
  Sets the right (output) type of the slot with the given slot_index to type. If the value is negative, all connections will be disallowed to be created via user inputs.

## Properties

- focus_mode: int (Control.FocusMode) = 3 [set set_focus_mode; get get_focus_mode; override Control]

- ignore_invalid_connection_type: bool = false [set set_ignore_invalid_connection_type; get is_ignoring_valid_connection_type]
  If true, you can connect ports with different types, even if the connection was not explicitly allowed in the parent GraphEdit.

- mouse_filter: int (Control.MouseFilter) = 0 [set set_mouse_filter; get get_mouse_filter; override Control]

- slots_focus_mode: int (Control.FocusMode) = 3 [set set_slots_focus_mode; get get_slots_focus_mode]
  Determines how connection slots can be focused. - If set to Control.FOCUS_CLICK, connections can only be made with the mouse. - If set to Control.FOCUS_ALL, slots can also be focused using the ProjectSettings.input/ui_up and ProjectSettings.input/ui_down and connected using ProjectSettings.input/ui_left and ProjectSettings.input/ui_right input actions. - If set to Control.FOCUS_ACCESSIBILITY, slot input actions are only enabled when the screen reader is active.

- title: String = "" [set set_title; get get_title]
  The text displayed in the GraphNode's title bar.

## Signals

- slot_sizes_changed()
  Emitted when any slot's size might have changed.

- slot_updated(slot_index: int)
  Emitted when any GraphNode's slot is updated.

## Theme Items

- resizer_color: Color [color] = Color(0.875, 0.875, 0.875, 1)
  The color modulation applied to the resizer icon.

- port_h_offset: int [constant] = 0
  Horizontal offset for the ports.

- separation: int [constant] = 2
  The vertical distance between ports.

- port: Texture2D [icon]
  The icon used for representing ports.

- panel: StyleBox [style]
  The default background for the slot area of the GraphNode.

- panel_focus: StyleBox [style]
  StyleBox used when the GraphNode is focused (when used with assistive apps).

- panel_selected: StyleBox [style]
  The StyleBox used for the slot area when selected.

- slot: StyleBox [style]
  The StyleBox used for each slot of the GraphNode.

- slot_selected: StyleBox [style]
  StyleBox used when the slot is focused (when used with assistive apps).

- titlebar: StyleBox [style]
  The StyleBox used for the title bar of the GraphNode.

- titlebar_selected: StyleBox [style]
  The StyleBox used for the title bar of the GraphNode when it is selected.
