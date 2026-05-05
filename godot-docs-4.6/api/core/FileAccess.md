# FileAccess

## Meta

- Name: FileAccess
- Source: FileAccess.xml
- Inherits: RefCounted
- Inheritance Chain: FileAccess -> RefCounted -> Object

## Brief Description

Provides methods for file reading and writing operations.

## Description

This class can be used to permanently store data in the user device's file system and to read from it. This is useful for storing game save data or player configuration files. **Example:** How to write and read from a file. The file named "save_game.dat" will be stored in the user data folder, as specified in the [Data paths]($DOCS_URL/tutorials/io/data_paths.html) documentation:

```
func save_to_file(content):
    var file = FileAccess.open("user://save_game.dat", FileAccess.WRITE)
    file.store_string(content)

func load_from_file():
    var file = FileAccess.open("user://save_game.dat", FileAccess.READ)
    var content = file.get_as_text()
    return content
```

```
public void SaveToFile(string content)
{
    using var file = FileAccess.Open("user://save_game.dat", FileAccess.ModeFlags.Write);
    file.StoreString(content);
}

public string LoadFromFile()
{
    using var file = FileAccess.Open("user://save_game.dat", FileAccess.ModeFlags.Read);
    string content = file.GetAsText();
    return content;
}
```

A FileAccess instance has its own file cursor, which is the position in bytes in the file where the next read/write operation will occur. Functions such as get_8(), get_16(), store_8(), and store_16() will move the file cursor forward by the number of bytes read/written. The file cursor can be moved to a specific position using seek() or seek_end(), and its position can be retrieved using get_position(). A FileAccess instance will close its file when the instance is freed. Since it inherits RefCounted, this happens automatically when it is no longer in use. close() can be called to close it earlier. In C#, the reference must be disposed manually, which can be done with the using statement or by calling the Dispose method directly. **Note:** To access project resources once exported, it is recommended to use ResourceLoader instead of FileAccess, as some files are converted to engine-specific formats and their original source files might not be present in the exported PCK package. If using FileAccess, make sure the file is included in the export by changing its import mode to **Keep File (exported as is)** in the Import dock, or, for files where this option is not available, change the non-resource export filter in the Export dialog to include the file's extension (e.g. *.txt). **Note:** Files are automatically closed only if the process exits "normally" (such as by clicking the window manager's close button or pressing Alt + F4). If you stop the project execution by pressing F8 while the project is running, the file won't be closed as the game process will be killed. You can work around this by calling flush() at regular intervals.

## Quick Reference

```
[methods]
close() -> void
create_temp(mode_flags: int (FileAccess.ModeFlags), prefix: String = "", extension: String = "", keep: bool = false) -> FileAccess [static]
eof_reached() -> bool [const]
file_exists(path: String) -> bool [static]
flush() -> void
get_8() -> int [const]
get_16() -> int [const]
get_32() -> int [const]
get_64() -> int [const]
get_access_time(file: String) -> int [static]
get_as_text() -> String [const]
get_buffer(length: int) -> PackedByteArray [const]
get_csv_line(delim: String = ",") -> PackedStringArray [const]
get_double() -> float [const]
get_error() -> int (Error) [const]
get_extended_attribute(file: String, attribute_name: String) -> PackedByteArray [static]
get_extended_attribute_string(file: String, attribute_name: String) -> String [static]
get_extended_attributes_list(file: String) -> PackedStringArray [static]
get_file_as_bytes(path: String) -> PackedByteArray [static]
get_file_as_string(path: String) -> String [static]
get_float() -> float [const]
get_half() -> float [const]
get_hidden_attribute(file: String) -> bool [static]
get_length() -> int [const]
get_line() -> String [const]
get_md5(path: String) -> String [static]
get_modified_time(file: String) -> int [static]
get_open_error() -> int (Error) [static]
get_pascal_string() -> String
get_path() -> String [const]
get_path_absolute() -> String [const]
get_position() -> int [const]
get_read_only_attribute(file: String) -> bool [static]
get_real() -> float [const]
get_sha256(path: String) -> String [static]
get_size(file: String) -> int [static]
get_unix_permissions(file: String) -> int (FileAccess.UnixPermissionFlags) [static]
get_var(allow_objects: bool = false) -> Variant [const]
is_open() -> bool [const]
open(path: String, flags: int (FileAccess.ModeFlags)) -> FileAccess [static]
open_compressed(path: String, mode_flags: int (FileAccess.ModeFlags), compression_mode: int (FileAccess.CompressionMode) = 0) -> FileAccess [static]
open_encrypted(path: String, mode_flags: int (FileAccess.ModeFlags), key: PackedByteArray, iv: PackedByteArray = PackedByteArray()) -> FileAccess [static]
open_encrypted_with_pass(path: String, mode_flags: int (FileAccess.ModeFlags), pass: String) -> FileAccess [static]
remove_extended_attribute(file: String, attribute_name: String) -> int (Error) [static]
resize(length: int) -> int (Error)
seek(position: int) -> void
seek_end(position: int = 0) -> void
set_extended_attribute(file: String, attribute_name: String, data: PackedByteArray) -> int (Error) [static]
set_extended_attribute_string(file: String, attribute_name: String, data: String) -> int (Error) [static]
set_hidden_attribute(file: String, hidden: bool) -> int (Error) [static]
set_read_only_attribute(file: String, ro: bool) -> int (Error) [static]
set_unix_permissions(file: String, permissions: int (FileAccess.UnixPermissionFlags)) -> int (Error) [static]
store_8(value: int) -> bool
store_16(value: int) -> bool
store_32(value: int) -> bool
store_64(value: int) -> bool
store_buffer(buffer: PackedByteArray) -> bool
store_csv_line(values: PackedStringArray, delim: String = ",") -> bool
store_double(value: float) -> bool
store_float(value: float) -> bool
store_half(value: float) -> bool
store_line(line: String) -> bool
store_pascal_string(string: String) -> bool
store_real(value: float) -> bool
store_string(string: String) -> bool
store_var(value: Variant, full_objects: bool = false) -> bool

[properties]
big_endian: bool
```

## Tutorials

- [File system]($DOCS_URL/tutorials/scripting/filesystem.html)
- [Runtime file loading and saving]($DOCS_URL/tutorials/io/runtime_file_loading_and_saving.html)
- [Binary serialization API]($DOCS_URL/tutorials/io/binary_serialization_api.html)
- [3D Voxel Demo](https://godotengine.org/asset-library/asset/2755)

## Methods

- close() -> void
  Closes the currently opened file and prevents subsequent read/write operations. Use flush() to persist the data to disk without closing the file. **Note:** FileAccess will automatically close when it's freed, which happens when it goes out of scope or when it gets assigned with null. In C# the reference must be disposed after we are done using it, this can be done with the using statement or calling the Dispose method directly.

- create_temp(mode_flags: int (FileAccess.ModeFlags), prefix: String = "", extension: String = "", keep: bool = false) -> FileAccess [static]
  Creates a temporary file. This file will be freed when the returned FileAccess is freed. If prefix is not empty, it will be prefixed to the file name, separated by a -. If extension is not empty, it will be appended to the temporary file name. If keep is true, the file is not deleted when the returned FileAccess is freed. Returns null if opening the file failed. You can use get_open_error() to check the error that occurred.

- eof_reached() -> bool [const]
  Returns true if the file cursor has already read past the end of the file. **Note:** eof_reached() == false cannot be used to check whether there is more data available. To loop while there is more data available, use:


```
  while file.get_position() < file.get_length():
      # Read data

```

```
  while (file.GetPosition() < file.GetLength())
  {
      // Read data
  }

```

- file_exists(path: String) -> bool [static]
  Returns true if the file exists in the given path. **Note:** Many resources types are imported (e.g. textures or sound files), and their source asset will not be included in the exported game, as only the imported version is used. See ResourceLoader.exists() for an alternative approach that takes resource remapping into account. For a non-static, relative equivalent, use DirAccess.file_exists().

- flush() -> void
  Writes the file's buffer to disk. Flushing is automatically performed when the file is closed. This means you don't need to call flush() manually before closing a file. Still, calling flush() can be used to ensure the data is safe even if the project crashes instead of being closed gracefully. **Note:** Only call flush() when you actually need it. Otherwise, it will decrease performance due to constant disk writes.

- get_8() -> int [const]
  Returns the next 8 bits from the file as an integer. This advances the file cursor by 1 byte. See store_8() for details on what values can be stored and retrieved this way.

- get_16() -> int [const]
  Returns the next 16 bits from the file as an integer. This advances the file cursor by 2 bytes. See store_16() for details on what values can be stored and retrieved this way.

- get_32() -> int [const]
  Returns the next 32 bits from the file as an integer. This advances the file cursor by 4 bytes. See store_32() for details on what values can be stored and retrieved this way.

- get_64() -> int [const]
  Returns the next 64 bits from the file as an integer. This advances the file cursor by 8 bytes. See store_64() for details on what values can be stored and retrieved this way.

- get_access_time(file: String) -> int [static]
  Returns the last time the file was accessed in Unix timestamp format, or 0 on error. This Unix timestamp can be converted to another format using the Time singleton.

- get_as_text() -> String [const]
  Returns the whole file as a String. Text is interpreted as being UTF-8 encoded. This ignores the file cursor and does not affect it.

- get_buffer(length: int) -> PackedByteArray [const]
  Returns next length bytes of the file as a PackedByteArray. This advances the file cursor by length bytes.

- get_csv_line(delim: String = ",") -> PackedStringArray [const]
  Returns the next value of the file in CSV (Comma-Separated Values) format. You can pass a different delimiter delim to use other than the default "," (comma). This delimiter must be one-character long, and cannot be a double quotation mark. Text is interpreted as being UTF-8 encoded. Text values must be enclosed in double quotes if they include the delimiter character. Double quotes within a text value can be escaped by doubling their occurrence. This advances the file cursor to after the newline character at the end of the line. For example, the following CSV lines are valid and will be properly parsed as two strings each: [codeblock lang=text] Alice,"Hello, Bob!" Bob,Alice! What a surprise! Alice,"I thought you'd reply with ""Hello, world""."


``` Note how the second line can omit the enclosing quotes as it does not include the delimiter. However it *could* very well use quotes, it was only written without for demonstration purposes. The third line must use "" for each quotation mark that needs to be interpreted as such instead of the end of a text value.

- get_double() -> float [const]
  Returns the next 64 bits from the file as a floating-point number. This advances the file cursor by 8 bytes.

- get_error() -> int (Error) [const]
  Returns the last error that happened when trying to perform operations. Compare with the ERR_FILE_* constants from Error.

- get_extended_attribute(file: String, attribute_name: String) -> PackedByteArray [static]
  Reads the file extended attribute with name attribute_name as a byte array. **Note:** This method is implemented on Linux, macOS, and Windows. **Note:** Extended attributes support depends on the file system. Attributes will be lost when the file is moved between incompatible file systems. **Note:** On Linux, only "user" namespace attributes are accessible, namespace prefix should not be included. **Note:** On Windows, alternate data streams are used to store extended attributes.

- get_extended_attribute_string(file: String, attribute_name: String) -> String [static]
  Reads the file extended attribute with name attribute_name as a UTF-8 encoded string. **Note:** This method is implemented on Linux, macOS, and Windows. **Note:** Extended attributes support depends on the file system. Attributes will be lost when the file is moved between incompatible file systems. **Note:** On Linux, only "user" namespace attributes are accessible, namespace prefix should not be included. **Note:** On Windows, alternate data streams are used to store extended attributes.

- get_extended_attributes_list(file: String) -> PackedStringArray [static]
  Returns a list of file extended attributes. **Note:** This method is implemented on Linux, macOS, and Windows. **Note:** Extended attributes support depends on the file system. Attributes will be lost when the file is moved between incompatible file systems. **Note:** On Linux, only "user" namespace attributes are accessible, namespace prefix should not be included. **Note:** On Windows, alternate data streams are used to store extended attributes.

- get_file_as_bytes(path: String) -> PackedByteArray [static]
  Returns the whole path file contents as a PackedByteArray without any decoding. Returns an empty PackedByteArray if an error occurred while opening the file. You can use get_open_error() to check the error that occurred.

- get_file_as_string(path: String) -> String [static]
  Returns the whole path file contents as a String. Text is interpreted as being UTF-8 encoded. Returns an empty String if an error occurred while opening the file. You can use get_open_error() to check the error that occurred.

- get_float() -> float [const]
  Returns the next 32 bits from the file as a floating-point number. This advances the file cursor by 4 bytes.

- get_half() -> float [const]
  Returns the next 16 bits from the file as a half-precision floating-point number. This advances the file cursor by 2 bytes.

- get_hidden_attribute(file: String) -> bool [static]
  Returns true if the **hidden** attribute is set on the file at the given path. **Note:** This method is implemented on iOS, BSD, macOS, and Windows.

- get_length() -> int [const]
  Returns the size of the file in bytes. For a pipe, returns the number of bytes available for reading from the pipe.

- get_line() -> String [const]
  Returns the next line of the file as a String. The returned string doesn't include newline (\n) or carriage return (\r) characters, but does include any other leading or trailing whitespace. This advances the file cursor to after the newline character at the end of the line. Text is interpreted as being UTF-8 encoded.

- get_md5(path: String) -> String [static]
  Returns an MD5 String representing the file at the given path or an empty String on failure.

- get_modified_time(file: String) -> int [static]
  Returns the last time the file was modified in Unix timestamp format, or 0 on error. This Unix timestamp can be converted to another format using the Time singleton.

- get_open_error() -> int (Error) [static]
  Returns the result of the last open() call in the current thread.

- get_pascal_string() -> String
  Returns a String saved in Pascal format from the file, meaning that the length of the string is explicitly stored at the start. See store_pascal_string(). This may include newline characters. The file cursor is advanced after the bytes read. Text is interpreted as being UTF-8 encoded.

- get_path() -> String [const]
  Returns the path as a String for the current open file.

- get_path_absolute() -> String [const]
  Returns the absolute path as a String for the current open file.

- get_position() -> int [const]
  Returns the file cursor's position in bytes from the beginning of the file. This is the file reading/writing cursor set by seek() or seek_end() and advanced by read/write operations.

- get_read_only_attribute(file: String) -> bool [static]
  Returns true if the **read only** attribute is set on the file at the given path. **Note:** This method is implemented on iOS, BSD, macOS, and Windows.

- get_real() -> float [const]
  Returns the next bits from the file as a floating-point number. This advances the file cursor by either 4 or 8 bytes, depending on the precision used by the Godot build that saved the file. If the file was saved by a Godot build compiled with the precision=single option (the default), the number of read bits for that file is 32. Otherwise, if compiled with the precision=double option, the number of read bits is 64.

- get_sha256(path: String) -> String [static]
  Returns an SHA-256 String representing the file at the given path or an empty String on failure.

- get_size(file: String) -> int [static]
  Returns the size of the file at the given path, in bytes, or -1 on error.

- get_unix_permissions(file: String) -> int (FileAccess.UnixPermissionFlags) [static]
  Returns the UNIX permissions of the file at the given path. **Note:** This method is implemented on iOS, Linux/BSD, and macOS.

- get_var(allow_objects: bool = false) -> Variant [const]
  Returns the next Variant value from the file. If allow_objects is true, decoding objects is allowed. This advances the file cursor by the number of bytes read. Internally, this uses the same decoding mechanism as the @GlobalScope.bytes_to_var() method, as described in the [Binary serialization API]($DOCS_URL/tutorials/io/binary_serialization_api.html) documentation. **Warning:** Deserialized objects can contain code which gets executed. Do not use this option if the serialized object comes from untrusted sources to avoid potential security threats such as remote code execution.

- is_open() -> bool [const]
  Returns true if the file is currently opened.

- open(path: String, flags: int (FileAccess.ModeFlags)) -> FileAccess [static]
  Creates a new FileAccess object and opens the file for writing or reading, depending on the flags. Returns null if opening the file failed. You can use get_open_error() to check the error that occurred.

- open_compressed(path: String, mode_flags: int (FileAccess.ModeFlags), compression_mode: int (FileAccess.CompressionMode) = 0) -> FileAccess [static]
  Creates a new FileAccess object and opens a compressed file for reading or writing. **Note:** open_compressed() can only read files that were saved by Godot, not third-party compression formats. See [GitHub issue #28999](https://github.com/godotengine/godot/issues/28999) for a workaround. Returns null if opening the file failed. You can use get_open_error() to check the error that occurred.

- open_encrypted(path: String, mode_flags: int (FileAccess.ModeFlags), key: PackedByteArray, iv: PackedByteArray = PackedByteArray()) -> FileAccess [static]
  Creates a new FileAccess object and opens an encrypted file in write or read mode. You need to pass a binary key to encrypt/decrypt it. **Note:** The provided key must be 32 bytes long. Returns null if opening the file failed. You can use get_open_error() to check the error that occurred.

- open_encrypted_with_pass(path: String, mode_flags: int (FileAccess.ModeFlags), pass: String) -> FileAccess [static]
  Creates a new FileAccess object and opens an encrypted file in write or read mode. You need to pass a password to encrypt/decrypt it. Returns null if opening the file failed. You can use get_open_error() to check the error that occurred.

- remove_extended_attribute(file: String, attribute_name: String) -> int (Error) [static]
  Removes file extended attribute with name attribute_name. **Note:** This method is implemented on Linux, macOS, and Windows. **Note:** Extended attributes support depends on the file system. Attributes will be lost when the file is moved between incompatible file systems. **Note:** On Linux, only "user" namespace attributes are accessible, namespace prefix should not be included. **Note:** On Windows, alternate data streams are used to store extended attributes.

- resize(length: int) -> int (Error)
  Resizes the file to a specified length. The file must be open in a mode that permits writing. If the file is extended, NUL characters are appended. If the file is truncated, all data from the end file to the original length of the file is lost.

- seek(position: int) -> void
  Sets the file cursor to the specified position in bytes, from the beginning of the file. This changes the value returned by get_position().

- seek_end(position: int = 0) -> void
  Sets the file cursor to the specified position in bytes, from the end of the file. This changes the value returned by get_position(). **Note:** This is an offset, so you should use negative numbers otherwise the file cursor will be at the end of the file.

- set_extended_attribute(file: String, attribute_name: String, data: PackedByteArray) -> int (Error) [static]
  Writes file extended attribute with name attribute_name as a byte array. **Note:** This method is implemented on Linux, macOS, and Windows. **Note:** Extended attributes support depends on the file system. Attributes will be lost when the file is moved between incompatible file systems. **Note:** On Linux, only "user" namespace attributes are accessible, namespace prefix should not be included. **Note:** On Windows, alternate data streams are used to store extended attributes.

- set_extended_attribute_string(file: String, attribute_name: String, data: String) -> int (Error) [static]
  Writes file extended attribute with name attribute_name as a UTF-8 encoded string. **Note:** This method is implemented on Linux, macOS, and Windows. **Note:** Extended attributes support depends on the file system. Attributes will be lost when the file is moved between incompatible file systems. **Note:** On Linux, only "user" namespace attributes are accessible, namespace prefix should not be included. **Note:** On Windows, alternate data streams are used to store extended attributes.

- set_hidden_attribute(file: String, hidden: bool) -> int (Error) [static]
  Sets file **hidden** attribute. **Note:** This method is implemented on iOS, BSD, macOS, and Windows.

- set_read_only_attribute(file: String, ro: bool) -> int (Error) [static]
  Sets file **read only** attribute. **Note:** This method is implemented on iOS, BSD, macOS, and Windows.

- set_unix_permissions(file: String, permissions: int (FileAccess.UnixPermissionFlags)) -> int (Error) [static]
  Sets file UNIX permissions. **Note:** This method is implemented on iOS, Linux/BSD, and macOS.

- store_8(value: int) -> bool
  Stores an integer as 8 bits in the file. This advances the file cursor by 1 byte. Returns true if the operation is successful. **Note:** The value should lie in the interval [0, 255]. Any other value will overflow and wrap around. **Note:** If an error occurs, the resulting value of the file position indicator is indeterminate. To store a signed integer, use store_64(), or convert it manually (see store_16() for an example).

- store_16(value: int) -> bool
  Stores an integer as 16 bits in the file. This advances the file cursor by 2 bytes. Returns true if the operation is successful. **Note:** The value should lie in the interval [0, 2^16 - 1]. Any other value will overflow and wrap around. **Note:** If an error occurs, the resulting value of the file position indicator is indeterminate. To store a signed integer, use store_64() or store a signed integer from the interval [-2^15, 2^15 - 1] (i.e. keeping one bit for the signedness) and compute its sign manually when reading. For example:


```
  const MAX_15B = 1 << 15
  const MAX_16B = 1 << 16

  func unsigned16_to_signed(unsigned):
      return (unsigned + MAX_15B) % MAX_16B - MAX_15B

  func _ready():
      var f = FileAccess.open("user://file.dat", FileAccess.WRITE_READ)
      f.store_16(-42) # This wraps around and stores 65494 (2^16 - 42).
      f.store_16(121) # In bounds, will store 121.
      f.seek(0) # Go back to start to read the stored value.
      var read1 = f.get_16() # 65494
      var read2 = f.get_16() # 121
      var converted1 = unsigned16_to_signed(read1) # -42
      var converted2 = unsigned16_to_signed(read2) # 121

```

```
  public override void _Ready()
  {
      using var f = FileAccess.Open("user://file.dat", FileAccess.ModeFlags.WriteRead);
      f.Store16(unchecked((ushort)-42)); // This wraps around and stores 65494 (2^16 - 42).
      f.Store16(121); // In bounds, will store 121.
      f.Seek(0); // Go back to start to read the stored value.
      ushort read1 = f.Get16(); // 65494
      ushort read2 = f.Get16(); // 121
      short converted1 = (short)read1; // -42
      short converted2 = (short)read2; // 121
  }

```

- store_32(value: int) -> bool
  Stores an integer as 32 bits in the file. This advances the file cursor by 4 bytes. Returns true if the operation is successful. **Note:** The value should lie in the interval [0, 2^32 - 1]. Any other value will overflow and wrap around. **Note:** If an error occurs, the resulting value of the file position indicator is indeterminate. To store a signed integer, use store_64(), or convert it manually (see store_16() for an example).

- store_64(value: int) -> bool
  Stores an integer as 64 bits in the file. This advances the file cursor by 8 bytes. Returns true if the operation is successful. **Note:** The value must lie in the interval [-2^63, 2^63 - 1] (i.e. be a valid int value). **Note:** If an error occurs, the resulting value of the file position indicator is indeterminate.

- store_buffer(buffer: PackedByteArray) -> bool
  Stores the given array of bytes in the file. This advances the file cursor by the number of bytes written. Returns true if the operation is successful. **Note:** If an error occurs, the resulting value of the file position indicator is indeterminate.

- store_csv_line(values: PackedStringArray, delim: String = ",") -> bool
  Stores the given PackedStringArray in the file as a line formatted in the CSV (Comma-Separated Values) format. You can pass a different delimiter delim to use other than the default "," (comma). This delimiter must be one-character long. Text will be encoded as UTF-8. Returns true if the operation is successful. **Note:** If an error occurs, the resulting value of the file position indicator is indeterminate.

- store_double(value: float) -> bool
  Stores a floating-point number as 64 bits in the file. This advances the file cursor by 8 bytes. Returns true if the operation is successful. **Note:** If an error occurs, the resulting value of the file position indicator is indeterminate.

- store_float(value: float) -> bool
  Stores a floating-point number as 32 bits in the file. This advances the file cursor by 4 bytes. Returns true if the operation is successful. **Note:** If an error occurs, the resulting value of the file position indicator is indeterminate.

- store_half(value: float) -> bool
  Stores a half-precision floating-point number as 16 bits in the file. This advances the file cursor by 2 bytes. Returns true if the operation is successful. **Note:** If an error occurs, the resulting value of the file position indicator is indeterminate.

- store_line(line: String) -> bool
  Stores line in the file followed by a newline character (\n), encoding the text as UTF-8. This advances the file cursor by the length of the line, after the newline character. The amount of bytes written depends on the UTF-8 encoded bytes, which may be different from String.length() which counts the number of UTF-32 codepoints. Returns true if the operation is successful. **Note:** If an error occurs, the resulting value of the file position indicator is indeterminate.

- store_pascal_string(string: String) -> bool
  Stores the given String as a line in the file in Pascal format (i.e. also store the length of the string). Text will be encoded as UTF-8. This advances the file cursor by the number of bytes written depending on the UTF-8 encoded bytes, which may be different from String.length() which counts the number of UTF-32 codepoints. Returns true if the operation is successful. **Note:** If an error occurs, the resulting value of the file position indicator is indeterminate.

- store_real(value: float) -> bool
  Stores a floating-point number in the file. This advances the file cursor by either 4 or 8 bytes, depending on the precision used by the current Godot build. If using a Godot build compiled with the precision=single option (the default), this method will save a 32-bit float. Otherwise, if compiled with the precision=double option, this will save a 64-bit float. Returns true if the operation is successful. **Note:** If an error occurs, the resulting value of the file position indicator is indeterminate.

- store_string(string: String) -> bool
  Stores string in the file without a newline character (\n), encoding the text as UTF-8. This advances the file cursor by the length of the string in UTF-8 encoded bytes, which may be different from String.length() which counts the number of UTF-32 codepoints. Returns true if the operation is successful. **Note:** This method is intended to be used to write text files. The string is stored as a UTF-8 encoded buffer without string length or terminating zero, which means that it can't be loaded back easily. If you want to store a retrievable string in a binary file, consider using store_pascal_string() instead. For retrieving strings from a text file, you can use get_buffer(length).get_string_from_utf8() (if you know the length) or get_as_text(). **Note:** If an error occurs, the resulting value of the file position indicator is indeterminate.

- store_var(value: Variant, full_objects: bool = false) -> bool
  Stores any Variant value in the file. If full_objects is true, encoding objects is allowed (and can potentially include code). This advances the file cursor by the number of bytes written. Returns true if the operation is successful. Internally, this uses the same encoding mechanism as the @GlobalScope.var_to_bytes() method, as described in the [Binary serialization API]($DOCS_URL/tutorials/io/binary_serialization_api.html) documentation. **Note:** Not all properties are included. Only properties that are configured with the PROPERTY_USAGE_STORAGE flag set will be serialized. You can add a new usage flag to a property by overriding the Object._get_property_list() method in your class. You can also check how property usage is configured by calling Object._get_property_list(). See PropertyUsageFlags for the possible usage flags. **Note:** If an error occurs, the resulting value of the file position indicator is indeterminate.

## Properties

- big_endian: bool [set set_big_endian; get is_big_endian]
  If true, the file is read with big-endian endianness(https://en.wikipedia.org/wiki/Endianness). If false, the file is read with little-endian endianness. If in doubt, leave this to false as most files are written with little-endian endianness. **Note:** This is always reset to system endianness, which is little-endian on all supported platforms, whenever you open the file. Therefore, you must set big_endian *after* opening the file, not before.

## Constants

### Enum ModeFlags

- READ = 1
  Opens the file for read operations. The file cursor is positioned at the beginning of the file.

- WRITE = 2
  Opens the file for write operations. If the file exists, it is truncated to zero length and its contents are cleared. Otherwise, it is created. **Note:** When creating a file it must be in an already existing directory. To recursively create directories for a file path, see DirAccess.make_dir_recursive().

- READ_WRITE = 3
  Opens the file for read and write operations. Does not truncate the file. The file cursor is positioned at the beginning of the file.

- WRITE_READ = 7
  Opens the file for read and write operations. If the file exists, it is truncated to zero length and its contents are cleared. Otherwise, it is created. The file cursor is positioned at the beginning of the file. **Note:** When creating a file it must be in an already existing directory. To recursively create directories for a file path, see DirAccess.make_dir_recursive().

### Enum CompressionMode

- COMPRESSION_FASTLZ = 0
  Uses the FastLZ(https://fastlz.org/) compression method.

- COMPRESSION_DEFLATE = 1
  Uses the DEFLATE(https://en.wikipedia.org/wiki/DEFLATE) compression method.

- COMPRESSION_ZSTD = 2
  Uses the Zstandard(https://facebook.github.io/zstd/) compression method.

- COMPRESSION_GZIP = 3
  Uses the gzip(https://www.gzip.org/) compression method.

- COMPRESSION_BROTLI = 4
  Uses the brotli(https://github.com/google/brotli) compression method (only decompression is supported).

### Enum UnixPermissionFlags

- UNIX_READ_OWNER = 256 [bitfield]
  Read for owner bit.

- UNIX_WRITE_OWNER = 128 [bitfield]
  Write for owner bit.

- UNIX_EXECUTE_OWNER = 64 [bitfield]
  Execute for owner bit.

- UNIX_READ_GROUP = 32 [bitfield]
  Read for group bit.

- UNIX_WRITE_GROUP = 16 [bitfield]
  Write for group bit.

- UNIX_EXECUTE_GROUP = 8 [bitfield]
  Execute for group bit.

- UNIX_READ_OTHER = 4 [bitfield]
  Read for other bit.

- UNIX_WRITE_OTHER = 2 [bitfield]
  Write for other bit.

- UNIX_EXECUTE_OTHER = 1 [bitfield]
  Execute for other bit.

- UNIX_SET_USER_ID = 2048 [bitfield]
  Set user id on execution bit.

- UNIX_SET_GROUP_ID = 1024 [bitfield]
  Set group id on execution bit.

- UNIX_RESTRICTED_DELETE = 512 [bitfield]
  Restricted deletion (sticky) bit.
