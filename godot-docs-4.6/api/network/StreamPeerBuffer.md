# StreamPeerBuffer

## Meta

- Name: StreamPeerBuffer
- Source: StreamPeerBuffer.xml
- Inherits: StreamPeer
- Inheritance Chain: StreamPeerBuffer -> StreamPeer -> RefCounted -> Object

## Brief Description

A stream peer used to handle binary data streams.

## Description

A data buffer stream peer that uses a byte array as the stream. This object can be used to handle binary data from network sessions. To handle binary data stored in files, FileAccess can be used directly. A StreamPeerBuffer object keeps an internal cursor which is the offset in bytes to the start of the buffer. Get and put operations are performed at the cursor position and will move the cursor accordingly.

## Quick Reference

```
[methods]
clear() -> void
duplicate() -> StreamPeerBuffer [const]
get_position() -> int [const]
get_size() -> int [const]
resize(size: int) -> void
seek(position: int) -> void

[properties]
data_array: PackedByteArray = PackedByteArray()
```

## Methods

- clear() -> void
  Clears the data_array and resets the cursor.

- duplicate() -> StreamPeerBuffer [const]
  Returns a new StreamPeerBuffer with the same data_array content.

- get_position() -> int [const]
  Returns the current cursor position.

- get_size() -> int [const]
  Returns the size of data_array.

- resize(size: int) -> void
  Resizes the data_array. This *doesn't* update the cursor.

- seek(position: int) -> void
  Moves the cursor to the specified position. position must be a valid index of data_array.

## Properties

- data_array: PackedByteArray = PackedByteArray() [set set_data_array; get get_data_array]
  The underlying data buffer. Setting this value resets the cursor.
