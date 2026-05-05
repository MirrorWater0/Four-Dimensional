# StreamPeerSocket

## Meta

- Name: StreamPeerSocket
- Source: StreamPeerSocket.xml
- Inherits: StreamPeer
- Inheritance Chain: StreamPeerSocket -> StreamPeer -> RefCounted -> Object

## Brief Description

Abstract base class for interacting with socket streams.

## Description

StreamPeerSocket is an abstract base class that defines common behavior for socket-based streams.

## Quick Reference

```
[methods]
disconnect_from_host() -> void
get_status() -> int (StreamPeerSocket.Status) [const]
poll() -> int (Error)
```

## Methods

- disconnect_from_host() -> void
  Disconnects from host.

- get_status() -> int (StreamPeerSocket.Status) [const]
  Returns the status of the connection.

- poll() -> int (Error)
  Polls the socket, updating its state. See get_status().

## Constants

### Enum Status

- STATUS_NONE = 0
  The initial status of the StreamPeerSocket. This is also the status after disconnecting.

- STATUS_CONNECTING = 1
  A status representing a StreamPeerSocket that is connecting to a host.

- STATUS_CONNECTED = 2
  A status representing a StreamPeerSocket that is connected to a host.

- STATUS_ERROR = 3
  A status representing a StreamPeerSocket in error state.
