# InputMap

## Meta

- Name: InputMap
- Source: InputMap.xml
- Inherits: Object
- Inheritance Chain: InputMap -> Object

## Brief Description

A singleton that manages all InputEventActions.

## Description

Manages all InputEventAction which can be created/modified from the project settings menu **Project > Project Settings > Input Map** or in code with add_action() and action_add_event(). See Node._input().

## Quick Reference

```
[methods]
action_add_event(action: StringName, event: InputEvent) -> void
action_erase_event(action: StringName, event: InputEvent) -> void
action_erase_events(action: StringName) -> void
action_get_deadzone(action: StringName) -> float
action_get_events(action: StringName) -> InputEvent[]
action_has_event(action: StringName, event: InputEvent) -> bool
action_set_deadzone(action: StringName, deadzone: float) -> void
add_action(action: StringName, deadzone: float = 0.2) -> void
erase_action(action: StringName) -> void
event_is_action(event: InputEvent, action: StringName, exact_match: bool = false) -> bool [const]
get_action_description(action: StringName) -> String [const]
get_actions() -> StringName[]
has_action(action: StringName) -> bool [const]
load_from_project_settings() -> void
```

## Tutorials

- [Using InputEvent: InputMap]($DOCS_URL/tutorials/inputs/inputevent.html#inputmap)

## Methods

- action_add_event(action: StringName, event: InputEvent) -> void
  Adds an InputEvent to an action. This InputEvent will trigger the action.

- action_erase_event(action: StringName, event: InputEvent) -> void
  Removes an InputEvent from an action.

- action_erase_events(action: StringName) -> void
  Removes all events from an action.

- action_get_deadzone(action: StringName) -> float
  Returns a deadzone value for the action.

- action_get_events(action: StringName) -> InputEvent[]
  Returns an array of InputEvents associated with a given action. **Note:** When used in the editor (e.g. a tool script or EditorPlugin), this method will return events for the editor action. If you want to access your project's input binds from the editor, read the input/* settings from ProjectSettings.

- action_has_event(action: StringName, event: InputEvent) -> bool
  Returns true if the action has the given InputEvent associated with it.

- action_set_deadzone(action: StringName, deadzone: float) -> void
  Sets a deadzone value for the action.

- add_action(action: StringName, deadzone: float = 0.2) -> void
  Adds an empty action to the InputMap with a configurable deadzone. An InputEvent can then be added to this action with action_add_event().

- erase_action(action: StringName) -> void
  Removes an action from the InputMap.

- event_is_action(event: InputEvent, action: StringName, exact_match: bool = false) -> bool [const]
  Returns true if the given event is part of an existing action. This method ignores keyboard modifiers if the given InputEvent is not pressed (for proper release detection). See action_has_event() if you don't want this behavior. If exact_match is false, it ignores additional input modifiers for InputEventKey and InputEventMouseButton events, and the direction for InputEventJoypadMotion events.

- get_action_description(action: StringName) -> String [const]
  Returns the human-readable description of the given action.

- get_actions() -> StringName[]
  Returns an array of all actions in the InputMap.

- has_action(action: StringName) -> bool [const]
  Returns true if the InputMap has a registered action with the given name.

- load_from_project_settings() -> void
  Clears all InputEventAction in the InputMap and load it anew from ProjectSettings.

## Signals

- project_settings_loaded()
  Emitted when the ProjectSettings InputMap has been loaded.
