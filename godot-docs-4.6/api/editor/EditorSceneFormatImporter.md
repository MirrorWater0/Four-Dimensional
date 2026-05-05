# EditorSceneFormatImporter

## Meta

- Name: EditorSceneFormatImporter
- Source: EditorSceneFormatImporter.xml
- Inherits: RefCounted
- Inheritance Chain: EditorSceneFormatImporter -> RefCounted -> Object

## Brief Description

Imports scenes from third-parties' 3D files.

## Description

EditorSceneFormatImporter allows to define an importer script for a third-party 3D format. To use EditorSceneFormatImporter, register it using the EditorPlugin.add_scene_format_importer_plugin() method first.

## Quick Reference

```
[methods]
_get_extensions() -> PackedStringArray [virtual required const]
_get_import_options(path: String) -> void [virtual]
_get_option_visibility(path: String, for_animation: bool, option: String) -> Variant [virtual const]
_import_scene(path: String, flags: int, options: Dictionary) -> Object [virtual required]
add_import_option(name: String, value: Variant) -> void
add_import_option_advanced(type: int (Variant.Type), name: String, default_value: Variant, hint: int (PropertyHint) = 0, hint_string: String = "", usage_flags: int = 6) -> void
```

## Methods

- _get_extensions() -> PackedStringArray [virtual required const]
  Return supported file extensions for this scene importer.

- _get_import_options(path: String) -> void [virtual]
  Override to add general import options. These will appear in the main import dock on the editor. Add options via add_import_option() and add_import_option_advanced(). **Note:** All EditorSceneFormatImporter and EditorScenePostImportPlugin instances will add options for all files. It is good practice to check the file extension when path is non-empty. When the user is editing project settings, path will be empty. It is recommended to add all options when path is empty to allow the user to customize Import Defaults.

- _get_option_visibility(path: String, for_animation: bool, option: String) -> Variant [virtual const]
  Should return true to show the given option, false to hide the given option, or null to ignore.

- _import_scene(path: String, flags: int, options: Dictionary) -> Object [virtual required]
  Perform the bulk of the scene import logic here, for example using GLTFDocument or FBXDocument.

- add_import_option(name: String, value: Variant) -> void
  Add a specific import option (name and default value only). This function can only be called from _get_import_options().

- add_import_option_advanced(type: int (Variant.Type), name: String, default_value: Variant, hint: int (PropertyHint) = 0, hint_string: String = "", usage_flags: int = 6) -> void
  Add a specific import option. This function can only be called from _get_import_options().

## Constants

- IMPORT_SCENE = 1

- IMPORT_ANIMATION = 2

- IMPORT_FAIL_ON_MISSING_DEPENDENCIES = 4

- IMPORT_GENERATE_TANGENT_ARRAYS = 8

- IMPORT_USE_NAMED_SKIN_BINDS = 16

- IMPORT_DISCARD_MESHES_AND_MATERIALS = 32

- IMPORT_FORCE_DISABLE_MESH_COMPRESSION = 64
