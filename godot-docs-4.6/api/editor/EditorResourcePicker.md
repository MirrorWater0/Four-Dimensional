# EditorResourcePicker

## Meta

- Name: EditorResourcePicker
- Source: EditorResourcePicker.xml
- Inherits: HBoxContainer
- Inheritance Chain: EditorResourcePicker -> HBoxContainer -> BoxContainer -> Container -> Control -> CanvasItem -> Node -> Object

## Brief Description

Godot editor's control for selecting Resource type properties.

## Description

This Control node is used in the editor's Inspector dock to allow editing of Resource type properties. It provides options for creating, loading, saving and converting resources. Can be used with EditorInspectorPlugin to recreate the same behavior. **Note:** This Control does not include any editor for the resource, as editing is controlled by the Inspector dock itself or sub-Inspectors.

## Quick Reference

```
[methods]
_handle_menu_selected(id: int) -> bool [virtual]
_set_create_options(menu_node: Object) -> void [virtual]
get_allowed_types() -> PackedStringArray [const]
set_toggle_pressed(pressed: bool) -> void

[properties]
base_type: String = ""
editable: bool = true
edited_resource: Resource
toggle_mode: bool = false
```

## Methods

- _handle_menu_selected(id: int) -> bool [virtual]
  This virtual method can be implemented to handle context menu items not handled by default. See _set_create_options().

- _set_create_options(menu_node: Object) -> void [virtual]
  This virtual method is called when updating the context menu of EditorResourcePicker. Implement this method to override the "New ..." items with your own options. menu_node is a reference to the PopupMenu node. **Note:** Implement _handle_menu_selected() to handle these custom items.

- get_allowed_types() -> PackedStringArray [const]
  Returns a list of all allowed types and subtypes corresponding to the base_type. If the base_type is empty, an empty list is returned.

- set_toggle_pressed(pressed: bool) -> void
  Sets the toggle mode state for the main button. Works only if toggle_mode is set to true.

## Properties

- base_type: String = "" [set set_base_type; get get_base_type]
  The base type of allowed resource types. Can be a comma-separated list of several options.

- editable: bool = true [set set_editable; get is_editable]
  If true, the value can be selected and edited.

- edited_resource: Resource [set set_edited_resource; get get_edited_resource]
  The edited resource value.

- toggle_mode: bool = false [set set_toggle_mode; get is_toggle_mode]
  If true, the main button with the resource preview works in the toggle mode. Use set_toggle_pressed() to manually set the state.

## Signals

- resource_changed(resource: Resource)
  Emitted when the value of the edited resource was changed.

- resource_selected(resource: Resource, inspect: bool)
  Emitted when the resource value was set and user clicked to edit it. When inspect is true, the signal was caused by the context menu "Edit" or "Inspect" option.
