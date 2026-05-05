# GDExtensionManager

## Meta

- Name: GDExtensionManager
- Source: GDExtensionManager.xml
- Inherits: Object
- Inheritance Chain: GDExtensionManager -> Object

## Brief Description

Provides access to GDExtension functionality.

## Description

The GDExtensionManager loads, initializes, and keeps track of all available GDExtension libraries in the project. **Note:** Do not worry about GDExtension unless you know what you are doing.

## Quick Reference

```
[methods]
get_extension(path: String) -> GDExtension
get_loaded_extensions() -> PackedStringArray [const]
is_extension_loaded(path: String) -> bool [const]
load_extension(path: String) -> int (GDExtensionManager.LoadStatus)
load_extension_from_function(path: String, init_func: const GDExtensionInitializationFunction*) -> int (GDExtensionManager.LoadStatus)
reload_extension(path: String) -> int (GDExtensionManager.LoadStatus)
unload_extension(path: String) -> int (GDExtensionManager.LoadStatus)
```

## Tutorials

- [GDExtension overview]($DOCS_URL/tutorials/scripting/gdextension/what_is_gdextension.html)
- [GDExtension example in C++]($DOCS_URL/tutorials/scripting/cpp/gdextension_cpp_example.html)

## Methods

- get_extension(path: String) -> GDExtension
  Returns the GDExtension at the given file path, or null if it has not been loaded or does not exist.

- get_loaded_extensions() -> PackedStringArray [const]
  Returns the file paths of all currently loaded extensions.

- is_extension_loaded(path: String) -> bool [const]
  Returns true if the extension at the given file path has already been loaded successfully. See also get_loaded_extensions().

- load_extension(path: String) -> int (GDExtensionManager.LoadStatus)
  Loads an extension by absolute file path. The path needs to point to a valid GDExtension. Returns LOAD_STATUS_OK if successful.

- load_extension_from_function(path: String, init_func: const GDExtensionInitializationFunction*) -> int (GDExtensionManager.LoadStatus)
  Loads the extension already in address space via the given path and initialization function. The path needs to be unique and start with "libgodot://". Returns LOAD_STATUS_OK if successful.

- reload_extension(path: String) -> int (GDExtensionManager.LoadStatus)
  Reloads the extension at the given file path. The path needs to point to a valid GDExtension, otherwise this method may return either LOAD_STATUS_NOT_LOADED or LOAD_STATUS_FAILED. **Note:** You can only reload extensions in the editor. In release builds, this method always fails and returns LOAD_STATUS_FAILED.

- unload_extension(path: String) -> int (GDExtensionManager.LoadStatus)
  Unloads an extension by file path. The path needs to point to an already loaded GDExtension, otherwise this method returns LOAD_STATUS_NOT_LOADED.

## Signals

- extension_loaded(extension: GDExtension)
  Emitted after the editor has finished loading a new extension. **Note:** This signal is only emitted in editor builds.

- extension_unloading(extension: GDExtension)
  Emitted before the editor starts unloading an extension. **Note:** This signal is only emitted in editor builds.

- extensions_reloaded()
  Emitted after the editor has finished reloading one or more extensions.

## Constants

### Enum LoadStatus

- LOAD_STATUS_OK = 0
  The extension has loaded successfully.

- LOAD_STATUS_FAILED = 1
  The extension has failed to load, possibly because it does not exist or has missing dependencies.

- LOAD_STATUS_ALREADY_LOADED = 2
  The extension has already been loaded.

- LOAD_STATUS_NOT_LOADED = 3
  The extension has not been loaded.

- LOAD_STATUS_NEEDS_RESTART = 4
  The extension requires the application to restart to fully load.
