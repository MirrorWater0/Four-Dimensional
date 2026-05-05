# Script

## Meta

- Name: Script
- Source: Script.xml
- Inherits: Resource
- Inheritance Chain: Script -> Resource -> RefCounted -> Object

## Brief Description

A class stored as a resource.

## Description

A class stored as a resource. A script extends the functionality of all objects that instantiate it. This is the base class for all scripts and should not be used directly. Trying to create a new script with this class will result in an error. The new method of a script subclass creates a new instance. Object.set_script() extends an existing object, if that object's class matches one of the script's base classes.

## Quick Reference

```
[methods]
can_instantiate() -> bool [const]
get_base_script() -> Script [const]
get_global_name() -> StringName [const]
get_instance_base_type() -> StringName [const]
get_property_default_value(property: StringName) -> Variant
get_rpc_config() -> Variant [const]
get_script_constant_map() -> Dictionary
get_script_method_list() -> Dictionary[]
get_script_property_list() -> Dictionary[]
get_script_signal_list() -> Dictionary[]
has_script_signal(signal_name: StringName) -> bool [const]
has_source_code() -> bool [const]
instance_has(base_object: Object) -> bool [const]
is_abstract() -> bool [const]
is_tool() -> bool [const]
reload(keep_state: bool = false) -> int (Error)

[properties]
source_code: String
```

## Tutorials

- [Scripting documentation index]($DOCS_URL/tutorials/scripting/index.html)

## Methods

- can_instantiate() -> bool [const]
  Returns true if the script can be instantiated.

- get_base_script() -> Script [const]
  Returns the script directly inherited by this script.

- get_global_name() -> StringName [const]
  Returns the class name associated with the script, if there is one. Returns an empty string otherwise. To give the script a global name, you can use the class_name keyword in GDScript and the GlobalClass attribute in C#.


```
  class_name MyNode
  extends Node

```

```
  using Godot;

  GlobalClass
  public partial class MyNode : Node
  {
  }

```

- get_instance_base_type() -> StringName [const]
  Returns the script's base type.

- get_property_default_value(property: StringName) -> Variant
  Returns the default value of the specified property.

- get_rpc_config() -> Variant [const]
  Returns a Dictionary mapping method names to their RPC configuration defined by this script.

- get_script_constant_map() -> Dictionary
  Returns a dictionary containing constant names and their values.

- get_script_method_list() -> Dictionary[]
  Returns the list of methods in this Script. **Note:** The dictionaries returned by this method are formatted identically to those returned by Object.get_method_list().

- get_script_property_list() -> Dictionary[]
  Returns the list of properties in this Script. **Note:** The dictionaries returned by this method are formatted identically to those returned by Object.get_property_list().

- get_script_signal_list() -> Dictionary[]
  Returns the list of signals defined in this Script. **Note:** The dictionaries returned by this method are formatted identically to those returned by Object.get_signal_list().

- has_script_signal(signal_name: StringName) -> bool [const]
  Returns true if the script, or a base class, defines a signal with the given name.

- has_source_code() -> bool [const]
  Returns true if the script contains non-empty source code. **Note:** If a script does not have source code, this does not mean that it is invalid or unusable. For example, a GDScript that was exported with binary tokenization has no source code, but still behaves as expected and could be instantiated. This can be checked with can_instantiate().

- instance_has(base_object: Object) -> bool [const]
  Returns true if base_object is an instance of this script.

- is_abstract() -> bool [const]
  Returns true if the script is an abstract script. An abstract script does not have a constructor and cannot be instantiated.

- is_tool() -> bool [const]
  Returns true if the script is a tool script. A tool script can run in the editor.

- reload(keep_state: bool = false) -> int (Error)
  Reloads the script's class implementation. Returns an error code.

## Properties

- source_code: String [set set_source_code; get get_source_code]
  The script source code or an empty string if source code is not available. When set, does not reload the class implementation automatically.
