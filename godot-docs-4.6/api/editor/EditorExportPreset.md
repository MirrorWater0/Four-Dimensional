# EditorExportPreset

## Meta

- Name: EditorExportPreset
- Source: EditorExportPreset.xml
- Inherits: RefCounted
- Inheritance Chain: EditorExportPreset -> RefCounted -> Object

## Brief Description

Export preset configuration.

## Description

Represents the configuration of an export preset, as created by the editor's export dialog. An EditorExportPreset instance is intended to be used a read-only configuration passed to the EditorExportPlatform methods when exporting the project.

## Quick Reference

```
[methods]
are_advanced_options_enabled() -> bool [const]
get_custom_features() -> String [const]
get_customized_files() -> Dictionary [const]
get_customized_files_count() -> int [const]
get_encrypt_directory() -> bool [const]
get_encrypt_pck() -> bool [const]
get_encryption_ex_filter() -> String [const]
get_encryption_in_filter() -> String [const]
get_encryption_key() -> String [const]
get_exclude_filter() -> String [const]
get_export_filter() -> int (EditorExportPreset.ExportFilter) [const]
get_export_path() -> String [const]
get_file_export_mode(path: String, default: int (EditorExportPreset.FileExportMode) = 0) -> int (EditorExportPreset.FileExportMode) [const]
get_files_to_export() -> PackedStringArray [const]
get_include_filter() -> String [const]
get_or_env(name: StringName, env_var: String) -> Variant [const]
get_patches() -> PackedStringArray [const]
get_preset_name() -> String [const]
get_project_setting(name: StringName) -> Variant
get_script_export_mode() -> int (EditorExportPreset.ScriptExportMode) [const]
get_version(name: StringName, windows_version: bool) -> String [const]
has(property: StringName) -> bool [const]
has_export_file(path: String) -> bool
is_dedicated_server() -> bool [const]
is_runnable() -> bool [const]
```

## Methods

- are_advanced_options_enabled() -> bool [const]
  Returns true if the "Advanced" toggle is enabled in the export dialog.

- get_custom_features() -> String [const]
  Returns a comma-separated list of custom features added to this preset, as a string. See [Feature tags]($DOCS_URL/tutorials/export/feature_tags.html) in the documentation for more information.

- get_customized_files() -> Dictionary [const]
  Returns a dictionary of files selected in the "Resources" tab of the export dialog. The dictionary's keys are file paths, and its values are the corresponding export modes: "strip", "keep", or "remove". See also get_file_export_mode().

- get_customized_files_count() -> int [const]
  Returns the number of files selected in the "Resources" tab of the export dialog.

- get_encrypt_directory() -> bool [const]
  Returns true if PCK directory encryption is enabled in the export dialog.

- get_encrypt_pck() -> bool [const]
  Returns true if PCK encryption is enabled in the export dialog.

- get_encryption_ex_filter() -> String [const]
  Returns file filters to exclude during PCK encryption.

- get_encryption_in_filter() -> String [const]
  Returns file filters to include during PCK encryption.

- get_encryption_key() -> String [const]
  Returns PCK encryption key.

- get_exclude_filter() -> String [const]
  Returns file filters to exclude during export.

- get_export_filter() -> int (EditorExportPreset.ExportFilter) [const]
  Returns export file filter mode selected in the "Resources" tab of the export dialog.

- get_export_path() -> String [const]
  Returns export target path.

- get_file_export_mode(path: String, default: int (EditorExportPreset.FileExportMode) = 0) -> int (EditorExportPreset.FileExportMode) [const]
  Returns file export mode for the specified file.

- get_files_to_export() -> PackedStringArray [const]
  Returns array of files to export.

- get_include_filter() -> String [const]
  Returns file filters to include during export.

- get_or_env(name: StringName, env_var: String) -> Variant [const]
  Returns export option value or value of environment variable if it is set.

- get_patches() -> PackedStringArray [const]
  Returns the list of packs on which to base a patch export on.

- get_preset_name() -> String [const]
  Returns this export preset's name.

- get_project_setting(name: StringName) -> Variant
  Returns the value of the setting identified by name using export preset feature tag overrides instead of current OS features.

- get_script_export_mode() -> int (EditorExportPreset.ScriptExportMode) [const]
  Returns the export mode used by GDScript files. 0 for "Text", 1 for "Binary tokens", and 2 for "Compressed binary tokens (smaller files)".

- get_version(name: StringName, windows_version: bool) -> String [const]
  Returns the preset's version number, or fall back to the ProjectSettings.application/config/version project setting if set to an empty string. If windows_version is true, formats the returned version number to be compatible with Windows executable metadata.

- has(property: StringName) -> bool [const]
  Returns true if the preset has the property named property.

- has_export_file(path: String) -> bool
  Returns true if the file at the specified path will be exported.

- is_dedicated_server() -> bool [const]
  Returns true if the dedicated server export mode is selected in the export dialog.

- is_runnable() -> bool [const]
  Returns true if the "Runnable" toggle is enabled in the export dialog.

## Constants

### Enum ExportFilter

- EXPORT_ALL_RESOURCES = 0

- EXPORT_SELECTED_SCENES = 1

- EXPORT_SELECTED_RESOURCES = 2

- EXCLUDE_SELECTED_RESOURCES = 3

- EXPORT_CUSTOMIZED = 4

### Enum FileExportMode

- MODE_FILE_NOT_CUSTOMIZED = 0

- MODE_FILE_STRIP = 1

- MODE_FILE_KEEP = 2

- MODE_FILE_REMOVE = 3

### Enum ScriptExportMode

- MODE_SCRIPT_TEXT = 0

- MODE_SCRIPT_BINARY_TOKENS = 1

- MODE_SCRIPT_BINARY_TOKENS_COMPRESSED = 2
