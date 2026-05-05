# EditorProperty

## Meta

- Name: EditorProperty
- Source: EditorProperty.xml
- Inherits: Container
- Inheritance Chain: EditorProperty -> Container -> Control -> CanvasItem -> Node -> Object

## Brief Description

Custom control for editing properties that can be added to the EditorInspector.

## Description

A custom control for editing properties that can be added to the EditorInspector. It is added via EditorInspectorPlugin.

## Quick Reference

```
[methods]
_set_read_only(read_only: bool) -> void [virtual]
_update_property() -> void [virtual]
add_focusable(control: Control) -> void
deselect() -> void
emit_changed(property: StringName, value: Variant, field: StringName = &"", changing: bool = false) -> void
get_edited_object() -> Object
get_edited_property() -> StringName [const]
is_selected() -> bool [const]
select(focusable: int = -1) -> void
set_bottom_editor(editor: Control) -> void
set_label_reference(control: Control) -> void
set_object_and_property(object: Object, property: StringName) -> void
update_property() -> void

[properties]
checkable: bool = false
checked: bool = false
deletable: bool = false
draw_background: bool = true
draw_label: bool = true
draw_warning: bool = false
focus_mode: int (Control.FocusMode) = 3
keying: bool = false
label: String = ""
name_split_ratio: float = 0.5
read_only: bool = false
selectable: bool = true
use_folding: bool = false
```

## Methods

- _set_read_only(read_only: bool) -> void [virtual]
  Called when the read-only status of the property is changed. It may be used to change custom controls into a read-only or modifiable state.

- _update_property() -> void [virtual]
  When this virtual function is called, you must update your editor.

- add_focusable(control: Control) -> void
  If any of the controls added can gain keyboard focus, add it here. This ensures that focus will be restored if the inspector is refreshed.

- deselect() -> void
  Draw property as not selected. Used by the inspector.

- emit_changed(property: StringName, value: Variant, field: StringName = &"", changing: bool = false) -> void
  If one or several properties have changed, this must be called. field is used in case your editor can modify fields separately (as an example, Vector3.x). The changing argument avoids the editor requesting this property to be refreshed (leave as false if unsure).

- get_edited_object() -> Object
  Returns the edited object. **Note:** This method could return null if the editor has not yet been associated with a property. However, in _update_property() and _set_read_only(), this value is *guaranteed* to be non-null.

- get_edited_property() -> StringName [const]
  Returns the edited property. If your editor is for a single property (added via EditorInspectorPlugin._parse_property()), then this will return the property. **Note:** This method could return null if the editor has not yet been associated with a property. However, in _update_property() and _set_read_only(), this value is *guaranteed* to be non-null.

- is_selected() -> bool [const]
  Returns true if property is drawn as selected. Used by the inspector.

- select(focusable: int = -1) -> void
  Draw property as selected. Used by the inspector.

- set_bottom_editor(editor: Control) -> void
  Puts the editor control below the property label. The control must be previously added using Node.add_child().

- set_label_reference(control: Control) -> void
  Used by the inspector, set to a control that will be used as a reference to calculate the size of the label.

- set_object_and_property(object: Object, property: StringName) -> void
  Assigns object and property to edit.

- update_property() -> void
  Forces a refresh of the property display.

## Properties

- checkable: bool = false [set set_checkable; get is_checkable]
  Used by the inspector, set to true when the property is checkable.

- checked: bool = false [set set_checked; get is_checked]
  Used by the inspector, set to true when the property is checked.

- deletable: bool = false [set set_deletable; get is_deletable]
  Used by the inspector, set to true when the property can be deleted by the user.

- draw_background: bool = true [set set_draw_background; get is_draw_background]
  Used by the inspector, set to true when the property background is drawn.

- draw_label: bool = true [set set_draw_label; get is_draw_label]
  Used by the inspector, set to true when the property label is drawn.

- draw_warning: bool = false [set set_draw_warning; get is_draw_warning]
  Used by the inspector, set to true when the property is drawn with the editor theme's warning color. This is used for editable children's properties.

- focus_mode: int (Control.FocusMode) = 3 [set set_focus_mode; get get_focus_mode; override Control]

- keying: bool = false [set set_keying; get is_keying]
  Used by the inspector, set to true when the property can add keys for animation.

- label: String = "" [set set_label; get get_label]
  Set this property to change the label (if you want to show one).

- name_split_ratio: float = 0.5 [set set_name_split_ratio; get get_name_split_ratio]
  Space distribution ratio between the label and the editing field.

- read_only: bool = false [set set_read_only; get is_read_only]
  Used by the inspector, set to true when the property is read-only.

- selectable: bool = true [set set_selectable; get is_selectable]
  Used by the inspector, set to true when the property is selectable.

- use_folding: bool = false [set set_use_folding; get is_using_folding]
  Used by the inspector, set to true when the property is using folding.

## Signals

- multiple_properties_changed(properties: PackedStringArray, value: Array)
  Emit it if you want multiple properties modified at the same time. Do not use if added via EditorInspectorPlugin._parse_property().

- object_id_selected(property: StringName, id: int)
  Used by sub-inspectors. Emit it if what was selected was an Object ID.

- property_can_revert_changed(property: StringName, can_revert: bool)
  Emitted when the revertability (i.e., whether it has a non-default value and thus is displayed with a revert icon) of a property has changed.

- property_changed(property: StringName, value: Variant, field: StringName, changing: bool)
  Do not emit this manually, use the emit_changed() method instead.

- property_checked(property: StringName, checked: bool)
  Emitted when a property was checked. Used internally.

- property_deleted(property: StringName)
  Emitted when a property was deleted. Used internally.

- property_favorited(property: StringName, favorited: bool)
  Emit it if you want to mark a property as favorited, making it appear at the top of the inspector.

- property_keyed(property: StringName)
  Emit it if you want to add this value as an animation key (check for keying being enabled first).

- property_keyed_with_value(property: StringName, value: Variant)
  Emit it if you want to key a property with a single value.

- property_overridden()
  Emitted when a setting override for the current project is requested.

- property_pinned(property: StringName, pinned: bool)
  Emit it if you want to mark (or unmark) the value of a property for being saved regardless of being equal to the default value. The default value is the one the property will get when the node is just instantiated and can come from an ancestor scene in the inheritance/instantiation chain, a script or a builtin class.

- resource_selected(path: String, resource: Resource)
  If you want a sub-resource to be edited, emit this signal with the resource.

- selected(path: String, focusable_idx: int)
  Emitted when selected. Used internally.
