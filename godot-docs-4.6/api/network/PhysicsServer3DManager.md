# PhysicsServer3DManager

## Meta

- Name: PhysicsServer3DManager
- Source: PhysicsServer3DManager.xml
- Inherits: Object
- Inheritance Chain: PhysicsServer3DManager -> Object

## Brief Description

A singleton for managing PhysicsServer3D implementations.

## Description

PhysicsServer3DManager is the API for registering PhysicsServer3D implementations and for setting the default implementation. **Note:** It is not possible to switch physics servers at runtime. This class is only used on startup at the server initialization level, by Godot itself and possibly by GDExtensions.

## Quick Reference

```
[methods]
register_server(name: String, create_callback: Callable) -> void
set_default_server(name: String, priority: int) -> void
```

## Methods

- register_server(name: String, create_callback: Callable) -> void
  Register a PhysicsServer3D implementation by passing a name and a Callable that returns a PhysicsServer3D object.

- set_default_server(name: String, priority: int) -> void
  Set the default PhysicsServer3D implementation to the one identified by name, if priority is greater than the priority of the current default implementation.
