# AnimationTree

## Meta

- Name: AnimationTree
- Source: AnimationTree.xml
- Inherits: AnimationMixer
- Inheritance Chain: AnimationTree -> AnimationMixer -> Node -> Object

## Brief Description

A node used for advanced animation transitions in an AnimationPlayer.

## Description

A node used for advanced animation transitions in an AnimationPlayer. **Note:** When linked with an AnimationPlayer, several properties and methods of the corresponding AnimationPlayer will not function as expected. Playback and transitions should be handled using only the AnimationTree and its constituent AnimationNode(s). The AnimationPlayer node should be used solely for adding, deleting, and editing animations.

## Quick Reference

```
[methods]
get_process_callback() -> int (AnimationTree.AnimationProcessCallback) [const]
set_process_callback(mode: int (AnimationTree.AnimationProcessCallback)) -> void

[properties]
advance_expression_base_node: NodePath = NodePath(".")
anim_player: NodePath = NodePath("")
callback_mode_discrete: int (AnimationMixer.AnimationCallbackModeDiscrete) = 2
deterministic: bool = true
tree_root: AnimationRootNode
```

## Tutorials

- [Using AnimationTree]($DOCS_URL/tutorials/animation/animation_tree.html)
- [Third Person Shooter (TPS) Demo](https://godotengine.org/asset-library/asset/2710)

## Methods

- get_process_callback() -> int (AnimationTree.AnimationProcessCallback) [const]
  Returns the process notification in which to update animations.

- set_process_callback(mode: int (AnimationTree.AnimationProcessCallback)) -> void
  Sets the process notification in which to update animations.

## Properties

- advance_expression_base_node: NodePath = NodePath(".") [set set_advance_expression_base_node; get get_advance_expression_base_node]
  The path to the Node used to evaluate the AnimationNode Expression if one is not explicitly specified internally.

- anim_player: NodePath = NodePath("") [set set_animation_player; get get_animation_player]
  The path to the AnimationPlayer used for animating.

- callback_mode_discrete: int (AnimationMixer.AnimationCallbackModeDiscrete) = 2 [set set_callback_mode_discrete; get get_callback_mode_discrete; override AnimationMixer]

- deterministic: bool = true [set set_deterministic; get is_deterministic; override AnimationMixer]

- tree_root: AnimationRootNode [set set_tree_root; get get_tree_root]
  The root animation node of this AnimationTree. See AnimationRootNode.

## Signals

- animation_player_changed()
  Emitted when the anim_player is changed.

## Constants

### Enum AnimationProcessCallback

- ANIMATION_PROCESS_PHYSICS = 0

- ANIMATION_PROCESS_IDLE = 1

- ANIMATION_PROCESS_MANUAL = 2
