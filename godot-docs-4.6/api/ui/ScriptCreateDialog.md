# ScriptCreateDialog

## Meta

- Name: ScriptCreateDialog
- Source: ScriptCreateDialog.xml
- Inherits: ConfirmationDialog
- Inheritance Chain: ScriptCreateDialog -> ConfirmationDialog -> AcceptDialog -> Window -> Viewport -> Node -> Object

## Brief Description

Godot editor's popup dialog for creating new Script files.

## Description

The ScriptCreateDialog creates script files according to a given template for a given scripting language. The standard use is to configure its fields prior to calling one of the Window.popup() methods.

```
func _ready():
    var dialog = ScriptCreateDialog.new();
    dialog.config("Node", "res://new_node.gd") # For in-engine types.
    dialog.config("\"res://base_node.gd\"", "res://derived_node.gd") # For script types.
    dialog.popup_centered()
```

```
public override void _Ready()
{
    var dialog = new ScriptCreateDialog();
    dialog.Config("Node", "res://NewNode.cs"); // For in-engine types.
    dialog.Config("\"res://BaseNode.cs\"", "res://DerivedNode.cs"); // For script types.
    dialog.PopupCentered();
}
```

## Quick Reference

```
[methods]
config(inherits: String, path: String, built_in_enabled: bool = true, load_enabled: bool = true) -> void

[properties]
dialog_hide_on_ok: bool = false
ok_button_text: String = "Create"
title: String = "Attach Node Script"
```

## Methods

- config(inherits: String, path: String, built_in_enabled: bool = true, load_enabled: bool = true) -> void
  Prefills required fields to configure the ScriptCreateDialog for use.

## Properties

- dialog_hide_on_ok: bool = false [set set_hide_on_ok; get get_hide_on_ok; override AcceptDialog]

- ok_button_text: String = "Create" [set set_ok_button_text; get get_ok_button_text; override AcceptDialog]

- title: String = "Attach Node Script" [set set_title; get get_title; override Window]

## Signals

- script_created(script: Script)
  Emitted when the user clicks the OK button.
