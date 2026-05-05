# StreamPeerExtension

## Meta

- Name: StreamPeerExtension
- Source: StreamPeerExtension.xml
- Inherits: StreamPeer
- Inheritance Chain: StreamPeerExtension -> StreamPeer -> RefCounted -> Object

## Quick Reference

```
[methods]
_get_available_bytes() -> int [virtual required const]
_get_data(r_buffer: uint8_t*, r_bytes: int, r_received: int32_t*) -> int (Error) [virtual]
_get_partial_data(r_buffer: uint8_t*, r_bytes: int, r_received: int32_t*) -> int (Error) [virtual]
_put_data(p_data: const uint8_t*, p_bytes: int, r_sent: int32_t*) -> int (Error) [virtual]
_put_partial_data(p_data: const uint8_t*, p_bytes: int, r_sent: int32_t*) -> int (Error) [virtual]
```

## Methods

- _get_available_bytes() -> int [virtual required const]

- _get_data(r_buffer: uint8_t*, r_bytes: int, r_received: int32_t*) -> int (Error) [virtual]

- _get_partial_data(r_buffer: uint8_t*, r_bytes: int, r_received: int32_t*) -> int (Error) [virtual]

- _put_data(p_data: const uint8_t*, p_bytes: int, r_sent: int32_t*) -> int (Error) [virtual]

- _put_partial_data(p_data: const uint8_t*, p_bytes: int, r_sent: int32_t*) -> int (Error) [virtual]
