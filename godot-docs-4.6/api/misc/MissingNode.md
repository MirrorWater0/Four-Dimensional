# MissingNode

## Meta

- Name: MissingNode
- Source: MissingNode.xml
- Inherits: Node
- Inheritance Chain: MissingNode -> Node -> Object

## Brief Description

An internal editor class intended for keeping the data of unrecognized nodes.

## Description

This is an internal editor class intended for keeping data of nodes of unknown type (most likely this type was supplied by an extension that is no longer loaded). It can't be manually instantiated or placed in a scene. **Warning:** Ignore missing nodes unless you know what you are doing. Existing properties on a missing node can be freely modified in code, regardless of the type they are intended to be.

## Quick Reference

```
[properties]
original_class: String
original_scene: String
recording_properties: bool
recording_signals: bool
```

## Properties

- original_class: String [set set_original_class; get get_original_class]
  The name of the class this node was supposed to be (see Object.get_class()).

- original_scene: String [set set_original_scene; get get_original_scene]
  Returns the path of the scene this node was instance of originally.

- recording_properties: bool [set set_recording_properties; get is_recording_properties]
  If true, allows new properties to be set along with existing ones. If false, only existing properties' values can be set, and new properties cannot be added.

- recording_signals: bool [set set_recording_signals; get is_recording_signals]
  If true, allows new signals to be connected to along with existing ones. If false, only existing signals can be connected to, and new signals cannot be added.
