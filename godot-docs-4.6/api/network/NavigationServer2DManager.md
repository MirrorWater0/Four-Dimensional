# NavigationServer2DManager

## Meta

- Name: NavigationServer2DManager
- Source: NavigationServer2DManager.xml
- Inherits: Object
- Inheritance Chain: NavigationServer2DManager -> Object

## Brief Description

A singleton for managing NavigationServer2D implementations.

## Description

NavigationServer2DManager is the API for registering NavigationServer2D implementations and setting the default implementation. **Note:** It is not possible to switch servers at runtime. This class is only used on startup at the server initialization level.

## Quick Reference

```
[methods]
register_server(name: String, create_callback: Callable) -> void
set_default_server(name: String, priority: int) -> void
```

## Methods

- register_server(name: String, create_callback: Callable) -> void
  Registers a NavigationServer2D implementation by passing a name and a Callable that returns a NavigationServer2D object.

- set_default_server(name: String, priority: int) -> void
  Sets the default NavigationServer2D implementation to the one identified by name, if priority is greater than the priority of the current default implementation.
