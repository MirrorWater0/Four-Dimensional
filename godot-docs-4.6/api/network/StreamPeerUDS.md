# StreamPeerUDS

## Meta

- Name: StreamPeerUDS
- Source: StreamPeerUDS.xml
- Inherits: StreamPeerSocket
- Inheritance Chain: StreamPeerUDS -> StreamPeerSocket -> StreamPeer -> RefCounted -> Object

## Brief Description

A stream peer that handles UNIX Domain Socket (UDS) connections.

## Description

A stream peer that handles UNIX Domain Socket (UDS) connections. This object can be used to connect to UDS servers, or also is returned by a UDS server. Unix Domain Sockets provide inter-process communication on the same machine using the filesystem namespace. **Note:** UNIX Domain Sockets are only available on UNIX-like systems (Linux, macOS, etc.) and are not supported on Windows.

## Quick Reference

```
[methods]
bind(path: String) -> int (Error)
connect_to_host(path: String) -> int (Error)
get_connected_path() -> String [const]
```

## Methods

- bind(path: String) -> int (Error)
  Opens the UDS socket, and binds it to the specified socket path. This method is generally not needed, and only used to force the subsequent call to connect_to_host() to use the specified path as the source address.

- connect_to_host(path: String) -> int (Error)
  Connects to the specified UNIX Domain Socket path. Returns OK on success.

- get_connected_path() -> String [const]
  Returns the socket path of this peer.
