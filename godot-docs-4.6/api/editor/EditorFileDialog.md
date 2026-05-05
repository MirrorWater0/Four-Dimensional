# EditorFileDialog

## Meta

- Name: EditorFileDialog
- Source: EditorFileDialog.xml
- Inherits: FileDialog
- Inheritance Chain: EditorFileDialog -> FileDialog -> ConfirmationDialog -> AcceptDialog -> Window -> Viewport -> Node -> Object

## Brief Description

A modified version of FileDialog used by the editor.

## Description

EditorFileDialog is a FileDialog tweaked to work in the editor. It automatically handles favorite and recent directory lists, and synchronizes some properties with their corresponding editor settings. EditorFileDialog will automatically show a native dialog based on the EditorSettings.interface/editor/use_native_file_dialogs editor setting and ignores FileDialog.use_native_dialog. **Note:** EditorFileDialog is invisible by default. To make it visible, call one of the popup_* methods from Window on the node, such as Window.popup_centered_clamped().

## Quick Reference

```
[methods]
add_side_menu(menu: Control, title: String = "") -> void

[properties]
disable_overwrite_warning: bool = false
```

## Methods

- add_side_menu(menu: Control, title: String = "") -> void
  This method is kept for compatibility and does nothing. As an alternative, you can display another dialog after showing the file dialog.

## Properties

- disable_overwrite_warning: bool = false [set set_disable_overwrite_warning; get is_overwrite_warning_disabled]
  If true, the EditorFileDialog will not warn the user before overwriting files.
