# PacketPeerExtension

## Meta

- Name: PacketPeerExtension
- Source: PacketPeerExtension.xml
- Inherits: PacketPeer
- Inheritance Chain: PacketPeerExtension -> PacketPeer -> RefCounted -> Object

## Quick Reference

```
[methods]
_get_available_packet_count() -> int [virtual required const]
_get_max_packet_size() -> int [virtual required const]
_get_packet(r_buffer: const uint8_t **, r_buffer_size: int32_t*) -> int (Error) [virtual]
_put_packet(p_buffer: const uint8_t*, p_buffer_size: int) -> int (Error) [virtual]
```

## Methods

- _get_available_packet_count() -> int [virtual required const]

- _get_max_packet_size() -> int [virtual required const]

- _get_packet(r_buffer: const uint8_t **, r_buffer_size: int32_t*) -> int (Error) [virtual]

- _put_packet(p_buffer: const uint8_t*, p_buffer_size: int) -> int (Error) [virtual]
