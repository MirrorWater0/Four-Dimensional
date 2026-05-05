# EditorDebuggerSession

## Meta

- Name: EditorDebuggerSession
- Source: EditorDebuggerSession.xml
- Inherits: RefCounted
- Inheritance Chain: EditorDebuggerSession -> RefCounted -> Object

## Brief Description

A class to interact with the editor debugger.

## Description

This class cannot be directly instantiated and must be retrieved via an EditorDebuggerPlugin. You can add tabs to the session UI via add_session_tab(), send messages via send_message(), and toggle EngineProfilers via toggle_profiler().

## Quick Reference

```
[methods]
add_session_tab(control: Control) -> void
is_active() -> bool
is_breaked() -> bool
is_debuggable() -> bool
remove_session_tab(control: Control) -> void
send_message(message: String, data: Array = []) -> void
set_breakpoint(path: String, line: int, enabled: bool) -> void
toggle_profiler(profiler: String, enable: bool, data: Array = []) -> void
```

## Methods

- add_session_tab(control: Control) -> void
  Adds the given control to the debug session UI in the debugger bottom panel. The control's node name will be used as the tab title.

- is_active() -> bool
  Returns true if the debug session is currently attached to a remote instance.

- is_breaked() -> bool
  Returns true if the attached remote instance is currently in the debug loop.

- is_debuggable() -> bool
  Returns true if the attached remote instance can be debugged.

- remove_session_tab(control: Control) -> void
  Removes the given control from the debug session UI in the debugger bottom panel.

- send_message(message: String, data: Array = []) -> void
  Sends the given message to the attached remote instance, optionally passing additionally data. See EngineDebugger for how to retrieve those messages.

- set_breakpoint(path: String, line: int, enabled: bool) -> void
  Enables or disables a specific breakpoint based on enabled, updating the Editor Breakpoint Panel accordingly.

- toggle_profiler(profiler: String, enable: bool, data: Array = []) -> void
  Toggle the given profiler on the attached remote instance, optionally passing additionally data. See EngineProfiler for more details.

## Signals

- breaked(can_debug: bool)
  Emitted when the attached remote instance enters a break state. If can_debug is true, the remote instance will enter the debug loop.

- continued()
  Emitted when the attached remote instance exits a break state.

- started()
  Emitted when a remote instance is attached to this session (i.e. the session becomes active).

- stopped()
  Emitted when a remote instance is detached from this session (i.e. the session becomes inactive).
