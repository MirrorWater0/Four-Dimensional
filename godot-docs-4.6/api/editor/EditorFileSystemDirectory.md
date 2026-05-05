# EditorFileSystemDirectory

## Meta

- Name: EditorFileSystemDirectory
- Source: EditorFileSystemDirectory.xml
- Inherits: Object
- Inheritance Chain: EditorFileSystemDirectory -> Object

## Brief Description

A directory for the resource filesystem.

## Description

A more generalized, low-level variation of the directory concept.

## Quick Reference

```
[methods]
find_dir_index(name: String) -> int [const]
find_file_index(name: String) -> int [const]
get_file(idx: int) -> String [const]
get_file_count() -> int [const]
get_file_import_is_valid(idx: int) -> bool [const]
get_file_path(idx: int) -> String [const]
get_file_script_class_extends(idx: int) -> String [const]
get_file_script_class_name(idx: int) -> String [const]
get_file_type(idx: int) -> StringName [const]
get_name() -> String
get_parent() -> EditorFileSystemDirectory
get_path() -> String [const]
get_subdir(idx: int) -> EditorFileSystemDirectory
get_subdir_count() -> int [const]
```

## Methods

- find_dir_index(name: String) -> int [const]
  Returns the index of the directory with name name or -1 if not found.

- find_file_index(name: String) -> int [const]
  Returns the index of the file with name name or -1 if not found.

- get_file(idx: int) -> String [const]
  Returns the name of the file at index idx.

- get_file_count() -> int [const]
  Returns the number of files in this directory.

- get_file_import_is_valid(idx: int) -> bool [const]
  Returns true if the file at index idx imported properly.

- get_file_path(idx: int) -> String [const]
  Returns the path to the file at index idx.

- get_file_script_class_extends(idx: int) -> String [const]
  Returns the base class of the script class defined in the file at index idx. If the file doesn't define a script class using the class_name syntax, this will return an empty string.

- get_file_script_class_name(idx: int) -> String [const]
  Returns the name of the script class defined in the file at index idx. If the file doesn't define a script class using the class_name syntax, this will return an empty string.

- get_file_type(idx: int) -> StringName [const]
  Returns the resource type of the file at index idx. This returns a string such as "Resource" or "GDScript", *not* a file extension such as ".gd".

- get_name() -> String
  Returns the name of this directory.

- get_parent() -> EditorFileSystemDirectory
  Returns the parent directory for this directory or null if called on a directory at res:// or user://.

- get_path() -> String [const]
  Returns the path to this directory.

- get_subdir(idx: int) -> EditorFileSystemDirectory
  Returns the subdirectory at index idx.

- get_subdir_count() -> int [const]
  Returns the number of subdirectories in this directory.
