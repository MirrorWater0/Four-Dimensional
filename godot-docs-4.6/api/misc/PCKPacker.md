# PCKPacker

## Meta

- Name: PCKPacker
- Source: PCKPacker.xml
- Inherits: RefCounted
- Inheritance Chain: PCKPacker -> RefCounted -> Object

## Brief Description

Creates packages that can be loaded into a running project.

## Description

The PCKPacker is used to create packages that can be loaded into a running project using ProjectSettings.load_resource_pack().

```
var packer = PCKPacker.new()
packer.pck_start("test.pck")
packer.add_file("res://text.txt", "text.txt")
packer.flush()
```

```
var packer = new PckPacker();
packer.PckStart("test.pck");
packer.AddFile("res://text.txt", "text.txt");
packer.Flush();
```

The above PCKPacker creates package test.pck, then adds a file named text.txt at the root of the package. **Note:** PCK is Godot's own pack file format. To create ZIP archives that can be read by any program, use ZIPPacker instead.

## Quick Reference

```
[methods]
add_file(target_path: String, source_path: String, encrypt: bool = false) -> int (Error)
add_file_removal(target_path: String) -> int (Error)
flush(verbose: bool = false) -> int (Error)
pck_start(pck_path: String, alignment: int = 32, key: String = "0000000000000000000000000000000000000000000000000000000000000000", encrypt_directory: bool = false) -> int (Error)
```

## Methods

- add_file(target_path: String, source_path: String, encrypt: bool = false) -> int (Error)
  Adds the source_path file to the current PCK package at the target_path internal path. The res:// prefix for target_path is optional and stripped internally. File content is immediately written to the PCK.

- add_file_removal(target_path: String) -> int (Error)
  Registers a file removal of the target_path internal path to the PCK. This is mainly used for patches. If the file at this path has been loaded from a previous PCK, it will be removed. The res:// prefix for target_path is optional and stripped internally.

- flush(verbose: bool = false) -> int (Error)
  Writes the file directory and closes the PCK. If verbose is true, a list of files added will be printed to the console for easier debugging. **Note:** PCKPacker will automatically flush when it's freed, which happens when it goes out of scope or when it gets assigned with null. In C# the reference must be disposed after use, either with the using statement or by calling the Dispose method directly.

- pck_start(pck_path: String, alignment: int = 32, key: String = "0000000000000000000000000000000000000000000000000000000000000000", encrypt_directory: bool = false) -> int (Error)
  Creates a new PCK file at the file path pck_path. The .pck file extension isn't added automatically, so it should be part of pck_path (even though it's not required).
