# EditorScript

## Meta

- Name: EditorScript
- Source: EditorScript.xml
- Inherits: RefCounted
- Inheritance Chain: EditorScript -> RefCounted -> Object

## Brief Description

Base script that can be used to add extension functions to the editor.

## Description

Scripts extending this class and implementing its _run() method can be executed from the Script Editor's **File > Run** menu option (or by pressing Ctrl + Shift + X) while the editor is running. This is useful for adding custom in-editor functionality to Godot. For more complex additions, consider using EditorPlugins instead. If a script extending this class also has a global class name, it will be included in the editor's command palette. **Note:** Extending scripts need to have tool mode enabled. **Example:** Running the following script prints "Hello from the Godot Editor!":

```
@tool
extends EditorScript

func _run():
    print("Hello from the Godot Editor!")
```

```
using Godot;

Tool
public partial class HelloEditor : EditorScript
{
    public override void _Run()
    {
        GD.Print("Hello from the Godot Editor!");
    }
}
```

**Note:** EditorScript is RefCounted, meaning it is destroyed when nothing references it. This can cause errors during asynchronous operations if there are no references to the script.

## Quick Reference

```
[methods]
_run() -> void [virtual required]
add_root_node(node: Node) -> void
get_editor_interface() -> EditorInterface [const]
get_scene() -> Node [const]
```

## Methods

- _run() -> void [virtual required]
  This method is executed by the Editor when **File > Run** is used.

- add_root_node(node: Node) -> void
  Makes node root of the currently opened scene. Only works if the scene is empty. If the node is a scene instance, an inheriting scene will be created.

- get_editor_interface() -> EditorInterface [const]
  Returns the EditorInterface singleton instance.

- get_scene() -> Node [const]
  Returns the edited (current) scene's root Node. Equivalent of EditorInterface.get_edited_scene_root().
