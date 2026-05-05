# AnimationNodeStateMachine

## Meta

- Name: AnimationNodeStateMachine
- Source: AnimationNodeStateMachine.xml
- Inherits: AnimationRootNode
- Inheritance Chain: AnimationNodeStateMachine -> AnimationRootNode -> AnimationNode -> Resource -> RefCounted -> Object

## Brief Description

A state machine with multiple AnimationRootNodes, used by AnimationTree.

## Description

Contains multiple AnimationRootNodes representing animation states, connected in a graph. State transitions can be configured to happen automatically or via code, using a shortest-path algorithm. Retrieve the AnimationNodeStateMachinePlayback object from the AnimationTree node to control it programmatically.

```
var state_machine = $AnimationTree.get("parameters/playback")
state_machine.travel("some_state")
```

```
var stateMachine = GetNode<AnimationTree>("AnimationTree").Get("parameters/playback") as AnimationNodeStateMachinePlayback;
stateMachine.Travel("some_state");
```

## Quick Reference

```
[methods]
add_node(name: StringName, node: AnimationNode, position: Vector2 = Vector2(0, 0)) -> void
add_transition(from: StringName, to: StringName, transition: AnimationNodeStateMachineTransition) -> void
get_graph_offset() -> Vector2 [const]
get_node(name: StringName) -> AnimationNode [const]
get_node_list() -> StringName[] [const]
get_node_name(node: AnimationNode) -> StringName [const]
get_node_position(name: StringName) -> Vector2 [const]
get_transition(idx: int) -> AnimationNodeStateMachineTransition [const]
get_transition_count() -> int [const]
get_transition_from(idx: int) -> StringName [const]
get_transition_to(idx: int) -> StringName [const]
has_node(name: StringName) -> bool [const]
has_transition(from: StringName, to: StringName) -> bool [const]
remove_node(name: StringName) -> void
remove_transition(from: StringName, to: StringName) -> void
remove_transition_by_index(idx: int) -> void
rename_node(name: StringName, new_name: StringName) -> void
replace_node(name: StringName, node: AnimationNode) -> void
set_graph_offset(offset: Vector2) -> void
set_node_position(name: StringName, position: Vector2) -> void

[properties]
allow_transition_to_self: bool = false
reset_ends: bool = false
state_machine_type: int (AnimationNodeStateMachine.StateMachineType) = 0
```

## Tutorials

- [Using AnimationTree]($DOCS_URL/tutorials/animation/animation_tree.html)

## Methods

- add_node(name: StringName, node: AnimationNode, position: Vector2 = Vector2(0, 0)) -> void
  Adds a new animation node to the graph. The position is used for display in the editor.

- add_transition(from: StringName, to: StringName, transition: AnimationNodeStateMachineTransition) -> void
  Adds a transition between the given animation nodes.

- get_graph_offset() -> Vector2 [const]
  Returns the draw offset of the graph. Used for display in the editor.

- get_node(name: StringName) -> AnimationNode [const]
  Returns the animation node with the given name.

- get_node_list() -> StringName[] [const]
  Returns a list containing the names of all animation nodes in this state machine.

- get_node_name(node: AnimationNode) -> StringName [const]
  Returns the given animation node's name.

- get_node_position(name: StringName) -> Vector2 [const]
  Returns the given animation node's coordinates. Used for display in the editor.

- get_transition(idx: int) -> AnimationNodeStateMachineTransition [const]
  Returns the given transition.

- get_transition_count() -> int [const]
  Returns the number of connections in the graph.

- get_transition_from(idx: int) -> StringName [const]
  Returns the given transition's start node.

- get_transition_to(idx: int) -> StringName [const]
  Returns the given transition's end node.

- has_node(name: StringName) -> bool [const]
  Returns true if the graph contains the given animation node.

- has_transition(from: StringName, to: StringName) -> bool [const]
  Returns true if there is a transition between the given animation nodes.

- remove_node(name: StringName) -> void
  Deletes the given animation node from the graph.

- remove_transition(from: StringName, to: StringName) -> void
  Deletes the transition between the two specified animation nodes.

- remove_transition_by_index(idx: int) -> void
  Deletes the given transition by index.

- rename_node(name: StringName, new_name: StringName) -> void
  Renames the given animation node.

- replace_node(name: StringName, node: AnimationNode) -> void
  Replaces the given animation node with a new animation node.

- set_graph_offset(offset: Vector2) -> void
  Sets the draw offset of the graph. Used for display in the editor.

- set_node_position(name: StringName, position: Vector2) -> void
  Sets the animation node's coordinates. Used for display in the editor.

## Properties

- allow_transition_to_self: bool = false [set set_allow_transition_to_self; get is_allow_transition_to_self]
  If true, allows teleport to the self state with AnimationNodeStateMachinePlayback.travel(). When the reset option is enabled in AnimationNodeStateMachinePlayback.travel(), the animation is restarted. If false, nothing happens on the teleportation to the self state.

- reset_ends: bool = false [set set_reset_ends; get are_ends_reset]
  If true, treat the cross-fade to the start and end nodes as a blend with the RESET animation. In most cases, when additional cross-fades are performed in the parent AnimationNode of the state machine, setting this property to false and matching the cross-fade time of the parent AnimationNode and the state machine's start node and end node gives good results.

- state_machine_type: int (AnimationNodeStateMachine.StateMachineType) = 0 [set set_state_machine_type; get get_state_machine_type]
  This property can define the process of transitions for different use cases. See also AnimationNodeStateMachine.StateMachineType.

## Constants

### Enum StateMachineType

- STATE_MACHINE_TYPE_ROOT = 0
  Seeking to the beginning is treated as playing from the start state. Transition to the end state is treated as exiting the state machine.

- STATE_MACHINE_TYPE_NESTED = 1
  Seeking to the beginning is treated as seeking to the beginning of the animation in the current state. Transition to the end state, or the absence of transitions in each state, is treated as exiting the state machine.

- STATE_MACHINE_TYPE_GROUPED = 2
  This is a grouped state machine that can be controlled from a parent state machine. It does not work independently. There must be a state machine with state_machine_type of STATE_MACHINE_TYPE_ROOT or STATE_MACHINE_TYPE_NESTED in the parent or ancestor.
