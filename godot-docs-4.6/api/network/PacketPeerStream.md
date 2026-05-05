# PacketPeerStream

## Meta

- Name: PacketPeerStream
- Source: PacketPeerStream.xml
- Inherits: PacketPeer
- Inheritance Chain: PacketPeerStream -> PacketPeer -> RefCounted -> Object

## Brief Description

Wrapper to use a PacketPeer over a StreamPeer.

## Description

PacketStreamPeer provides a wrapper for working using packets over a stream. This allows for using packet based code with StreamPeers. PacketPeerStream implements a custom protocol over the StreamPeer, so the user should not read or write to the wrapped StreamPeer directly. **Note:** When exporting to Android, make sure to enable the INTERNET permission in the Android export preset before exporting the project or using one-click deploy. Otherwise, network communication of any kind will be blocked by Android.

## Quick Reference

```
[properties]
input_buffer_max_size: int = 65532
output_buffer_max_size: int = 65532
stream_peer: StreamPeer
```

## Properties

- input_buffer_max_size: int = 65532 [set set_input_buffer_max_size; get get_input_buffer_max_size]

- output_buffer_max_size: int = 65532 [set set_output_buffer_max_size; get get_output_buffer_max_size]

- stream_peer: StreamPeer [set set_stream_peer; get get_stream_peer]
  The wrapped StreamPeer object.
