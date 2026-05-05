# EditorDock

## Meta

- Name: EditorDock
- Source: EditorDock.xml
- Inherits: MarginContainer
- Inheritance Chain: EditorDock -> MarginContainer -> Container -> Control -> CanvasItem -> Node -> Object

## Brief Description

Dockable container for the editor.

## Description

EditorDock is a Container node that can be docked in one of the editor's dock slots. Docks are added by plugins to provide space for controls related to an EditorPlugin. The editor comes with a few built-in docks, such as the Scene dock, FileSystem dock, etc. You can add a dock by using EditorPlugin.add_dock(). The dock can be customized by changing its properties.

```
@tool
extends EditorPlugin

# Dock reference.
var dock

# Plugin initialization.
func _enter_tree():
    dock = EditorDock.new()
    dock.title = "My Dock"
    dock.dock_icon = preload("./dock_icon.png")
    dock.default_slot = EditorDock.DOCK_SLOT_RIGHT_UL
    var dock_content = preload("./dock_content.tscn").instantiate()
    dock.add_child(dock_content)
    add_dock(dock)

# Plugin clean-up.
func _exit_tree():
    remove_dock(dock)
    dock.queue_free()
    dock = null
```

## Quick Reference

```
[methods]
_load_layout_from_config(config: ConfigFile, section: String) -> void [virtual]
_save_layout_to_config(config: ConfigFile, section: String) -> void [virtual const]
_update_layout(layout: int) -> void [virtual]
close() -> void
make_visible() -> void
open() -> void

[properties]
available_layouts: int (EditorDock.DockLayout) = 5
closable: bool = false
default_slot: int (EditorDock.DockSlot) = -1
dock_icon: Texture2D
dock_shortcut: Shortcut
force_show_icon: bool = false
global: bool = true
icon_name: StringName = &""
layout_key: String = ""
title: String = ""
title_color: Color = Color(0, 0, 0, 0)
transient: bool = false
```

## Tutorials

- [Making plugins]($DOCS_URL/tutorials/plugins/editor/making_plugins.html)

## Methods

- _load_layout_from_config(config: ConfigFile, section: String) -> void [virtual]
  Implement this method to handle loading this dock's layout. It's equivalent to EditorPlugin._set_window_layout(). section is a unique section based on layout_key.

- _save_layout_to_config(config: ConfigFile, section: String) -> void [virtual const]
  Implement this method to handle saving this dock's layout. It's equivalent to EditorPlugin._get_window_layout(). section is a unique section based on layout_key.

- _update_layout(layout: int) -> void [virtual]
  Implement this method to handle the layout switching for this dock. layout is one of the DockLayout constants.


```
  func _update_layout(layout):
      box_container.vertical = (layout == DOCK_LAYOUT_VERTICAL)

```

- close() -> void
  Closes the dock, making its tab hidden.

- make_visible() -> void
  Focuses the dock's tab (or window if it's floating). If the dock was closed, it will be opened. If it's a bottom dock, makes the bottom panel visible.

- open() -> void
  Opens the dock. It will appear in the last used dock slot. If the dock has no default slot, it will be opened floating. **Note:** This does not focus the dock. If you want to open and focus the dock, use make_visible().

## Properties

- available_layouts: int (EditorDock.DockLayout) = 5 [set set_available_layouts; get get_available_layouts]
  The available layouts for this dock, as a bitmask. By default, the dock allows vertical and floating layouts.

- closable: bool = false [set set_closable; get is_closable]
  If true, the dock can be closed with the Close button in the context popup. Docks with global enabled are always closable.

- default_slot: int (EditorDock.DockSlot) = -1 [set set_default_slot; get get_default_slot]
  The default dock slot used when adding the dock with EditorPlugin.add_dock(). After the dock is added, it can be moved to a different slot and the editor will automatically remember its position between sessions. If you remove and re-add the dock, it will be reset to default.

- dock_icon: Texture2D [set set_dock_icon; get get_dock_icon]
  The icon for the dock, as a texture. If specified, it will override icon_name.

- dock_shortcut: Shortcut [set set_dock_shortcut; get get_dock_shortcut]
  The shortcut used to open the dock.

- force_show_icon: bool = false [set set_force_show_icon; get get_force_show_icon]
  If true, the dock will always display an icon, regardless of EditorSettings.interface/editor/dock_tab_style or EditorSettings.interface/editor/bottom_dock_tab_style.

- global: bool = true [set set_global; get is_global]
  If true, the dock appears in the **Editor > Editor Docks** menu and can be closed. Non-global docks can still be closed using close() or when closable is true.

- icon_name: StringName = &"" [set set_icon_name; get get_icon_name]
  The icon for the dock, as a name from the EditorIcons theme type in the editor theme. You can find the list of available icons here(https://godot-editor-icons.github.io/).

- layout_key: String = "" [set set_layout_key; get get_layout_key]
  The key representing this dock in the editor's layout file. If empty, the dock's displayed name will be used instead.

- title: String = "" [set set_title; get get_title]
  The title of the dock's tab. If empty, the dock's Node.name will be used. If the name is auto-generated (contains @), the first child's name will be used instead.

- title_color: Color = Color(0, 0, 0, 0) [set set_title_color; get get_title_color]
  The color of the dock tab's title. If its alpha is 0.0, the default font color will be used.

- transient: bool = false [set set_transient; get is_transient]
  If true, the dock is not automatically opened or closed when loading an editor layout, only moved. It also can't be opened using a shortcut. This is meant for docks that are opened and closed in specific cases, such as when selecting a TileMap or AnimationTree node.

## Signals

- closed()
  Emitted when the dock is closed with the Close button in the context popup, before it's removed from its parent. See closable.

## Constants

### Enum DockLayout

- DOCK_LAYOUT_VERTICAL = 1 [bitfield]
  Allows placing the dock in the vertical dock slots on either side of the editor.

- DOCK_LAYOUT_HORIZONTAL = 2 [bitfield]
  Allows placing the dock in the editor's bottom panel.

- DOCK_LAYOUT_FLOATING = 4 [bitfield]
  Allows making the dock floating (opened as a separate window).

- DOCK_LAYOUT_ALL = 7 [bitfield]
  Allows placing the dock in all available slots.

### Enum DockSlot

- DOCK_SLOT_NONE = -1
  The dock is closed.

- DOCK_SLOT_LEFT_UL = 0
  Dock slot, left side, upper-left (empty in default layout).

- DOCK_SLOT_LEFT_BL = 1
  Dock slot, left side, bottom-left (empty in default layout).

- DOCK_SLOT_LEFT_UR = 2
  Dock slot, left side, upper-right (in default layout includes Scene and Import docks).

- DOCK_SLOT_LEFT_BR = 3
  Dock slot, left side, bottom-right (in default layout includes FileSystem and History docks).

- DOCK_SLOT_RIGHT_UL = 4
  Dock slot, right side, upper-left (in default layout includes Inspector, Signal, and Group docks).

- DOCK_SLOT_RIGHT_BL = 5
  Dock slot, right side, bottom-left (empty in default layout).

- DOCK_SLOT_RIGHT_UR = 6
  Dock slot, right side, upper-right (empty in default layout).

- DOCK_SLOT_RIGHT_BR = 7
  Dock slot, right side, bottom-right (empty in default layout).

- DOCK_SLOT_BOTTOM = 8
  Bottom panel.

- DOCK_SLOT_MAX = 9
  Represents the size of the DockSlot enum.
