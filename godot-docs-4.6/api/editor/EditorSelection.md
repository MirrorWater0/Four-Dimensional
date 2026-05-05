# EditorSelection

## Meta

- Name: EditorSelection
- Source: EditorSelection.xml
- Inherits: Object
- Inheritance Chain: EditorSelection -> Object

## Brief Description

Manages the SceneTree selection in the editor.

## Description

This object manages the SceneTree selection in the editor. **Note:** This class shouldn't be instantiated directly. Instead, access the singleton using EditorInterface.get_selection().

## Quick Reference

```
[methods]
add_node(node: Node) -> void
clear() -> void
get_selected_nodes() -> Node[]
get_top_selected_nodes() -> Node[]
get_transformable_selected_nodes() -> Node[]
remove_node(node: Node) -> void
```

## Methods

- add_node(node: Node) -> void
  Adds a node to the selection. **Note:** The newly selected node will not be automatically edited in the inspector. If you want to edit a node, use EditorInterface.edit_node().

- clear() -> void
  Clear the selection.

- get_selected_nodes() -> Node[]
  Returns the list of selected nodes.

- get_top_selected_nodes() -> Node[]
  Returns the list of top selected nodes only, excluding any children. This is useful for performing transform operations (moving them, rotating, etc.). For example, if there is a node A with a child B and a sibling C, then selecting all three will cause this method to return only A and C. Changing the global transform of A will affect the global transform of B, so there is no need to change B separately.

- get_transformable_selected_nodes() -> Node[]
  Returns the list of top selected nodes only, excluding any children. This is useful for performing transform operations (moving them, rotating, etc.). See get_top_selected_nodes().

- remove_node(node: Node) -> void
  Removes a node from the selection.

## Signals

- selection_changed()
  Emitted when the selection changes.
