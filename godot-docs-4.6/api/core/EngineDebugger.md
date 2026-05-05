# EngineDebugger

## Meta

- Name: EngineDebugger
- Source: EngineDebugger.xml
- Inherits: Object
- Inheritance Chain: EngineDebugger -> Object

## Brief Description

Exposes the internal debugger.

## Description

EngineDebugger handles the communication between the editor and the running game. It is active in the running game. Messages can be sent/received through it. It also manages the profilers.

## Quick Reference

```
[methods]
clear_breakpoints() -> void
debug(can_continue: bool = true, is_error_breakpoint: bool = false) -> void
get_depth() -> int [const]
get_lines_left() -> int [const]
has_capture(name: StringName) -> bool
has_profiler(name: StringName) -> bool
insert_breakpoint(line: int, source: StringName) -> void
is_active() -> bool
is_breakpoint(line: int, source: StringName) -> bool [const]
is_profiling(name: StringName) -> bool
is_skipping_breakpoints() -> bool [const]
line_poll() -> void
profiler_add_frame_data(name: StringName, data: Array) -> void
profiler_enable(name: StringName, enable: bool, arguments: Array = []) -> void
register_message_capture(name: StringName, callable: Callable) -> void
register_profiler(name: StringName, profiler: EngineProfiler) -> void
remove_breakpoint(line: int, source: StringName) -> void
script_debug(language: ScriptLanguage, can_continue: bool = true, is_error_breakpoint: bool = false) -> void
send_message(message: String, data: Array) -> void
set_depth(depth: int) -> void
set_lines_left(lines: int) -> void
unregister_message_capture(name: StringName) -> void
unregister_profiler(name: StringName) -> void
```

## Methods

- clear_breakpoints() -> void
  Clears all breakpoints.

- debug(can_continue: bool = true, is_error_breakpoint: bool = false) -> void
  Starts a debug break in script execution, optionally specifying whether the program can continue based on can_continue and whether the break was due to a breakpoint.

- get_depth() -> int [const]
  Returns the current debug depth.

- get_lines_left() -> int [const]
  Returns the number of lines that remain.

- has_capture(name: StringName) -> bool
  Returns true if a capture with the given name is present otherwise false.

- has_profiler(name: StringName) -> bool
  Returns true if a profiler with the given name is present otherwise false.

- insert_breakpoint(line: int, source: StringName) -> void
  Inserts a new breakpoint with the given source and line.

- is_active() -> bool
  Returns true if the debugger is active otherwise false.

- is_breakpoint(line: int, source: StringName) -> bool [const]
  Returns true if the given source and line represent an existing breakpoint.

- is_profiling(name: StringName) -> bool
  Returns true if a profiler with the given name is present and active otherwise false.

- is_skipping_breakpoints() -> bool [const]
  Returns true if the debugger is skipping breakpoints otherwise false.

- line_poll() -> void
  Forces a processing loop of debugger events. The purpose of this method is just processing events every now and then when the script might get too busy, so that bugs like infinite loops can be caught.

- profiler_add_frame_data(name: StringName, data: Array) -> void
  Calls the add callable of the profiler with given name and data.

- profiler_enable(name: StringName, enable: bool, arguments: Array = []) -> void
  Calls the toggle callable of the profiler with given name and arguments. Enables/Disables the same profiler depending on enable argument.

- register_message_capture(name: StringName, callable: Callable) -> void
  Registers a message capture with given name. If name is "my_message" then messages starting with "my_message:" will be called with the given callable. The callable must accept a message string and a data array as argument. The callable should return true if the message is recognized. **Note:** The callable will receive the message with the prefix stripped, unlike EditorDebuggerPlugin._capture(). See the EditorDebuggerPlugin description for an example.

- register_profiler(name: StringName, profiler: EngineProfiler) -> void
  Registers a profiler with the given name. See EngineProfiler for more information.

- remove_breakpoint(line: int, source: StringName) -> void
  Removes a breakpoint with the given source and line.

- script_debug(language: ScriptLanguage, can_continue: bool = true, is_error_breakpoint: bool = false) -> void
  Starts a debug break in script execution, optionally specifying whether the program can continue based on can_continue and whether the break was due to a breakpoint.

- send_message(message: String, data: Array) -> void
  Sends a message with given message and data array.

- set_depth(depth: int) -> void
  Sets the current debugging depth.

- set_lines_left(lines: int) -> void
  Sets the current debugging lines that remain.

- unregister_message_capture(name: StringName) -> void
  Unregisters the message capture with given name.

- unregister_profiler(name: StringName) -> void
  Unregisters a profiler with given name.
