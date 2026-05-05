# AudioListener2D

## Meta

- Name: AudioListener2D
- Source: AudioListener2D.xml
- Inherits: Node2D
- Inheritance Chain: AudioListener2D -> Node2D -> CanvasItem -> Node -> Object

## Brief Description

Overrides the location sounds are heard from.

## Description

Once added to the scene tree and enabled using make_current(), this node will override the location sounds are heard from. Only one AudioListener2D can be current. Using make_current() will disable the previous AudioListener2D. If there is no active AudioListener2D in the current Viewport, center of the screen will be used as a hearing point for the audio. AudioListener2D needs to be inside SceneTree to function.

## Quick Reference

```
[methods]
clear_current() -> void
is_current() -> bool [const]
make_current() -> void
```

## Methods

- clear_current() -> void
  Disables the AudioListener2D. If it's not set as current, this method will have no effect.

- is_current() -> bool [const]
  Returns true if this AudioListener2D is currently active.

- make_current() -> void
  Makes the AudioListener2D active, setting it as the hearing point for the sounds. If there is already another active AudioListener2D, it will be disabled. This method will have no effect if the AudioListener2D is not added to SceneTree.
