# EditorExportPlatformExtension

## Meta

- Name: EditorExportPlatformExtension
- Source: EditorExportPlatformExtension.xml
- Inherits: EditorExportPlatform
- Inheritance Chain: EditorExportPlatformExtension -> EditorExportPlatform -> RefCounted -> Object

## Brief Description

Base class for custom EditorExportPlatform implementations (plugins).

## Description

External EditorExportPlatform implementations should inherit from this class. To use EditorExportPlatform, register it using the EditorPlugin.add_export_platform() method first.

## Quick Reference

```
[methods]
_can_export(preset: EditorExportPreset, debug: bool) -> bool [virtual const]
_cleanup() -> void [virtual]
_export_pack(preset: EditorExportPreset, debug: bool, path: String, flags: int (EditorExportPlatform.DebugFlags)) -> int (Error) [virtual]
_export_pack_patch(preset: EditorExportPreset, debug: bool, path: String, patches: PackedStringArray, flags: int (EditorExportPlatform.DebugFlags)) -> int (Error) [virtual]
_export_project(preset: EditorExportPreset, debug: bool, path: String, flags: int (EditorExportPlatform.DebugFlags)) -> int (Error) [virtual required]
_export_zip(preset: EditorExportPreset, debug: bool, path: String, flags: int (EditorExportPlatform.DebugFlags)) -> int (Error) [virtual]
_export_zip_patch(preset: EditorExportPreset, debug: bool, path: String, patches: PackedStringArray, flags: int (EditorExportPlatform.DebugFlags)) -> int (Error) [virtual]
_get_binary_extensions(preset: EditorExportPreset) -> PackedStringArray [virtual required const]
_get_debug_protocol() -> String [virtual const]
_get_device_architecture(device: int) -> String [virtual const]
_get_export_option_visibility(preset: EditorExportPreset, option: String) -> bool [virtual const]
_get_export_option_warning(preset: EditorExportPreset, option: StringName) -> String [virtual const]
_get_export_options() -> Dictionary[] [virtual const]
_get_logo() -> Texture2D [virtual required const]
_get_name() -> String [virtual required const]
_get_option_icon(device: int) -> Texture2D [virtual const]
_get_option_label(device: int) -> String [virtual const]
_get_option_tooltip(device: int) -> String [virtual const]
_get_options_count() -> int [virtual const]
_get_options_tooltip() -> String [virtual const]
_get_os_name() -> String [virtual required const]
_get_platform_features() -> PackedStringArray [virtual required const]
_get_preset_features(preset: EditorExportPreset) -> PackedStringArray [virtual required const]
_get_run_icon() -> Texture2D [virtual const]
_has_valid_export_configuration(preset: EditorExportPreset, debug: bool) -> bool [virtual required const]
_has_valid_project_configuration(preset: EditorExportPreset) -> bool [virtual required const]
_initialize() -> void [virtual]
_is_executable(path: String) -> bool [virtual const]
_poll_export() -> bool [virtual]
_run(preset: EditorExportPreset, device: int, debug_flags: int (EditorExportPlatform.DebugFlags)) -> int (Error) [virtual]
_should_update_export_options() -> bool [virtual]
get_config_error() -> String [const]
get_config_missing_templates() -> bool [const]
set_config_error(error_text: String) -> void [const]
set_config_missing_templates(missing_templates: bool) -> void [const]
```

## Methods

- _can_export(preset: EditorExportPreset, debug: bool) -> bool [virtual const]
  Returns true if the specified preset is valid and can be exported. Use set_config_error() and set_config_missing_templates() to set error details. Usual implementations call _has_valid_export_configuration() and _has_valid_project_configuration() to determine if exporting is possible.

- _cleanup() -> void [virtual]
  Called by the editor before platform is unregistered.

- _export_pack(preset: EditorExportPreset, debug: bool, path: String, flags: int (EditorExportPlatform.DebugFlags)) -> int (Error) [virtual]
  Creates a PCK archive at path for the specified preset. This method is called when "Export PCK/ZIP" button is pressed in the export dialog, with "Export as Patch" disabled, and PCK is selected as a file type.

- _export_pack_patch(preset: EditorExportPreset, debug: bool, path: String, patches: PackedStringArray, flags: int (EditorExportPlatform.DebugFlags)) -> int (Error) [virtual]
  Creates a patch PCK archive at path for the specified preset, containing only the files that have changed since the last patch. This method is called when "Export PCK/ZIP" button is pressed in the export dialog, with "Export as Patch" enabled, and PCK is selected as a file type. **Note:** The patches provided in patches have already been loaded when this method is called and are merely provided as context. When empty the patches defined in the export preset have been loaded instead.

- _export_project(preset: EditorExportPreset, debug: bool, path: String, flags: int (EditorExportPlatform.DebugFlags)) -> int (Error) [virtual required]
  Creates a full project at path for the specified preset. This method is called when "Export" button is pressed in the export dialog. This method implementation can call EditorExportPlatform.save_pack() or EditorExportPlatform.save_zip() to use default PCK/ZIP export process, or calls EditorExportPlatform.export_project_files() and implement custom callback for processing each exported file.

- _export_zip(preset: EditorExportPreset, debug: bool, path: String, flags: int (EditorExportPlatform.DebugFlags)) -> int (Error) [virtual]
  Create a ZIP archive at path for the specified preset. This method is called when "Export PCK/ZIP" button is pressed in the export dialog, with "Export as Patch" disabled, and ZIP is selected as a file type.

- _export_zip_patch(preset: EditorExportPreset, debug: bool, path: String, patches: PackedStringArray, flags: int (EditorExportPlatform.DebugFlags)) -> int (Error) [virtual]
  Create a ZIP archive at path for the specified preset, containing only the files that have changed since the last patch. This method is called when "Export PCK/ZIP" button is pressed in the export dialog, with "Export as Patch" enabled, and ZIP is selected as a file type. **Note:** The patches provided in patches have already been loaded when this method is called and are merely provided as context. When empty the patches defined in the export preset have been loaded instead.

- _get_binary_extensions(preset: EditorExportPreset) -> PackedStringArray [virtual required const]
  Returns array of supported binary extensions for the full project export.

- _get_debug_protocol() -> String [virtual const]
  Returns protocol used for remote debugging. Default implementation return tcp://.

- _get_device_architecture(device: int) -> String [virtual const]
  Returns device architecture for one-click deploy.

- _get_export_option_visibility(preset: EditorExportPreset, option: String) -> bool [virtual const]
  Validates option and returns visibility for the specified preset. Default implementation return true for all options.

- _get_export_option_warning(preset: EditorExportPreset, option: StringName) -> String [virtual const]
  Validates option and returns warning message for the specified preset. Default implementation return empty string for all options.

- _get_export_options() -> Dictionary[] [virtual const]
  Returns a property list, as an Array of dictionaries. Each Dictionary must at least contain the name: StringName and type: Variant.Type entries. Additionally, the following keys are supported: - hint: PropertyHint - hint_string: String - usage: PropertyUsageFlags - class_name: StringName - default_value: Variant, default value of the property. - update_visibility: bool, if set to true, _get_export_option_visibility() is called for each property when this property is changed. - required: bool, if set to true, this property warnings are critical, and should be resolved to make export possible. This value is a hint for the _has_valid_export_configuration() implementation, and not used by the engine directly. See also Object._get_property_list().

- _get_logo() -> Texture2D [virtual required const]
  Returns the platform logo displayed in the export dialog. The logo should be 32×32 pixels, adjusted for the current editor scale (see EditorInterface.get_editor_scale()).

- _get_name() -> String [virtual required const]
  Returns export platform name.

- _get_option_icon(device: int) -> Texture2D [virtual const]
  Returns the item icon for the specified device in the one-click deploy menu. The icon should be 16×16 pixels, adjusted for the current editor scale (see EditorInterface.get_editor_scale()).

- _get_option_label(device: int) -> String [virtual const]
  Returns one-click deploy menu item label for the specified device.

- _get_option_tooltip(device: int) -> String [virtual const]
  Returns one-click deploy menu item tooltip for the specified device.

- _get_options_count() -> int [virtual const]
  Returns the number of devices (or other options) available in the one-click deploy menu.

- _get_options_tooltip() -> String [virtual const]
  Returns tooltip of the one-click deploy menu button.

- _get_os_name() -> String [virtual required const]
  Returns target OS name.

- _get_platform_features() -> PackedStringArray [virtual required const]
  Returns array of platform specific features.

- _get_preset_features(preset: EditorExportPreset) -> PackedStringArray [virtual required const]
  Returns array of platform specific features for the specified preset.

- _get_run_icon() -> Texture2D [virtual const]
  Returns the icon of the one-click deploy menu button. The icon should be 16×16 pixels, adjusted for the current editor scale (see EditorInterface.get_editor_scale()).

- _has_valid_export_configuration(preset: EditorExportPreset, debug: bool) -> bool [virtual required const]
  Returns true if export configuration is valid.

- _has_valid_project_configuration(preset: EditorExportPreset) -> bool [virtual required const]
  Returns true if project configuration is valid.

- _initialize() -> void [virtual]
  Initializes the plugin. Called by the editor when platform is registered.

- _is_executable(path: String) -> bool [virtual const]
  Returns true if specified file is a valid executable (native executable or script) for the target platform.

- _poll_export() -> bool [virtual]
  Returns true if one-click deploy options are changed and editor interface should be updated.

- _run(preset: EditorExportPreset, device: int, debug_flags: int (EditorExportPlatform.DebugFlags)) -> int (Error) [virtual]
  This method is called when device one-click deploy menu option is selected. Implementation should export project to a temporary location, upload and run it on the specific device, or perform another action associated with the menu item.

- _should_update_export_options() -> bool [virtual]
  Returns true if export options list is changed and presets should be updated.

- get_config_error() -> String [const]
  Returns current configuration error message text. This method should be called only from the _can_export(), _has_valid_export_configuration(), or _has_valid_project_configuration() implementations.

- get_config_missing_templates() -> bool [const]
  Returns true is export templates are missing from the current configuration. This method should be called only from the _can_export(), _has_valid_export_configuration(), or _has_valid_project_configuration() implementations.

- set_config_error(error_text: String) -> void [const]
  Sets current configuration error message text. This method should be called only from the _can_export(), _has_valid_export_configuration(), or _has_valid_project_configuration() implementations.

- set_config_missing_templates(missing_templates: bool) -> void [const]
  Set to true is export templates are missing from the current configuration. This method should be called only from the _can_export(), _has_valid_export_configuration(), or _has_valid_project_configuration() implementations.
