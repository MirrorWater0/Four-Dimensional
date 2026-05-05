# EditorInspector

## Meta

- Name: EditorInspector
- Source: EditorInspector.xml
- Inherits: ScrollContainer
- Inheritance Chain: EditorInspector -> ScrollContainer -> Container -> Control -> CanvasItem -> Node -> Object

## Brief Description

A control used to edit properties of an object.

## Description

This is the control that implements property editing in the editor's Settings dialogs, the Inspector dock, etc. To get the EditorInspector used in the editor's Inspector dock, use EditorInterface.get_inspector(). EditorInspector will show properties in the same order as the array returned by Object.get_property_list(). If a property's name is path-like (i.e. if it contains forward slashes), EditorInspector will create nested sections for "directories" along the path. For example, if a property is named highlighting/gdscript/node_path_color, it will be shown as "Node Path Color" inside the "GDScript" section nested inside the "Highlighting" section. If a property has PROPERTY_USAGE_GROUP usage, it will group subsequent properties whose name starts with the property's hint string. The group ends when a property does not start with that hint string or when a new group starts. An empty group name effectively ends the current group. EditorInspector will create a top-level section for each group. For example, if a property with group usage is named Collide With and its hint string is collide_with_, a subsequent collide_with_area property will be shown as "Area" inside the "Collide With" section. There is also a special case: when the hint string contains the name of a property, that property is grouped too. This is mainly to help grouping properties like font, font_color and font_size (using the hint string font_). If a property has PROPERTY_USAGE_SUBGROUP usage, a subgroup will be created in the same way as a group, and a second-level section will be created for each subgroup. **Note:** Unlike sections created from path-like property names, EditorInspector won't capitalize the name for sections created from groups. So properties with group usage usually use capitalized names instead of snake_cased names.

## Quick Reference

```
[methods]
edit(object: Object) -> void
get_edited_object() -> Object
get_selected_path() -> String [const]
instantiate_property_editor(object: Object, type: int (Variant.Type), path: String, hint: int (PropertyHint), hint_text: String, usage: int, wide: bool = false) -> EditorProperty [static]

[properties]
draw_focus_border: bool = true
focus_mode: int (Control.FocusMode) = 2
follow_focus: bool = true
horizontal_scroll_mode: int (ScrollContainer.ScrollMode) = 0
```

## Methods

- edit(object: Object) -> void
  Shows the properties of the given object in this inspector for editing. To clear the inspector, call this method with null. **Note:** If you want to edit an object in the editor's main inspector, use the edit_* methods in EditorInterface instead.

- get_edited_object() -> Object
  Returns the object currently selected in this inspector.

- get_selected_path() -> String [const]
  Gets the path of the currently selected property.

- instantiate_property_editor(object: Object, type: int (Variant.Type), path: String, hint: int (PropertyHint), hint_text: String, usage: int, wide: bool = false) -> EditorProperty [static]
  Creates a property editor that can be used by plugin UI to edit the specified property of an object.

## Properties

- draw_focus_border: bool = true [set set_draw_focus_border; get get_draw_focus_border; override ScrollContainer]

- focus_mode: int (Control.FocusMode) = 2 [set set_focus_mode; get get_focus_mode; override Control]

- follow_focus: bool = true [set set_follow_focus; get is_following_focus; override ScrollContainer]

- horizontal_scroll_mode: int (ScrollContainer.ScrollMode) = 0 [set set_horizontal_scroll_mode; get get_horizontal_scroll_mode; override ScrollContainer]

## Signals

- edited_object_changed()
  Emitted when the object being edited by the inspector has changed.

- object_id_selected(id: int)
  Emitted when the Edit button of an Object has been pressed in the inspector. This is mainly used in the remote scene tree Inspector.

- property_deleted(property: String)
  Emitted when a property is removed from the inspector.

- property_edited(property: String)
  Emitted when a property is edited in the inspector.

- property_keyed(property: String, value: Variant, advance: bool)
  Emitted when a property is keyed in the inspector. Properties can be keyed by clicking the "key" icon next to a property when the Animation panel is toggled.

- property_selected(property: String)
  Emitted when a property is selected in the inspector.

- property_toggled(property: String, checked: bool)
  Emitted when a boolean property is toggled in the inspector. **Note:** This signal is never emitted if the internal autoclear property enabled. Since this property is always enabled in the editor inspector, this signal is never emitted by the editor itself.

- resource_selected(resource: Resource, path: String)
  Emitted when a resource is selected in the inspector.

- restart_requested()
  Emitted when a property that requires a restart to be applied is edited in the inspector. This is only used in the Project Settings and Editor Settings.
