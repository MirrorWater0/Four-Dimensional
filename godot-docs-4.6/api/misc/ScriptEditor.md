# ScriptEditor

## Meta

- Name: ScriptEditor
- Source: ScriptEditor.xml
- Inherits: PanelContainer
- Inheritance Chain: ScriptEditor -> PanelContainer -> Container -> Control -> CanvasItem -> Node -> Object

## Brief Description

Godot editor's script editor.

## Description

Godot editor's script editor. **Note:** This class shouldn't be instantiated directly. Instead, access the singleton using EditorInterface.get_script_editor().

## Quick Reference

```
[methods]
clear_docs_from_script(script: Script) -> void
get_breakpoints() -> PackedStringArray
get_current_editor() -> ScriptEditorBase [const]
get_current_script() -> Script
get_open_script_editors() -> ScriptEditorBase[] [const]
get_open_scripts() -> Script[] [const]
goto_help(topic: String) -> void
goto_line(line_number: int) -> void
open_script_create_dialog(base_name: String, base_path: String) -> void
register_syntax_highlighter(syntax_highlighter: EditorSyntaxHighlighter) -> void
unregister_syntax_highlighter(syntax_highlighter: EditorSyntaxHighlighter) -> void
update_docs_from_script(script: Script) -> void
```

## Methods

- clear_docs_from_script(script: Script) -> void
  Removes the documentation for the given script. **Note:** This should be called whenever the script is changed to keep the open documentation state up to date.

- get_breakpoints() -> PackedStringArray
  Returns array of breakpoints.

- get_current_editor() -> ScriptEditorBase [const]
  Returns the ScriptEditorBase object that the user is currently editing.

- get_current_script() -> Script
  Returns a Script that is currently active in editor.

- get_open_script_editors() -> ScriptEditorBase[] [const]
  Returns an array with all ScriptEditorBase objects which are currently open in editor.

- get_open_scripts() -> Script[] [const]
  Returns an array with all Script objects which are currently open in editor.

- goto_help(topic: String) -> void
  Opens help for the given topic. The topic is an encoded string that controls which class, method, constant, signal, annotation, property, or theme item should be focused. The supported topic formats include class_name:class, class_method:class:method, class_constant:class:constant, class_signal:class:signal, class_annotation:class:@annotation, class_property:class:property, and class_theme_item:class:item, where class is the class name, method is the method name, constant is the constant name, signal is the signal name, annotation is the annotation name, property is the property name, and item is the theme item.


```
  # Shows help for the Node class.
  class_name:Node
  # Shows help for the global min function.
  # Global objects are accessible in the `@GlobalScope` namespace, shown here.
  class_method:@GlobalScope:min
  # Shows help for get_viewport in the Node class.
  class_method:Node:get_viewport
  # Shows help for the Input constant MOUSE_BUTTON_MIDDLE.
  class_constant:Input:MOUSE_BUTTON_MIDDLE
  # Shows help for the BaseButton signal pressed.
  class_signal:BaseButton:pressed
  # Shows help for the CanvasItem property visible.
  class_property:CanvasItem:visible
  # Shows help for the GDScript annotation export.
  # Annotations should be prefixed with the `@` symbol in the descriptor, as shown here.
  class_annotation:@GDScript:@export
  # Shows help for the GraphNode theme item named panel_selected.
  class_theme_item:GraphNode:panel_selected

```

- goto_line(line_number: int) -> void
  Goes to the specified line in the current script.

- open_script_create_dialog(base_name: String, base_path: String) -> void
  Opens the script create dialog. The script will extend base_name. The file extension can be omitted from base_path. It will be added based on the selected scripting language.

- register_syntax_highlighter(syntax_highlighter: EditorSyntaxHighlighter) -> void
  Registers the EditorSyntaxHighlighter to the editor, the EditorSyntaxHighlighter will be available on all open scripts. **Note:** Does not apply to scripts that are already opened.

- unregister_syntax_highlighter(syntax_highlighter: EditorSyntaxHighlighter) -> void
  Unregisters the EditorSyntaxHighlighter from the editor. **Note:** The EditorSyntaxHighlighter will still be applied to scripts that are already opened.

- update_docs_from_script(script: Script) -> void
  Updates the documentation for the given script. **Note:** This should be called whenever the script is changed to keep the open documentation state up to date.

## Signals

- editor_script_changed(script: Script)
  Emitted when user changed active script. Argument is a freshly activated Script.

- script_close(script: Script)
  Emitted when editor is about to close the active script. Argument is a Script that is going to be closed.
