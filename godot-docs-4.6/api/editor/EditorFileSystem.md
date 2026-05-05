# EditorFileSystem

## Meta

- Name: EditorFileSystem
- Source: EditorFileSystem.xml
- Inherits: Node
- Inheritance Chain: EditorFileSystem -> Node -> Object

## Brief Description

Resource filesystem, as the editor sees it.

## Description

This object holds information of all resources in the filesystem, their types, etc. **Note:** This class shouldn't be instantiated directly. Instead, access the singleton using EditorInterface.get_resource_filesystem().

## Quick Reference

```
[methods]
get_file_type(path: String) -> String [const]
get_filesystem() -> EditorFileSystemDirectory
get_filesystem_path(path: String) -> EditorFileSystemDirectory
get_scanning_progress() -> float [const]
is_scanning() -> bool [const]
reimport_files(files: PackedStringArray) -> void
scan() -> void
scan_sources() -> void
update_file(path: String) -> void
```

## Methods

- get_file_type(path: String) -> String [const]
  Returns the resource type of the file, given the full path. This returns a string such as "Resource" or "GDScript", *not* a file extension such as ".gd".

- get_filesystem() -> EditorFileSystemDirectory
  Gets the root directory object.

- get_filesystem_path(path: String) -> EditorFileSystemDirectory
  Returns a view into the filesystem at path.

- get_scanning_progress() -> float [const]
  Returns the scan progress for 0 to 1 if the FS is being scanned.

- is_scanning() -> bool [const]
  Returns true if the filesystem is being scanned.

- reimport_files(files: PackedStringArray) -> void
  Reimports a set of files. Call this if these files or their .import files were directly edited by script or an external program. If the file type changed or the file was newly created, use update_file() or scan(). **Note:** This function blocks until the import is finished. However, the main loop iteration, including timers and Node._process(), will occur during the import process due to progress bar updates. Avoid calls to reimport_files() or scan() while an import is in progress.

- scan() -> void
  Scan the filesystem for changes.

- scan_sources() -> void
  Check if the source of any imported resource changed.

- update_file(path: String) -> void
  Add a file in an existing directory, or schedule file information to be updated on editor restart. Can be used to update text files saved by an external program. This will not import the file. To reimport, call reimport_files() or scan() methods.

## Signals

- filesystem_changed()
  Emitted if the filesystem changed.

- resources_reimported(resources: PackedStringArray)
  Emitted if a resource is reimported.

- resources_reimporting(resources: PackedStringArray)
  Emitted before a resource is reimported.

- resources_reload(resources: PackedStringArray)
  Emitted if at least one resource is reloaded when the filesystem is scanned.

- script_classes_updated()
  Emitted when the list of global script classes gets updated.

- sources_changed(exist: bool)
  Emitted if the source of any imported file changed.
