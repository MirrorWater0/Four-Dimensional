# EditorExportPlatform

## Meta

- Name: EditorExportPlatform
- Source: EditorExportPlatform.xml
- Inherits: RefCounted
- Inheritance Chain: EditorExportPlatform -> RefCounted -> Object

## Brief Description

Identifies a supported export platform, and internally provides the functionality of exporting to that platform.

## Description

Base resource that provides the functionality of exporting a release build of a project to a platform, from the editor. Stores platform-specific metadata such as the name and supported features of the platform, and performs the exporting of projects, PCK files, and ZIP files. Uses an export template for the platform provided at the time of project exporting. Used in scripting by EditorExportPlugin to configure platform-specific customization of scenes and resources. See EditorExportPlugin._begin_customize_scenes() and EditorExportPlugin._begin_customize_resources() for more details.

## Quick Reference

```
[methods]
add_message(type: int (EditorExportPlatform.ExportMessageType), category: String, message: String) -> void
clear_messages() -> void
create_preset() -> EditorExportPreset
export_pack(preset: EditorExportPreset, debug: bool, path: String, flags: int (EditorExportPlatform.DebugFlags) = 0) -> int (Error)
export_pack_patch(preset: EditorExportPreset, debug: bool, path: String, patches: PackedStringArray = PackedStringArray(), flags: int (EditorExportPlatform.DebugFlags) = 0) -> int (Error)
export_project(preset: EditorExportPreset, debug: bool, path: String, flags: int (EditorExportPlatform.DebugFlags) = 0) -> int (Error)
export_project_files(preset: EditorExportPreset, debug: bool, save_cb: Callable, shared_cb: Callable = Callable()) -> int (Error)
export_zip(preset: EditorExportPreset, debug: bool, path: String, flags: int (EditorExportPlatform.DebugFlags) = 0) -> int (Error)
export_zip_patch(preset: EditorExportPreset, debug: bool, path: String, patches: PackedStringArray = PackedStringArray(), flags: int (EditorExportPlatform.DebugFlags) = 0) -> int (Error)
find_export_template(template_file_name: String) -> Dictionary [const]
gen_export_flags(flags: int (EditorExportPlatform.DebugFlags)) -> PackedStringArray
get_current_presets() -> Array [const]
get_forced_export_files(preset: EditorExportPreset = null) -> PackedStringArray [static]
get_internal_export_files(preset: EditorExportPreset, debug: bool) -> Dictionary
get_message_category(index: int) -> String [const]
get_message_count() -> int [const]
get_message_text(index: int) -> String [const]
get_message_type(index: int) -> int (EditorExportPlatform.ExportMessageType) [const]
get_os_name() -> String [const]
get_worst_message_type() -> int (EditorExportPlatform.ExportMessageType) [const]
save_pack(preset: EditorExportPreset, debug: bool, path: String, embed: bool = false) -> Dictionary
save_pack_patch(preset: EditorExportPreset, debug: bool, path: String) -> Dictionary
save_zip(preset: EditorExportPreset, debug: bool, path: String) -> Dictionary
save_zip_patch(preset: EditorExportPreset, debug: bool, path: String) -> Dictionary
ssh_push_to_remote(host: String, port: String, scp_args: PackedStringArray, src_file: String, dst_file: String) -> int (Error) [const]
ssh_run_on_remote(host: String, port: String, ssh_arg: PackedStringArray, cmd_args: String, output: Array = [], port_fwd: int = -1) -> int (Error) [const]
ssh_run_on_remote_no_wait(host: String, port: String, ssh_args: PackedStringArray, cmd_args: String, port_fwd: int = -1) -> int [const]
```

## Methods

- add_message(type: int (EditorExportPlatform.ExportMessageType), category: String, message: String) -> void
  Adds a message to the export log that will be displayed when exporting ends.

- clear_messages() -> void
  Clears the export log.

- create_preset() -> EditorExportPreset
  Create a new preset for this platform.

- export_pack(preset: EditorExportPreset, debug: bool, path: String, flags: int (EditorExportPlatform.DebugFlags) = 0) -> int (Error)
  Creates a PCK archive at path for the specified preset.

- export_pack_patch(preset: EditorExportPreset, debug: bool, path: String, patches: PackedStringArray = PackedStringArray(), flags: int (EditorExportPlatform.DebugFlags) = 0) -> int (Error)
  Creates a patch PCK archive at path for the specified preset, containing only the files that have changed since the last patch. **Note:** patches is an optional override of the set of patches defined in the export preset. When empty the patches defined in the export preset will be used instead.

- export_project(preset: EditorExportPreset, debug: bool, path: String, flags: int (EditorExportPlatform.DebugFlags) = 0) -> int (Error)
  Creates a full project at path for the specified preset.

- export_project_files(preset: EditorExportPreset, debug: bool, save_cb: Callable, shared_cb: Callable = Callable()) -> int (Error)
  Exports project files for the specified preset. This method can be used to implement custom export format, other than PCK and ZIP. One of the callbacks is called for each exported file. save_cb is called for all exported files and have the following arguments: file_path: String, file_data: PackedByteArray, file_index: int, file_count: int, encryption_include_filters: PackedStringArray, encryption_exclude_filters: PackedStringArray, encryption_key: PackedByteArray. shared_cb is called for exported native shared/static libraries and have the following arguments: file_path: String, tags: PackedStringArray, target_folder: String. **Note:** file_index and file_count are intended for progress tracking only and aren't necessarily unique and precise.

- export_zip(preset: EditorExportPreset, debug: bool, path: String, flags: int (EditorExportPlatform.DebugFlags) = 0) -> int (Error)
  Create a ZIP archive at path for the specified preset.

- export_zip_patch(preset: EditorExportPreset, debug: bool, path: String, patches: PackedStringArray = PackedStringArray(), flags: int (EditorExportPlatform.DebugFlags) = 0) -> int (Error)
  Create a patch ZIP archive at path for the specified preset, containing only the files that have changed since the last patch. **Note:** patches is an optional override of the set of patches defined in the export preset. When empty the patches defined in the export preset will be used instead.

- find_export_template(template_file_name: String) -> Dictionary [const]
  Locates export template for the platform, and returns Dictionary with the following keys: path: String and error: String. This method is provided for convenience and custom export platforms aren't required to use it or keep export templates stored in the same way official templates are.

- gen_export_flags(flags: int (EditorExportPlatform.DebugFlags)) -> PackedStringArray
  Generates array of command line arguments for the default export templates for the debug flags and editor settings.

- get_current_presets() -> Array [const]
  Returns array of EditorExportPresets for this platform.

- get_forced_export_files(preset: EditorExportPreset = null) -> PackedStringArray [static]
  Returns array of core file names that always should be exported regardless of preset config.

- get_internal_export_files(preset: EditorExportPreset, debug: bool) -> Dictionary
  Returns additional files that should always be exported regardless of preset configuration, and are not part of the project source. The returned Dictionary contains filename keys (String) and their corresponding raw data (PackedByteArray).

- get_message_category(index: int) -> String [const]
  Returns the message category for the message with the given index.

- get_message_count() -> int [const]
  Returns the number of messages in the export log.

- get_message_text(index: int) -> String [const]
  Returns the text for the message with the given index.

- get_message_type(index: int) -> int (EditorExportPlatform.ExportMessageType) [const]
  Returns the type for the message with the given index.

- get_os_name() -> String [const]
  Returns the name of the export operating system handled by this EditorExportPlatform class, as a friendly string. Possible return values are Windows, Linux, macOS, Android, iOS, and Web.

- get_worst_message_type() -> int (EditorExportPlatform.ExportMessageType) [const]
  Returns most severe message type currently present in the export log.

- save_pack(preset: EditorExportPreset, debug: bool, path: String, embed: bool = false) -> Dictionary
  Saves PCK archive and returns Dictionary with the following keys: result: Error, so_files: Array (array of the shared/static objects which contains dictionaries with the following keys: path: String, tags: PackedStringArray, and target_folder: String). If embed is true, PCK content is appended to the end of path file and return Dictionary additionally include following keys: embedded_start: int (embedded PCK offset) and embedded_size: int (embedded PCK size).

- save_pack_patch(preset: EditorExportPreset, debug: bool, path: String) -> Dictionary
  Saves patch PCK archive and returns Dictionary with the following keys: result: Error, so_files: Array (array of the shared/static objects which contains dictionaries with the following keys: path: String, tags: PackedStringArray, and target_folder: String).

- save_zip(preset: EditorExportPreset, debug: bool, path: String) -> Dictionary
  Saves ZIP archive and returns Dictionary with the following keys: result: Error, so_files: Array (array of the shared/static objects which contains dictionaries with the following keys: path: String, tags: PackedStringArray, and target_folder: String).

- save_zip_patch(preset: EditorExportPreset, debug: bool, path: String) -> Dictionary
  Saves patch ZIP archive and returns Dictionary with the following keys: result: Error, so_files: Array (array of the shared/static objects which contains dictionaries with the following keys: path: String, tags: PackedStringArray, and target_folder: String).

- ssh_push_to_remote(host: String, port: String, scp_args: PackedStringArray, src_file: String, dst_file: String) -> int (Error) [const]
  Uploads specified file over SCP protocol to the remote host.

- ssh_run_on_remote(host: String, port: String, ssh_arg: PackedStringArray, cmd_args: String, output: Array = [], port_fwd: int = -1) -> int (Error) [const]
  Executes specified command on the remote host via SSH protocol and returns command output in the output.

- ssh_run_on_remote_no_wait(host: String, port: String, ssh_args: PackedStringArray, cmd_args: String, port_fwd: int = -1) -> int [const]
  Executes specified command on the remote host via SSH protocol and returns process ID (on the remote host) without waiting for command to finish.

## Constants

### Enum ExportMessageType

- EXPORT_MESSAGE_NONE = 0
  Invalid message type used as the default value when no type is specified.

- EXPORT_MESSAGE_INFO = 1
  Message type for informational messages that have no effect on the export.

- EXPORT_MESSAGE_WARNING = 2
  Message type for warning messages that should be addressed but still allow to complete the export.

- EXPORT_MESSAGE_ERROR = 3
  Message type for error messages that must be addressed and fail the export.

### Enum DebugFlags

- DEBUG_FLAG_DUMB_CLIENT = 1 [bitfield]
  Flag is set if the remotely debugged project is expected to use the remote file system. If set, gen_export_flags() will append --remote-fs and --remote-fs-password (if EditorSettings.filesystem/file_server/password is defined) command line arguments to the returned list.

- DEBUG_FLAG_REMOTE_DEBUG = 2 [bitfield]
  Flag is set if remote debug is enabled. If set, gen_export_flags() will append --remote-debug and --breakpoints (if breakpoints are selected in the script editor or added by the plugin) command line arguments to the returned list.

- DEBUG_FLAG_REMOTE_DEBUG_LOCALHOST = 4 [bitfield]
  Flag is set if remotely debugged project is running on the localhost. If set, gen_export_flags() will use localhost instead of EditorSettings.network/debug/remote_host as remote debugger host.

- DEBUG_FLAG_VIEW_COLLISIONS = 8 [bitfield]
  Flag is set if the "Visible Collision Shapes" remote debug option is enabled. If set, gen_export_flags() will append the --debug-collisions command line argument to the returned list.

- DEBUG_FLAG_VIEW_NAVIGATION = 16 [bitfield]
  Flag is set if the "Visible Navigation" remote debug option is enabled. If set, gen_export_flags() will append the --debug-navigation command line argument to the returned list.
