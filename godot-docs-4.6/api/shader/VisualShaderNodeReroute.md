# VisualShaderNodeReroute

## Meta

- Name: VisualShaderNodeReroute
- Source: VisualShaderNodeReroute.xml
- Inherits: VisualShaderNode
- Inheritance Chain: VisualShaderNodeReroute -> VisualShaderNode -> Resource -> RefCounted -> Object

## Brief Description

A node that allows rerouting a connection within the visual shader graph.

## Description

Automatically adapts its port type to the type of the incoming connection and ensures valid connections.

## Quick Reference

```
[methods]
get_port_type() -> int (VisualShaderNode.PortType) [const]
```

## Methods

- get_port_type() -> int (VisualShaderNode.PortType) [const]
  Returns the port type of the reroute node.
