# EditorFileSystemImportFormatSupportQuery

## Meta

- Name: EditorFileSystemImportFormatSupportQuery
- Source: EditorFileSystemImportFormatSupportQuery.xml
- Inherits: RefCounted
- Inheritance Chain: EditorFileSystemImportFormatSupportQuery -> RefCounted -> Object

## Brief Description

Used to query and configure import format support.

## Description

This class is used to query and configure a certain import format. It is used in conjunction with asset format import plugins.

## Quick Reference

```
[methods]
_get_file_extensions() -> PackedStringArray [virtual required const]
_is_active() -> bool [virtual required const]
_query() -> bool [virtual required const]
```

## Methods

- _get_file_extensions() -> PackedStringArray [virtual required const]
  Return the file extensions supported.

- _is_active() -> bool [virtual required const]
  Return whether this importer is active.

- _query() -> bool [virtual required const]
  Query support. Return false if import must not continue.
